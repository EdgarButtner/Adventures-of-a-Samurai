using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Serialization
{
    /// <summary>
    ///     Single codec for component property values: string serialization for the stores,
    ///     boxed values for in-memory snapshots, and comparison against stored values.
    ///     Stored type names are part of the persisted format; legacy names stay readable.
    /// </summary>
    public static class ComponentPropertySerializer
    {
        // Legacy type names, still written or read by older data.
        internal const string IntegerType = "Integer";
        internal const string BooleanType = "Boolean";
        internal const string FloatType = "Float";
        internal const string StringType = "String";
        internal const string LegacyColorType = "Color"; // "#RRGGBBAA", 8 bit per channel, clamped to 0..1
        internal const string LegacyEnumType = "Enum"; // enumValueIndex, cannot express flag combinations

        // Lossless formats written since the codec consolidation.
        internal const string ColorType = "ColorRGBA"; // "r,g,b,a" floats, keeps HDR values
        internal const string EnumValueType = "EnumValue"; // underlying enum value, works for [Flags]

        internal const string ObjectReferenceType = "ObjectReference";

        private const float Tolerance = 0.0001f;
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, (string, string)>>
            SerializationStrategies =
                new()
                {
                    { SerializedPropertyType.Integer, p => (IntegerType, SerializeInteger(p)) },
                    { SerializedPropertyType.Boolean, p => (BooleanType, p.boolValue.ToString()) },
                    { SerializedPropertyType.Float, p => (FloatType, SnapshotSerializer.FormatFloat(p.floatValue)) },
                    { SerializedPropertyType.String, p => (StringType, p.stringValue ?? string.Empty) },
                    { SerializedPropertyType.Color, p => (ColorType, SnapshotSerializer.SerializeColor(p.colorValue)) },
                    {
                        SerializedPropertyType.Vector2,
                        p => ("Vector2", SnapshotSerializer.SerializeValue(p.vector2Value).SerializedValue)
                    },
                    {
                        SerializedPropertyType.Vector3,
                        p => ("Vector3", SnapshotSerializer.SerializeValue(p.vector3Value).SerializedValue)
                    },
                    {
                        SerializedPropertyType.Vector4,
                        p => ("Vector4", SnapshotSerializer.SerializeValue(p.vector4Value).SerializedValue)
                    },
                    {
                        SerializedPropertyType.Quaternion,
                        p => ("Quaternion", SnapshotSerializer.SerializeValue(p.quaternionValue).SerializedValue)
                    },
                    { SerializedPropertyType.Enum, p => (EnumValueType, p.intValue.ToString(Invariant)) },
                    {
                        SerializedPropertyType.Vector2Int,
                        p => ("Vector2Int", SnapshotSerializer.SerializeValue(p.vector2IntValue).SerializedValue)
                    },
                    {
                        SerializedPropertyType.Vector3Int,
                        p => ("Vector3Int", SnapshotSerializer.SerializeValue(p.vector3IntValue).SerializedValue)
                    },
                    {
                        SerializedPropertyType.Rect,
                        p => ("Rect", SnapshotSerializer.SerializeValue(p.rectValue).SerializedValue)
                    },
                    {
                        SerializedPropertyType.RectInt,
                        p => ("RectInt", SnapshotSerializer.SerializeValue(p.rectIntValue).SerializedValue)
                    },
                    {
                        SerializedPropertyType.Bounds,
                        p => ("Bounds", SnapshotSerializer.SerializeValue(p.boundsValue).SerializedValue)
                    },
                    {
                        SerializedPropertyType.BoundsInt,
                        p => ("BoundsInt", SnapshotSerializer.SerializeValue(p.boundsIntValue).SerializedValue)
                    },
                    { SerializedPropertyType.LayerMask, p => (IntegerType, p.intValue.ToString(Invariant)) },
                    {
                        SerializedPropertyType.Gradient,
                        p => ("Gradient", SnapshotSerializer.SerializeGradient(p.gradientValue))
                    },
                    {
                        SerializedPropertyType.AnimationCurve,
                        p => ("AnimationCurve", SnapshotSerializer.SerializeAnimationCurve(p.animationCurveValue))
                    },
                    {
                        SerializedPropertyType.ObjectReference,
                        p => (ObjectReferenceType, SerializeObjectReference(p.objectReferenceValue))
                    }
                };

        private static readonly Dictionary<string, Action<SerializedProperty, string>> DeserializationStrategies =
            new()
            {
                { IntegerType, ApplyInteger },
                {
                    BooleanType, (p, v) =>
                    {
                        if (bool.TryParse(v, out var val))
                        {
                            p.boolValue = val;
                        }
                    }
                },
                {
                    FloatType, (p, v) =>
                    {
                        if (float.TryParse(v, NumberStyles.Float, Invariant, out var val))
                        {
                            p.floatValue = val;
                        }
                    }
                },
                { StringType, (p, v) => p.stringValue = v ?? string.Empty },
                {
                    ColorType, (p, v) =>
                    {
                        if (SnapshotSerializer.TryDeserializeColor(v, out var col))
                        {
                            p.colorValue = col;
                        }
                    }
                },
                {
                    LegacyColorType, (p, v) =>
                    {
                        if (ColorUtility.TryParseHtmlString(v, out var col))
                        {
                            p.colorValue = col;
                        }
                    }
                },
                {
                    "Vector2", (p, v) =>
                    {
                        if (HasComponents(v, 2, false))
                        {
                            p.vector2Value = SnapshotSerializer.DeserializeVector2(v);
                        }
                    }
                },
                {
                    "Vector3", (p, v) =>
                    {
                        if (HasComponents(v, 3, false))
                        {
                            p.vector3Value = SnapshotSerializer.DeserializeVector3(v);
                        }
                    }
                },
                {
                    "Vector4", (p, v) =>
                    {
                        if (HasComponents(v, 4, false))
                        {
                            p.vector4Value = SnapshotSerializer.DeserializeVector4(v);
                        }
                    }
                },
                {
                    "Quaternion", (p, v) =>
                    {
                        if (HasComponents(v, 4, false))
                        {
                            p.quaternionValue = SnapshotSerializer.DeserializeQuaternion(v);
                        }
                    }
                },
                {
                    EnumValueType, (p, v) =>
                    {
                        if (int.TryParse(v, NumberStyles.Integer, Invariant, out var val))
                        {
                            p.intValue = val;
                        }
                    }
                },
                {
                    LegacyEnumType, (p, v) =>
                    {
                        if (int.TryParse(v, NumberStyles.Integer, Invariant, out var val) && val >= 0)
                        {
                            p.enumValueIndex = val;
                        }
                    }
                },
                {
                    "Vector2Int", (p, v) =>
                    {
                        if (HasComponents(v, 2, true))
                        {
                            p.vector2IntValue = SnapshotSerializer.DeserializeVector2Int(v);
                        }
                    }
                },
                {
                    "Vector3Int", (p, v) =>
                    {
                        if (HasComponents(v, 3, true))
                        {
                            p.vector3IntValue = SnapshotSerializer.DeserializeVector3Int(v);
                        }
                    }
                },
                {
                    "Rect", (p, v) =>
                    {
                        if (HasComponents(v, 4, false))
                        {
                            p.rectValue = SnapshotSerializer.DeserializeRect(v);
                        }
                    }
                },
                {
                    "RectInt", (p, v) =>
                    {
                        if (HasComponents(v, 4, true))
                        {
                            p.rectIntValue = SnapshotSerializer.DeserializeRectInt(v);
                        }
                    }
                },
                {
                    "Bounds", (p, v) =>
                    {
                        if (HasBoundsComponents(v, false))
                        {
                            p.boundsValue = SnapshotSerializer.DeserializeBounds(v);
                        }
                    }
                },
                {
                    "BoundsInt", (p, v) =>
                    {
                        if (HasBoundsComponents(v, true))
                        {
                            p.boundsIntValue = SnapshotSerializer.DeserializeBoundsInt(v);
                        }
                    }
                },
                {
                    "Gradient", (p, v) =>
                    {
                        var g = SnapshotSerializer.DeserializeGradient(v);
                        if (g != null)
                        {
                            p.gradientValue = g;
                        }
                    }
                },
                {
                    "AnimationCurve", (p, v) =>
                    {
                        var c = SnapshotSerializer.DeserializeAnimationCurve(v);
                        if (c != null)
                        {
                            p.animationCurveValue = c;
                        }
                    }
                },
                { ObjectReferenceType, ApplyObjectReference }
            };

        /// <summary>
        ///     Property types each stored type name may be written to. A stored value whose type no longer matches
        ///     the property (e.g. a script field changed from float to string since recording) is skipped, because
        ///     Unity's setters log "Mismatched types" errors instead of converting. Includes the combinations older
        ///     versions wrote: "Integer" for layer masks, array sizes and enum values taken from snapshots.
        /// </summary>
        private static readonly Dictionary<string, SerializedPropertyType[]> CompatiblePropertyTypes =
            new()
            {
                {
                    IntegerType,
                    new[]
                    {
                        SerializedPropertyType.Integer, SerializedPropertyType.Enum, SerializedPropertyType.LayerMask,
                        SerializedPropertyType.ArraySize
                    }
                },
                { BooleanType, new[] { SerializedPropertyType.Boolean } },
                { FloatType, new[] { SerializedPropertyType.Float } },
                { StringType, new[] { SerializedPropertyType.String } },
                { ColorType, new[] { SerializedPropertyType.Color } },
                { LegacyColorType, new[] { SerializedPropertyType.Color } },
                { "Vector2", new[] { SerializedPropertyType.Vector2 } },
                { "Vector3", new[] { SerializedPropertyType.Vector3 } },
                { "Vector4", new[] { SerializedPropertyType.Vector4 } },
                { "Quaternion", new[] { SerializedPropertyType.Quaternion } },
                { EnumValueType, new[] { SerializedPropertyType.Enum, SerializedPropertyType.Integer } },
                { LegacyEnumType, new[] { SerializedPropertyType.Enum } },
                { "Vector2Int", new[] { SerializedPropertyType.Vector2Int } },
                { "Vector3Int", new[] { SerializedPropertyType.Vector3Int } },
                { "Rect", new[] { SerializedPropertyType.Rect } },
                { "RectInt", new[] { SerializedPropertyType.RectInt } },
                { "Bounds", new[] { SerializedPropertyType.Bounds } },
                { "BoundsInt", new[] { SerializedPropertyType.BoundsInt } },
                { "Gradient", new[] { SerializedPropertyType.Gradient } },
                { "AnimationCurve", new[] { SerializedPropertyType.AnimationCurve } },
                { ObjectReferenceType, new[] { SerializedPropertyType.ObjectReference } }
            };

        private static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, object>>
            GetValueStrategies =
                new()
                {
                    { SerializedPropertyType.Integer, GetIntegerValue },
                    { SerializedPropertyType.Boolean, p => p.boolValue },
                    { SerializedPropertyType.Float, p => p.floatValue },
                    { SerializedPropertyType.String, p => p.stringValue },
                    { SerializedPropertyType.Color, p => p.colorValue },
                    { SerializedPropertyType.Vector2, p => p.vector2Value },
                    { SerializedPropertyType.Vector3, p => p.vector3Value },
                    { SerializedPropertyType.Vector4, p => p.vector4Value },
                    { SerializedPropertyType.Quaternion, p => p.quaternionValue },
                    // The underlying value, not enumValueIndex: the index is -1 for [Flags] combinations.
                    { SerializedPropertyType.Enum, p => p.intValue },
                    { SerializedPropertyType.Vector2Int, p => p.vector2IntValue },
                    { SerializedPropertyType.Vector3Int, p => p.vector3IntValue },
                    { SerializedPropertyType.Rect, p => p.rectValue },
                    { SerializedPropertyType.RectInt, p => p.rectIntValue },
                    { SerializedPropertyType.Bounds, p => p.boundsValue },
                    { SerializedPropertyType.BoundsInt, p => p.boundsIntValue },
                    { SerializedPropertyType.LayerMask, p => p.intValue },
                    { SerializedPropertyType.Gradient, p => p.gradientValue },
                    { SerializedPropertyType.AnimationCurve, p => p.animationCurveValue },
                    { SerializedPropertyType.ObjectReference, p => p.objectReferenceValue }
                };

        public static void SerializeProperty(SerializedProperty prop, out string typeName, out string serializedValue)
        {
            typeName = "";
            serializedValue = "";

            if (SerializationStrategies.TryGetValue(prop.propertyType, out var strategy))
            {
                (typeName, serializedValue) = strategy(prop);
            }
        }

        public static void ApplyPropertyValue(SerializedProperty prop, string typeName, string value)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return;
            }

            if (!DeserializationStrategies.TryGetValue(typeName, out var strategy))
            {
                return;
            }

            if (!IsCompatible(typeName, prop.propertyType))
            {
                Debug.LogWarning(
                    $"[Play Mode Changes Saver] Skipped '{prop.propertyPath}' on '{DescribeTarget(prop)}': the stored " +
                    $"value is {typeName}, but the property is now {prop.propertyType}. The property is left unchanged.");
                return;
            }

            strategy(prop, value ?? string.Empty);
        }

        private static bool IsCompatible(string typeName, SerializedPropertyType propertyType)
        {
            return CompatiblePropertyTypes.TryGetValue(typeName, out var types) &&
                   Array.IndexOf(types, propertyType) >= 0;
        }

        /// <summary>
        ///     An empty value is a reference cleared on purpose and is applied as None. A material that cannot be
        ///     resolved (a runtime instance from renderer.material, or a deleted asset) leaves the slot unchanged,
        ///     otherwise the renderer would lose its material.
        /// </summary>
        private static void ApplyObjectReference(SerializedProperty p, string v)
        {
            var obj = DeserializeObjectReference(v);
            if (obj == null && !string.IsNullOrEmpty(v) && IsMaterialReference(p))
            {
                var slot = MaterialSlotIndex(p.propertyPath);
                var what = slot >= 0 ? $"Material slot {slot}" : $"Material '{p.propertyPath}'";
                Debug.LogWarning(
                    $"[Play Mode Changes Saver] {what} of '{DescribeTarget(p)}' could not be resolved, probably a " +
                    "material instance created at runtime that is not an asset. The slot is left unchanged.");
                return;
            }

            p.objectReferenceValue = obj;
        }

        private static bool IsMaterialReference(SerializedProperty p)
        {
            // Built-in fields report "PPtr<Material>", script fields "PPtr<$Material>".
            return p.type == "PPtr<Material>" || p.type == "PPtr<$Material>" || p.objectReferenceValue is Material;
        }

        private static int MaterialSlotIndex(string propertyPath)
        {
            const string prefix = "m_Materials.Array.data[";
            if (!propertyPath.StartsWith(prefix, StringComparison.Ordinal) || !propertyPath.EndsWith("]"))
            {
                return -1;
            }

            var digits = propertyPath.Substring(prefix.Length, propertyPath.Length - prefix.Length - 1);
            return int.TryParse(digits, NumberStyles.Integer, Invariant, out var index) ? index : -1;
        }

        private static string DescribeTarget(SerializedProperty p)
        {
            var target = p.serializedObject.targetObject;
            if (target is Component component)
            {
                return SceneAndPathUtilities.GetGameObjectPath(component.transform);
            }

            return target ? target.name : "(missing object)";
        }

        /// <summary>
        ///     Applies parallel path/type/value lists to <paramref name="so" />. Tolerates lists of different
        ///     length (damaged or hand-edited store data) and values that cannot be applied.
        /// </summary>
        public static void ApplyStoredValues(SerializedObject so, List<string> propertyPaths, List<string> valueTypes,
            List<string> serializedValues)
        {
            if (so == null || propertyPaths == null)
            {
                return;
            }

            for (var i = 0; i < propertyPaths.Count; i++)
            {
                var prop = so.FindProperty(propertyPaths[i]);
                if (prop == null)
                {
                    continue;
                }

                var typeName = GetAt(valueTypes, i);
                var value = GetAt(serializedValues, i);
                try
                {
                    ApplyPropertyValue(prop, typeName, value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[Play Mode Changes Saver] Could not apply '{propertyPaths[i]}' ({typeName}): {e.Message}");
                }
            }
        }

        public static object GetPropertyValue(SerializedProperty prop)
        {
            return GetValueStrategies.TryGetValue(prop.propertyType, out var strategy) ? strategy(prop) : null;
        }

        /// <summary>
        ///     Counterpart of <see cref="GetPropertyValue" />: writes a boxed snapshot value back.
        ///     Returns false if the value could not be applied to this property type.
        /// </summary>
        public static bool SetPropertyValue(SerializedProperty prop, object value)
        {
            if (prop == null)
            {
                return false;
            }

            if (prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = value as Object;
                return true;
            }

            if (value == null)
            {
                return false;
            }

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    SetIntegerValue(prop, value);
                    return true;
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.LayerMask:
                    prop.intValue = Convert.ToInt32(value, Invariant);
                    return true;
                case SerializedPropertyType.ArraySize:
                    prop.intValue = Convert.ToInt32(value, Invariant);
                    return true;
                case SerializedPropertyType.Boolean when value is bool b:
                    prop.boolValue = b;
                    return true;
                case SerializedPropertyType.Float:
                    prop.floatValue = Convert.ToSingle(value, Invariant);
                    return true;
                case SerializedPropertyType.String when value is string s:
                    prop.stringValue = s;
                    return true;
                case SerializedPropertyType.Color when value is Color c:
                    prop.colorValue = c;
                    return true;
                case SerializedPropertyType.Vector2 when value is Vector2 v2:
                    prop.vector2Value = v2;
                    return true;
                case SerializedPropertyType.Vector3 when value is Vector3 v3:
                    prop.vector3Value = v3;
                    return true;
                case SerializedPropertyType.Vector4 when value is Vector4 v4:
                    prop.vector4Value = v4;
                    return true;
                case SerializedPropertyType.Quaternion when value is Quaternion q:
                    prop.quaternionValue = q;
                    return true;
                case SerializedPropertyType.Vector2Int when value is Vector2Int v2I:
                    prop.vector2IntValue = v2I;
                    return true;
                case SerializedPropertyType.Vector3Int when value is Vector3Int v3I:
                    prop.vector3IntValue = v3I;
                    return true;
                case SerializedPropertyType.Rect when value is Rect r:
                    prop.rectValue = r;
                    return true;
                case SerializedPropertyType.RectInt when value is RectInt rI:
                    prop.rectIntValue = rI;
                    return true;
                case SerializedPropertyType.Bounds when value is Bounds bo:
                    prop.boundsValue = bo;
                    return true;
                case SerializedPropertyType.BoundsInt when value is BoundsInt boI:
                    prop.boundsIntValue = boI;
                    return true;
                case SerializedPropertyType.Gradient when value is Gradient g:
                    prop.gradientValue = g;
                    return true;
                case SerializedPropertyType.AnimationCurve when value is AnimationCurve curve:
                    prop.animationCurveValue = curve;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     True if the live property value differs from a stored (typeName, value) pair.
        ///     Floating point types are compared with a small tolerance; unknown or empty types never differ.
        /// </summary>
        public static bool ValueDiffers(SerializedProperty liveProp, string storedType, string storedValue)
        {
            if (liveProp == null || string.IsNullOrEmpty(storedType))
            {
                return false;
            }

            storedValue ??= string.Empty;

            switch (storedType)
            {
                case IntegerType:
                    return TryParseInteger(liveProp, storedValue, out var stored) &&
                           !Equals(GetIntegerValue(liveProp), stored);
                case BooleanType:
                    return bool.TryParse(storedValue, out var boolVal) && liveProp.boolValue != boolVal;
                case FloatType:
                    return float.TryParse(storedValue, NumberStyles.Float, Invariant, out var floatVal) &&
                           Mathf.Abs(liveProp.floatValue - floatVal) > Tolerance;
                case StringType:
                    return liveProp.stringValue != storedValue;
                case ColorType:
                    return SnapshotSerializer.TryDeserializeColor(storedValue, out var col) &&
                           !ApproximatelyEqual(liveProp.colorValue, col);
                case LegacyColorType:
                    // The legacy format is quantized to 8 bit, so compare at that precision.
                    return !string.Equals("#" + ColorUtility.ToHtmlStringRGBA(liveProp.colorValue), storedValue,
                        StringComparison.OrdinalIgnoreCase);
                case "Vector2":
                    return Vector2.Distance(liveProp.vector2Value, SnapshotSerializer.DeserializeVector2(storedValue)) >
                           Tolerance;
                case "Vector3":
                    return Vector3.Distance(liveProp.vector3Value, SnapshotSerializer.DeserializeVector3(storedValue)) >
                           Tolerance;
                case "Vector4":
                    return Vector4.Distance(liveProp.vector4Value, SnapshotSerializer.DeserializeVector4(storedValue)) >
                           Tolerance;
                case "Quaternion":
                    return Quaternion.Angle(liveProp.quaternionValue,
                        SnapshotSerializer.DeserializeQuaternion(storedValue)) > Tolerance;
                case EnumValueType:
                    return int.TryParse(storedValue, NumberStyles.Integer, Invariant, out var enumVal) &&
                           liveProp.intValue != enumVal;
                case LegacyEnumType:
                    return int.TryParse(storedValue, NumberStyles.Integer, Invariant, out var enumIndex) &&
                           liveProp.enumValueIndex != enumIndex;
                case ObjectReferenceType:
                    SerializeProperty(liveProp, out _, out var liveReference);
                    return !SameObjectReference(liveReference, storedValue);
                default:
                    SerializeProperty(liveProp, out _, out var currentSerialized);
                    return currentSerialized != storedValue;
            }
        }

        /// <summary>
        ///     Compares two stored references. The same object of a prefab instance is stored with another id in
        ///     Play Mode than in Edit Mode, so usable ids are compared in their scene-local form.
        /// </summary>
        private static bool SameObjectReference(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.Ordinal))
            {
                return true;
            }

            var idA = (a ?? string.Empty).Split('|')[0];
            var idB = (b ?? string.Empty).Split('|')[0];
            return SceneAndPathUtilities.IsUsableGlobalObjectId(idA) &&
                   SceneAndPathUtilities.IsUsableGlobalObjectId(idB) &&
                   string.Equals(SceneAndPathUtilities.CanonicalGlobalObjectId(idA),
                       SceneAndPathUtilities.CanonicalGlobalObjectId(idB), StringComparison.Ordinal);
        }

        public static bool IsTypeSupported(SerializedPropertyType type)
        {
            return SerializationStrategies.ContainsKey(type);
        }

        /// <summary>
        ///     Stores a reference as "GlobalObjectId|assetPath"; the asset path is a fallback for assets.
        /// </summary>
        internal static string SerializeObjectReference(Object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            var globalId = GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
            var assetPath = AssetDatabase.GetAssetPath(obj);

            return $"{globalId}|{assetPath}";
        }

        internal static Object DeserializeObjectReference(string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                return null;
            }

            var parts = v.Split('|');
            var globalIdInfo = parts.Length > 0 ? parts[0] : "";
            var assetPath = parts.Length > 1 ? parts[1] : "";

            if (!string.IsNullOrEmpty(globalIdInfo) && GlobalObjectId.TryParse(globalIdInfo, out var globalId))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                if (obj != null)
                {
                    return obj;
                }

                // Objects of a prefab instance have other ids in Play Mode than in Edit Mode.
                obj = SceneAndPathUtilities.FindSceneObjectByCanonicalId(globalIdInfo);
                if (obj != null)
                {
                    return obj;
                }
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (obj != null)
                {
                    return obj;
                }
            }

            return null;
        }

        private static string GetAt(List<string> list, int index)
        {
            return list != null && index < list.Count ? list[index] : string.Empty;
        }

        /// <summary>
        ///     True if <paramref name="v" /> holds exactly <paramref name="count" /> comma separated numbers.
        ///     Struct values are only written when this holds; the Deserialize methods return zero for damaged
        ///     values, which would otherwise reset the property.
        /// </summary>
        private static bool HasComponents(string v, int count, bool integers)
        {
            var parts = (v ?? string.Empty).Split(',');
            if (parts.Length != count)
            {
                return false;
            }

            foreach (var part in parts)
            {
                var parsed = integers
                    ? int.TryParse(part, NumberStyles.Integer, Invariant, out _)
                    : float.TryParse(part, NumberStyles.Float, Invariant, out _);
                if (!parsed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     True if <paramref name="v" /> has the bounds format "x,y,z|x,y,z" (center or position, then size).
        /// </summary>
        private static bool HasBoundsComponents(string v, bool integers)
        {
            var halves = (v ?? string.Empty).Split('|');
            return halves.Length == 2 && HasComponents(halves[0], 3, integers) && HasComponents(halves[1], 3, integers);
        }

        private static bool ApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= Tolerance && Mathf.Abs(a.g - b.g) <= Tolerance &&
                   Mathf.Abs(a.b - b.b) <= Tolerance && Mathf.Abs(a.a - b.a) <= Tolerance;
        }

        // Integer handling: 64-bit and unsigned 32-bit fields do not fit into intValue.

        private static bool IsUnsigned64(SerializedProperty p)
        {
            return p.propertyType == SerializedPropertyType.Integer &&
                   p.numericType == SerializedPropertyNumericType.UInt64;
        }

        private static bool IsWide(SerializedProperty p)
        {
            return p.propertyType == SerializedPropertyType.Integer &&
                   p.numericType is SerializedPropertyNumericType.Int64 or SerializedPropertyNumericType.UInt32;
        }

        private static object GetIntegerValue(SerializedProperty p)
        {
            if (IsUnsigned64(p))
            {
                return p.ulongValue;
            }

            // Separate returns: a conditional expression would box every value as long, and a boxed
            // long never equals the boxed int parsed from the store.
            if (IsWide(p))
            {
                return p.longValue;
            }

            return p.intValue;
        }

        private static void SetIntegerValue(SerializedProperty p, object value)
        {
            if (IsUnsigned64(p))
            {
                p.ulongValue = Convert.ToUInt64(value, Invariant);
            }
            else if (IsWide(p))
            {
                p.longValue = Convert.ToInt64(value, Invariant);
            }
            else
            {
                p.intValue = Convert.ToInt32(value, Invariant);
            }
        }

        private static string SerializeInteger(SerializedProperty p)
        {
            return Convert.ToString(GetIntegerValue(p), Invariant);
        }

        private static bool TryParseInteger(SerializedProperty p, string v, out object value)
        {
            value = null;
            if (IsUnsigned64(p))
            {
                if (!ulong.TryParse(v, NumberStyles.Integer, Invariant, out var u))
                {
                    return false;
                }

                value = u;
                return true;
            }

            if (!long.TryParse(v, NumberStyles.Integer, Invariant, out var l))
            {
                return false;
            }

            if (IsWide(p))
            {
                value = l;
                return true;
            }

            if (l < int.MinValue || l > int.MaxValue)
            {
                return false;
            }

            value = (int)l;
            return true;
        }

        private static void ApplyInteger(SerializedProperty p, string v)
        {
            if (TryParseInteger(p, v, out var value))
            {
                SetIntegerValue(p, value);
            }
        }
    }
}
