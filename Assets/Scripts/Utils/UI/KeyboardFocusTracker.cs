using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardFocusTracker : MonoBehaviour
{
    HasKeyboardFocus currentlyFocused;
    
    void Update()
    {
        if (currentlyFocused == null && Selectable.allSelectables.Count > 0)
            currentlyFocused = Selectable.allSelectables[0].gameObject.AddComponent<HasKeyboardFocus>();
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (currentlyFocused == null)
                return;
            var previouslyFocused = currentlyFocused;
            for (var i = 0; i < Selectable.allSelectables.Count; i++)
            {
                if (Selectable.allSelectables[i].gameObject == currentlyFocused.gameObject)
                {
                    Selectable nextSelectable = null;
                    if (Selectable.allSelectables.Count > i + 1)
                        nextSelectable = Selectable.allSelectables[i + 1];
                    else
                        nextSelectable = Selectable.allSelectables[0];
                    currentlyFocused = nextSelectable.gameObject.AddComponent<HasKeyboardFocus>();
                    break;
                }
            }
            UnityEngine.Object.Destroy(previouslyFocused);
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (currentlyFocused == null)
                return;
            foreach (var selectable in Selectable.allSelectables)
            {
                if (selectable.gameObject == currentlyFocused.gameObject && selectable.IsInteractable())
                {
                    if (selectable is Button)
                        ((Button)selectable).onClick.Invoke();
                    break;
                }
            }
        }
    }
}