using System;
using UnityEngine;

namespace AnalogControl
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AnalogControl : MonoBehaviour
    {
        enum activationState
        {
            Inactive,
            Active,
            Paused
        }
        bool firstStart = true;
        // state
        bool isRollMode = true;
        activationState controlState = activationState.Inactive;
        // settings
        bool isPitchInverted = true;
        float transparency = 1; // 0 == transparent, 1 == opaque
        Vector2 deadzone;
        Rect controlZone;
        // config
        KSP.IO.PluginConfiguration config;
        // display
        static Texture2D target;
        Rect targetRect;

        static Texture2D markerSpot;
        Rect markerRect;

        bool showWindow = false;
        customKeybind activate, modeSwitch, windowKey, lockKey, pauseKey;

        bool lockInput = false;

        class customKeybind
        {
            public KeyCode currentBind { get; set; }
            public bool set { get; set; }
            public customKeybind(KeyCode defKey)
            {
                currentBind = defKey;
                set = true;
            }
        }
        
        /// <summary>
        /// Initialise control region size and other user specific params
        /// </summary>
        public void Start()
        {
            loadConfig();

            try
            {
                if (target == null)
                {
                    target = new Texture2D(500, 500);
                    target.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control/PluginData/AnalogControl/crosshair.png"));
                }
                setTransparency(target, transparency);
                targetRect = new Rect();
                targetRect.width = controlZone.width * deadzone.x * 1.5f;
                targetRect.height = controlZone.height * deadzone.y * 1.5f;
                targetRect.center = controlZone.center;
            }
            catch
            {
                Debug.Log("Target overlay setup failed");
            }
            try
            {
                if (markerSpot == null)
                {
                    markerSpot = new Texture2D(20, 20);
                    markerSpot.LoadImage(System.IO.File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Analog Control/PluginData/AnalogControl/spot.png"));
                }
                    setTransparency(markerSpot, transparency);
                    markerRect = new Rect(0, 0, 20, 20);
            }
            catch
            {
                Debug.Log("Marker overlay setup failed");
            }
        }
        
        private void loadConfig()
        {
            config = KSP.IO.PluginConfiguration.CreateForType<AnalogControl>();
            config.load();

            isPitchInverted = config.GetValue<bool>("pitchInvert", true);
            transparency = config.GetValue<float>("transparency", 1);
            deadzone = config.GetValue<Vector2>("deadzone", new Vector2(0.05f, 0.05f));
            controlZone = config.GetValue<Rect>("controlZone", new Rect(Screen.width / 6, Screen.height / 6, Screen.width * 2 / 3, Screen.height * 2 / 3));
            activate = new customKeybind(config.GetValue<KeyCode>("activate", KeyCode.Return));
            modeSwitch = new customKeybind(config.GetValue<KeyCode>("modeSwitch", KeyCode.Tab));
            windowKey = new customKeybind(config.GetValue<KeyCode>("windowKey", KeyCode.O));
            lockKey = new customKeybind(config.GetValue<KeyCode>("lockKey", KeyCode.L));
            pauseKey = new customKeybind(config.GetValue<KeyCode>("pauseKey", KeyCode.Mouse0));
        }
        
        private void saveConfig()
        {
            config["pitchInvert"] = isPitchInverted;
            config["transparency"] = transparency;
            config["deadzone"] = deadzone;
            config["controlZone"] = controlZone;
            config["activate"] = activate.currentBind;
            config["modeSwitch"] = modeSwitch.currentBind;
            config["windowKey"] = windowKey.currentBind;
            config["lockKey"] = lockKey.currentBind;
            config["pauseKey"] = pauseKey.currentBind;
            config.save();
        }
        
        public void OnDestroy()
        {
            //MonoBehaviour.Destroy(markerSpot);
            //MonoBehaviour.Destroy(target);
            saveConfig();
        }

        /// <summary>
        /// Handle interfacing in update
        /// </summary>
        public void Update()
        {
            dragWindow();
            if (Input.GetKeyDown(lockKey.currentBind) && GameSettings.MODIFIER_KEY.GetKey())
                lockInput = !lockInput;
            if (lockInput)
                return;
            if (Input.GetKeyDown(activate.currentBind))
                controlState = (controlState == activationState.Inactive) ? activationState.Paused : activationState.Inactive;
            if (controlState == activationState.Inactive)
                return;

            if (Input.GetKeyDown(modeSwitch.currentBind))
                isRollMode = !isRollMode;

            if (Input.GetKeyDown(pauseKey.currentBind))
            {
                controlState = controlState == activationState.Paused ? activationState.Active : activationState.Paused;
                firstStart = false;
            }
        }

        public void dragWindow()
        {
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(windowKey.currentBind))
                showWindow = !showWindow;
            if (showWindow)
            {
                if (draggingBottomRight)
                {
                    if (!Input.GetMouseButton(0))
                        draggingBottomRight = false;
                    else
                    {
                        Vector2 diff = (Vector2)Input.mousePosition - dragPos;
                        controlZone.width += diff.x;
                        controlZone.height -= diff.y;
                        dragPos = Input.mousePosition;
                    }
                }
                targetRect.center = controlZone.center;
                targetRect.width = controlZone.width * deadzone.x * 1.5f;
                targetRect.height = controlZone.height * deadzone.y * 1.5f;
            }
        }

        public void OnGUI()
        {
            if (showWindow)
                controlZone = GUILayout.Window(GetInstanceID(), controlZone, locationWindow, "", GUI.skin.box);
            if (controlState != activationState.Inactive || showWindow)
            {
                Graphics.DrawTexture(targetRect, target);
                Graphics.DrawTexture(markerRect, markerSpot);
            }
        }
        
        bool draggingBottomRight;
        Vector2 dragPos;
        void locationWindow(int id)
        {
            if (controlState == activationState.Inactive)
            {
                if (GUILayout.Button("Window Key: " + GameSettings.MODIFIER_KEY.primary.ToString() + " + => " + (windowKey.set ? windowKey.currentBind.ToString() : "Not Assigned")))
                    windowKey.set = !windowKey.set;
                if (!windowKey.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        windowKey.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        windowKey.currentBind = Event.current.keyCode;
                        windowKey.set = true;
                    }
                }
                
                if (GUILayout.Button("Activate Key => " + (activate.set ? activate.currentBind.ToString() : "Not Assigned")))
                    activate.set = !activate.set;
                if (!activate.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        activate.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        activate.currentBind = Event.current.keyCode;
                        activate.set = true;
                    }
                }

                if (GUILayout.Button("Mode Switch Key => " + (modeSwitch.set ? modeSwitch.currentBind.ToString() : "Not Assigned")))
                    modeSwitch.set = !modeSwitch.set;
                if (!modeSwitch.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        modeSwitch.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        modeSwitch.currentBind = Event.current.keyCode;
                        modeSwitch.set = true;
                    }
                }

                if (GUILayout.Button("Lock Key: " + GameSettings.MODIFIER_KEY.primary.ToString() + " + => " + (lockKey.set ? lockKey.currentBind.ToString() : "Not Assigned")))
                    lockKey.set = !lockKey.set;
                if (!lockKey.set)
                {
                    if (Input.GetMouseButton(0) || Event.current.keyCode == KeyCode.Escape)
                        lockKey.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        lockKey.currentBind = Event.current.keyCode;
                        lockKey.set = true;
                    }
                }
                if (GUILayout.Button("Pause Key => " + (pauseKey.set ? pauseKey.currentBind.ToString() : "Not Assigned")))
                    pauseKey.set = !pauseKey.set;
                if (!pauseKey.set)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                        pauseKey.set = true;
                    else if (Event.current.type == EventType.KeyDown)
                    {
                        pauseKey.currentBind = Event.current.keyCode;
                        pauseKey.set = true;
                    }
                }
                isPitchInverted = GUILayout.Toggle(isPitchInverted, "Invert Pitch Control");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Deadzone width percentage");
                deadzone.x = float.Parse(GUILayout.TextField((deadzone.x * 100).ToString("0.00"))) / 100;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Deadzone height percentage");
                deadzone.y = float.Parse(GUILayout.TextField((deadzone.y * 100).ToString("0.00"))) / 100;
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.RepeatButton("*"))
                {
                    draggingBottomRight = true;
                    dragPos = Input.mousePosition;
                }
                GUILayout.EndHorizontal();
                GUI.DragWindow();
            }
            else
            {
                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// fixed update for the actual control
        /// </summary>
        public void FixedUpdate()
        {
            if (firstStart || controlState == activationState.Inactive)
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
            // (0,0) is bottom left of screen for mouse pos, top left for UI
            if (controlState == activationState.Active)
                markerRect.center = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            float vertDisplacement = 2 * (markerRect.center.y - controlZone.center.y) / controlZone.height;
            state.pitch = (isPitchInverted ? -1 : 1) * response(vertDisplacement, deadzone.y, state.pitchTrim);

            float hrztDisplacement = 2 * (markerRect.center.x - controlZone.center.x) / controlZone.width;
            if (isRollMode)
                state.roll = response(hrztDisplacement, deadzone.x, state.rollTrim);
            else
                state.yaw = response(hrztDisplacement, deadzone.x, state.yawTrim);

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

            float response = displacement * displacement * Math.Sign(displacement); // displacement^2 gives nice fine control
            // trim compensation
            if (response > 0)
                response = trim + response * (1 - trim);
            else
                response = trim + response * (1 + trim);

            return Mathf.Clamp(response, -1, 1);
        }

        private void setTransparency(Texture2D tex, float transparency)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = (byte)((float)pixels[i].a * Mathf.Clamp01(transparency));
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }
    }
}
