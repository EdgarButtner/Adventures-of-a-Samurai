using System.Collections.Generic;

// Everything written to disk
[System.Serializable]
public class SaveData
{
    // Bump when fields change
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public PlayerSaveData player = new PlayerSaveData();
    public List<LevelRecord> levels = new List<LevelRecord>();
    public string lastLevel = "";

    // Null if never played
    public LevelRecord GetLevel(string sceneName)
    {
        return levels.Find(level => level.sceneName == sceneName);
    }

    public LevelRecord GetOrAddLevel(string sceneName)
    {
        LevelRecord record = GetLevel(sceneName);
        if (record == null)
        {
            record = new LevelRecord { sceneName = sceneName };
            levels.Add(record);
        }

        return record;
    }
}

// Carries between levels
[System.Serializable]
public class PlayerSaveData
{
    public int maxHealth = 100;

    // Not gated yet
    public List<string> unlockedAbilities = new List<string> { "Dash", "Glide" };

    public bool HasAbility(string id)
    {
        return unlockedAbilities.Contains(id);
    }

    public void UnlockAbility(string id)
    {
        if (!HasAbility(id))
            unlockedAbilities.Add(id);
    }
}

// One per level scene
[System.Serializable]
public class LevelRecord
{
    public string sceneName;
    public bool completed;
    public float bestScore;
}
