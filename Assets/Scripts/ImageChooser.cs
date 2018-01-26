using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ImageChooser : MonoBehaviour {

	[System.Serializable]
	public class StringEvent : UnityEvent<string> {};
	public StringEvent onValueChanged;

	public List<string> titles;
	public List<Sprite> sprites;

	public GameObject buttonForward;
	public GameObject buttonBackward;

	private int index = 0;

	public string Value {
		get { return titles [index]; }
		set {
			index = 0;
			for (int i = 0; i < titles.Count; i++)
				if (titles [i] == value)
					index = i;
			updateSprite ();
		}
	}

	public void setEditable(bool v) {
		buttonForward.SetActive (v);
		buttonBackward.SetActive (v);
	}

	private void updateSprite() {
		Image img = GetComponent<Image> ();
		img.sprite = sprites [index];
	}

	public void next() {
		index++;
		if (index >= titles.Count)
			index = 0;
		updateSprite ();
		onValueChanged.Invoke(titles[index]);
	}

	public void prev() {
		index--;
		if (index < 0)
			index = titles.Count-1;
		updateSprite ();
		onValueChanged.Invoke(titles[index]);
	}

}
