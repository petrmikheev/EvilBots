using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelScript : MonoBehaviour {

	public WheelCollider wheelCollider;
	private float angle;

	// Use this for initialization
	void Start () {
		angle = 0;
	}
	
	// Update is called once per frame
	void Update () {
		angle += wheelCollider.rpm * Time.deltaTime * (360/60);
		transform.localRotation = Quaternion.Euler(angle, wheelCollider.steerAngle, 0);
	}
}
