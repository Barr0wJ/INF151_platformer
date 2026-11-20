using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneInputs : MonoBehaviour
{
    [Header ("Text & Image")]
    [SerializeField] public DialogueHandler DialogueHandler;
    [SerializeField] private string[] dialogueList;
    [SerializeField] private Sprite[] imageList;
    [SerializeField] private TMP_Text text;
    [SerializeField] private SpriteRenderer sprite;

    [Header ("Game Management")]
    [SerializeField] public gameManager gameManager;
    [SerializeField] private string nextLevelName;

    private int counter;
    private string currentDialogue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TMP_Text>();
        counter = 0;
        currentDialogue = dialogueList[counter];
        sprite.sprite = imageList[counter];
        DialogueHandler.WriteText(currentDialogue);
        Debug.Log("Current dialogue " + counter);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame && counter == (dialogueList.Length - 1))
        {
            gameManager.instance.changeLevelTo(nextLevelName);
        }
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            text.text = "";
            counter++;
            currentDialogue = dialogueList[counter];
            sprite.sprite = imageList[counter];
            DialogueHandler.WriteText(currentDialogue);
        }
        
    }
}
