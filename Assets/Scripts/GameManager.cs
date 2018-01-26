using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

	public static bool showLidars = false;
	public static bool showVariables = false;
	public static List<GameObject> Bots;
	private static Queue<GameObject> spawnQueue = new Queue<GameObject>();
	public static List<GameObject> Bonuses = new List<GameObject>();
	public static HashSet<GameObject> DetectableObjects = new HashSet<GameObject>();
	public List<GameObject> bots;
	public Text botTitlesText;
	public Text scoreText;
	public Text timeText;
	public GameObject respawnPrefab;
	public GameObject bulletBonusPrefab;
	public GameObject rocketBonusPrefab;
	public GameObject repairBonusPrefab;
	public List<GameObject> levelPrefabs;
	public static int levelNumber = 0;
	private List<Vector3> spawnPoints;
	private float[] spawnTimes;
	public static bool pause { get; private set;}
	public GameObject pausePanel, resultsPanel;

	public static void addToSpawnQueue(GameObject o) {
		o.SetActive (false);
		spawnQueue.Enqueue (o);
	}

	private void instantiateBots() {
		Bots = new List<GameObject> ();
		foreach (var bs in SetupGame.players.Values) {
			GameObject model = bots [0];
			foreach (var m in bots)
				if (m.name == bs.model)
					model = m;
			GameObject b = Instantiate<GameObject> (model);
			b.name = bs.name;
			b.GetComponent<BotAI> ().setCode(bs.ai);
			Bots.Add (b);
		}
	}

	// Use this for initialization
	void Start () {
		AudioListener.volume = 0;
		pause = false;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		showLidars = false;
		instantiateBots();
		string titles = "";
		foreach (GameObject x in Bots) {
			titles += x.name + '\n';
		}
		botTitlesText.text = titles;
		Instantiate(levelPrefabs[levelNumber % levelPrefabs.Count]);
		spawnPoints = new List<Vector3> ();
		Bonuses.Clear();
		DetectableObjects.Clear ();
		foreach (GameObject r in GameObject.FindGameObjectsWithTag ("Respawn")) {
			spawnPoints.Add (r.transform.position);
			Instantiate (respawnPrefab, r.transform.position, Quaternion.identity);
			Destroy (r);
		}
		foreach (GameObject r in GameObject.FindGameObjectsWithTag ("BulletBonus")) {
			Bonuses.Add(Instantiate (bulletBonusPrefab, r.transform.position, Quaternion.identity));
			Destroy (r);
		}
		foreach (GameObject r in GameObject.FindGameObjectsWithTag ("RepairBonus")) {
			Bonuses.Add(Instantiate (repairBonusPrefab, r.transform.position, Quaternion.identity));
			Destroy (r);
		}
		foreach (GameObject r in GameObject.FindGameObjectsWithTag ("RocketBonus")) {
			Bonuses.Add(Instantiate (rocketBonusPrefab, r.transform.position, Quaternion.identity));
			Destroy (r);
		}
		spawnTimes = new float[spawnPoints.Count];
		for (int i = 0; i < spawnPoints.Count; ++i)
			spawnTimes [i] = 0;
	}
	
	// Update is called once per frame
	void Update () {
		if (Time.timeSinceLevelLoad > 0.5 && AudioListener.volume == 0 && !pause)
			AudioListener.volume = 1;
		#if UNITY_EDITOR
		{
			if (Input.GetKeyUp(KeyCode.E)) {
				if (Cursor.visible) {
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				} else {
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
			}
		}
		#endif
		if (resultsPanel.activeSelf && (Input.GetKeyUp (KeyCode.Escape) || Input.GetKeyUp (KeyCode.Return)))
			endRound ();
		else if (Input.GetKeyUp(KeyCode.Escape)) {
			if (pause)
				continueGame ();
			else
				pauseGame ();
		}
		if (Input.GetKeyUp(KeyCode.L) || Input.GetKeyUp(KeyCode.R)) {
			showLidars = !showLidars;
		}
		if (Input.GetKeyUp(KeyCode.V)) {
			showVariables = !showVariables;
		}
	}

	public void pauseGame(bool showResults = false) {
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		if (showResults) {
			resultsPanel.SetActive (true);
		} else
			pausePanel.SetActive (true);
		Time.timeScale = 0;
		pause = true;
		AudioListener.volume = 0;
	}

	public void continueGame() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		pausePanel.SetActive (false);
		Time.timeScale = 1;
		pause = false;
		AudioListener.volume = 1;
	}

	public void endRound() {
		continueGame ();
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		SceneManager.LoadScene (0);
		spawnQueue.Clear ();
	}

	public static float timeRemaining = 300;
	void FixedUpdate() {
		string scoreString = "";
		foreach (GameObject x in Bots) {
			int score = x.GetComponent<BotControl>().Score;
			scoreString += ": " + score + '\n';
		}
		scoreText.text = scoreString;
		timeRemaining = 300 - Time.timeSinceLevelLoad;
		int remain = (int)timeRemaining;
		timeText.text = string.Format("{0}:{1,2:00}", remain / 60, remain % 60);
		if (timeRemaining <= 0)
			pauseGame (true);
		int freeCount = 0;
		for (int i = 0; i < spawnPoints.Count; ++i) {
			if (spawnTimes [i] > 0)
				spawnTimes [i] -= Time.fixedDeltaTime;
			else
				freeCount++;
		}
		float btime = 0;
		if (spawnQueue.Count > 0) {
			BotControl b = spawnQueue.Peek().GetComponent<BotControl> ();
			btime = b.dieTime + 5;
		}
		while (freeCount > 0 && spawnQueue.Count > 0 && btime < Time.timeSinceLevelLoad) {
			GameObject b = spawnQueue.Dequeue ();
			Rigidbody rb = b.GetComponent<Rigidbody> ();
			BotControl bc = b.GetComponent<BotControl> ();
			int sk = Random.Range (0, freeCount);
			int i = 0;
			while (sk > 0 || spawnTimes [i] > 0) {
				if (spawnTimes [i++] <= 0)
					sk--;
			}
			spawnTimes [i] = 2f;
			freeCount--;
			b.SetActive (true);
			bc.spawn ();
			rb.MovePosition (spawnPoints[i] + new Vector3 (0f, 0.5f, 0f));
		}
	}
}
