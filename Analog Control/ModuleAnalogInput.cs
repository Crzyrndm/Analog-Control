using System;
using UnityEngine;

namespace AnalogControl
{
    public class ModuleAnalogInput : PartModule
    {
        private double[] deadzonePitch = { 0.1, 1.5 }; // 10% movement range is deadzone, 150% will deactivate control
        private double responseExponentPitch = 1; // reactiveness of controls, used so we can have fine control in close if wanted. 1 is linear, 0-1 = faster response, 1+ = finer response

        private double[] deadzoneRoll = { 0.1, 1.5 }; // 5% movement range is deadzone, 150% will deactivate control
        private double responseExponentRoll = 1; // reactiveness of controls, used so we can have fine control in close if wanted. 1 is linear

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
            horizontalRange = Screen.width / 2; // full displacement from center == full extension
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

            if (Input.GetKeyDown(KeyCode.Keypad5))
            {
                isRollMode = !isRollMode;
            }
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
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

            if (Math.Abs(vertDisplacement) < deadzonePitch[0])
            {
                vertDisplacement = 0;
                state.pitch = state.pitchTrim; // If in the deadzone, output control will be the trim value
            }
            else
            {
                // we're outside the deadzone. Re-normalise the value from the edge of the deadzone according to current trim
                if (vertDisplacement > 0)
                    vertDisplacement = (vertDisplacement - deadzonePitch[0]) * (1 - state.pitchTrim) / (1 - deadzonePitch[0]);
                else
                    vertDisplacement = (vertDisplacement + deadzonePitch[0]) * (1 + state.pitchTrim) / (1 - deadzonePitch[0]);

                state.pitch = (float)Math.Pow(vertDisplacement, responseExponentPitch);
            }

            if (Math.Abs(hrztDisplacement) < deadzoneRoll[0])
            {
                hrztDisplacement = 0;
                state.roll = state.rollTrim; // If in the deadzone, output control will be the trim value
            }
            else
            {
                // we're outside the deadzone. Re-normalise the value from the edge of the deadzone according to current trim
                if (hrztDisplacement > 0) // +ve values
                    hrztDisplacement = (hrztDisplacement - deadzoneRoll[0]) * (1 - state.rollTrim) / (1 - deadzoneRoll[0]);
                else // -ve values
                    hrztDisplacement = (hrztDisplacement + deadzoneRoll[0]) * (1 + state.rollTrim) / (1 - deadzoneRoll[0]);

                state.roll = (float)Math.Pow(hrztDisplacement, responseExponentRoll);
            }
            return state;
        }
    }
}
