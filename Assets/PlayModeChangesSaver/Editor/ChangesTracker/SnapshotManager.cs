using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker
{
    public static class SnapshotManager
    {
        public static readonly Dictionary<string, Snapshot> Snapshots = new();
        public static readonly Dictionary<string, Dictionary<string, CompSnapshot>> ComponentSnapshots = new();
        public static readonly Dictionary<string, NameSnapshot> NameSnapshots = new();

        public static void CaptureSnapshots()
        {
            Snapshots.Clear();
            ComponentSnapshots.Clear();
            NameSnapshots.Clear();
            TransientKeys.Clear();

            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                CaptureSnapshotsForScene(SceneManager.GetSceneAt(i));
            }
        }

        /// <summary>
        ///     Captures baselines for one scene without discarding the baselines of other loaded scenes,
        ///     e.g. when a scene is loaded additively during Play Mode.
        /// </summary>
        public static void CaptureSnapshotsForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (var rootGo in scene.GetRootGameObjects())
            {
                CaptureGameObjectRecursive(rootGo);
            }
        }

        public static Snapshot GetSnapshot(GameObject go)
        {
            if (!go)
            {
                return null;
            }

            var key = GetGoKey(go);
            if (Snapshots.TryGetValue(key, out var snap))
            {
                return snap;
            }

            var fallbackKey = SceneAndPathUtilities.GetGameObjectKey(go);
            if (!string.IsNullOrEmpty(fallbackKey) && fallbackKey != key && Snapshots.TryGetValue(fallbackKey, out snap))
            {
                return snap;
            }

            return null;
        }

        public static void SetSnapshot(GameObject go, Snapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var key = GetGoKey(go);
            Snapshots[key] = snapshot;
        }

        private static int CaptureGameObjectRecursive(GameObject go)
        {
            var count = 1;

            var goKey = GetGoKey(go);
            var sceneKey = SceneAndPathUtilities.GetGameObjectKey(go);

            var snapshot = new Snapshot(go);
            var nameSnapshot = new NameSnapshot(go);
            StoreSnapshotVariants(goKey, sceneKey, snapshot, nameSnapshot);

            var compDict = new Dictionary<string, CompSnapshot>();
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp is null or Transform)
                {
                    continue;
                }

                var compKey = SceneAndPathUtilities.GetComponentKey(comp);
                var compSnapshot = ComponentSH.CaptureComponentSnapshot(comp);
                compDict[compKey] = compSnapshot;
            }

            StoreComponentSnapshotVariants(goKey, sceneKey, compDict);

            foreach (Transform child in go.transform)
            {
                count += CaptureGameObjectRecursive(child.gameObject);
            }

            return count;
        }

        private static void StoreSnapshotVariants(string goKey, string sceneKey, Snapshot snapshot,
            NameSnapshot nameSnapshot)
        {
            StoreSnapshotVariant(goKey, sceneKey, snapshot, nameSnapshot);
        }

        private static void StoreSnapshotVariant(string goKey, string sceneKey, Snapshot snapshot,
            NameSnapshot nameSnapshot)
        {
            if (!string.IsNullOrEmpty(goKey))
            {
                Snapshots[goKey] = snapshot;
                NameSnapshots[goKey] = nameSnapshot;
            }

            if (!string.IsNullOrEmpty(sceneKey) && sceneKey != goKey)
            {
                Snapshots[sceneKey] = snapshot;
                NameSnapshots[sceneKey] = nameSnapshot;
            }
        }

        private static void StoreComponentSnapshotVariants(string goKey, string sceneKey,
            Dictionary<string, CompSnapshot> compDict)
        {
            if (!string.IsNullOrEmpty(goKey))
            {
                ComponentSnapshots[goKey] = compDict;
            }

            if (!string.IsNullOrEmpty(sceneKey) && sceneKey != goKey)
            {
                ComponentSnapshots[sceneKey] = compDict;
            }
        }


        public static List<string> GetChangedProperties(Snapshot original, Snapshot current)
        {
            List<string> changed = new();

            if (original.position != current.position)
            {
                changed.Add("position");
            }

            if (original.rotation != current.rotation)
            {
                changed.Add("rotation");
            }

            if (original.scale != current.scale)
            {
                changed.Add("scale");
            }

            if (original.isRectTransform)
            {
                TransformSH.CheckRectTransformChanges(original, current, changed);
            }

            return changed;
        }


        public static string GetGoKey(GameObject go)
        {
            if (!go)
            {
                return string.Empty;
            }

            var guid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            if (SceneAndPathUtilities.IsUsableGlobalObjectId(guid))
            {
                return guid;
            }

            // Unusable ids (runtime-created objects, unsaved scenes) are shared by many objects. A path key
            // would change on rename or reparent and lose the baseline, so key the object itself instead.
            // Snapshots only live for the current session, which is all this key has to survive.
            if (!TransientKeys.TryGetValue(go, out var key))
            {
                key = $"transient:{++_transientKeyCounter}";
                TransientKeys[go] = key;
            }

            return key;
        }

        private static readonly Dictionary<GameObject, string> TransientKeys = new();
        private static int _transientKeyCounter;
    }
}