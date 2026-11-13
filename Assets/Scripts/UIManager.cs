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
        GameSession.Instance.LoadLevel(name);
    }

    public void LoadByIndex(int index)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(index);
        GameSession.Instance.LoadLevel(System.IO.Path.GetFileNameWithoutExtension(path));
    }
}
