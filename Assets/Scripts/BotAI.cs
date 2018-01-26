using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EvilC;

public class BotAI : MonoBehaviour {

	public EvilRuntime aiRuntime = null;

	private Rigidbody body;
	private BotControl bot;
	private LineRenderer[] rays;
	private LineRenderer cameraScope;

	public bool controlEnabled = true;

	private List<GameObject> visibleObjects = new List<GameObject> ();

	// Use this for initialization
	void Start () {
		body = GetComponent<Rigidbody> ();
		bot = GetComponent<BotControl> ();
		rays = new LineRenderer[4];
		for (int i = 0; i < rays.Length; i++) {
			GameObject r = new GameObject ();
			r.transform.parent = transform;
			float y = (transform.InverseTransformPoint (bot.gun.transform.position).y + bot.wheelPosition.y) * 0.5f;
			r.transform.localPosition = new Vector3(0, y, 0);
			LineRenderer lr = r.AddComponent<LineRenderer> ();
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			lr.startWidth = 0.05f;
			lr.endWidth = 0.05f;
			lr.startColor = Color.green;
			lr.endColor = Color.red;
			lr.positionCount = 2;
			lr.SetPosition (0, Vector3.zero);
			lr.SetPosition (1, Vector3.zero);
			lr.useWorldSpace = true;
			lr.material = new Material(Shader.Find("Sprites/Default"));
			rays [i] = lr;
		}
		GameObject c = new GameObject ();
		c.transform.parent = bot.gun.transform;
		c.transform.localPosition = Vector3.zero;
		c.transform.localRotation = Quaternion.identity;
		cameraScope = c.AddComponent<LineRenderer> ();
		cameraScope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		cameraScope.receiveShadows = false;
		cameraScope.widthMultiplier = 0.015f;
		cameraScope.material = new Material(Shader.Find("Sprites/Default"));
		cameraScope.useWorldSpace = false;
		cameraScope.positionCount = 3;
		cameraScope.SetPosition (0, new Vector3(15f, 0, 26f));
		cameraScope.SetPosition (1, Vector3.zero);
		cameraScope.SetPosition (2, new Vector3(-15f, 0, 26f));
		cameraScope.enabled = false;
	}

	void OnDisable() {
		foreach (var r in rays)
			r.enabled = false;
		cameraScope.enabled = false;
	}

	private bool trace(Vector3 direction, out RaycastHit hitInfo, bool all=false) {
		direction.Normalize ();
		Vector3 position = bot.getCenter();
		float offset = 0;
		while (true) {
			if (!Physics.Raycast (position + offset * direction, direction, out hitInfo))
				return false;
			bool ignore = hitInfo.collider.attachedRigidbody == body;
			if (!all && hitInfo.collider.attachedRigidbody != null) {
				Rigidbody b = hitInfo.collider.attachedRigidbody;
				if (b.isKinematic)
					ignore = true;
			}
			if (ignore) {
				offset += hitInfo.distance + 0.1f;
			} else {
				hitInfo.distance = hitInfo.distance + offset;
				return true;
			}
		}
	}

	private void checkVisibility(Vector3 point, GameObject g) {
		Rigidbody b = g.GetComponent<Rigidbody> ();
		Vector3 lp = bot.gun.transform.InverseTransformPoint (point);
		if (lp.z <= 0)
			return;
		lp /= lp.z;
		if (Mathf.Abs (lp.x) > 0.578 || Mathf.Abs (lp.y) > 0.4)
			return;
		RaycastHit hitInfo;
		Vector3 dir = point - bot.getCenter ();
		if (trace(dir, out hitInfo, true)) {
			if (b != null && hitInfo.rigidbody == b)
				visibleObjects.Add (g);
			else if (b == null && hitInfo.distance > dir.magnitude)
				visibleObjects.Add (g);
		} else if (b == null) visibleObjects.Add (g);
	}

	private int currentRay = 0;
	public class AIRuntime : EvilRuntime {
		BotAI bai;
		public AIRuntime(BotAI bai, BinaryCode code) : base(code) { this.bai = bai; }

