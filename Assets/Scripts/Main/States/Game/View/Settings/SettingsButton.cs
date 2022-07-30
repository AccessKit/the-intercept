using UnityEngine;
using System.Collections;
using AccessKit;

public class SettingsButton : MonoBehaviour {

	public FloatTween alphaTween = new FloatTween();

	private CanvasGroup canvasGroup {
		get {
			return GetComponent<CanvasGroup>();
		}
	}
    AccessibleNode accessibleNode
    {
        get { return GetComponent<AccessibleNode>(); }
    }

	private void Awake () {
		alphaTween.OnChange += OnChangeAlphaTween;
	}

	void OnChangeAlphaTween (float currentValue) {
		canvasGroup.alpha = currentValue;
	}

	private void Update () {
		alphaTween.Loop();
		if(Input.GetKeyDown(KeyCode.Escape)) {
			Main.Instance.paused = !Main.Instance.paused;
		}
	}

	public void FadeIn () {
		alphaTween.Tween(0, 1, 5, AnimationCurve.EaseInOut(0,0,1,1));
        accessibleNode.invisible = false;
	}

	public void Show () {
		canvasGroup.alpha = 1;
        accessibleNode.invisible = false;
	}

	public void Hide () {
		alphaTween.Stop();
		canvasGroup.alpha = 0;
        accessibleNode.invisible = true;
	}

	public void OnClickSettingsButton () {
		Main.Instance.paused = true;
	}
}
