using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public class TokenControl
    {
        private readonly string _hint;
        private readonly bool _foldout;

        private bool _showAccessTokens;
        private string _proposedToken = "New Token";

        public TokenControl(string hint, bool foldout = true)
        {
            _hint = hint;
            _foldout = foldout;
        }

        public void DrawInspectorGui(Object parent, IAccessTokenCollection receiver)
        {
            if (_foldout)
            {
                _showAccessTokens = EditorGUILayout.Foldout(_showAccessTokens, "Access Tokens");
                if (!_showAccessTokens)
                    return;
            }

            EditorGUILayout.HelpBox(_hint, MessageType.Info);

            var tokensToRemove = new List<string>();
            foreach (var token in receiver.Tokens)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(token);
                    if (GUILayout.Button("Delete", GUILayout.MaxWidth(50)))
                        tokensToRemove.Add(token);
                }
            }

            foreach (var token in tokensToRemove)
            {
                Undo.RecordObject(parent, "Removed Dissonance Access Token");
                receiver.RemoveToken(token);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _proposedToken = EditorGUILayout.TextField(_proposedToken);
                if (GUILayout.Button("Add Token"))
                {
                    Undo.RecordObject(parent, "Added Dissonance Access Token");

                    receiver.AddToken(_proposedToken);
                    _proposedToken = "New Token";
                }
            }
        }
    }
}
