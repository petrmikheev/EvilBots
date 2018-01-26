using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LanguageScript : MonoBehaviour {

	public Text russian;
	public Text english;
	public Text code;
	public RectTransform field;
	public Dropdown dropdown;

	private float updateHeight(Text text) {
		TextGenerationSettings settings = text.GetGenerationSettings(new Vector2 (Screen.width * 0.8f - 70, 1000));
		float height = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings);
		text.GetComponent<RectTransform> ().sizeDelta = new Vector2(0, height);
		return height;
	}

	// Use this for initialization
	void Start () {
		dropdown.value = (Application.systemLanguage == SystemLanguage.Russian) ? 1 : 0;
	}

	public void OnEnable() {
		setLanguage (dropdown.value);
	}
	
	public void setLanguage(int v) {
		english.gameObject.SetActive (v == 0);
		russian.gameObject.SetActive (v == 1);
		float height;
		if (v == 0)
			height = updateHeight (english);
		else
			height = updateHeight (russian);
		code.text = CodeEditor.highlight (v == 0 ? EditorController.DEMO_CODE_EN : EditorController.DEMO_CODE_RU);
		height += updateHeight (code) + 50;
		field.sizeDelta = new Vector2 (0, height);
	}
}
