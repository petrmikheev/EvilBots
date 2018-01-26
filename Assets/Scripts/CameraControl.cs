using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraControl : MonoBehaviour {

	public enum Mode { Bot, Gun, Free }
	public Mode mode;
	public bool isPlayer;
	public GameObject scope;
	public Text bulletsText;
	public Text livesText;
	public Text rocketsText;
	public Text variablesText;

	private float angleX;
	private float angleY;
	private int targetIndex;
	private bool control;
	private const float ax_offset = 4;
	private List<GameObject> targets;

	// Use this for initialization
	void Start () {
		mode = Mode.Bot;
		angleX = 0;
		angleY = 0;
		targetIndex = 0;
		targets = GameManager.Bots;
		for (int i = 0; i < targets.Count; ++i)
			if (targets [i].name == "Player") {
				targetIndex = i;
				isPlayer = true;
				BotAI ai = targets[i].GetComponent<BotAI> ();
				if (ai != null)
					ai.enabled = false;
			}
	}
	
	// Update is called once per frame
	void Update () {
		if (GameManager.pause)
			return;
		if (isPlayer)
			control = true;
		GameObject target = null;
		if (targetIndex >= 0 && targetIndex < targets.Count)
			target = targets [targetIndex];
		if (!isPlayer && target != null && Input.GetKeyUp(KeyCode.Return)) {
			control = !control;
			BotAI ai = target.GetComponent<BotAI> ();
			if (!control)
				mode = CameraControl.Mode.Bot;
			if (ai != null) ai.controlEnabled = !control;
		}
		BotControl bot = null;
		if (target == null) {
			control = false;
			mode = Mode.Free;
		} else {
			bot = target.GetComponent<BotControl> ();
			bool showScope = control && target.activeSelf;
			if (showScope != scope.activeSelf) scope.SetActive (showScope);
		}

		if (control || mode != Mode.Gun) {
			angleY += Input.GetAxis ("Mouse X") * 2;
			angleX += Input.GetAxis ("Mouse Y") * 2;
		}
		if (control) {
			scope.transform.localRotation = Quaternion.Euler (0, 0, angleY * 3);
			float velocity = bot.GetComponent<Rigidbody> ().velocity.magnitude;
			scope.transform.localScale = new Vector3 (0.025f, 0.025f, 0.025f) * (1 + Mathf.Log(1.0f + velocity*0.25f));
			angleX = Mathf.Clamp (angleX, -10-ax_offset, 30-ax_offset);
			mode = Mode.Gun;
			bot.move = Input.GetAxis ("Vertical");
			bot.turnCorpus = Input.GetAxis ("Horizontal");
			bot.fire1 = Input.GetAxis ("Fire1") > 0.5; // Input.GetMouseButton (0);
			bot.fire2 = Input.GetAxis ("Fire2") > 0.5; // Input.GetMouseButton (1);
			float turretAngle = bot.TurretAngle + target.transform.rotation.eulerAngles.y;
			float turnTurret = angleY - turretAngle;
			while (turnTurret > 180)
				turnTurret -= 360;
			while (turnTurret < -180)
				turnTurret += 360;
			bot.turnTurret = turnTurret * 0.2f;
			bot.turnGun = (angleX + ax_offset - bot.GunAngle) * 0.2f;
		} else {
			if (Input.GetMouseButtonDown (0)) {
				targetIndex += 1;
				if (targetIndex >= targets.Count)
					targetIndex = 0;
			}
			if (Input.GetMouseButtonDown (1)) {
				if (mode == Mode.Bot) mode = Mode.Gun;
				else if (mode == Mode.Gun) mode = Mode.Free;
				else mode = Mode.Bot;
			}
			if (mode == Mode.Gun) {
				Vector3 angles = new Vector3(-bot.GunAngle, bot.TurretAngle + bot.transform.rotation.eulerAngles.y, 0);
				if (angles.x > 180) angles.x -= 360;
				float dx = -angles.x - ax_offset - angleX;
				float dy = angles.y - angleY;
				while (dy > 180) dy -= 360;
				while (dy < -180) dy += 360;
				angleX += dx * 0.3f;
				angleY += dy * 0.3f;
			}
		}
		if (mode == Mode.Bot)
			angleX = Mathf.Clamp (angleX, -35, 25);
		if (mode == Mode.Free)
			angleX = Mathf.Clamp (angleX, -80, 80);
		if (angleY < 0) angleY += 360;
		if (angleY >= 360) angleY -= 360;

		Quaternion r = Quaternion.Euler (-angleX, angleY, 0);
		transform.rotation = r;
		if (mode == Mode.Gun) {
			Vector3 offset = Quaternion.Euler (0, angleY, 0) * new Vector3 (0, 0.8f, -2.0f);
			transform.position = bot.gun.transform.position + offset;
		} else if (mode == Mode.Bot) {
			Vector3 offset = r * new Vector3 (0, 0.8f, -2.5f);
			transform.position = target.transform.position + offset;
		} else {
			Vector3 move = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical"));
			transform.position += r * move * Time.deltaTime * 5f;
		}
		if (mode == Mode.Free) {
			bulletsText.enabled = false;
			livesText.enabled = false;
			rocketsText.enabled = false;
		} else {
			bulletsText.enabled = true;
			livesText.enabled = true;
			rocketsText.enabled = true;
			livesText.text = string.Format("[{0}] Lives : {1}", target.name, bot.Lives);
			bulletsText.text = string.Format("Bullets : {0}", bot.Bullets);
			rocketsText.text = string.Format("Rockets : {0}", bot.Rockets);
			if (!isPlayer && GameManager.showVariables)
				variablesText.text = target.GetComponent<BotAI> ().aiRuntime.showVariables ();
			else
				variablesText.text = "";
		}
	}
}
