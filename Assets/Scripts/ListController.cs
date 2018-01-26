using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ListController : MonoBehaviour {

	[System.Serializable]
	public class StringEvent : UnityEvent<string> {};
	public StringEvent onValueChanged;

	public Toggle defaultItem;
	private List<Toggle> toggles = new List<Toggle>();
	private List<string> keys = new List<string>();
	public Scrollbar scrollbar;

	// Use this for initialization
	private bool initialized = false;
	void Start () {
		if (initialized)
			return;
		toggles.Add (defaultItem);
		keys.Add ("");
		initialized = true;
	}

	public void setDefaultVisible(bool v) {
		defaultItem.gameObject.SetActive (v);
	}

	private void updateSize() {
		float itemSizeY = defaultItem.GetComponent<RectTransform> ().sizeDelta.y;
		float sizey = itemSizeY * keys.Count;
		if (sizey < 200)
			sizey = 200;
		GetComponent<RectTransform> ().sizeDelta = new Vector2 (0, sizey);
	}

	public void addItem(string key, string value, string new_key = "") {
		if (!initialized)
			Start ();
		if (key == "")
			return;
		Toggle t = null;
		for (int i = 0; i < keys.Count; i++)
			if (keys [i] == key) {
				t = toggles [i];
				if (new_key != "") {
					keys [i] = new_key;
					if (selected == key)
						selected = new_key;
				}
			}
		if (t == null) {
			t = Instantiate (defaultItem, transform).GetComponent<Toggle> ();
			t.gameObject.SetActive (true);
			toggles.Add (t);
			keys.Add (key);
			t.isOn = false;
			updateSize ();
			scrollbar.value = 0;
		}
		t.GetComponentInChildren<Text> ().text = value;
	}

	public void scrollUp() {
		scrollbar.value = 1;
	}

	public void clear() {
		if (!initialized)
			Start ();
		for (int i = keys.Count-1; i > 0; i--) {
			Destroy (toggles [i].gameObject);
			toggles.RemoveAt (i);
			keys.RemoveAt (i);
		}
		updateSize ();
	}

	public void deleteItem(string key) {
		if (!initialized)
			Start ();
		if (key == "")
			return;
		for (int i = 0; i < keys.Count; i++)
			if (keys [i] == key) {
				Destroy (toggles [i].gameObject);
				toggles.RemoveAt (i);
				keys.RemoveAt (i);
				updateSize ();
				return;
			}
	}

	private string selected = "";
	public string Selected {
		get { return selected; }
		set {
			for (int i = 0; i < keys.Count; i++)
				if (keys [i] == value)
					toggles [i].isOn = true;
		}
	}

	public void itemSelected(Toggle t) {
		if (!t.isOn)
			return;
		for (int i = 0; i < toggles.Count; i++)
			if (toggles [i] == t) {
				selected = keys [i];
				onValueChanged.Invoke(selected);
				return;
			}
	}
}
