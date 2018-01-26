using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketScript : MonoBehaviour {

	private float lifeTimer;
	public GameObject source;
	private GameObject enemy;
	private Vector3 direction;
	public GameObject explosionPrefab;

	private const float rocketSpeed = 7.0f;

	// Use this for initialization
	void Start () {
		lifeTimer = 3;
		enemy = null;
		direction = (transform.forward + transform.up * 0.1f).normalized;
		GameManager.DetectableObjects.Add (gameObject);
	}

	void OnDestroy() {
		GameManager.DetectableObjects.Remove (gameObject);
	}

	private void findEnemy() {
		enemy = null;
		float dist = 0;
		foreach (GameObject x in GameManager.Bots) {
			BotControl ec = x.GetComponent<BotControl> ();
			if (ec.Lives == 0 || !x.activeSelf)
				continue;
			Vector3 direction = x.transform.position - transform.position;
			float d = direction.magnitude;
			if (d > 50)
				continue;
			RaycastHit hitInfo;
			Physics.Raycast (transform.position, direction, out hitInfo, d);
			if (hitInfo.collider == null || hitInfo.collider.attachedRigidbody == null)
				continue;
			if (hitInfo.collider.attachedRigidbody.gameObject != x)
				continue;
			if (x != source && (enemy == null || d < dist)) {
				enemy = x;
				dist = d;
			}
		}
	}

	public Vector3 getVelocity() {
		return direction * rocketSpeed;
	}

	void FixedUpdate () {
		if (enemy == null || !enemy.activeSelf)
			findEnemy();
		if (enemy != null) {
			Vector3 dir = enemy.transform.position - transform.position;
			dir.y += 0.65f;
			Vector3 espeed = enemy.GetComponent<Rigidbody> ().velocity;
			float rel_speed = (dir.normalized * rocketSpeed - espeed).magnitude;
			dir += espeed * (dir.magnitude / rel_speed);
			dir.Normalize();
			direction += dir * 0.1f;
			direction.Normalize ();
		}
		transform.position += getVelocity() * Time.fixedDeltaTime;
		lifeTimer -= Time.fixedDeltaTime;
		if (lifeTimer <= 0) {
			GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
			Destroy (explosion, 4);
			Destroy (gameObject);
		}
	}

	bool hit = false;
	void OnTriggerEnter(Collider col) {
		if ((col.attachedRigidbody != null && col.attachedRigidbody.gameObject == source) || hit) return;
		hit = true;
		GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
		if (col.attachedRigidbody != null) {
			BotControl bot = col.attachedRigidbody.GetComponent<BotControl> ();
			if (bot != null) bot.rocketHit(source.GetComponent<BotControl>());
		}
		Destroy (explosion, 4);
		Destroy (gameObject);
	}
}
