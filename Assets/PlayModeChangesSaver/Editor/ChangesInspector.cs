using PlayModeChangesSaver.Editor.ChangesTracker;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    [InitializeOnLoad]
    public static class ChangesInspector
    {
        static ChangesInspector()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
            // Without a domain reload on entering Play Mode the cache would survive into the next session.
            EditorApplication.playModeStateChanged += _ => _cachedGo = null;
        }

        private static void OnPostHeaderGUI(UnityEditor.Editor editor)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!TryGetTargetGameObject(editor, out var go))
            {
                return;
            }

            bool hasChanges = HasChangesCached(go);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            using (new EditorGUI.DisabledScope(!hasChanges))
            {
                GUIContent buttonContent = new("Play Mode Overrides");
                Rect buttonRect =
                    GUILayoutUtility.GetRect(buttonContent, EditorStyles.miniButton, GUILayout.Width(140f));
                if (GUI.Button(buttonRect, buttonContent, EditorStyles.miniButton))
                {
                    PopupWindow.Show(buttonRect, new OverridesPopUp(go));
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        // The header is drawn on every Inspector repaint while playing; a full serialized diff of all
        // components each time is expensive on large objects. A short-lived cache keeps the button responsive.
        private const double CacheSeconds = 0.25;
        private static GameObject _cachedGo;
        private static double _cachedAt;
        private static bool _cachedHasChanges;

        private static bool HasChangesCached(GameObject go)
        {
            double now = EditorApplication.timeSinceStartup;
            if (go == _cachedGo && now - _cachedAt < CacheSeconds)
            {
                return _cachedHasChanges;
            }

            _cachedGo = go;
            _cachedAt = now;
            _cachedHasChanges = ChangesTrackerCore.HasNameDelta(go) ||
                                ChangesTrackerCore.GetChangedComponents(go).Count > 0;
            return _cachedHasChanges;
        }

        private static bool TryGetTargetGameObject(UnityEditor.Editor editor, out GameObject go)
        {
            go = null;
            if (!editor)
            {
                return false;
            }

            if (!editor.target)
            {
                return false;
            }

            go = editor.target as GameObject;
            if (go != null)
            {
                return true;
            }

            var comp = editor.target as Component;
            if (!comp)
            {
                return false;
            }

            go = comp.gameObject;
            return go != null;
        }
    }
}