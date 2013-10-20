using UnityEngine;
using System.Collections;

public class shoot : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	
	public enum AttackMethod { Fire, Water, Wind, Lightning }
	public Rigidbody projecticle;
	public float speed = 5f;
	
	public void shootAttack(AttackMethod attack, Vector3 startPosition, Quaternion angle)
	{
		Rigidbody shot = (Rigidbody)Instantiate(projecticle, startPosition, angle);
		shot.velocity = transform.forward * speed;
		// You can also acccess other components / scripts of the clone
		//projecticle = shot.GetComponent<Rigidbody>();
	}
}
