using UnityEngine;
using System.Collections;

public class AnimateAll : MonoBehaviour {


public float maxHealth = 100f;
public float health = 100f;
public bool dead  = false; 
public float damage = 25;
public bool isCol = false;	
private BoxCollider wallCol;
private bool attacking;
private Health wallHealth;

	
public void Awake () {
		wallCol = GetComponent<BoxCollider>();
		//wallHealth = new Health();
}

public void Update () {
	if(Input.GetKeyDown("r")){
		animation.Play("walk",PlayMode.StopAll);
		//gameObject.rigidbody.velocity = new Vector3(0, 0, -5);
		Vector3 v; v.x = v.y = 0; v.z = -5;
		gameObject.rigidbody.velocity = v;
	}
		if (isCol == true){
			attack();
		}
		
}

public void OnCollisionEnter(Collision collision){
		isCol = true;
	//animation.Play("attack", PlayMode.StopAll);
}
public void attack (){
		animation.Play("attack", PlayMode.StopAll);
		
		wallHealth.TakeDamage(damage);
	}
}
