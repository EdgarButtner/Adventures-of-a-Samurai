using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persists across scenes, owns the save
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    // Lets systems add data before writing
    public static event Action<SaveData> Saving;

    public int activeSlot = 0;
    public bool autoSave = true;

    // Not counted as lastLevel
    public List<string> nonLevelScenes = new List<string> {"TitleScene", "Levels" };

    public SaveData data;

    // Below 0 means full health
    private int pendingStartHealth = -1;

    // Exists before any scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("GameSession").AddComponent<GameSession>();
    }

    void Awake()
    {
        // Keep first copy only
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        data = SaveSystem.Load(activeSlot);
    }

    void OnApplicationQuit()
    {
        if (autoSave)
            Save();
    }

    // Scenes

    // Starts at full health
    public void LoadLevel(string sceneName)
    {
        pendingStartHealth = -1;
        LoadScene(sceneName);
    }

    // Starts at given health
    public void LoadLevel(string sceneName, int startHealth)
    {
        pendingStartHealth = startHealth;
        LoadScene(sceneName);
    }

    private void LoadScene(string sceneName)
    {
        // Unpause before leaving
        if (PauseController.instance != null)
            PauseController.instance.UnpauseGame();
        Time.timeScale = 1f;

        if (!nonLevelScenes.Contains(sceneName))
            data.lastLevel = sceneName;

        SceneManager.LoadScene(sceneName);
    }

    // Health

    // Used once, then resets
    public int TakeStartHealth(int maxHealth)
    {
        int health = pendingStartHealth > 0 ? Mathf.Min(pendingStartHealth, maxHealth) : maxHealth;
        pendingStartHealth = -1;
        return health;
    }

    // Progress

    public void CompleteLevel(string sceneName, float score)
    {
        LevelRecord record = data.GetOrAddLevel(sceneName);
        record.completed = true;
        record.bestScore = Mathf.Max(record.bestScore, score);
        data.lastLevel = sceneName;

        if (autoSave)
            Save();
    }

    public float GetBestScore(string sceneName)
    {
        LevelRecord record = data.GetLevel(sceneName);
        return record != null ? record.bestScore : 0f;
    }

    // Saving

    public void Save()
    {
        Saving?.Invoke(data);
        SaveSystem.Save(data, activeSlot);
    }

    // Wipes the active slot
    public void NewGame()
    {
        data = new SaveData();
        Save();
    }
}
