using System;
using UnityEngine;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogControl : MonoBehaviour
    {
        float deadzonePitch = 0.05f; // 5% movement range is deadzone
        float deadzoneRoll = 0.05f; // 5% movement range is deadzone
        float deadzoneYaw = 0.05f; // 5% movement range is deadzone

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
            range.y = Screen.height / 3; // 2/3 displacement from center == full extension
            range.x = Screen.width / 3; // full displacement from center == full extension
            screenCenter.y = Screen.height / 2;
            screenCenter.x = Screen.width / 2;
            
            loadConfig();

            try
            {
                target = new Texture2D(500, 500);
                target.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control/PluginData/AnalogControl/crosshair.png"));
                setTransparency(target, centerlineTransparency);
                targetRect = new Rect(screenCenter.x - deadzoneRoll * range.x * 2.5f, screenCenter.y - deadzonePitch * range.y * 2.5f, range.x * 2 * deadzoneRoll * 2.5f, range.y * 2 * deadzonePitch * 2.5f);
                if (displayCenterline)
                {
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
            deadzoneRoll = deadzoneYaw = float.Parse(config.GetValue("deadzoneX", "0.05"));
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

            Graphics.DrawTexture(new Rect(screenCenter.x - deadzoneRoll * range.x * 2.5f, screenCenter.y - deadzonePitch * range.y * 2.5f, range.x * 2 * deadzoneRoll * 2.5f, range.y * 2 * deadzonePitch * 2.5f), target);
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
            state.pitch = invert * response(vertDisplacement, deadzonePitch);
            if (isRollMode)
                state.roll = response(hrztDisplacement, deadzoneRoll);
            else
                state.yaw = response(hrztDisplacement, deadzoneYaw);

            return state;
        }

        private float response(float displacement, float deadzone) //, double exponent)
        {
            float response = 0;
            if (Math.Abs(displacement) < deadzone) // deadzone
                return 0;
            else if (displacement > 0) // +ve displacement
                displacement = (displacement - deadzone) / (1 - deadzone);
            else // -ve displacement
                displacement = (displacement + deadzone) / (1 - deadzone);

            // response = (float)Math.Pow(displacement, exponent); // do we want configurable exponent?
            response = displacement * displacement;

            if (displacement < 0) // -ve displacement becomes positive response if not checked
                response *= -1;
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
