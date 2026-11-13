using System;
using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Lifecycle
{
    [InitializeOnLoad]
    public static class PlayModeLifecycleManager
    {
        // Session-scoped flags survive the domain reload on entering Play Mode. They used to live in
        // EditorPrefs (registry on Windows), which were read on every editor update and outlived the session.
        private const string CaptureNeededKey = "PlayModeChangesTracker_CaptureNeeded";
        private const string StartSceneKey = "PlayModeChangesTracker_StartScenePath";

        // Scenes loaded additively during Play Mode; their baselines are taken on the next editor update.
        private static readonly List<Scene> ScenesToCapture = new();

        private static string StartScenePathAtPlayEnter
        {
            get => SessionState.GetString(StartSceneKey, null);
            set => SessionState.SetString(StartSceneKey, value ?? string.Empty);
        }

        static PlayModeLifecycleManager()
        {
            EditorPrefs.DeleteKey(CaptureNeededKey); // leftover from earlier versions
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            CaptureLoadedScenes();

            if (!SessionState.GetBool(CaptureNeededKey, false))
            {
                return;
            }

            SessionState.EraseBool(CaptureNeededKey);
            // Captured right here: with Enter Play Mode Options (no domain reload) a delayCall queued now did not
            // run before the end of Play Mode, so the session had no baselines of its own.
            SnapshotManager.CaptureSnapshots();
        }

        private static void CaptureLoadedScenes()
        {
            if (ScenesToCapture.Count == 0)
            {
                return;
            }

            var scenes = ScenesToCapture.ToArray();
            ScenesToCapture.Clear();
            foreach (var scene in scenes)
            {
                SnapshotManager.CaptureSnapshotsForScene(scene);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    HandleExitingEditMode();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    SessionState.SetBool(CaptureNeededKey, true);
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    SessionState.EraseBool(CaptureNeededKey);
                    HandlePlayExitFlow();
                    break;
            }
        }

        private static void HandleExitingEditMode()
        {
            // A domain reload used to reset this flag on every Play Mode entry. Without one (Enter Play Mode
            // Options), a flow that never finished would block the apply flow of every later session.
            PlayModeOverrideFlow.StopProcessing();
            ScenesToCapture.Clear();
            ChangesTrackerCore.ClearStoresOnPlayEnter();
            var startScene = SceneManager.GetActiveScene();
            StartScenePathAtPlayEnter = startScene.IsValid() ? startScene.path : null;

            SnapshotManager.CaptureSnapshots();
            SessionState.SetBool(CaptureNeededKey, true);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!RuntimeOverrideApplicator.CanApplyRuntimeOverrides(scene))
            {
                return;
            }

            RuntimeOverrideApplicator.ApplyRuntimeOverridesForScene(scene);

            // Like the capture on entering Play Mode, this waits for the next editor update (OnEditorUpdate)
            // instead of a delayCall, which did not run during Play Mode without a domain reload.
            if (mode == LoadSceneMode.Single)
            {
                SessionState.SetBool(CaptureNeededKey, true);
                return;
            }

            // Additive load: a full recapture would turn changes already made in the other loaded
            // scenes into the new baseline, so they would no longer be detected.
            ScenesToCapture.Add(scene);
        }

        private static void HandlePlayExitFlow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!IsPlayExitSceneValid())
            {
                return;
            }

            EditorApplication.delayCall += TriggerApplyFlowAfterPlayExit;
        }

        private static bool IsPlayExitSceneValid()
        {
            var activeAfterPlay = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(StartScenePathAtPlayEnter))
            {
                StartScenePathAtPlayEnter = activeAfterPlay.IsValid() ? activeAfterPlay.path : null;
            }

            var activePathNow = activeAfterPlay.IsValid()
                ? SceneAndPathUtilities.NormalizeScenePath(activeAfterPlay.path)
                : string.Empty;
            var startPathNow = SceneAndPathUtilities.NormalizeScenePath(StartScenePathAtPlayEnter);

            if (string.IsNullOrEmpty(startPathNow))
            {
                // Started from an unsaved scene, which has no path. Its own overrides cannot be applied, but
                // those of saved scenes loaded next to it can, so the flow runs if it is still active.
                return string.IsNullOrEmpty(activePathNow);
            }

            if (!string.Equals(activePathNow, startPathNow, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsApplyFlowInProgress()
        {
            return Application.isPlaying || PlayModeOverrideFlow.IsProcessing;
        }

        private static bool IsSceneNotReady(Scene scene)
        {
            return !scene.isLoaded || EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

        private static void TriggerApplyFlowAfterPlayExit()
        {
            if (IsApplyFlowInProgress())
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (IsSceneNotReady(activeScene))
            {
                EditorApplication.delayCall += TriggerApplyFlowAfterPlayExit;
                return;
            }

            EditorApplication.delayCall += ApplyFlowWhenStable;
        }

        private static void ApplyFlowWhenStable()
        {
            if (IsApplyFlowInProgress())
            {
                return;
            }

            PlayModeOverrideFlow.HandleApplyChangesFromStoreOnPlayExit();
        }
    }
}