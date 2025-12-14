using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

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
}
