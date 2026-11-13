using PlayModeChangesSaver.Editor.ChangesTracker.Serialization;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Popup-facing facade over <see cref="ComponentPropertySerializer" />, so the compare popup,
    ///     the store applicators and the recorders share one codec and support the same value types.
    /// </summary>
    internal static class OcpSerialization
    {
        /// <summary>
        ///     Sets a property from a boxed snapshot value.
        /// </summary>
        public static void SetPropertyValue(SerializedProperty prop, object value)
        {
            ComponentPropertySerializer.SetPropertyValue(prop, value);
        }

        /// <summary>
        ///     Applies a serialized component value from string representation.
        /// </summary>
        public static void ApplySerializedComponentValue(SerializedProperty prop, string typeName, string value)
        {
            ComponentPropertySerializer.ApplyPropertyValue(prop, typeName, value);
        }

        public static Vector2 DeserializeVector2(string s)
        {
            return SnapshotSerializer.DeserializeVector2(s);
        }

        public static Vector3 DeserializeVector3(string s)
        {
            return SnapshotSerializer.DeserializeVector3(s);
        }

        public static Vector4 DeserializeVector4(string s)
        {
            return SnapshotSerializer.DeserializeVector4(s);
        }

        public static Quaternion DeserializeQuaternion(string s)
        {
            return SnapshotSerializer.DeserializeQuaternion(s);
        }

        /// <summary>
        ///     Serializes a property to string representation.
        /// </summary>
        public static string SerializeProperty(SerializedProperty prop)
        {
            ComponentPropertySerializer.SerializeProperty(prop, out _, out var value);
            return value;
        }

        /// <summary>
        ///     Checks if two serialized properties differ in value.
        /// </summary>
        public static bool PropertiesDiffer(SerializedProperty a, SerializedProperty b)
        {
            if (a == null || b == null || a.propertyType != b.propertyType)
            {
                return a?.propertyType != b?.propertyType;
            }

            ComponentPropertySerializer.SerializeProperty(b, out var typeName, out var value);
            return ComponentPropertySerializer.ValueDiffers(a, typeName, value);
        }
    }
}
