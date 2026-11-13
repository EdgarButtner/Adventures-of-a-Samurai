using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Score Settings")]
    public TMP_Text scoreText;
    public TMP_Text bestScoreText;
    int currScore = 1;
    float currMult = 1;
    public float currMaxScore;
    public float savedBestScore;
    public float maxLevelScore = 1000;
    public bool isShowingFinalScore;
    public bool isLevelCompleted = false;

    [Header("Player Settings")]
    public GameObject levelCompletedScreen;
    public GameObject deathScreen;

    [Header("General Settings")]
    public PauseController pauseController;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pauseController = GameObject.FindObjectOfType<PauseController>();
        SetDeathScreen(false);
        SetLevelCompleteScreen(false);

        // Best from save
        savedBestScore = GameSession.Instance.GetBestScore(SceneManager.GetActiveScene().name);
        if (savedBestScore > 0f && bestScoreText != null)
            bestScoreText.text = "Best Score: " + savedBestScore.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(currScore);
        //Debug.Log(currMult);
    }

    // Score -------------------------------------------------


    public void AddScore(int score)
    {
        currScore += score;
        currScore = (int)currScore;
        if (!isShowingFinalScore)
            UpdateScoreText();
    }

    public void IncreaseMultiplier(float mult)
    {
        currMult += mult;
        currMult = (int)currMult;
        if (!isShowingFinalScore)
            UpdateScoreText();
    }

    public void UpdateScoreText()
    {
        scoreText.text = currScore + " x " + currMult;
    }

    public void DisplayAndResetScore()
    {
        float score = currScore * currMult;
        score = (int)score;
        if(score > currMaxScore) 
        {
            currMaxScore = score;
            if (score >= maxLevelScore) 
            {
                isLevelCompleted = true;
                bestScoreText.text = "Best Score: " + Mathf.Max(currMaxScore, savedBestScore).ToString();
                OnLevelComplete();
            }
        }
        isShowingFinalScore = true;
        scoreText.text = score.ToString();

        StartCoroutine(WaitForScoreAndReset());
    }

    IEnumerator WaitForScoreAndReset()
    {
        yield return new WaitForSeconds(1f);

        currScore = 1;
        currMult = 1f;
        isShowingFinalScore = false;

        UpdateScoreText();
    }

    // --------------------------------------------------------

    public void PlayGame() 
    {
        LoadByName("InitialScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LoadByName(string name)
    {
        GameSession.Instance.LoadLevel(name);
    }

    public void LoadByIndex(int index)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(index);
        GameSession.Instance.LoadLevel(System.IO.Path.GetFileNameWithoutExtension(path));
    }

    public void ReloadScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        GameSession.Instance.LoadLevel(currentSceneName);
    }

    // Next level at current health
    public void LoadNextLevelKeepHealth(string name)
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null && playerHealth.isAlive)
            GameSession.Instance.LoadLevel(name, playerHealth.CurrentHealth);
        else
            GameSession.Instance.LoadLevel(name);
    }

    public void OnPlayerDeath() 
    {
        SetDeathScreen(true);
    }

    public void SetDeathScreen(bool toSet) 
    {
        deathScreen.SetActive(toSet);
    }

    public void SetLevelCompleteScreen(bool toSet)
    {
        levelCompletedScreen.SetActive(toSet);
    }

    public void OnLevelComplete()
    {
        isLevelCompleted = true;
        SetLevelCompleteScreen(true);

        // Record and save
        GameSession.Instance.CompleteLevel(SceneManager.GetActiveScene().name, currMaxScore);

        pauseController.PauseGame();
    }   
}
