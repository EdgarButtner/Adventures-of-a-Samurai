using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.ChangesTracker
{
    public static class SceneAndPathUtilities
    {
        public static string NormalizeScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/').Trim();
        }

        /// <summary>
        ///     False for empty ids and for ids without a target object ("...-0-0"), which Unity returns for
        ///     objects in never-saved scenes and for objects created at runtime. Such ids are identical for
        ///     many objects and must not be used to tell objects apart.
        /// </summary>
        public static bool IsUsableGlobalObjectId(string globalObjectId)
        {
            if (string.IsNullOrEmpty(globalObjectId))
            {
                return false;
            }

            // Format: GlobalObjectId_V1-{identifierType}-{assetGUID}-{targetObjectId}-{targetPrefabId}
            var parts = globalObjectId.Split('-');
            if (parts.Length != 5)
            {
                return true; // unknown format: keep the previous behaviour and compare verbatim
            }

            return parts[1] != "0" && parts[3] != "0";
        }

        /// <summary>
        ///     True if both ids are usable and equal; callers then fall back to scene/object path matching.
        /// </summary>
        public static bool SameGlobalObjectId(string a, string b)
        {
            return IsUsableGlobalObjectId(a) &&
                   string.Equals(CanonicalGlobalObjectId(a), CanonicalGlobalObjectId(b), StringComparison.Ordinal);
        }

        /// <summary>
        ///     Scene-local form of an id. An object of a prefab instance reports (source file id, prefab instance
        ///     file id) in Edit Mode, but in Play Mode the instance is unpacked and the object reports
        ///     ((source file id XOR prefab instance file id) without the top bit, 0). Both map to the second form.
        ///     Other ids are returned unchanged.
        /// </summary>
        public static string CanonicalGlobalObjectId(string globalObjectId)
        {
            if (string.IsNullOrEmpty(globalObjectId))
            {
                return globalObjectId;
            }

            // Format: GlobalObjectId_V1-{identifierType}-{assetGUID}-{targetObjectId}-{targetPrefabId}
            var parts = globalObjectId.Split('-');
            if (parts.Length != 5 ||
                !ulong.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var objectId) ||
                !ulong.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var prefabId) ||
                prefabId == 0)
            {
                return globalObjectId;
            }

            var localId = (objectId ^ prefabId) & 0x7FFFFFFFFFFFFFFFUL;
            return $"{parts[0]}-{parts[1]}-{parts[2]}-{localId.ToString(CultureInfo.InvariantCulture)}-0";
        }

        /// <summary>
        ///     Matches a store entry to an object. When both ids are usable they decide alone: two objects can
        ///     share a path (same-named prefab instances, or a rename onto the old path of another object).
        ///     The path only decides when at least one id is missing or a placeholder.
        /// </summary>
        public static bool IsSameObject(string entryId, string liveId, bool pathMatches)
        {
            if (IsUsableGlobalObjectId(entryId) && IsUsableGlobalObjectId(liveId))
            {
                return string.Equals(CanonicalGlobalObjectId(entryId), CanonicalGlobalObjectId(liveId),
                    StringComparison.Ordinal);
            }

            return pathMatches;
        }

        public static Scene GetSceneByPathOrName(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
            {
                scene = SceneManager.GetSceneByName(scenePath);
            }

            return scene;
        }

        /// <summary>
        ///     Hybrid lookup: attempts GUID-based lookup first, falls back to path-based lookup.
        ///     Logs warning if fallback is used.
        /// </summary>
        public static GameObject FindGameObjectByGuidOrPath(Scene scene, string globalObjectIdStr, string objectPath)
        {
            // Attempt GUID lookup first (primary method)
            if (!string.IsNullOrEmpty(globalObjectIdStr))
            {
                GameObject guidResult = FindGameObjectByGuid(globalObjectIdStr);
                if (guidResult != null)
                {
                    return guidResult;
                }
            }

            if (IsUsableGlobalObjectId(globalObjectIdStr))
            {
                // Ids of prefab instance objects recorded in Play Mode do not resolve in Edit Mode (and the
                // other way round); their scene-local form still identifies the object.
                GameObject canonicalResult = FindInSceneByCanonicalId(scene, globalObjectIdStr);
                if (canonicalResult != null)
                {
                    return canonicalResult;
                }

                // The object of the id is not there. The path may only stand in if no other object shares it,
                // otherwise the change would land on whichever same-named object comes first.
                return IsPathUnique(scene, objectPath) ? FindInSceneByPath(scene, objectPath) : null;
            }

            // Fallback to path-based lookup
            GameObject pathResult = FindInSceneByPath(scene, objectPath);
            if (pathResult != null)
            {
                return pathResult;
            }

            return null;
        }

        /// <summary>
        ///     Attempts to find a GameObject by its GlobalObjectId string.
        ///     Returns null if GUID is invalid or object not found.
        /// </summary>
        public static GameObject FindGameObjectByGuid(string globalObjectIdStr)
        {
            if (!IsUsableGlobalObjectId(globalObjectIdStr))
            {
                return null;
            }

            if (!GlobalObjectId.TryParse(globalObjectIdStr, out GlobalObjectId globalObjectId))
            {
                return null;
            }

            Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            if (obj is GameObject go)
            {
                return go;
            }

            if (obj is Component comp)
            {
                return comp.gameObject;
            }

            return null;
        }

        /// <summary>
        ///     Resolves the id of a scene object (GameObject or component) that GlobalObjectIdentifierToObjectSlow
        ///     cannot resolve, through its scene-local form: references to objects of a prefab instance recorded
        ///     in Play Mode, resolved in Edit Mode, and the other way round. Only the loaded scene named by the id
        ///     is searched. Returns null if nothing matches.
        /// </summary>
        public static Object FindSceneObjectByCanonicalId(string globalObjectIdStr)
        {
            if (!IsUsableGlobalObjectId(globalObjectIdStr) ||
                !GlobalObjectId.TryParse(globalObjectIdStr, out var id) ||
                id.identifierType != SceneObjectIdentifierType)
            {
                return null;
            }

            var scenePath = AssetDatabase.GUIDToAssetPath(id.assetGUID.ToString());
            if (string.IsNullOrEmpty(scenePath))
            {
                return null;
            }

            return FindInSceneByCanonicalId(SceneManager.GetSceneByPath(scenePath), globalObjectIdStr, true);
        }

        // GlobalObjectId.identifierType of objects in scenes (1 is an imported asset, 3 a source asset).
        private const int SceneObjectIdentifierType = 2;

        private static GameObject FindInSceneByCanonicalId(Scene scene, string globalObjectIdStr)
        {
            return FindInSceneByCanonicalId(scene, globalObjectIdStr, false) as GameObject;
        }

        private static Object FindInSceneByCanonicalId(Scene scene, string globalObjectIdStr, bool includeComponents)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var objects = new List<Object>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    objects.Add(t.gameObject);
                    if (!includeComponents)
                    {
                        continue;
                    }

                    foreach (var component in t.GetComponents<Component>())
                    {
                        // Missing scripts show up as null components.
                        if (component)
                        {
                            objects.Add(component);
                        }
                    }
                }
            }

            var objectArray = objects.ToArray();
            var ids = new GlobalObjectId[objectArray.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(objectArray, ids);

            var wanted = CanonicalGlobalObjectId(globalObjectIdStr);
            for (var i = 0; i < ids.Length; i++)
            {
                if (string.Equals(CanonicalGlobalObjectId(ids[i].ToString()), wanted, StringComparison.Ordinal))
                {
                    return objectArray[i];
                }
            }

            return null;
        }

        private static bool IsPathUnique(Scene scene, string path)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(path))
            {
                return false;
            }

            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GetGameObjectPath(t) == path && ++count > 1)
                    {
                        return false;
                    }
                }
            }

            return count == 1;
        }

        public static GameObject FindInSceneByPath(Scene scene, string path)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            var parts = path.Split('/');
            if (parts.Length == 0)
            {
                return null;
            }

            GameObject current = FindRootGameObjectByName(scene, parts[0]);
            if (!current)
            {
                return null;
            }

            current = TraverseChildPath(current, parts, 1);
            return current;
        }

        private static GameObject FindRootGameObjectByName(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject TraverseChildPath(GameObject root, string[] parts, int startIdx)
        {
            GameObject current = root;
            for (int i = startIdx; i < parts.Length; i++)
            {
                var childName = parts[i];
                Transform child = null;
                foreach (Transform t in current.transform)
                {
                    if (t.name == childName)
                    {
                        child = t;
                        break;
                    }
                }

                if (!child)
                {
                    return null;
                }

                current = child.gameObject;
            }

            return current;
        }

        public static string GetGameObjectPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        public static string GetGameObjectKey(GameObject go)
        {
            if (!go)
            {
                return "";
            }

            string scenePath = NormalizeScenePath(go.scene.path);
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = go.scene.name;
            }

            string goPath = GetGameObjectPath(go.transform);
            return $"{scenePath}|{goPath}";
        }

        public static string GetComponentKey(Component comp)
        {
            var allComps = comp.gameObject.GetComponents(comp.GetType());
            int index = Array.IndexOf(allComps, comp);
            return $"{comp.GetType().Name}_{index}";
        }
    }
}