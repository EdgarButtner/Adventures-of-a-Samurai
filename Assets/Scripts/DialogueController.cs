using UnityEngine;
using TMPro;
using System.Collections;


public class DialogueController : MonoBehaviour
{
    public GameObject dialogueBox;
    public bool dialogueBoxEnabled = false;
    public TMP_Text textComponent;
    public string[] dialogue;
    public float textSpeed = 0.2f;
    private int index;
    
    public GameObject enemies;

    public NPCController npccontroller;

    void Start()
    {
        dialogueBox.SetActive(false);
        npccontroller = FindObjectOfType<NPCController>(); 
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            Debug.Log("pressed");
            if (textComponent.text == dialogue[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = dialogue[index];
            }
        }
    }

    public void StartDialogue(string[] newDialogue)
    {
        dialogue = newDialogue;
        textComponent.text = string.Empty;
        index = 0;
        dialogueBox.SetActive(true);
        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (index < dialogue.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            // This line ensures the last line is fully shown before closing
            StopAllCoroutines();
            textComponent.text = dialogue[index]; 
            if(npccontroller) 
            {
                npccontroller.dialogueStarted = false;
            }
            dialogueBox.SetActive(false);

            enemies.SetActive(true);
        }
    }

    IEnumerator TypeLine()
    {
        foreach (char c in dialogue[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }
}
