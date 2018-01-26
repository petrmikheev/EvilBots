using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusScript : MonoBehaviour {

	private Vector3 basePosition;
	private float timeout;
	private AudioSource sound;

	// Use this for initialization
	void Start () {
		basePosition = transform.position;
		timeout = 0;
		sound = GetComponent<AudioSource> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (timeout <= 0) {
			transform.position = basePosition + new Vector3 (0, Mathf.Sin (Time.timeSinceLevelLoad * 3) * 0.2f + 0.55f, 0);
			transform.rotation = Quaternion.Euler (new Vector3 (0, Time.timeSinceLevelLoad * 100, 0));
		} else
			timeout -= Time.deltaTime;
	}

	void OnTriggerEnter(Collider col) {
		Rigidbody body = col.attachedRigidbody;
		if (body == null || timeout > 0)
			return;
		BotControl bot = body.GetComponent<BotControl> ();
		if (bot == null || bot.isDying())
			return;
		sound.Play ();
		timeout = 30;
		transform.position = basePosition - new Vector3 (0, 1, 0);
		if (tag == "BulletBonus")
			bot.bulletBonus ();
		if (tag == "RocketBonus")
			bot.rocketBonus ();
		if (tag == "RepairBonus")
			bot.repairBonus ();
	}
}
