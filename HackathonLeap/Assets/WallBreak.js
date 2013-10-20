#pragma strict

function Start () {

}

function Update () {
	if(Input.GetKeyDown("b")){
		animation.Play("take 001",PlayMode.StopAll);
	}

}