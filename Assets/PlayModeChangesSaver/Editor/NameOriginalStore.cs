using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor
{
    public class NameOriginalStore : ScriptableObject
    {
        public List<NameOriginal> entries = new();

        private static NameOriginalStore _cached;

        public static NameOriginalStore LoadExisting()
        {
            return PlayModeChangesSaverPaths.FindStoreAsset(ref _cached);
        }

        public static NameOriginalStore LoadOrCreate()
        {
            var store = LoadExisting();
            return store ? store : PlayModeChangesSaverPaths.CreateStoreAsset(ref _cached, "GO_Name_Original_Store.asset");
        }

        public void Clear()
        {
            entries.Clear();
            EditorUtility.SetDirty(this);
        }

        [Serializable]
        public class NameOriginal
        {
            public string scenePath;
            public string objectPath;
            public string globalObjectId;
            public string originalName;
            public int siblingIndex = -1;
        }
    }
}