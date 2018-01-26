using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CodeEditor : InputField, ISelectHandler {

	private Scrollbar scrollBar;
	private Event ev = new Event();
	private bool moveScrollBar = false;
	private bool justSelected = false;
	private int pos;
	private int linesOnScreen = 0;
	private List<int> lineSizes = new List<int>();
	private Text highlitedText;
	public Text positionLabel;
	private static bool numLock = true;

	private class CodeState {
		public int pos;
		public string text;
		public CodeState(int pos, string text) {
			this.pos = pos;
			this.text = text;
		}
	}
	private List<CodeState> undoList = new List<CodeState>();
	private int undoPos = 0;

	protected override void Start() {
		base.Start ();
		base.customCaretColor = true;
		scrollBar = GetComponentInChildren<Scrollbar> ();
		scrollBar.onValueChanged.AddListener(scrollBarChanged);
		pos = 0;
		Transform t = transform.Find ("Highlited text");
		if (t != null)
			highlitedText = t.gameObject.GetComponent<Text> ();
		else {
			GameObject highlitedTextGO = new GameObject ("Highlited text", typeof(RectTransform));
			highlitedText = highlitedTextGO.AddComponent<Text> ();
			highlitedText.font = m_TextComponent.font;
			highlitedText.alignment = m_TextComponent.alignment;
			highlitedText.fontSize = m_TextComponent.fontSize;
			highlitedText.horizontalOverflow = m_TextComponent.horizontalOverflow;
			highlitedText.lineSpacing = m_TextComponent.lineSpacing;
			highlitedText.transform.SetParent (transform);
			highlitedText.supportRichText = true;
			highlitedText.color = Color.black;
			RectTransform hrt = highlitedText.GetComponent<RectTransform> ();
			RectTransform rt = m_TextComponent.GetComponent<RectTransform> ();
			hrt.anchorMax = rt.anchorMax;
			hrt.anchorMin = rt.anchorMin;
			hrt.offsetMax = rt.offsetMax;
			hrt.offsetMin = rt.offsetMin;
			hrt.sizeDelta = rt.sizeDelta;
			hrt.localScale = rt.localScale;
			m_TextComponent.supportRichText = false;
			m_TextComponent.color = base.colors.normalColor;
			ColorBlock cb = colors;
			cb.highlightedColor = cb.normalColor;
			colors = cb;
		}
		updateHighlighting ();
		base.ActivateInputField ();
		calculateLinesAndUpdateScrollbar ();
	}

	#pragma warning disable
	public void OnSelect(BaseEventData data) {
		justSelected = true;
	}
	#pragma warning restore

	private int changes = 0;
	public void setText(string s) {
		base.text = s;
		updateHighlighting ();
		undoList.Clear ();
		undoList.Add (new CodeState (0, s));
		undoPos = 0;
		changes = 0;
	}

	public void scrollBarChanged(float v) {
		if (moveScrollBar)
			return;
		int linesBefore = (int)((lineSizes.Count + 1 - linesOnScreen) * v);
		int newBegin, newEnd;
		if (linesBefore == 0)
			newBegin = 0;
		else if (linesBefore <= lineSizes.Count)
			newBegin = lineSizes [linesBefore - 1];
		else
			newBegin = m_Text.Length;
		if (linesBefore + linesOnScreen - 2 < 0)
			newEnd = 0;
		else if (linesBefore+linesOnScreen <= lineSizes.Count)
			newEnd = lineSizes [linesBefore+linesOnScreen - 2];
		else
			newEnd = m_Text.Length;
		if (pos < newBegin)
			pos = newBegin;
		if (pos > newEnd)
			pos = newEnd;
		if (newBegin < m_DrawStart)
			caretPosition = newBegin;
		else if (newEnd > m_DrawEnd)
			caretPosition = newEnd;
		base.ActivateInputField ();
		base.UpdateLabel ();
		caretPosition = pos;
		updateHighlighting ();
	}

	private void undo() {
		if (changes > 0)
			undoList.Add (new CodeState(m_CaretSelectPosition, m_Text));
		else if (undoPos > 0) undoPos--;
		base.text = undoList [undoPos].text;
		base.caretPosition = undoList [undoPos].pos;
		changes = 0;
	}

	private void redo() {
		if (undoPos < undoList.Count - 1) {
			undoPos++;
			base.text = undoList [undoPos].text;
			base.caretPosition = undoList [undoPos].pos;
			changes = 0;
		}
	}

	public override void OnUpdateSelected(BaseEventData eventData) {
		if (!isFocused)
			return;
		if (justSelected) {
			justSelected = false;
			base.caretPosition = pos;
			calculateLinesAndUpdateScrollbar();
		}
		bool keyPressed = false;
		while (Event.PopEvent(ev)) {
			switch (ev.rawType) {
			case EventType.ScrollWheel:
				{
					float newVal = scrollBar.value - scrollBar.size * 0.5f * ev.delta.y;
					newVal = Mathf.Clamp (newVal, 0, 1);
					if (newVal != scrollBar.value)
						scrollBar.value = newVal;
				} break;
			case EventType.KeyDown:
				if (ev.keyCode == KeyCode.Numlock) {
					numLock = !numLock;
					break;
				}
				if (ev.keyCode == KeyCode.S && ev.control) {
					base.onEndEdit.Invoke (base.text);
					break;
				}
				keyPressed = true;
				if (ev.keyCode == KeyCode.Z && ev.control) {
					if (ev.shift)
						redo ();
					else
						undo ();
					break;
				}
				if (ev.keyCode == KeyCode.Y && ev.control) {
					redo ();
					break;
				}
				if (numLock && ev.keyCode == KeyCode.Keypad1)
					ev.keyCode = KeyCode.End;
				if (numLock && ev.keyCode == KeyCode.Keypad2)
					ev.keyCode = KeyCode.DownArrow;
				if (numLock && ev.keyCode == KeyCode.Keypad3)
					ev.keyCode = KeyCode.PageDown;
				if (numLock && ev.keyCode == KeyCode.Keypad4)
					ev.keyCode = KeyCode.LeftArrow;
				if (numLock && ev.keyCode == KeyCode.Keypad6)
					ev.keyCode = KeyCode.RightArrow;
				if (numLock && ev.keyCode == KeyCode.Keypad7)
					ev.keyCode = KeyCode.Home;
				if (numLock && ev.keyCode == KeyCode.Keypad8)
					ev.keyCode = KeyCode.UpArrow;
				if (numLock && ev.keyCode == KeyCode.Keypad9)
					ev.keyCode = KeyCode.PageUp;
				if (ev.keyCode == KeyCode.Home) {
					while (m_CaretSelectPosition > 0 && m_Text [m_CaretSelectPosition - 1] != '\n')
						m_CaretSelectPosition--;
					if (!ev.shift)
						m_CaretPosition = m_CaretSelectPosition;
					break;
				}
				if (ev.keyCode == KeyCode.End) {
					while (m_CaretSelectPosition < text.Length && m_Text [m_CaretSelectPosition] != '\n')
						m_CaretSelectPosition++;
					if (!ev.shift)
						m_CaretPosition = m_CaretSelectPosition;
					break;
				}
				if (ev.keyCode == KeyCode.PageUp || ev.keyCode == KeyCode.PageDown) {
					ev.keyCode = ev.keyCode == KeyCode.PageUp ? KeyCode.UpArrow : KeyCode.DownArrow;
					for (int i = 0; i < linesOnScreen; ++i)
						base.KeyPressed (ev);
					break;
				}
				{
					int size_before = base.text.Length;
					base.KeyPressed (ev);
					changes += Mathf.Abs (size_before - base.text.Length);
				}
				break;
			case EventType.ValidateCommand:
			case EventType.ExecuteCommand:
				if (ev.commandName == "SelectAll") {
					base.SelectAll ();
					break;
				}
				{
					int size_before = base.text.Length;
					base.ProcessEvent (ev);
					changes += Mathf.Abs (size_before - base.text.Length);
				}
				break;
			}
		}
		if (changes > 0 && undoPos != undoList.Count - 1) {
			undoList.RemoveRange (undoPos + 1, undoList.Count - undoPos - 1);
		}
		if (changes >= 10) {
			undoList.Add (new CodeState(m_CaretSelectPosition, m_Text));
			undoPos = undoList.Count - 1;
			changes = 0;
		}
		UpdateLabel ();
		updateHighlighting ();
		eventData.Use ();
		if (keyPressed) {
			calculateLinesAndUpdateScrollbar ();
			pos = m_CaretSelectPosition;
		}
	}

	private void calculateLinesAndUpdateScrollbar() {
		lineSizes.Clear ();
		lineSizes.Add (0);
		int linesBefore = 0, lines = 1;
		linesOnScreen = 1;
		int column = 0;
		for (int i = 0; i < text.Length; ++i) {
			if (i == m_CaretSelectPosition) {
				positionLabel.text = string.Format ("[{0}:{1}]", lines, column+1);
			}
			if (m_Text [i] == '\n') {
				column = 0;
				if (i < m_DrawStart)
					linesBefore++;
				else if (i < m_DrawEnd)
					linesOnScreen++;
				lines++;
				lineSizes.Add (i + 1);
			} else
				column++;
		}
		if (m_CaretSelectPosition == text.Length && positionLabel != null) positionLabel.text = string.Format ("[{0}:{1}]", lines, column+1);
		moveScrollBar = true;
		scrollBar.size = (float)linesOnScreen / lines;
		if (lines > linesOnScreen) scrollBar.value = (float)linesBefore / (lines - linesOnScreen);
		moveScrollBar = false;
	}

	private void updateHighlighting() {
		bool startFromBlockComment = false;
		bool startFromLineComment = false;
		if (m_DrawStart > 0) {
			string before = m_Text.Substring (0, m_DrawStart);
			int nl = before.LastIndexOf ('\n');
			int slc = before.LastIndexOf ("//");
			int mlc = before.LastIndexOf ("/*");
			int mlb = before.LastIndexOf ("*/");
			if (mlc >= 0)
				startFromBlockComment = mlc > mlb;
			if (slc >= 0)
				startFromLineComment = slc > nl;
		}
		highlitedText.text = highlight (m_TextComponent.text, startFromBlockComment, startFromLineComment);
	}

	public static string highlight(string text, bool startFromBlockComment = false,
												bool startFromLineComment = false) {
		const string types = @"\b(int|bool|float|vector|void)\b";
		const string keywords = @"\b(if|else|for|while|break|return|continue|visible|" +
			@"length|normalize|dot|cross|rotateRight|rotateUp|angleHorizontal|angleVertical|" +
			@"abs|min|max|round|floor|ceil|sqrt|sin|cos|asin|acos|atan2|exp|log|randomFloat|randomInt|" +
			@"scanObstacle|objType|objPosition|objVelocity|objDistance|objDirection|objGunDirection)\b";
		const string variables = @"\b(move|turnCorpus|turnTurret|turnGun|fire1|fire2|M_PI|M_E|" +
			@"obstacleDistance|obstaclePosition|obstacleNormal|" +
			@"justStarted|justSpawned|deltaTime|runTime|runtimeError|" +
			@"lives|bullets|rockets|position|gunPosition|velocity|visibleObjectsCount|" +
			@"corpusDirection|gunDirection|upwardDirection|collision|collisionPosition|" +
			@"gravity|firingVelocity|remainingTime|totalBotsCount)\b";
		const string values = @"\b(true|false|NONE|ENEMY|BULLET_BONUS|ROCKET_BONUS|REPAIR_BONUS|" +
			@"BULLET|ROCKET|\d+(?:-?\.\d+)?(?:[eE]-?\d+)?)\b";
		const string define = @"(#define)\b";

		text = Regex.Replace (text, "<", "<<b></b>");

		string answer = "";

		int index = 0;
		while (index < text.Length) {
			int blockCommentStart = text.IndexOf ("/*", index);
			int lineCommentStart = text.IndexOf ("//", index);
			if (index == 0) {
				if (startFromBlockComment)
					blockCommentStart = 0;
				else if (startFromLineComment)
					lineCommentStart = 0;
			}
			if (blockCommentStart < 0)
				blockCommentStart = text.Length;
			if (lineCommentStart < 0)
				lineCommentStart = text.Length;
			int commentStart = Mathf.Min (lineCommentStart, blockCommentStart);
			if (commentStart > index) {
				string s = text.Substring (index, commentStart - index);
				s = Regex.Replace (s, types, "<color=teal>$1</color>");
				s = Regex.Replace (s, keywords, "<color=blue>$1</color>");
				s = Regex.Replace (s, variables, "<color=green>$1</color>");
				s = Regex.Replace (s, values, "<color=magenta>$1</color>");
				s = Regex.Replace (s, define, "<color=grey>$1</color>");
				answer += s;
				index = commentStart;
			}
			if (commentStart < text.Length) {
				int commentEnd;
				if (lineCommentStart < blockCommentStart)
					commentEnd = text.IndexOf ('\n', index);
				else {
					commentEnd = text.IndexOf ("*/", index);
					if (commentEnd >= 0)
						commentEnd += 2;
				}
				if (commentEnd == -1)
					commentEnd = text.Length;
				answer += "<color=grey>" + text.Substring (index, commentEnd - index) + "</color>";
				index = commentEnd;
			}
		}
		return answer;
	}
}
