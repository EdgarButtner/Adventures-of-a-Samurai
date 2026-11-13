using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class CompOriginalStore : ScriptableObject
    {
        public List<ComponentOriginal> entries = new();

        private static CompOriginalStore _cached;

        public static CompOriginalStore LoadExisting()
        {
            return PlayModeChangesSaverPaths.FindStoreAsset(ref _cached);
        }

        public static CompOriginalStore LoadOrCreate()
        {
            var store = LoadExisting();
            return store ? store : PlayModeChangesSaverPaths.CreateStoreAsset(ref _cached, "Comp_OriginalStore.asset");
        }

        public void Clear()
        {
            entries.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class ComponentOriginal
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string componentType;
            public int componentIndex;

            public List<string> propertyPaths = new();
            public List<string> serializedValues = new();
            public List<string> valueTypes = new();
            public List<string> materialGuids = new();
        }
    }
}