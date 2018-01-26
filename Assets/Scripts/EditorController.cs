using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorController : MonoBehaviour {
	public static string DEMO_CODE_RU =
		@"/* Просматриваем объекты в поле зрения камеры, ищем врагов и бонусы */
visible int enemy = -1;
visible int bonus = -1;
for (int i = 0; i < visibleObjectsCount; i++) {
    int t = objType(i);
    if (t == ENEMY) enemy = i;
    if (t == BULLET_BONUS || t == ROCKET_BONUS || t == REPAIR_BONUS) bonus = i;
}

/* Управление стрельбой */
if (enemy >= 0 && objDistance(enemy) < 50 && bullets>0) {
    // если в зоне видимости есть враг, он не слишком далеко и у нас остались патроны
    vector directionToEnemy = objPosition(enemy) - position; // определяем направление к нему
    turnTurret = angleHorizontal(gunDirection, directionToEnemy); // наводимся по горизонтали
    turnGun = angleVertical(gunDirection, directionToEnemy); // наводимся по вертикали
    fire1 = true; // и начинаем стрелять
} else {
    fire1 = false;
    turnGun = 0;
}

/* Управление поворотом */
if (bonus >= 0) { // если в зоне видимости есть бонус
    vector directionToBonus = objPosition(bonus) - position; // определяем направление к нему
    float delta = angleHorizontal(corpusDirection, directionToBonus); // определяем, на какой угол нужно повернуть
    if (delta > 10)
        turnCorpus = 1; // если бонус справа - поворачиваем направо
    else if (delta < -10)
        turnCorpus = -1; // если бонус слева - поворачиваем налево
    else
        turnCorpus = 0;
} else { // если бонусов не видно - едем прямо и пытаемся объезжать препятствия
    turnCorpus = 0;
    if (scanObstacle(corpusDirection) < 5) { // если препятствие близко и прямо по курсу
        float leftDist = scanObstacle( rotateRight(corpusDirection, -30) ); // смотрим влево на 30 градусов от направления движения
        float rightDist = scanObstacle( rotateRight(corpusDirection, 30) ); // и вправо на 30 градусов от направления движения
        if (rightDist > leftDist) // с какой стороны препятствие дальше - туда и поворачиваем
            turnCorpus = 1;
        else
            turnCorpus = -1;
    }
}

/* Едем вперед */
move = 1;

/* Но если все-таки врезались - нужно отъехать назад */
visible int collisionTimer;
if (collision && collisionTimer == 0) collisionTimer = 150;
if (collisionTimer > 0) { // будем отъезжать в течении 100 циклов (2 секунды)
                          // и потом еще 50 циклов ехать вперед с поворотом в другую сторону
    collisionTimer--;
    if (collisionTimer > 50) {
        move = -1; // отъезжаем
        turnCorpus = -1; // и разворачиваемся
    } else {
        move = 1;
        turnCorpus = 1;
    }
}";

	public static string DEMO_CODE_EN =
		@"/* A loop through objects in the scope of the camera, search for enemies and bonuses */
visible int enemy = -1;
visible int bonus = -1;
for (int i = 0; i < visibleObjectsCount; i++) {
    int t = objType(i);
    if (t == ENEMY) enemy = i;
    if (t == BULLET_BONUS || t == ROCKET_BONUS || t == REPAIR_BONUS) bonus = i;
}

/* Shooting */
if (enemy >= 0 && objDistance(enemy) < 50 && bullets>0) { // if enemy is visible and near
    vector directionToEnemy = objPosition(enemy) - position;
    turnTurret = angleHorizontal(gunDirection, directionToEnemy); // point the gun along the horizontal axis
    turnGun = angleVertical(gunDirection, directionToEnemy); // point the gun along the vertical axis
    fire1 = true; // and fire!
} else {
    fire1 = false;
    turnGun = 0;
}

/* Turn left or right */
if (bonus >= 0) { // if bonus is visible
    vector directionToBonus = objPosition(bonus) - position;
    // find difference between our orientation and direction to the bonus
    float delta = angleHorizontal(corpusDirection, directionToBonus);
    if (delta > 10)
    turnCorpus = 1; // if the bonus is to the right - turn right
    else if (delta < -10)
    turnCorpus = -1; // if the bonus is to the left - turn left
    else
    turnCorpus = 0;
} else { // if we do not see bonuses - we go forward and try not to run into obstacles
    turnCorpus = 0;
    if (scanObstacle(corpusDirection) < 5) { // if an obstacle is near
    float leftDist = scanObstacle( rotateRight(corpusDirection, -30) ); // look to the left (30 degrees)
    float rightDist = scanObstacle( rotateRight(corpusDirection, 30) ); // look to the right (30 degrees)
        if (rightDist > leftDist) // turn to the side where the obstacle is farther
        turnCorpus = 1;
    else
        turnCorpus = -1;
    }
}

/* Go forward */
move = 1;

/* But if we are smashed into the obstacle - go backward */
visible int collisionTimer;
if (collision && collisionTimer == 0) collisionTimer = 150;
if (collisionTimer > 0) { // we will back out for 100 iterations (2 seconds) and then go forward for 50 iterations
    collisionTimer--;
    if (collisionTimer > 50) {
        move = -1; // back out
        turnCorpus = -1; // and turn
    } else {
        move = 1;
        turnCorpus = 1;
    }
}";

	private static string SETTING_SELECTED_AI = "selectedAI";

	class AI_source {
		public string path;
		public string name;
		public string model;
		public string code;
		public void save() {
			if (path == "")
				return;
			if (!Directory.Exists (ai_src_dir))
				Directory.CreateDirectory (ai_src_dir);
			string content = string.Format ("name={0}\nmodel={1}\n[code]\n{2}", name, model, code);
			File.WriteAllText (path, content);
		}
	}
	AI_source demoAI;
	Dictionary<string, AI_source> ais = new Dictionary<string, AI_source>();

	public CodeEditor editor;
	public ImageChooser modelChooser;
	public InputField nameInput;
	public ListController list;
	public Text status;
	public Text positionLabel;
	public SetupGame setupGame;

	private static string ai_src_dir;

	// Use this for initialization
	void Start () {
		editor.positionLabel = positionLabel;
		positionLabel.text = "[1:1]";
		ai_src_dir = Directory.GetParent (Application.dataPath).FullName;
		ai_src_dir += Path.DirectorySeparatorChar + "ai_src";
		demoAI = new AI_source();
		demoAI.name = "Demo";
		demoAI.model = "Truck";
		demoAI.path = "";
		demoAI.code = (Application.systemLanguage == SystemLanguage.Russian) ? DEMO_CODE_RU : DEMO_CODE_EN;
		updateAIlist ();
	}

	private void updateAIlist() {
		ais.Clear ();
		list.clear ();
		ais.Add (demoAI.path, demoAI);
		if (Directory.Exists (ai_src_dir)) {
			foreach (string path in Directory.GetFiles(ai_src_dir)) {
				if (!path.EndsWith (".ec"))
					continue;
				try {
					string content = File.ReadAllText (path);
					const string code_start = "\n[code]";
					int code_pos = content.IndexOf (code_start);
					string settings = content.Substring (0, code_pos);
					AI_source ai = new AI_source ();
					ai.path = path;
					ai.code = content.Substring (code_pos + code_start.Length);
					if (ai.code.StartsWith ("\r\n"))
						ai.code = content.Substring (code_pos + code_start.Length + 2);
					else
						ai.code = content.Substring (code_pos + code_start.Length + 1);
					ai.name = "";
					ai.model = "";
					foreach (string s in settings.Split(new [] { '\r', '\n' })) {
						if (s.ToLower ().StartsWith ("name"))
							ai.name = s.Substring (s.IndexOf ('=') + 1).Trim ();
						if (s.ToLower ().StartsWith ("model"))
							ai.model = s.Substring (s.IndexOf ('=') + 1).Trim ();
					}
					ais.Add (ai.path, ai);
					list.addItem (ai.path, ai.name);
				} catch (System.Exception) {
				}
			}
		}
		if (PlayerPrefs.HasKey(SETTING_SELECTED_AI))
			list.Selected = PlayerPrefs.GetString (SETTING_SELECTED_AI);
		else
			list.Selected = "";
	}

	public void AIselected(string key) {
		editor.setText (ais [key].code);
		nameInput.text = ais [key].name;
		modelChooser.Value = ais [key].model;
		PlayerPrefs.SetString (SETTING_SELECTED_AI, key);
		status.text = "";
	}

	private string generatePath(string name, string current = "", string dir = "") {
		if (dir == "")
			dir = ai_src_dir;
		string s = dir + Path.DirectorySeparatorChar;
		foreach (char c in name) {
			if (char.IsLetterOrDigit(c))
				s += c;
		}
		if (!File.Exists (s + ".ec") || s + ".ec" == current)
			return s + ".ec";
		int i = 1;
		while (File.Exists (s + '.' + i + ".ec") && (s + '.' + i + ".ec") != current)
			i++;
		return s + '.' + i + ".ec";
	}

	public void createPressed() {
		AI_source ai = new AI_source ();
		ai.name = "New AI";
		ai.model = "Truck";
		ai.code = "";
		ai.path = generatePath (ai.name);
		ai.save ();
		ais.Add (ai.path, ai);
		list.addItem(ai.path, ai.name);
		list.Selected = ai.path;
		status.text = "";
	}

	public void codeChanged(string s) {
		AI_source ai = ais [list.Selected];
		if (ai.code == s)
			return;
		if (ai.path == "") {
			copyPressed ();
			ai = ais [list.Selected];
			editor.setText (s);
		}
		ai.code = s;
		ai.save ();
		status.text = "";
	}

	public void AInameChanged(string name) {
		AI_source ai = ais [list.Selected];
		if (ai.name == name)
			return;
		if (ai.path == "") {
			copyPressed ();
			nameInput.text = name;
			ai = ais [list.Selected];
		}
		ai.name = name;
		string new_path = generatePath (name, ai.path);
		list.addItem (ai.path, name, new_path);
		if (ai.path != new_path) {
			File.Delete (ai.path);
			ais.Remove (ai.path);
			ai.path = new_path;
			ais.Add (ai.path, ai);
			PlayerPrefs.SetString (SETTING_SELECTED_AI, new_path);
		}
		ai.save ();
		status.text = "";
	}

	public void AImodelChanged(string model) {
		AI_source ai = ais [list.Selected];
		if (ai.model == model)
			return;
		if (ai.path == "") {
			copyPressed ();
			ai = ais [list.Selected];
			modelChooser.Value = model;
		}
		ai.model = model;
		ai.save ();
	}

	public void compilePressed() {
		EvilC.EvilCompiler compiler = new EvilC.EvilCompiler ();
		AI_source ai = ais [list.Selected];
		try {
			EvilC.BinaryCode code = compiler.compile(editor.text);
			code.name = ai.name;
			code.model = ai.model;
			string filename;
			if (ai.path == "")
				filename = "Demo.ec";
			else
				filename = ai.path.Substring(ai.path.LastIndexOf('/') + 1);
			filename = filename.Substring(0, filename.Length-3) + ".ai";
			string path = Directory.GetParent (Application.dataPath).FullName;
			path += Path.DirectorySeparatorChar + "ai_bin";
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);
			path += Path.DirectorySeparatorChar + filename;
			FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
			BinaryWriter bw = new BinaryWriter(fs);
			code.writeToStream(bw);
			bw.Close();
			fs.Close();
			status.text = "Compilation successful";
		} catch (System.Exception e) {
			status.text = e.Message;
		}
	}

	public void runPressed() {
		compilePressed ();
		setupGame.OnEnable ();
		setupGame.startGame ();
	}

	public void copyPressed() {
		AI_source ai = ais [list.Selected];
		AI_source new_ai = new AI_source ();
		new_ai.name = ai.name + " (copy)";
		new_ai.model = ai.model;
		new_ai.code = ai.code;
		new_ai.path = generatePath (new_ai.name);
		new_ai.save ();
		ais.Add (new_ai.path, new_ai);
		list.addItem(new_ai.path, new_ai.name);
		list.Selected = new_ai.path;
		status.text = "";
	}

	public void deletePressed() {
		if (list.Selected == "")
			return;
		AI_source ai = ais [list.Selected];
		ais.Remove(ai.path);
		File.Delete (ai.path);
		list.deleteItem (list.Selected);
		list.Selected = "";
	}

	public void renewPressed() {
		updateAIlist ();
	}
}
