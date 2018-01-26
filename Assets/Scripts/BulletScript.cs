using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour {

	public Rigidbody source;
	public GameObject explosionPrefab;
	public GameObject hitExplosionPrefab;
	public GameObject startEffectPrefab;

	private Rigidbody rb;
	private float delta;
	private bool hit;
	private static int bulletId = 0;
	private bool detectable = (bulletId++) % 4 == 0;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody> ();
		delta = 0;
		hit = false;
		if (detectable) GameManager.DetectableObjects.Add (gameObject);
	}

	void OnDestroy() {
		if (detectable) GameManager.DetectableObjects.Remove (gameObject);
	}

	void FixedUpdate () {
		if (transform.position.y < -100) Destroy (gameObject);
		delta = Time.fixedDeltaTime;
	}

	void OnTriggerEnter(Collider col) {
		if (col.attachedRigidbody == source || rb == null || hit) return;
		if (col.attachedRigidbody != null && col.attachedRigidbody.GetComponent<BotControl> () == null)
			return;
		hit = true;
		RaycastHit hitInfo;
		if (Physics.Raycast (rb.position-rb.velocity*delta*2, rb.velocity, out hitInfo)) {
			Rigidbody t = col.attachedRigidbody;
			GameObject explosion = Instantiate(t==null ? explosionPrefab : hitExplosionPrefab,
				hitInfo.point, Quaternion.LookRotation(hitInfo.normal), col.transform);
			if (t != null) {
				t.AddForceAtPosition ((rb.velocity - t.velocity) * 10f, hitInfo.point);
				BotControl bot = t.GetComponent<BotControl> ();
				if (bot != null) bot.bulletHit(source.GetComponent<BotControl>());
			}
			Destroy (explosion, 4);
		} else
			Debug.Log ("[Bullet] no point for collision");
		Destroy (gameObject);
	}
}
