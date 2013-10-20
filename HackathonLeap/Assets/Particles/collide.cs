using UnityEngine;
using System.Collections;

public class collide : MonoBehaviour {
	/*
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}*/
	
	void OnCollisionEnter(Collision c)
	{
		foreach (ContactPoint contact in c.contacts) {
			if(contact.otherCollider.gameObject.name == "Caveworm")
			{
				/*
				Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
				Vector3 pos = contact.point;
				Instantiate(explosionPrefab, pos, rot) as Transform;*/
				
				contact.otherCollider.renderer.material.color = new Color(1.0f, 0.0f, 0.0f);
				
				Destroy(contact.otherCollider.gameObject);
			}

		}
		
		// Remove shot after it collides with an enemey
        Destroy(gameObject);
    }
}
