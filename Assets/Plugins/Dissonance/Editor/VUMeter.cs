using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    // ReSharper disable once InconsistentNaming
    public class VUMeter
    {
        private readonly string _label;
        private float _maxVolume;

        public VUMeter(string label)
        {
            _label = label;
        }

        public void DrawInspectorGui(Object target, float amplitude, bool clear)
        {
            EditorUtility.SetDirty(target);

            var volume = Mathf.Pow(amplitude, 0.25f);
            _maxVolume = Mathf.Max(volume, Mathf.Max(0, _maxVolume - 0.005f));

            if (clear)
            {
                volume = 0;
                _maxVolume = 0;
            }

            EditorGUILayout.MinMaxSlider(new GUIContent(_label), ref volume, ref _maxVolume, 0, 1);
        }
    }
}
