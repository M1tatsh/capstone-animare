using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialScript : MonoBehaviour
{
    private List<GameObject> dialogues = new List<GameObject>();
    private int currentDialogueIndex = 0;

    void Start()
    {
        AddChildrenWithTag(transform, "Dialogue", dialogues);
    }


    void Update()
    {

    }

    public void AdvanceDialogue()
    {
        dialogues[currentDialogueIndex].SetActive(false);
        currentDialogueIndex++;

        if (currentDialogueIndex >= dialogues.Count)
        {
            gameObject.SetActive(false);
            GameController.ResetGameSpeed();
        }
        else
            dialogues[currentDialogueIndex].SetActive(true);
    }

    private void AddChildrenWithTag(Transform parent, string tag, List<GameObject> list)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.tag == tag)
            {
                list.Add(child.gameObject);
            }
            AddChildrenWithTag(child, tag, list);
        }
    }
}
