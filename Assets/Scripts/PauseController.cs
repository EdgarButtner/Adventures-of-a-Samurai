using UnityEngine;

public class PauseController : MonoBehaviour
{
    public static PauseController instance { get; private set; }

    public bool gamePaused = false;
    
    // Singleton Pattern 
    private void Awake()
    {
        if (instance != null && instance != this) 
        {
            Destroy(this);
        }
        else 
        {
            instance = this;
        }
    }

    // Pause the game
    public void PauseGame() 
    {
        // If the game is already paused dont do anything
        if(gamePaused)
            return;
        
        // Otherwise pause the game 
        else 
        {
            Time.timeScale = 0f;
            gamePaused = true;
        }
    }

    // Unpause the game 
    public void UnpauseGame()
    {
        // If the game is already unpaused dont do anything
        if(!gamePaused)
            return;

        // Otherwise unpause the game 
        else 
        {
            Time.timeScale = 1f;
            gamePaused = false;
        }
    }
}
