using System;
using System.Collections.Generic;
using Dissonance.Audio.Capture;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Dissonance.Editor
{
    public abstract class BaseIMicrophoneCaptureEditor<T>
        : UnityEditor.Editor
        where T : UnityEngine.Object, IMicrophoneCapture
    {
        #region fields and properties
        private Texture2D _logo;

        private readonly VUMeter _micMeter = new VUMeter("Mic Amplitude");
        private bool _micList;

        private static GUIStyle _dropdownButtonStyleNormal;
        private static GUIStyle _dropdownButtonStyleToggled;

        private DissonanceComms _comms;
        #endregion

        #region initialisation
        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        private static void CreateSkin()
        {
            if (_dropdownButtonStyleNormal == null)
                _dropdownButtonStyleNormal = new GUIStyle(GUI.skin.button);

            if (_dropdownButtonStyleToggled == null)
            {
                _dropdownButtonStyleToggled = new GUIStyle(GUI.skin.button);
                _dropdownButtonStyleToggled.normal.background = _dropdownButtonStyleToggled.active.background;
            }
        }
        #endregion

        [CanBeNull]
        private DissonanceComms FindComms()
        {
            if (!_comms)
                _comms = FindObjectOfType<DissonanceComms>();
            return _comms;
        }

        public override void OnInspectorGUI()
        {
            CreateSkin();

            GUILayout.Label(_logo);

            var capture = (T)target;
            DrawAmplitudeGui(capture);
        }

        private void DrawAmplitudeGui([NotNull] T capture)
        {
            var comms = FindComms();
            if (Application.isPlaying && comms != null)
            {
                var player = comms.FindPlayer(comms.LocalPlayerName);
                _micMeter.DrawInspectorGui(capture, player == null ? 0 : player.Amplitude, player == null);
            }
        }

        protected void DrawMicSelectorGui([NotNull] BasicMicrophoneCapture capture)
        {
            var comms = FindComms();
            if (comms == null)
            {
                EditorGUILayout.HelpBox("Cannot find DissonanceComms component in scene (required to configure microphone)", MessageType.Error);
                return;
            }

            string inputString;
            using (new EditorGUILayout.HorizontalScope())
            {
                //Allow the user to type an arbitrary input string
                inputString = EditorGUILayout.DelayedTextField("Microphone Device Name", comms.MicrophoneName ?? "None (Default)");

                //Toggle device list
                if (GUILayout.Button(new GUIContent("Devices"), _micList ? _dropdownButtonStyleToggled : _dropdownButtonStyleNormal, GUILayout.MaxWidth(55)))
                    _micList = !_micList;
            }

            //Show device list
            if (_micList)
            {
                var devices = new List<string> { "None (Default)" };
                devices.AddRange(Microphone.devices);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var device in devices)
                    {
                        if (GUILayout.Button(device))
                        {
                            inputString = device;
                            _micList = false;
                        }
                    }
                }
            }

            //If the name is any of these special strings, default it back to null
            var nulls = new[] {
                "null", "(null)",
                "default", "(default)", "none default", "none (default)",
                "none", "(none)"
            };
            if (string.IsNullOrEmpty(inputString) || nulls.Contains(inputString, StringComparer.InvariantCultureIgnoreCase))
                inputString = null;

            if (comms.MicrophoneName != inputString)
            {
                capture.ChangeWithUndo(
                    "Changed Dissonance Microphone",
                    inputString,
                    comms.MicrophoneName,
                    a => comms.MicrophoneName = a
                );
            }
        }
    }
}
