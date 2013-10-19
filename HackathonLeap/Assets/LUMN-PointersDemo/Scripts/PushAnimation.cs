// Copyright (c) 2013, Luminary Productions Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
//
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the 
//       following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the 
//      following disclaimer in the documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
// USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// Use also subject to the terms of the Leap Motion SDK Agreement available at 
// https://developer.leapmotion.com/sdk_agreement

using UnityEngine;
using System.Collections;

public class PushAnimation : MonoBehaviour 
{	
	public float pushDistance = 0.25f;
	public float goSpeed = 5f;
	public float returnSpeed = 2.5f;
		
	private Vector3 original = Vector3.zero;
	private Vector3 target = Vector3.zero;
	private float speed = 0f;
	
	void Start () 
	{
		original = transform.position;
	}
	
	void Update()
	{
		transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
		
		// Once the object reaches the target it can retract
		if (Vector3.Distance(transform.position, target) < 0.1f)
		{
			target = original;		
			speed = returnSpeed;		
		}
	}
	
	void OnTriggerEnter(Collider other)
	{
		target = original + Vector3.forward * pushDistance;		
		speed = goSpeed;
	}	
}
