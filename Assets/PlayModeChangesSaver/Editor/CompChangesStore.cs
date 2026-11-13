using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class CompChangesStore : ScriptableObject
    {
        public List<ComponentChange> changes = new();

        private static CompChangesStore _cached;

        public static CompChangesStore LoadExisting()
        {
            return PlayModeChangesSaverPaths.FindStoreAsset(ref _cached);
        }

        public static CompChangesStore LoadOrCreate()
        {
            var store = LoadExisting();
            return store ? store : PlayModeChangesSaverPaths.CreateStoreAsset(ref _cached, "Comp_Changes_Store.asset");
        }

        public void Clear()
        {
            changes.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class ComponentChange
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string componentType;
            public int componentIndex;

            public List<string> propertyPaths = new();
            public List<string> serializedValues = new();
            public List<string> valueTypes = new();

            public bool includeMaterialChanges;
            public List<string> materialGuids = new();
        }
    }
}