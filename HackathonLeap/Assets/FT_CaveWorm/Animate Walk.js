#pragma strict

function Start () {

}

function Update () {
	if(Input.GetKeyDown("r")){
		animation.Play("walk",PlayMode.StopAll);
		rigidbody.AddForce(0,0,-30);
	}

}