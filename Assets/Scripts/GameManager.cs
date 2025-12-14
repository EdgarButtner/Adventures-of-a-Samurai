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
                bestScoreText.text = "Best Score: " + currMaxScore.ToString();
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
        SceneManager.LoadScene(name);
    }

    public void LoadByIndex(int index) 
    {
        SceneManager.LoadScene(index);
    }

    public void ReloadScene() 
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void OnPlayerDeath() 
    {
        SetDeathScreen(true);
    }

    public void SetDeathScreen(bool toSet) 
    {
        deathScreen.SetActive(toSet);
    }

    public void OnLevelComplete() 
    {
        isLevelCompleted = true;
        levelCompletedScreen.SetActive(true);

        pauseController.PauseGame();
    }   
}
