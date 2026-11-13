using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow
{
    /// <summary>
    ///     The modal prompts of the flow that runs after leaving Play Mode, and how it defers its next step.
    ///     Tests replace the delegates (via reflection) to answer without a modal window and to run deferred
    ///     steps themselves; the editor always uses the defaults.
    /// </summary>
    internal static class ExitFlowPrompts
    {
        /// <summary>Title, message, ok, cancel; true for ok.</summary>
        internal static Func<string, string, string, string, bool> DisplayDialog = EditorUtility.DisplayDialog;

        /// <summary>Asks to save the given modified scenes; false if the user cancels.</summary>
        internal static Func<Scene[], bool> SaveModifiedScenesIfUserWantsTo =
            EditorSceneManager.SaveModifiedScenesIfUserWantsTo;

        /// <summary>
        ///     Runs a step on a later editor update. In batch mode Unity calls delayCall only every few hundred
        ///     updates, so tests run the deferred steps themselves.
        /// </summary>
        internal static Action<Action> Defer = DeferToDelayCall;

        internal static void ResetToDefaults()
        {
            DisplayDialog = EditorUtility.DisplayDialog;
            SaveModifiedScenesIfUserWantsTo = EditorSceneManager.SaveModifiedScenesIfUserWantsTo;
            Defer = DeferToDelayCall;
        }

        private static void DeferToDelayCall(Action step)
        {
            EditorApplication.delayCall += () => step();
        }
    }
}
