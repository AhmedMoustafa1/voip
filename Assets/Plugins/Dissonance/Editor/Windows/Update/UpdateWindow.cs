using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows.Update
{
    internal class UpdateWindow
        : BaseDissonanceEditorWindow
    {
        #region constants
        private const float WindowWidth = 300f;
        private const float WindowHeight = 290f;
        private static readonly Vector2 WindowSize = new Vector2(WindowWidth, WindowHeight);

        private const string Title = "Dissonance Update Available";
        #endregion

        #region construction
        internal static void Show(SemanticVersion latest, SemanticVersion current)
        {
            var window = GetWindow<UpdateWindow>(true, "Title", true);

            window.minSize = WindowSize;
            window.maxSize = WindowSize;
            window.titleContent = new GUIContent(Title);

            window.Latest = latest;
            window.Current = current;

            window.position = new Rect(150, 150, WindowWidth, WindowHeight);
            window.Repaint();
        }
        #endregion

        private SemanticVersion Latest { get; set; }
        private SemanticVersion Current { get; set; }

        protected override void DrawContent()
        {
            if (Current.Equals(Latest))
                EditorGUILayout.LabelField("You are using the latest available version of Dissonance Voice Chat.", LabelFieldStyle);
            else
                EditorGUILayout.LabelField("There is an update for Dissonance Voice Chat available on the asset store:", LabelFieldStyle);

            EditorGUILayout.LabelField(" - Current Version: " + Current, LabelFieldStyle);
            EditorGUILayout.LabelField(" - Latest Version: " + Latest, LabelFieldStyle);

            if (GUILayout.Button("Open Release Notes"))
                Application.OpenURL(string.Format("https://placeholder-software.co.uk/dissonance/releases/{0}.html{1}", Latest, EditorMetadata.GetQueryString("update_notification")));

            EditorGUILayout.Space();

            var enabled = UpdateLauncher.GetUpdaterEnabled();
            var disabled = !enabled;
            using (new EditorGUILayout.HorizontalScope())
            {
                disabled = EditorGUILayout.Toggle(disabled, GUILayout.MaxWidth(25));
                EditorGUILayout.LabelField("Do not notify me about new versions again", LabelFieldStyle);
            }

            if (disabled == enabled)
                UpdateLauncher.SetUpdaterEnabled(!disabled);
        }
    }
}