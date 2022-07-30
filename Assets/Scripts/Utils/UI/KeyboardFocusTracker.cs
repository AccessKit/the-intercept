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
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            AccessibleNode node = null;
            if (currentlyFocused != null)
            {
                node = currentlyFocused.GetComponent<AccessibleNode>();
                if (node == null)
                {
                    UnityEngine.Object.Destroy(currentlyFocused);
                    currentlyFocused = null;
                    node = null;
                }
            }
            var backward = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var previouslyFocused = currentlyFocused;
            int nextIndex = -1;
            if (previouslyFocused != null)
            {
                var index = AccessibleNode.allAccessibles.IndexOfValue(node);
                nextIndex = backward ? previousFocusableIndex(index) : nextFocusableIndex(index);
            }
            else
            {
                nextIndex = backward ? lastFocusableIndex() : firstFocusableIndex();
            }
            if (nextIndex >= 0)
            {
                var nextAccessible = AccessibleNode.allAccessibles.Values[nextIndex];
                currentlyFocused = nextAccessible.gameObject.AddComponent<HasKeyboardFocus>();
                if (previouslyFocused != null)
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

    int firstFocusableIndex()
    {
        for (var i = 0; i < AccessibleNode.allAccessibles.Count; i++)
        {
            if (AccessibleNode.allAccessibles.Values[i].canBeFocused)
                return i;
        }
        return -1;
    }
        
    int nextFocusableIndex(int startIndex)
    {
        for (var i = startIndex + 1; i < AccessibleNode.allAccessibles.Count; i++)
        {
            if (AccessibleNode.allAccessibles.Values[i].canBeFocused)
                return i;
        }
        for (var i = 0; i < startIndex; i++)
        {
            if (AccessibleNode.allAccessibles.Values[i].canBeFocused)
                return i;
        }
        return -1;
    }

    int lastFocusableIndex()
    {
        for (var i = AccessibleNode.allAccessibles.Count - 1; i >= 0; i--)
        {
            if (AccessibleNode.allAccessibles.Values[i].canBeFocused)
                return i;
        }
        return -1;
    }

    int previousFocusableIndex(int startIndex)
    {
        for (var i = startIndex - 1; i >= 0; i--)
        {
            if (AccessibleNode.allAccessibles.Values[i].canBeFocused)
                return i;
        }
        for (var i = AccessibleNode.allAccessibles.Count - 1; i > startIndex; i--)
        {
            if (AccessibleNode.allAccessibles.Values[i].canBeFocused)
                return i;
        }
        return -1;
    }
}
