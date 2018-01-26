using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupGame : MonoBehaviour {

	private const string LEVEL_SETTING = "level";

	public ImageChooser levelChooser;
	public ImageChooser modelChooser;
	public ListController listAvailable;
	public ListController listSelected;
	private Dictionary<string, EvilC.BinaryCode> ais = new Dictionary<string, EvilC.BinaryCode>();

	public class BotSettings {
		public string name;
		public string model;
		public string ai_path;
		public EvilC.BinaryCode ai;
	}
	public static Dictionary<string, BotSettings> players = new Dictionary<string, BotSettings> ();

	public void savePlayers() {
		string p = "";
		foreach (BotSettings s in players.Values) {
			p += s.name + "\n" + s.model + "\n" + s.ai_path + "\n";
		}
		PlayerPrefs.SetString("players_list", p);
	}

	public void loadPlayers() {
		players.Clear ();
		listSelected.clear ();
		if (!PlayerPrefs.HasKey ("players_list")) {
			if (ais.Count > 1) {
				int count = 4;
				while (count > 0) {
					foreach (var kv in ais) {
						if (kv.Key == "")
							continue;
						BotSettings bs = new BotSettings ();
						bs.ai = kv.Value;
						bs.ai_path = kv.Key;
						bs.name = generateName (bs.ai.name);
						count--;
						bs.model = modelChooser.titles [count % modelChooser.titles.Count];
						players [bs.name] = bs;
						if (count == 0)
							break;
					}
				}
			}
			foreach (BotSettings bs in players.Values) {
				listSelected.addItem (bs.name, bs.name);
			}
			return;
		}
		string[] strs = PlayerPrefs.GetString ("players_list").Split ('\n');
		for (int i = 0; i < strs.Length / 3; i++) {
			BotSettings bs = new BotSettings ();
			bs.name = strs [i * 3];
			bs.model = strs [i * 3 + 1];
			bs.ai_path = strs [i * 3 + 2];
			if (players.ContainsKey (bs.name))
				continue;
			if (bs.ai_path != "") {
				if (!ais.ContainsKey (bs.ai_path))
					continue;
				bs.ai = ais[bs.ai_path];
			} else bs.ai = null;
			players [bs.name] = bs;
		}
		foreach (BotSettings bs in players.Values) {
			listSelected.addItem (bs.name, bs.name);
		}
	}

	void Start () {
		string level = levelChooser.titles [0];
		if (PlayerPrefs.HasKey (LEVEL_SETTING))
			level = PlayerPrefs.GetString (LEVEL_SETTING);
		levelChooser.Value = level;
		GameManager.levelNumber = int.Parse (level);
		listSelected.setDefaultVisible (false);
	}

	public void OnEnable() {
		updateAIlist ();
		loadPlayers ();
	}

	void OnDisable() {
		savePlayers ();
	}

	private void updateAIlist() {
		string ai_bin_dir = Directory.GetParent (Application.dataPath).FullName;
		ai_bin_dir += Path.DirectorySeparatorChar + "ai_bin";
		ais.Clear ();
		ais.Add ("", null);
		listAvailable.clear();
		if (!Directory.Exists (ai_bin_dir)) return;
		foreach (string path in Directory.GetFiles(ai_bin_dir)) {
			if (!path.EndsWith (".ai"))
				continue;
			try {
				FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
				BinaryReader br = new BinaryReader(fs);
				EvilC.BinaryCode code = new EvilC.BinaryCode(br);
				br.Close();
				fs.Close();
				ais.Add(path, code);
				listAvailable.addItem(path, code.name);
			} catch (System.Exception e) {
				Debug.Log (e.Message + "\n" + e.StackTrace);
			}
		}
	}

	public void levelChanged(string s) {
		PlayerPrefs.SetString (LEVEL_SETTING, s);
		GameManager.levelNumber = int.Parse (s);
	}

	public void modelChanged(string s) {
		string name = listSelected.Selected;
		if (players.ContainsKey (name))
			players [name].model = s;
	}

	public void setCurrent(string name) {
		if (players.ContainsKey (name))
			modelChooser.Value = players [name].model;
	}

	public void deleteAI() {
		string path = listAvailable.Selected;
		if (path == null || path == "")
			return;
		File.Delete (path);
		ais.Remove (path);
		listAvailable.deleteItem (path);
	}

	private string generateName(string name) {
		while (name == "Player" || players.ContainsKey (name)) {
			int i = name.LastIndexOf ('#');
			if (i < 0)
				i = name.Length;
			try {
				int n = int.Parse (name.Substring (i + 1));
				name = name.Substring (0, i + 1) + (n + 1);
			} catch (System.Exception) {
				name = name + " #2";
			}
		}
		return name;
	}

	public void selectItem() {
		Debug.Log ("!!!");
		string path = listAvailable.Selected;
		if (path == null)
			return;
		BotSettings bs;
		if (path == "") {
			if (players.ContainsKey ("Player")) {
				listSelected.Selected = "Player";
				return;
			}
			bs = new BotSettings ();
			bs.name = "Player";
			bs.model = "Truck";
			bs.ai_path = "";
			bs.ai = null;
		} else {
			bs = new BotSettings ();
			EvilC.BinaryCode code = ais [path];
			bs.ai = code;
			bs.ai_path = path;
			bs.model = code.model;
			bs.name = generateName(code.name);
		}
		players [bs.name] = bs;
		listSelected.addItem (bs.name, bs.name);
		listSelected.Selected = bs.name;
	}

	public void deselectItem() {
		string name = listSelected.Selected;
		if (players.ContainsKey (name)) {
			players.Remove (name);
			listSelected.deleteItem (name);
		}
	}

	public void startGame() {
		if (players.Count == 0)
			return;
		SceneManager.LoadScene (1);
	}
}
