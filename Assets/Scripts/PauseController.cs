using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    public static PauseController instance { get; private set; }
    
    public GameObject pauseMenu;

    public bool gamePaused = false;

    // Found automatically if empty
    public PlayerMovement playerMovement;
    public GameManager gameManager;

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

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            LocationInfoPanel.HideAllUnder(pauseMenu);
        }
    }

    // Esc toggles pause
    public void TogglePause(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        // Ignore on level complete
        if (gameManager != null && gameManager.isLevelCompleted)
            return;

        if (gamePaused)
            UnpauseGame();
        else
            OpenPauseMenu();
    }

    // Pause with menu
    public void OpenPauseMenu()
    {
        // Stop dash charge firing
        if (playerMovement != null && playerMovement.activeAbility != null)
            playerMovement.activeAbility.Cancel();

        PauseGame();

        if (pauseMenu != null)
        {
            // Panels start closed
            LocationInfoPanel.HideAllUnder(pauseMenu);
            pauseMenu.SetActive(true);
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

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
            LocationInfoPanel.HideAllUnder(pauseMenu);
        }

        // Otherwise unpause the game
        Time.timeScale = 1f;
        gamePaused = false;
    }
}
