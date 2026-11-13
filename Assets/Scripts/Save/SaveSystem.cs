using System.IO;
using UnityEngine;

// Reads and writes save files
public static class SaveSystem
{
    public static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "save_" + slot + ".json");
    }

    public static bool Exists(int slot = 0)
    {
        return File.Exists(GetPath(slot));
    }

    public static void Save(SaveData data, int slot = 0)
    {
        string path = GetPath(slot);
        string tempPath = path + ".tmp";

        try
        {
            // Temp first so a crash cant corrupt
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

            if (File.Exists(path))
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);

            Debug.Log("Saved to " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Save failed " + e.Message);
        }
    }

    // Fresh save if missing or broken
    public static SaveData Load(int slot = 0)
    {
        string path = GetPath(slot);
        if (!File.Exists(path))
            return new SaveData();

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data == null)
                throw new System.Exception("Empty file");

            Migrate(data);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Save unreadable, starting fresh " + e.Message);
            return new SaveData();
        }
    }

    public static void Delete(int slot = 0)
    {
        string path = GetPath(slot);
        if (File.Exists(path))
            File.Delete(path);
    }

    // Upgrade old saves here
    private static void Migrate(SaveData data)
    {
        // Missing in old files
        if (data.player == null)
            data.player = new PlayerSaveData();
        if (data.levels == null)
            data.levels = new System.Collections.Generic.List<LevelRecord>();

        // Future version steps go here
        data.version = SaveData.CurrentVersion;
    }
}