		public override float scanObstacle(float x, float y, float z) {
			RaycastHit hitInfo;
			float distance = 1e9f;
			Vector3 direction = new Vector3 (x, y, z).normalized;
			if (bai.trace(direction, out hitInfo)) {
				distance = hitInfo.distance;
				Vector3 p = hitInfo.normal;
				setVector (BuiltinVariable.OBSTACLE_NORMAL, p.x, p.y, p.z);
			}
			setFloat (BuiltinVariable.OBSTACLE_DISTANCE, distance);
			Vector3 point = bai.bot.getCenter() + direction * distance;
			setVector (BuiltinVariable.OBSTACLE_POSITION, point.x, point.y, point.z);
			if (GameManager.showLidars && bai.currentRay < bai.rays.Length) {
				bai.rays [bai.currentRay].SetPosition (0, bai.bot.getCenter() + direction*0.2f);
				bai.rays [bai.currentRay++].SetPosition (1, point);
			}
			return distance;
		}

		public override int objType(int i) {
			if (i < 0 || i >= bai.visibleObjects.Count)
				return 0;
			GameObject b = bai.visibleObjects [i];
			if (b.GetComponent<BotControl> () != null) return 1;
			if (b.tag == "BulletBonus") return 2;
			if (b.tag == "RocketBonus") return 3;
			if (b.tag == "RepairBonus") return 4;
			if (b.tag == "Bullet") return 5;
			if (b.tag == "Rocket") return 6;
			return 0;
		}

		public override float objDistance(int i) {
			if (i < 0 || i >= bai.visibleObjects.Count)
				return 1e9f;
			GameObject b = bai.visibleObjects [i];
			BotControl bot = b.GetComponent<BotControl> ();
			Vector3 pos = bot == null ? b.transform.position : bot.getCenter();
			return (pos - bai.bot.getCenter()).magnitude;
		}

		public override void objPosition(int i, out float x, out float y, out float z) {
			if (i < 0 || i >= bai.visibleObjects.Count) {
				x = y = z = 0;
				return;
			}
			GameObject b = bai.visibleObjects [i];
			BotControl bot = b.GetComponent<BotControl> ();
			Vector3 pos = bot == null ? b.transform.position : bot.getCenter();
			x = pos.x;
			y = pos.y;
			z = pos.z;
		}
		public override void objVelocity(int i, out float x, out float y, out float z) {
			if (i < 0 || i >= bai.visibleObjects.Count) {
				x = y = z = 0;
				return;
			}
			GameObject go = bai.visibleObjects [i];
			if (go.tag == "Rocket") {
				RocketScript rs = go.GetComponent<RocketScript> ();
				Vector3 v = rs.getVelocity ();
				x = v.x;
				y = v.y;
				z = v.z;
				return;
			}
			Rigidbody b = go.GetComponent<Rigidbody>();
			if (b == null)
				b = go.transform.parent.GetComponent<Rigidbody> ();
			if (b == null)
				x = y = z = 0;
			else {
				x = b.velocity.x;
				y = b.velocity.y;
				z = b.velocity.z;
			}
		}
		public override void objDirection(int i, out float x, out float y, out float z) {
			if (i < 0 || i >= bai.visibleObjects.Count) {
				x = y = z = 0;
				return;
			}
			BotControl bot = bai.visibleObjects [i].GetComponent<BotControl> ();
			if (bot == null) {
				x = y = z = 0;
				return;
			}
			Vector3 dir = bot.transform.forward;
			x = dir.x;
			y = dir.y;
			z = dir.z;
		}
		public override void objGunDirection(int i, out float x, out float y, out float z) {
			if (i < 0 || i >= bai.visibleObjects.Count) {
				x = y = z = 0;
				return;
			}
			BotControl bot = bai.visibleObjects [i].GetComponent<BotControl> ();
			if (bot == null) {
				x = y = z = 0;
				return;
			}
			Vector3 dir = bot.gun.transform.forward;
			x = dir.x;
			y = dir.y;
			z = dir.z;
		}
	}

	public void setCode(BinaryCode code) {
		aiRuntime = new AIRuntime (this, code);
	}
	
	private bool justStarted = true;
	private bool justSpawned = true;
	private float runTime = 0;
	private float runDelta = 0;

