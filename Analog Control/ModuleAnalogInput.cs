using System;
using UnityEngine;

namespace AnalogControl
{
    public class ModuleAnalogInput : PartModule
    {
        [KSPField(isPersistant = true)]
        private double deadzonePitch = 0.05; // 5% movement range is deadzone
        [KSPField(isPersistant = true)]
        private double responseExponentPitch = 2; // reactiveness of controls, used so we can have fine control in close if wanted. 1 is linear, 0-1 = faster response, 1+ = finer response

        [KSPField(isPersistant = true)]
        private double deadzoneRoll = 0.05; // 5% movement range is deadzone
        [KSPField(isPersistant = true)]
        private double responseExponentRoll = 2; // reactiveness of controls, used so we can have fine control in close if wanted. 1 is linear

        [KSPField(isPersistant = true)]
        private double deadzoneYaw = 0.05; // 5% movement range is deadzone
        [KSPField(isPersistant = true)]
        private double responseExponentYaw = 2; // reactiveness of controls, used so we can have fine control in close if wanted. 1 is linear

        [KSPField(isPersistant = true)]
        private bool isRollMode = true;
        private bool isActive = false;

        private Vector2 mousePos, screenCenter;

        private int verticalRange, horizontalRange;

        /// <summary>
        /// Initialise control region size and other user specific params
        /// </summary>
        public override void OnStart(StartState start)
        {
            verticalRange = Screen.height / 3; // 2/3 displacement from center == full extension
            horizontalRange = Screen.width / 3; // full displacement from center == full extension
            screenCenter.y = Screen.height / 2;
            screenCenter.x = Screen.width / 2;

            Debug.Log("Analog Control module loaded");
        }

        /// <summary>
        /// Handle interfacing in update
        /// </summary>
        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isRollMode = !isRollMode;
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                isActive = !isActive;
            }
        }

        /// <summary>
        /// fixed update for the actual control
        /// </summary>
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight 
                || this.vessel == null 
                || this.vessel != FlightGlobals.ActiveVessel 
                || !isActive)
                return;

            base.vessel.ctrlState = mouseControlVessel(base.vessel.ctrlState);
        }

        /// <summary>
        /// vessel control output according to mouse position
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private FlightCtrlState mouseControlVessel(FlightCtrlState state)
        {
            // (0,0) is bottom left of screen for mouse pos
            mousePos.x = Input.mousePosition.x;
            mousePos.y = Input.mousePosition.y;
            double vertDisplacement = (mousePos.y - screenCenter.y) / verticalRange; // displacement of mouse from center as a normalised value
            double hrztDisplacement = (mousePos.x - screenCenter.x) / horizontalRange; // displacement of mouse from center as a normalised value

            state.pitch = response(vertDisplacement, deadzonePitch, responseExponentPitch);
            if (isRollMode)
                state.roll = response(hrztDisplacement, deadzoneRoll, responseExponentRoll);
            else
                state.yaw = response(hrztDisplacement, deadzoneYaw, responseExponentYaw);

            return state;
        }

        private float response(double displacement, double deadzone, double exponent)
        {
            float response = 0;
            if (Math.Abs(displacement) < deadzone) // deadzone
            {
                return 0;
            }
            else if (displacement > 0) // +ve displacement
            {
                displacement = (displacement - deadzone) / (1 - deadzone);
            }
            else // -ve displacement
            {
                displacement = (displacement + deadzone) / (1 - deadzone);
            }

            response = (float)Math.Pow(displacement, exponent);
            if (Math.Sign(response) != Math.Sign(displacement))
                response *= -1;
            response = Math.Max(Math.Min(response, 1), -1);

            return response;
        }
    }
}
