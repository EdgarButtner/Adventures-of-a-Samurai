using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.App;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Lifecycle
{
    public static class RuntimeOverrideApplicator
    {
        public static bool CanApplyRuntimeOverrides(Scene scene)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (!scene.isLoaded)
            {
                return false;
            }

            return true;
        }

        public static void ApplyRuntimeOverridesForScene(Scene scene)
        {
            ApplyTransformOverridesForScene(scene);
            ApplyComponentOverridesForScene(scene);
            ApplyNameOverridesForScene(scene);
        }

        private static bool IsStoreValid<T>(List<T> list) where T : class
        {
            return list is { Count: > 0 };
        }

        private static bool IsSceneValid(Scene sceneToCheck, Scene expectedScene)
        {
            return sceneToCheck.IsValid() && sceneToCheck == expectedScene;
        }

        private static Scene GetValidScene(string scenePath)
        {
            var changeScene = SceneManager.GetSceneByPath(scenePath);
            return changeScene.IsValid() ? changeScene : SceneManager.GetSceneByName(scenePath);
        }

        private static void ApplyTransformOverridesForScene(Scene scene)
        {
            var transformStore = ChangesStore.LoadExisting();
            if (!transformStore)
            {
                return;
            }

            if (!IsStoreValid(transformStore.changes))
            {
                return;
            }

            foreach (var change in transformStore.changes)
            {
                var changeScene = GetValidScene(change.scenePath);
                if (!IsSceneValid(changeScene, scene))
                {
                    continue;
                }

                GameObject go =
                    SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
                if (!go)
                {
                    continue;
                }

                TransformChangeApplicator.ApplyTransformChange(go, change);
            }
        }

        private static void ApplyComponentOverridesForScene(Scene scene)
        {
            var compStore = CompChangesStore.LoadExisting();
            if (!compStore)
            {
                return;
            }

            if (!IsStoreValid(compStore.changes))
            {
                return;
            }

            foreach (var change in compStore.changes)
            {
                var changeScene = GetValidScene(change.scenePath);
                if (!IsSceneValid(changeScene, scene))
                {
                    continue;
                }

                GameObject go =
                    SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
                if (!go)
                {
                    continue;
                }

                ComponentChangeApplicator.ApplyComponentChange(go, change);
            }
        }

        /// <summary>
        ///     Names come last, like in the edit-mode apply, so the lookups above still see the loaded names.
        ///     Only the GameObjects are renamed; the store entries stay as they are.
        /// </summary>
        private static void ApplyNameOverridesForScene(Scene scene)
        {
            var nameStore = NameChangesStore.LoadExisting();
            if (!nameStore)
            {
                return;
            }

            if (!IsStoreValid(nameStore.changes))
            {
                return;
            }

            foreach (var change in nameStore.changes)
            {
                var changeScene = GetValidScene(change.scenePath);
                if (!IsSceneValid(changeScene, scene))
                {
                    continue;
                }

                GameObject go =
                    SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
                if (!go || string.IsNullOrEmpty(change.newName))
                {
                    continue;
                }

                go.name = change.newName;
            }
        }
    }
}