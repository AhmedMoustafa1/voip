using Dissonance.Audio.Playback;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(SamplePlaybackComponent))]
    public class SamplePlaybackComponentEditor : UnityEditor.Editor
    {
        private Texture2D _logo;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            var component = (SamplePlaybackComponent) target;
            var session = component.Session;

            if (Application.isPlaying)
            {
                if (session != null)
                {
                    var s = session.Value;

                    EditorGUILayout.LabelField(string.Format("Buffered Packets: {0}", s.BufferCount));
                    EditorGUILayout.LabelField(string.Format("Playback Position: {0:0.00}s", component.PlaybackPosition.TotalSeconds));
                    EditorGUILayout.LabelField(string.Format("Ideal Position: {0:0.00}s", component.IdealPlaybackPosition.TotalSeconds));
                    EditorGUILayout.LabelField(string.Format("Desync: {0:000}ms ({1:P0})", component.Desync.TotalMilliseconds, component.Desync.TotalSeconds / component.IdealPlaybackPosition.TotalSeconds));
                    EditorGUILayout.LabelField(string.Format("Compensated Playback Speed: {0:P1}", component.CorrectedPlaybackSpeed));
                }
                else
                {
                    EditorGUILayout.LabelField("No Active Session");
                }

                EditorUtility.SetDirty(component);
            }
        }
    }
}