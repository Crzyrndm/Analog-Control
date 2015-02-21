using System;
using UnityEngine;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogControl : MonoBehaviour
    {
        float deadzonePitch = 0.05f; // 5% movement range is deadzone
        float deadzoneRoll = 0.05f; // 5% movement range is deadzone

        bool isRollMode = true;
        bool isActive = false;
        bool isPitchInverted = true;
        bool displayCenterline = true;
        
        float centerlineTransparency = 1; // 0 == transparent, 1 == opaque

        Vector2 screenCenter, range;
        
        KSP.IO.PluginConfiguration config;

        // display
        Texture2D target;
        Rect targetRect;
        
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
                    target = new Texture2D(500, 500);
                    target.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control/PluginData/AnalogControl/crosshair.png"));
                    setTransparency(target, centerlineTransparency);
                    targetRect = new Rect(screenCenter.x - deadzoneRoll * range.x * 2.5f, screenCenter.y - deadzonePitch * range.y * 2.5f, range.x * 2 * deadzoneRoll * 2.5f, range.y * 2 * deadzonePitch * 2.5f);

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
                
            // drawCenterline();
        }

        public void Draw()
        {
            if (!isActive)
                return;

            Graphics.DrawTexture(targetRect, target);
        }

        /// <summary>
        /// fixed update for the actual control
        /// </summary>
        public void FixedUpdate()
        {
            if (!isActive)
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
            float vertDisplacement = (Input.mousePosition.y - screenCenter.y) / range.y; // displacement of mouse from center as a normalised value
            float hrztDisplacement = (Input.mousePosition.x - screenCenter.x) / range.x; // displacement of mouse from center as a normalised value
            
            int invert = isPitchInverted ? -1 : 1;
            state.pitch = invert * response(vertDisplacement, deadzonePitch, state.pitchTrim);
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
