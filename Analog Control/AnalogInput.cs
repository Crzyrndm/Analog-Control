using System;
using UnityEngine;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogInput : MonoBehaviour
    {
        private double deadzonePitch = 0.05; // 5% movement range is deadzone
        private double deadzoneRoll = 0.05; // 5% movement range is deadzone
        private double deadzoneYaw = 0.05; // 5% movement range is deadzone

        private bool isRollMode = true;
        private bool isActive = false;
        private bool isPitchInverted = true;
        private bool displayCenterline = false;
        
        private int centerlineTransparency = 0; // 100 == transparent, 0 == opaque

        private Vector2 mousePos, screenCenter, range;
        
        static KSP.IO.PluginConfiguration config;
        
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
        }
        
        private void loadConfig()
        {
            config = KSP.IO.PluginConfiguration.CreateForType<AnalogInput>();
            config.load();
            
            isPitchInverted = config.GetValue("pitchInvert", new bool());
            displayCenterline = config.GetValue("centerlineVisible", new bool());
            centerlineTransparency = config.GetValue("transparency", new int());
        }
        
        private void saveConfig()
        {
            config.SetValue("pitchInvert", isPitchInverted);
            config.SetValue("centerlineVisible", displayCenterline);
            config.SetValue("transparency", centerlineTransparency);
            config.Save();
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
            double vertDisplacement = (Input.mousePosition.y - screenCenter.y) / range.y; // displacement of mouse from center as a normalised value
            double hrztDisplacement = (Input.mousePosition.x - screenCenter.x) / range.x; // displacement of mouse from center as a normalised value
            
            int invert;
            isPitchInverted ? invert = -1 : invert = 1;
            state.pitchTrim = invert * response(vertDisplacement, deadzonePitch);
            
            if (isRollMode)
                state.rollTrim = response(hrztDisplacement, deadzoneRoll);
            else
                state.yawTrim = response(hrztDisplacement, deadzoneYaw);

            return state;
        }

        private float response(double displacement, double deadzone) //, double exponent)
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
            response = Mathf.Clamp(response, 1, -1);

            return response;
        }
    }
}
