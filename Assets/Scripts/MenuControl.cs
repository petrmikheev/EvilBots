using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuControl : MonoBehaviour {

	public Toggle fullscreenToggle = null;
	public Dropdown resolutionDropDown = null;
	public Slider musicVolume = null;
	public AudioSource backgroundMusic = null;
	private List<Resolution> resolutions = new List<Resolution>();
	private const string MUSIC_VOLUME = "BackGroundMusicVolume";

	private static MenuControl THIS = null;

	public void Start() {
		if (THIS != null)
			throw new System.Exception ("MenuControl duplicate: " + THIS.gameObject.name);
		THIS = this;
		bool firstRun = false;
		if (!Application.isEditor && !PlayerPrefs.HasKey("initialized")) {
			PlayerPrefs.DeleteAll();
			PlayerPrefs.SetInt("initialized", 1);
			firstRun = true;
		}
		if (fullscreenToggle != null)
			fullscreenToggle.isOn = Screen.fullScreen;
		if (Screen.resolutions.Length > 0) {
			List<string> options = new List<string> ();
			Resolution maxRes = Screen.resolutions [Screen.resolutions.Length - 1];
			float bestAspect = (float)maxRes.width / maxRes.height;
			int current = -1;
			int bestNum = -1;
			foreach (var r in Screen.resolutions) {
				if (r.width < 800 || r.height < 600)
					continue;
				if (r.width == Screen.width && r.height == Screen.height) current = options.Count;
				float aspect = (float)r.width / r.height;
				if (r.width <= 1400 && Mathf.Abs (aspect / bestAspect - 1) < 0.1) {
					bestNum = options.Count;
					options.Add ("" + r.width + "x" + r.height);
				} else
				options.Add ("" + r.width + "x" + r.height);
				resolutions.Add (r);
			}
			if (options.Count == 0) {
				options.Add ("" + maxRes.width + "x" + maxRes.height);
				resolutions.Add (maxRes);
			}
			if (bestNum == -1) bestNum = options.Count - 1;
			if (firstRun) {
				current = bestNum;
				SetResolution (current);
			}
			resolutionDropDown.AddOptions (options);
			if (current >= 0) resolutionDropDown.value = current;
		}
		if (musicVolume != null) {
			float v = 0.2f;
			if (PlayerPrefs.HasKey (MUSIC_VOLUME))
				v = PlayerPrefs.GetFloat (MUSIC_VOLUME);
			else
				PlayerPrefs.SetFloat (MUSIC_VOLUME, v);
			musicVolume.value = v;
			if (backgroundMusic != null)
				backgroundMusic.volume = v;
		}
	}

	public void SetFullScreen(bool v) {
		Screen.fullScreen = v;
	}

	public void SetResolution(int r) {
		Resolution rs = resolutions [r];
		Screen.SetResolution(rs.width, rs.height, Screen.fullScreen);
	}

	public void SetMusicVolume(float f) {
		if (backgroundMusic != null)
			backgroundMusic.volume = f;
		PlayerPrefs.SetFloat (MUSIC_VOLUME, f);
	}

	public void Quit() {
		#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
		#else
		Application.Quit();
		#endif
	}

}
