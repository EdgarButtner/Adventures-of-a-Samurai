using UnityEngine;
using TMPro;

public class NPCController : MonoBehaviour
{
    public TMP_Text canInteractText;
    public bool dialogueStarted = false;
    public string[] dialogueLines;
    private bool playerInRange = false;

    void Start() 
    {
        if (canInteractText != null)
            canInteractText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerInRange)
        {
            if (!dialogueStarted && canInteractText != null)
            {
                canInteractText.gameObject.SetActive(true);
            }
            else if (dialogueStarted && canInteractText != null)
            {
                canInteractText.gameObject.SetActive(false);
            }

            if (!dialogueStarted && Input.GetKeyDown(KeyCode.E))
            {
                DialogueController dialogue = FindObjectOfType<DialogueController>();
                if (dialogue != null)
                {
                    dialogueStarted = true;
                    canInteractText.gameObject.SetActive(false);
                    dialogue.StartDialogue(dialogueLines);
                }
                else
                {
                    Debug.LogWarning("DialogueController not found in the scene.");
                }
            }
        }
        else
        {
            if (canInteractText != null)
                canInteractText.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) 
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            dialogueStarted = false;

            if (canInteractText != null)
                canInteractText.gameObject.SetActive(false);
        }
    }
}
