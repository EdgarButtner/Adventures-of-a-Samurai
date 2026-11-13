using System;
using System.Collections.Generic;
using System.Linq;
using PlayModeChangesSaver.Editor.ChangesTracker.Serialization;
using UnityEditor;
using UnityEngine;
using static PlayModeChangesSaver.Editor.ChangesTracker.SnapshotManager;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper
{
    public static class ComponentSH //Component SnapShot Helper
    {
        // Properties that should be ignored when checking for changes
        // These are typically auto-calculated or internal Unity properties
        // ONLY add properties that are truly auto-calculated and never user-editable
        private static readonly HashSet<string> IgnoredPropertyPaths = new()
        {
            // Renderer properties (auto-calculated bounds/batching)
            "m_Bounds",
            "m_AABB",
            "m_StaticBatchInfo",
            "m_SubsetIndices",

            // Collider properties (auto-calculated)
            "m_BoundingVolume",

            // Common internal Unity properties (prefab system)
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset"
        };

        private static bool ShouldIgnoreProperty(string propertyPath)
        {
            // Check exact match
            if (IgnoredPropertyPaths.Contains(propertyPath))
            {
                return true;
            }

            // Check if any ignored path is a prefix (for nested properties)
            foreach (var ignoredPath in IgnoredPropertyPaths)
            {
                if (propertyPath.StartsWith(ignoredPath + "."))
                {
                    return true;
                }
            }

            return false;
        }

        // Public wrapper for external use
        public static bool ShouldIgnorePropertyPath(string propertyPath)
        {
            return ShouldIgnoreProperty(propertyPath);
        }

        // Hidden properties tracked in addition to the visible ones: the enabled checkbox in the component
        // header and the connected bodies of joints, which the joint inspector draws itself. Other hidden
        // properties, including [HideInInspector] fields, stay untracked.
        private static readonly string[] TrackedHiddenPropertyPaths =
        {
            "m_Enabled",
            "m_ConnectedBody",
            "m_ConnectedArticulationBody"
        };

        /// <summary>
        ///     The tracked hidden properties that exist on <paramref name="so" />. Every place that iterates the
        ///     visible properties for capture, compare, record or revert handles these as well. A property that
        ///     is visible on this component is left out, the visible iteration already covers it.
        /// </summary>
        public static List<SerializedProperty> GetTrackedHiddenProperties(SerializedObject so)
        {
            var result = new List<SerializedProperty>();
            if (so == null)
            {
                return result;
            }

            var visible = new HashSet<string>();
            var it = so.GetIterator();
            var enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                visible.Add(it.propertyPath);
            }

            foreach (var path in TrackedHiddenPropertyPaths)
            {
                var prop = so.FindProperty(path);
                if (prop != null && !visible.Contains(path) &&
                    ComponentPropertySerializer.IsTypeSupported(prop.propertyType))
                {
                    result.Add(prop);
                }
            }

            return result;
        }

        public static void ResetComponentBaseline(Component comp)
        {
            if (!comp)
            {
                return;
            }

            var go = comp.gameObject;
            var goKey = GetGoKey(go);

            if (!ComponentSnapshots.TryGetValue(goKey, out var dict))
            {
                dict = new Dictionary<string, CompSnapshot>();
                ComponentSnapshots[goKey] = dict;
            }

            var compKey = SceneAndPathUtilities.GetComponentKey(comp);
            dict[compKey] = CaptureComponentSnapshot(comp);
        }

        public static List<Component> GetChangedComponents(GameObject go)
        {
            if (!go)
            {
                return new List<Component>();
            }

            var key = GetGoKey(go);

            if (!ComponentSnapshots.ContainsKey(key))
            {
                return new List<Component>();
            }

            var changed = new List<Component>();
            var compSnapshots = ComponentSnapshots[key];

            TransformSH.CheckTransformChanges(go, key, changed);
            CheckComponentChanges(go, compSnapshots, changed);

            return changed;
        }


        private static void CheckComponentChanges(GameObject go, Dictionary<string, CompSnapshot> compSnapshots,
            List<Component> changed)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp is null or Transform)
                {
                    continue;
                }

                var compKey = SceneAndPathUtilities.GetComponentKey(comp);

                if (!compSnapshots.TryGetValue(compKey, out var snapshot))
                {
                    continue;
                }

                var hasChanged = HasComponentChanged(comp, snapshot);

                if (hasChanged)
                {
                    changed.Add(comp);
                }
            }
        }

        private static bool HasComponentChanged(Component comp, CompSnapshot snapshot)
        {
            // Additional safety check - component might be destroyed between enumeration and check
            if (!comp)
            {
                return false;
            }

            try
            {
                var so = new SerializedObject(comp);
                if (HasComponentPropertiesChanged(so, snapshot))
                {
                    return true;
                }

                return comp is Renderer renderer && HaveRendererMaterialsChanged(renderer, snapshot.materialGuids);
            }
            catch (Exception)
            {
                // Component might have been destroyed or is in an invalid state
                return false;
            }
        }

        private static bool HasComponentPropertiesChanged(SerializedObject so, CompSnapshot snapshot)
        {
            var prop = so.GetIterator();
            var enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                if (prop.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                // Skip ignored properties
                if (ShouldIgnoreProperty(prop.propertyPath))
                {
                    enterChildren = false;
                    continue;
                }

                // If the property type is supported directly (e.g. primitives, vectors), check it.
                // If it is NOT supported (e.g. Generic, Array), we need to enter children.
                var isSupported = ComponentPropertySerializer.IsTypeSupported(prop.propertyType);

                if (isSupported)
                {
                    enterChildren = false; // We handle this "leaf", don't go deeper

                    if (!snapshot.Properties.TryGetValue(prop.propertyPath, out var originalValue))
                    {
                        // Property not in snapshot - likely added in newer Unity version or was filtered
                        // Skip it rather than marking as changed to avoid false positives
                        continue;
                    }

                    try
                    {
                        var currentValue = ComponentPropertySerializer.GetPropertyValue(prop);
                        if (HasPropertyValueChanged(currentValue, originalValue))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    // Not a supported leaf type, so it must be a container (e.g., Array, List, Struct)
                    // we want to dive into.
                    if (prop.isArray && prop.propertyType != SerializedPropertyType.String &&
                        snapshot.Properties.TryGetValue(prop.propertyPath + ".Array.size", out var originalSize) &&
                        originalSize is int size && prop.arraySize != size)
                    {
                        return true;
                    }

                    enterChildren = true;
                }
            }

            foreach (var hidden in GetTrackedHiddenProperties(so))
            {
                if (snapshot.Properties.TryGetValue(hidden.propertyPath, out var originalValue) &&
                    HasPropertyValueChanged(ComponentPropertySerializer.GetPropertyValue(hidden), originalValue))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPropertyValueChanged(object currentValue, object originalValue)
        {
            if (AreValuesNullEqual(currentValue, originalValue))
            {
                return false;
            }

            if (IsEitherValueNull(currentValue, originalValue))
            {
                return true;
            }

            return ComparePropertyValues(currentValue, originalValue);
        }

        private static bool AreValuesNullEqual(object currentValue, object originalValue)
        {
            return currentValue == null && originalValue == null;
        }

        private static bool IsEitherValueNull(object currentValue, object originalValue)
        {
            return currentValue == null || originalValue == null;
        }

        private static bool ComparePropertyValues(object currentValue, object originalValue)
        {
            return currentValue switch
            {
                Vector2 v2Current when originalValue is Vector2 v2Original => HasVector2Changed(v2Current, v2Original),
                Vector3 v3Current when originalValue is Vector3 v3Original => HasVector3Changed(v3Current, v3Original),
                Quaternion qCurrent when originalValue is Quaternion qOriginal => HasQuaternionChanged(qCurrent,
                    qOriginal),
                Rect rCurrent when originalValue is Rect rOriginal => rCurrent != rOriginal,
                RectInt riCurrent when originalValue is RectInt riOriginal => !riCurrent.Equals(riOriginal),
                Bounds bCurrent when originalValue is Bounds bOriginal => bCurrent != bOriginal,
                BoundsInt biCurrent when originalValue is BoundsInt biOriginal => biCurrent != biOriginal,
                Vector2Int v2ICurrent when originalValue is Vector2Int v2IOriginal => v2ICurrent != v2IOriginal,
                Vector3Int v3ICurrent when originalValue is Vector3Int v3IOriginal => v3ICurrent != v3IOriginal,
                float fCurrent when originalValue is float fOriginal => HasFloatChanged(fCurrent, fOriginal),
                Object objCurrent when originalValue is Object objOriginal => objCurrent != objOriginal,
                _ => !Equals(currentValue, originalValue)
            };
        }

        private static bool HasVector2Changed(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) > 0.0001f;
        }

        private static bool HasVector3Changed(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) > 0.0001f;
        }

        private static bool HasQuaternionChanged(Quaternion a, Quaternion b)
        {
            return Quaternion.Angle(a, b) > 0.0001f;
        }

        private static bool HasFloatChanged(float a, float b)
        {
            return Mathf.Abs(a - b) > 0.0001f;
        }

        private static bool HaveRendererMaterialsChanged(Renderer renderer, List<string> originalGuids)
        {
            var currentGuids = CaptureMaterialGuids(renderer);
            var origGuids = originalGuids ?? new List<string>();

            return AreGuidListsDifferent(currentGuids, origGuids);
        }

        private static bool AreGuidListsDifferent(List<string> currentGuids, List<string> originalGuids)
        {
            if (currentGuids.Count != originalGuids.Count)
            {
                return true;
            }

            return currentGuids.Where((guid, i) => !string.Equals(guid, originalGuids[i], StringComparison.Ordinal))
                .Any();
        }

        public static List<string> CaptureMaterialGuids(Renderer renderer)
        {
            var result = new List<string>();
            if (!renderer)
            {
                return result;
            }

            var materials = renderer.sharedMaterials;
            foreach (var mat in materials)
            {
                if (!mat)
                {
                    result.Add(string.Empty);
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(mat);
                var guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
                result.Add(guid);
            }

            return result;
        }

        public static CompSnapshot CaptureComponentSnapshot(Component comp)
        {
            var snapshot = new CompSnapshot
            {
                componentType = comp.GetType().AssemblyQualifiedName,
                globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(comp.gameObject).ToString()
            };

            if (comp is Renderer renderer)
            {
                snapshot.materialGuids = CaptureMaterialGuids(renderer);
            }

            SerializedObject so = new(comp);
            var prop = so.GetIterator();

            var enterChildren = true;
            while (prop.NextVisible(enterChildren))
            {
                if (prop.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                // Skip ignored properties
                if (ShouldIgnoreProperty(prop.propertyPath))
                {
                    enterChildren = false;
                    continue;
                }

                var isSupported = ComponentPropertySerializer.IsTypeSupported(prop.propertyType);
                if (isSupported)
                {
                    enterChildren = false;
                    try
                    {
                        var value = ComponentPropertySerializer.GetPropertyValue(prop);
                        // Keep empty object references, otherwise assigning one later goes undetected.
                        if (value != null || prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            snapshot.Properties[prop.propertyPath] = value;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    // Recurse into complex types / arrays
                    if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                    {
                        // Explicitly capture array size, because sometimes NextVisible skips it or order matters
                        var sizePath = prop.propertyPath + ".Array.size";
                        snapshot.Properties[sizePath] = prop.arraySize;
                    }

                    enterChildren = true;
                }
            }

            foreach (var hidden in GetTrackedHiddenProperties(so))
            {
                // Same rule as above: keep an empty connected body, otherwise assigning one goes undetected.
                snapshot.Properties[hidden.propertyPath] = ComponentPropertySerializer.GetPropertyValue(hidden);
            }

            return snapshot;
        }

        public static CompSnapshot GetComponentSnapshot(GameObject go, string componentKey)
        {
            var goKey = GetGoKey(go);
            if (!ComponentSnapshots.TryGetValue(goKey, out var componentSnapshot))
            {
                return null;
            }

            return componentSnapshot.GetValueOrDefault(componentKey);
        }
    }
}