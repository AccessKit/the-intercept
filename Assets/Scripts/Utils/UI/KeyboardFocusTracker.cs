using AccessKit;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardFocusTracker : MonoBehaviour
{
    HasKeyboardFocus currentlyFocused;
    
    void Update()
    {
        if (currentlyFocused == null)
        {
            var i = nextFocusableIndex(0);
            if (i >= 0)
                currentlyFocused = AccessibleNode.allAccessibles.Values[i].gameObject.AddComponent<HasKeyboardFocus>();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (currentlyFocused == null)
                return;
            var node = currentlyFocused.GetComponent<AccessibleNode>();
            if (node == null)
            {
                UnityEngine.Object.Destroy(currentlyFocused);
                return;
            }
            var previouslyFocused = currentlyFocused;
            var index = AccessibleNode.allAccessibles.IndexOfValue(node);
            var nextIndex = nextFocusableIndex(index);
            if (nextIndex >= 0)
            {
                var nextAccessible = AccessibleNode.allAccessibles.Values[nextIndex];
                currentlyFocused = nextAccessible.gameObject.AddComponent<HasKeyboardFocus>();
                UnityEngine.Object.Destroy(previouslyFocused);
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return))
        {
            if (currentlyFocused == null)
                return;
            var button = currentlyFocused.GetComponent<Button>();
            if (button != null && button.IsInteractable())
                button.onClick.Invoke();
        }
    }
        
    int nextFocusableIndex(int startIndex)
    {
        for (var i = startIndex + 1; i < AccessibleNode.allAccessibles.Count; i++)
        {
            if (AccessibleNode.allAccessibles.Values[i].focusable)
                return i;
        }
        for (var i = 0; i <= startIndex; i++)
        {
            if (AccessibleNode.allAccessibles.Values[i].focusable)
                return i;
        }
        return -1;
    }
}