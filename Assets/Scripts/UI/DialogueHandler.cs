using System.Collections;
using UnityEngine;
using TMPro;
using System.Linq;

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
        int totalVisibleCharacters = message.Count();

        if (typingSpeed > 0)
        {
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            for (int i = 0; i <= totalVisibleCharacters; i++)
            {
                dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
        }
        
        dialogueText.maxVisibleCharacters = totalVisibleCharacters;
        dialogueText.ForceMeshUpdate();

        yield return new WaitForSecondsRealtime(endlag);

        gameObject.SetActive(false);
        typingCoroutine = null;
    }
}