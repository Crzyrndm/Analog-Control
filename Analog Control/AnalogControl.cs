using System;
using UnityEngine;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogControl : MonoBehaviour
    {
        // state
        bool isRollMode = true;
        bool isActive = false;
        bool isPaused = true;

        Vector2 lastInput = new Vector2();
        bool holdInput = false;
        // settings
        bool isPitchInverted = true;
        bool displayCenterline = true;
        float centerlineTransparency = 1; // 0 == transparent, 1 == opaque
        Vector2 screenCenter, range;
        float deadzonePitch = 0.05f; // 5% movement range is deadzone
        float deadzoneRoll = 0.05f; // 5% movement range is deadzone
        // config
        KSP.IO.PluginConfiguration config;
        // display
        static Texture2D target;
        Rect targetRect;

        static Texture2D markerSpot;
        Rect markerRect;
        
        /// <summary>
        /// Initialise control region size and other user specific params
        /// </summary>
        public void Start()
        {
            loadConfig();

            try
            {
                if (displayCenterline)
                {
                    if (target == null)
                        target = new Texture2D(500, 500);
                    target.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control/PluginData/AnalogControl/crosshair.png"));
                    setTransparency(target, centerlineTransparency);
                    targetRect = new Rect(screenCenter.x - deadzoneRoll * range.x * 2.5f, screenCenter.y - deadzonePitch * range.y * 2.5f, range.x * 2 * deadzoneRoll * 2.5f, range.y * 2 * deadzonePitch * 2.5f);

                    if (markerSpot == null)
                        markerSpot = new Texture2D(20, 20);
                    markerSpot.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control/PluginData/AnalogControl/spot.png"));
                    // markerSpot.Apply();
                    markerRect = new Rect(0, 0, 20, 20);

                    RenderingManager.AddToPostDrawQueue(5, Draw);
                    Debug.Log("[Analog Control] renderer call added");
                }
            }
            catch (Exception ex)
            {
                Debug.Log("crosshair.png not available at\r\nGameData/Analog Control/PluginData/AnalogControl/crosshair.png");
                Debug.Log(ex.StackTrace);
            }
        }
        
        private void loadConfig()
        {
            config = KSP.IO.PluginConfiguration.CreateForType<AnalogControl>();
            config.load();

            isPitchInverted = config.GetValue("pitchInvert", true);
            displayCenterline = config.GetValue("centerlineVisible", true);
            centerlineTransparency = float.Parse(config.GetValue("transparency", "1"));
            range.x = Screen.width * float.Parse(config.GetValue("rangeX", "0.67")) / 2;
            range.y = Screen.height * float.Parse(config.GetValue("rangeY", "0.67")) / 2;
            deadzonePitch = float.Parse(config.GetValue("deadzoneY", "0.05"));
            deadzoneRoll = float.Parse(config.GetValue("deadzoneX", "0.05"));
            screenCenter.x = float.Parse(config.GetValue("centerX", (Screen.width / 2).ToString()));
            screenCenter.y = float.Parse(config.GetValue("centerY", (Screen.height / 2).ToString()));
        }
        
        private void saveConfig()
        {
            config["pitchInvert"] = isPitchInverted;
            config["centerlineVisible"] = displayCenterline;
            config["transparency"] = centerlineTransparency.ToString();
            config["rangeX"] = (2 * range.x / Screen.width).ToString();
            config["rangeY"] = (2 * range.y / Screen.height).ToString();
            config["deadzoneX"] = deadzoneRoll.ToString();
            config["deadzoneY"] = deadzonePitch.ToString();
            config["centerX"] = screenCenter.x.ToString();
            config["centerY"] = screenCenter.y.ToString();
            config.save();
        }
        
        public void OnDestroy()
        {
            saveConfig();
        }

        /// <summary>
        /// Handle interfacing in update
        /// </summary>
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                isRollMode = !isRollMode;
            if (Input.GetKeyDown(KeyCode.Return))
                isActive = !isActive;
            if (isActive && Input.GetMouseButtonDown(0))
                isPaused = !isPaused;
            else if (!isActive)
            {
                isPaused = true;
                holdInput = false;
                lastInput = Vector2.zero;
            }
            else
            {
                if (!isPaused)
                    holdInput = true;
            }
            if (!displayCenterline)
                isPaused = !isActive;
            else
            {
                markerRect.x = lastInput.x + screenCenter.x - 10;
                markerRect.y = screenCenter.y - lastInput.y - 10;
            }
        }

        public void Draw()
        {
            if (!isActive)
                return;

            Graphics.DrawTexture(targetRect, target);

            if (holdInput && isPaused)
                Graphics.DrawTexture(markerRect, markerSpot);
        }

        /// <summary>
        /// fixed update for the actual control
        /// </summary>
        public void FixedUpdate()
        {
            if (!isActive || (isPaused && !holdInput))
                return;
            
            FlightGlobals.ActiveVessel.ctrlState = mouseControlVessel(FlightGlobals.ActiveVessel.ctrlState);
        }

        /// <summary>
        /// vessel control output according to mouse position
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private FlightCtrlState mouseControlVessel(FlightCtrlState state)
        {
            // (0,0) is bottom left of screen for mouse pos
            if (!isPaused)
            {
                lastInput.x = Input.mousePosition.x - screenCenter.x;
                lastInput.y = Input.mousePosition.y - screenCenter.y;
            }

            float vertDisplacement = lastInput.y / range.y; // displacement of mouse from center as a normalised value
            float hrztDisplacement = lastInput.x / range.x; // displacement of mouse from center as a normalised value
            
            int invert = isPitchInverted ? -1 : 1;
            state.pitch = invert * response(vertDisplacement, deadzonePitch, -state.pitchTrim);
            if (isRollMode)
                state.roll = response(hrztDisplacement, deadzoneRoll, state.rollTrim);
            else
                state.yaw = response(hrztDisplacement, deadzoneRoll, state.yawTrim);

            return state;
        }

        private float response(float displacement, float deadzone, float trim)
        {
            if (Math.Abs(displacement) < deadzone) // deadzone
                return trim;
            
            if (displacement > 0) // +ve displacement
                displacement = (displacement - deadzone) / (1 - deadzone);
            else // -ve displacement
                displacement = (displacement + deadzone) / (1 - deadzone);

            float response = displacement * displacement; // displacement^2 gives nice fine control
            if (displacement < 0) // -ve displacement becomes positive response if not checked
                response *= -1;
            // trim compensation
            if (response > 0)
                response = trim + response * (1 - trim);
            else
                response = trim + response * (1 + trim);
            
            response = Mathf.Clamp(response, -1, 1);

            return response;
        }

        private void setTransparency(Texture2D tex, float transparency)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = (byte)((float)pixels[i].a * Mathf.Clamp(transparency, 0, 1));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }
    }
}
