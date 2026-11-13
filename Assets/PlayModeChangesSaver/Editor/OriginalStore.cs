using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class OriginalStore : ScriptableObject
    {
        public List<TransformOriginal> entries = new();

        private static OriginalStore _cached;

        public static OriginalStore LoadExisting()
        {
            return PlayModeChangesSaverPaths.FindStoreAsset(ref _cached);
        }

        public static OriginalStore LoadOrCreate()
        {
            var store = LoadExisting();
            return store ? store : PlayModeChangesSaverPaths.CreateStoreAsset(ref _cached, "T_Original_Store.asset");
        }

        public void Clear()
        {
            entries.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class TransformOriginal
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public bool isRectTransform;

            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public Vector2 anchoredPosition;
            public Vector3 anchoredPosition3D;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 sizeDelta;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
        }
    }
}