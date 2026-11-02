using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueHandler : MonoBehaviour
{
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float typingSpeed = 0.04f;

    private Coroutine typingCoroutine;
    
    public void WriteText(string message, float endlag = 3)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        Debug.Log(message);

        gameObject.SetActive(true);
        typingCoroutine = StartCoroutine(Type(message, endlag));
    }

    private IEnumerator Type(string message, float endlag)
    {
        dialogueText.text = message;
        dialogueText.maxVisibleCharacters = 0;

        dialogueText.ForceMeshUpdate();

        int totalVisibleCharacters = dialogueText.textInfo.characterCount;

        for (int i = 0; i <= totalVisibleCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        yield return new WaitForSecondsRealtime(endlag);

        gameObject.SetActive(false);
        typingCoroutine = null;
    }
}