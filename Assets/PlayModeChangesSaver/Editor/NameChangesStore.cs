using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class NameChangesStore : ScriptableObject
    {
        public List<NameChange> changes = new();

        private static NameChangesStore _cached;

        public static NameChangesStore LoadExisting()
        {
            return PlayModeChangesSaverPaths.FindStoreAsset(ref _cached);
        }

        public static NameChangesStore LoadOrCreate()
        {
            var store = LoadExisting();
            return store ? store : PlayModeChangesSaverPaths.CreateStoreAsset(ref _cached, "GO_Name_Changes_Store.asset");
        }

        public void Clear()
        {
            changes.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class NameChange
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string newName;
            public int siblingIndex = -1;
        }
    }
}