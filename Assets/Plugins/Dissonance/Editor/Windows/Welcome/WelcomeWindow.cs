using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    internal class WelcomeWindow
        : BaseDissonanceEditorWindow
    {
        #region constants
        private const float WindowWidth = 300f;
        private const float WindowHeight = 290f;
        private static readonly Vector2 WindowSize = new Vector2(WindowWidth, WindowHeight);

        private const string Title = "Welcome To Dissonance";
        #endregion

        #region construction
        public static void ShowWindow(WelcomeState state, bool showRateLink)
        {
            var window = GetWindow<WelcomeWindow>(true, Title, true);

            var size = WindowSize + new Vector2(0, showRateLink ? 22 : 0);

            window.minSize = size;
            window.maxSize = size;
            window.titleContent = new GUIContent(Title);

            window.State = state;
            window.ShowRatingLink = showRateLink;

            window.position = new Rect(150, 150, size.x, size.y);
            window.Repaint();
        }
        #endregion

        #region fields and properties
        public WelcomeState State { get; private set; }
        public bool ShowRatingLink { get; private set; }
        #endregion

        protected override void DrawContent()
        {
            EditorGUILayout.LabelField("Thankyou for installing Dissonance Voice Chat!", LabelFieldStyle);
            EditorGUILayout.LabelField(string.Format("Version {0}", DissonanceComms.Version), LabelFieldStyle);
            EditorGUILayout.LabelField("", LabelFieldStyle);
            EditorGUILayout.LabelField("Dissonance includes several optional integrations with other assets. Please visit the website to download and install them.", LabelFieldStyle);
            EditorGUILayout.LabelField("", LabelFieldStyle);

            if (GUILayout.Button("Open Integrations List"))
                Application.OpenURL(string.Format("https://placeholder-software.co.uk/dissonance/releases/{0}.html{1}", DissonanceComms.Version, GetQueryString()));

            if (ShowRatingLink)
                if (GUILayout.Button("Rate And Review"))
                    Application.OpenURL("http://u3d.as/za2?aid=1100lJDF");
        }

        [NotNull] private static string GetQueryString()
        {
            return EditorMetadata.GetQueryStringWithIntegrations("welcome_window");
        }
    }
}