	void FixedUpdate () {
		currentRay = 0;
		if (aiRuntime == null || bot.liveTimer < 1) {
			justSpawned = true;
			return;
		}
		if (justSpawned || justStarted)
			aiRuntime.restart ();
		if (aiRuntime.isOnStart ()) {
			aiRuntime.setInt (BuiltinVariable.JUST_STARTED, justStarted ? 1 : 0);
			aiRuntime.setInt (BuiltinVariable.JUST_SPAWNED, justSpawned ? 1 : 0);
			aiRuntime.setInt (BuiltinVariable.RUNTIME_ERROR, aiRuntime.runtimeError ? 1 : 0);
			aiRuntime.setFloat (BuiltinVariable.RUN_TIME, runTime);
			aiRuntime.setFloat (BuiltinVariable.DELTA_TIME, runDelta);

			aiRuntime.setInt (BuiltinVariable.LIVES, bot.Lives);
			aiRuntime.setInt (BuiltinVariable.BULLETS, bot.Bullets);
			aiRuntime.setInt (BuiltinVariable.ROCKETS, bot.Rockets);

			aiRuntime.setFloat (BuiltinVariable.GRAVITY, Physics.gravity.magnitude);
			aiRuntime.setFloat (BuiltinVariable.FIRING_VELOCITY, BotControl.bulletSpeed);
			aiRuntime.setFloat (BuiltinVariable.REMAINING_TIME, GameManager.timeRemaining);
			aiRuntime.setInt (BuiltinVariable.TOTAL_BOTS_COUNT, GameManager.Bots.Count);

			Vector3 p = bot.getCenter ();
			aiRuntime.setVector (BuiltinVariable.POSITION, p.x, p.y, p.z);
			p = bot.gunEnd.transform.position;
			aiRuntime.setVector (BuiltinVariable.GUN_POSITION, p.x, p.y, p.z);
			p = body.velocity;
			aiRuntime.setVector (BuiltinVariable.VELOCITY, p.x, p.y, p.z);
			p = transform.forward;
			aiRuntime.setVector (BuiltinVariable.CORPUS_DIRECTION, p.x, p.y, p.z);
			p = bot.gun.transform.forward;
			aiRuntime.setVector (BuiltinVariable.GUN_DIRECTION, p.x, p.y, p.z);
			p = transform.up;
			aiRuntime.setVector (BuiltinVariable.UPWARD_DIRECTION, p.x, p.y, p.z);

			aiRuntime.setInt (BuiltinVariable.COLLISION, bot.collision == null ? 0 : 1);
			if (bot.collision != null) {
				p = bot.collision.contacts [0].point;
				aiRuntime.setVector (BuiltinVariable.COLLISION_POSITION, p.x, p.y, p.z);
			}

			visibleObjects.Clear ();
			foreach (GameObject x in GameManager.Bots) {
				BotControl ec = x.GetComponent<BotControl> ();
				if (ec.Lives == 0 || !x.activeSelf)
					continue;
				checkVisibility (ec.getCenter (), x);
			}
			foreach (GameObject b in GameManager.Bonuses) {
				if (b.transform.position.y < 0)
					continue;
				checkVisibility (b.transform.position, b);
			}
			foreach (GameObject b in GameManager.DetectableObjects)
				checkVisibility (b.transform.position, b);
			aiRuntime.setInt (BuiltinVariable.OBJ_COUNT, visibleObjects.Count);

			runTime = 0;
			runDelta = 0;
		}
		justStarted = false;
		justSpawned = false;
		if (aiRuntime.run () && controlEnabled) {
			bot.move = aiRuntime.getFloat (BuiltinVariable.MOVE);
			bot.turnCorpus = aiRuntime.getFloat (BuiltinVariable.TURN_CORPUS);
			bot.turnTurret = aiRuntime.getFloat (BuiltinVariable.TURN_TURRET);
			bot.turnGun = aiRuntime.getFloat (BuiltinVariable.TURN_GUN);
			bot.fire1 = aiRuntime.getInt (BuiltinVariable.FIRE1) > 0;
			bot.fire2 = aiRuntime.getInt (BuiltinVariable.FIRE2) > 0;
		}
		runTime += aiRuntime.loadAverage * Time.fixedDeltaTime;
		runDelta += Time.fixedDeltaTime;

		for (int i = 0; i < rays.Length; ++i)
			rays [i].enabled = i < currentRay;
		cameraScope.enabled = GameManager.showLidars;
	}
}
