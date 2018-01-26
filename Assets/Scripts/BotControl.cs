using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotControl : MonoBehaviour {

	public GameObject gun;
	public GameObject turret;
	public GameObject gunEnd;

	public GameObject wheelPrefab;
	public Vector3 wheelPosition;
	public float wheelRadius;
	public Vector3 centerOfMass;

	public const float maxSteerAngle = 25;
	public const float maxMotorTorque = 20;
	public const float turretRotationSpeed = 180;
	public const float gunRotationSpeed = 90;
	public const float bulletSpeed = 45;
	public const int maxLives = 10;
	public const int maxBullets = 50;
	public const int maxRockets = 5;

	public GameObject gravelParticlesPrefab;
	public Rigidbody bulletPrefab;
	public GameObject explosionPrefab;
	public GameObject rocketPrefab;

	public AudioClip motorSound;
	public AudioClip fire1Sound;
	public AudioClip fire2Sound;
	public AudioClip collisionSound;
	public AudioClip brakeSound;

	public float move;
	public float turnCorpus;
	public float turnTurret;
	public float turnGun;
	public bool fire1;
	public bool fire2;

	public float TurretAngle { get; private set; }
	public float GunAngle { get; private set; }

	public Collision collision { get; private set; }
	private float centerOffset;
	public Vector3 getCenter() {
		return transform.position + centerOffset * transform.up;
	}

	public int Lives { get; private set; }
	public int Bullets { get; private set; }
	public int Rockets { get; private set; }
	public int Score { get; private set; }
	public int Kills { get; private set; }
	public int Deaths { get; private set; }
	public void bulletHit(BotControl source) {
		if (dieTimer >= 0)
			return;
		source.Score += 5;
		Lives -= 1;
		if (Lives <= 0) {
			source.Score += 10;
			source.Kills++;
			die();
		}
	}
	public void rocketHit(BotControl source) {
		if (dieTimer >= 0)
			return;
		source.Score += 15;
		source.Kills++;
		Lives = 0;
		die();
	}
	public float liveTimer { get; private set; }
	private float dieTimer;
	public float dieTime;
	private void die() {
		Score -= 10;
		Deaths++;
		if (Score < 0)
			Score = 0;
		dieTimer = 0;
		for (int i=0; i < 4; ++i) wheels [i].SetActive (false);
		Vector3 pos = new Vector3(Random.Range(-wheelPosition.x, wheelPosition.x), 0, Random.Range(-wheelPosition.z, wheelPosition.z));
		body.AddForceAtPosition (new Vector3(0, 20000, 0), transform.TransformPoint(pos));
		dieTime = Time.timeSinceLevelLoad;
		Instantiate (explosionPrefab, transform.position, Quaternion.identity);
	}
	public void spawn() {
		body.MoveRotation (Quaternion.Euler (0, Random.Range (0f, 360f), 0));
		body.velocity = Vector3.zero;
		body.angularVelocity = Vector3.zero;
		liveTimer = 0;
		dieTimer = -1;
		TurretAngle = 0;
		GunAngle = 0;
		collision = null;
		fire1Wait = fire2Wait = 0;
		lastVelocity = 0;
		gunAngleError = 0;
		exploded = false;
		for (int i=0; i < 4; ++i) wheels [i].SetActive (true);
		foreach (Transform t in gameObject.transform)
			if (t.tag == "Explosion")
				Destroy (t);
	}
	private void respawn() {
		GameManager.addToSpawnQueue (gameObject);
	}

	private Rigidbody body;
	private WheelCollider wheelBL;
	private WheelCollider wheelBR;
	private WheelCollider wheelFL;
	private WheelCollider wheelFR;
	private ParticleSystem bl_particles;
	private ParticleSystem br_particles;
	private AudioSource motorAudioSource;
	private AudioSource audioSource;

	private float fire1Wait, fire2Wait;
	private float lastVelocity;
	private float gunAngleError;

	private List<GameObject> wheels = new List<GameObject> ();
	private WheelCollider addWheel(int x, int z) {
		GameObject wheelBase = new GameObject ();
		wheels.Add (wheelBase);
		wheelBase.transform.parent = transform;
		wheelBase.transform.localPosition = new Vector3 (wheelPosition.x * x, wheelPosition.y, wheelPosition.z * z);
		BoxCollider bc = wheelBase.AddComponent<BoxCollider> ();
		bc.size = new Vector3 (wheelRadius, wheelRadius, wheelRadius*2);
		bc.center = new Vector3 (-wheelRadius * x * 0.5f, wheelRadius*0.5f, 0);

		WheelCollider collider = wheelBase.AddComponent<WheelCollider> ();
		collider.mass = 4;
		collider.radius = wheelRadius;
		collider.suspensionDistance = 0.1f;
		collider.center = new Vector3 (0, 0.05f, 0);
		var spring = new JointSpring ();
		spring.spring = 4000;
		spring.damper = 1000;
		spring.targetPosition = 0.5f;
		collider.suspensionSpring = spring;

		GameObject wheel = Instantiate(wheelPrefab, wheelBase.transform);
		WheelScript ws = wheel.AddComponent<WheelScript> ();
		ws.wheelCollider = collider;
		wheel.transform.localPosition = Vector3.zero;
		if (x < 0)
			wheel.transform.localScale = new Vector3 (-1, 1, 1);
		if (z < 0)
			Instantiate (gravelParticlesPrefab, wheelBase.transform);

		return collider;
	}

	// Use this for initialization
	void Start () {
		// Prepare wheels
		wheelFR = addWheel(1, 1);
		wheelFL = addWheel(-1, 1);
		wheelBR = addWheel(1, -1);
		wheelBL = addWheel(-1, -1);
		bl_particles = wheelBL.GetComponentInChildren<ParticleSystem> ();
		br_particles = wheelBR.GetComponentInChildren<ParticleSystem> ();

		// Prepare corpus
		body = GetComponent<Rigidbody>();
		body.centerOfMass = centerOfMass;
		centerOffset = (transform.InverseTransformPoint (gun.transform.position).y + wheelPosition.y) * 0.5f;

		// Prepare sounds
		audioSource = gameObject.AddComponent<AudioSource> ();
		audioSource.spatialBlend = 1f;
		audioSource.volume = 0.5f;
		motorAudioSource = gameObject.AddComponent<AudioSource> ();
		motorAudioSource.spatialBlend = 1f;
		motorAudioSource.clip = motorSound;
		motorAudioSource.loop = true;
		motorAudioSource.volume = 0;
		motorAudioSource.Play ();

		// Input variables initialization
		move = 0;
		turnCorpus = 0;
		turnTurret = 0;
		turnGun = 0;
		fire1 = false;
		fire2 = false;

		// Internal variables initialization
		Lives = maxLives;
		Bullets = maxBullets;
		Rockets = maxRockets;
		Score = 0;
		Kills = 0;
		Deaths = 0;
		dieTime = -100;
		respawn ();
	}

	public void bulletBonus() {
		Bullets = Mathf.Min (Bullets + 20, maxBullets);
	}
	public void rocketBonus() {
		Rockets = Mathf.Min (Rockets + 1, maxRockets);
	}
	public void repairBonus() {
		Lives = maxLives;
	}

	public bool isDying() { return dieTimer >= 0; }

	bool exploded = false;
	void FixedUpdate () {
		if (dieTimer >= 0) {
			dieTimer += Time.fixedDeltaTime;
			if (dieTimer > 2 && !exploded) {
				Instantiate (explosionPrefab, transform.position, Quaternion.identity);
				exploded = true;
			}
			if (dieTimer > 2.1) {
				Lives = maxLives;
				respawn ();
			}
			return;
		}
		liveTimer += Time.fixedDeltaTime;
		if (liveTimer < 1) {
			move = 1;
			turnCorpus = 0;
			turnTurret = 0;
			turnGun = 0;
			fire1 = false;
			fire2 = false;
		} else {
			TurretAngle += Mathf.Clamp (turnTurret, -1, 1) * turretRotationSpeed * Time.fixedDeltaTime;
			GunAngle += Mathf.Clamp (turnGun, -1, 1) * gunRotationSpeed * Time.fixedDeltaTime;
			GunAngle = Mathf.Clamp (GunAngle, -10, 30);
			while (TurretAngle < 0)
				TurretAngle += 360;
			while (TurretAngle >= 360)
				TurretAngle -= 360;
			turret.transform.localRotation = Quaternion.Euler (0, TurretAngle, 0);
			gun.transform.localRotation = Quaternion.Euler (-GunAngle - gunAngleError, 0, 0);
			gunAngleError *= 0.9f;
		}
		float cm = Mathf.Clamp (move, -1, 1);
		wheelBL.motorTorque = cm * maxMotorTorque;
		wheelBR.motorTorque = cm * maxMotorTorque;
		float steerAngle = Mathf.Clamp (turnCorpus, -1, 1) * maxSteerAngle;
		float max_delta = Time.fixedDeltaTime * 180;
		steerAngle = Mathf.Clamp (steerAngle, wheelFL.steerAngle - max_delta, wheelFL.steerAngle + max_delta);
		wheelFL.steerAngle = steerAngle;
		wheelFR.steerAngle = steerAngle;
		if (fire1Wait > 0)
			fire1Wait -= Time.fixedDeltaTime;
		if (fire2Wait > 0)
			fire2Wait -= Time.fixedDeltaTime;
		motorAudioSource.volume = Mathf.Abs (move) * audioSource.volume;
		if (wheelBL.isGrounded || wheelBR.isGrounded || wheelFL.isGrounded || wheelBR.isGrounded) {
			float rv = Mathf.Min (wheelBL.rpm * cm, wheelBR.rpm * cm);
			if (rv < -50)
				motorAudioSource.volume /= 3;
			if (body.velocity.magnitude > 2f && rv < -150)
				audioSource.PlayOneShot (brakeSound, 0.15f);
		}
		if (fire1 && fire1Wait <= 0 && Bullets > 0) {
			Bullets--;
			fire1Wait = 0.2f;
			audioSource.PlayOneShot (fire1Sound);
			Rigidbody bullet = Instantiate (bulletPrefab, gunEnd.transform.position, gun.transform.rotation);
			bullet.velocity = gun.transform.forward * bulletSpeed + body.velocity;
			BulletScript bs = bullet.GetComponent<BulletScript> ();
			bs.source = body;
			GameObject smoke = Instantiate (bs.startEffectPrefab, gunEnd.transform.position, gun.transform.rotation, transform);
			Destroy (smoke, 1);
			body.AddForceAtPosition (gun.transform.forward * bulletSpeed * -10f, gun.transform.position);
			gunAngleError += Random.Range (0, (body.velocity.magnitude + 3f));
		}
		if (fire2 && fire2Wait <= 0 && Rockets > 0) {
			Rockets--;
			fire2Wait = 1.0f;
			audioSource.PlayOneShot (fire2Sound);
			GameObject rocket = Instantiate (rocketPrefab, gun.transform.position, turret.transform.rotation);
			rocket.GetComponent<RocketScript> ().source = gameObject;
		}
		float velocity = Vector3.Dot (body.velocity, transform.forward);
		float acceleration = (velocity - lastVelocity) / Time.fixedDeltaTime;
		if (wheelBL.isGrounded && acceleration > 1f && move > 0)
			bl_particles.Play ();
		else
			bl_particles.Stop ();
		if (wheelBR.isGrounded && acceleration > 1f && move > 0)
			br_particles.Play ();
		else
			br_particles.Stop ();
		lastVelocity =  velocity;

		if (transform.position.y < -100)
			respawn ();
	}

	void OnCollisionEnter(Collision collision) {
		if (dieTimer >= 0)
			return;
		if (collision.relativeVelocity.magnitude > 1)
			audioSource.PlayOneShot (collisionSound);
		if (collision.rigidbody != null) {
			BotControl b = collision.rigidbody.GetComponent<BotControl> ();
			if (b != null && b.dieTimer >= 0) {
				Lives -= 1;
				if (Lives <=0) die ();
			}
		}
	}

	void OnCollisionStay(Collision collision) {
		this.collision = collision;
		if (dieTimer > 0.5f && dieTimer < 2f)
			dieTimer = 2f;
	}

	void OnCollisionExit(Collision collision) {
		this.collision = null;
	}

	void OnGUI() {
		if (GameManager.pause)
			return;
		Vector3 screenPosition = Camera.main.WorldToScreenPoint(gun.transform.position);
		Vector3 cameraRelative = Camera.main.transform.InverseTransformPoint(transform.position);
		if (cameraRelative.z > 0)
		{
			Rect position = new Rect(screenPosition.x, Screen.height - screenPosition.y - 20, 100f, 20f);
			GUI.Label(position, gameObject.name);
		}
	}
}
