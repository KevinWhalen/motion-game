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
using System;
using System.Collections;
using System.Collections.Generic;
using Leap;
using Object = UnityEngine.Object;
using Screen = UnityEngine.Screen;

public class Pointers : MonoBehaviour 
{
	public bool showPointers = true;
	public Font font;
	public Texture2D logo;
	
	public GameObject pointablesTemplate = null;
	
	[HideInInspector]
	public GameObject[] pointables = null;
	
	private int[] pointableIDs = null;
	private GUISkin skin;
	
	
	// Tap variables
	//public float pointableScale = 0.01f;  //    <====================    scaling
	//public float hoverTime = 0.1f; // was 1.5f
	//public Material pointableMaterial = null;
	
	//[HideInInspector]
	//private SelectionMethod selectionMethod = SelectionMethod.Hover;
	//private string[] selectionMethods = System.Enum.GetNames(typeof(SelectionMethod));
	/*private string[] selectionDescriptions = new string[] {
		"Hover over a piece to select; Drag to a new location and hover over to de-select",
		"Tap on a piece once it is highlighted to select; Tap anywhere to de-select",
		"Pinch a piece once it is highlighted to select; Pinch anywhere to de-select",
	};*/
	//private GameObject selected = null;
	//private float selectedTime = 0f;
	
	Vector3 position = Vector3.zero;
	
	shoot s = new shoot();
	
	public void Start () 
	{
		// Set defaults and fix incorrect scaling issue with distributed 
		// Unity project (units are in mm, not cm)
		Leap.UnityVectorExtension.InputOffset = Vector3.zero;		
		//Leap.UnityVectorExtension.InputScale = Vector3.one * 0.001f;
		Leap.UnityVectorExtension.InputScale = Vector3.one * 0.004f;
		
		// (Gesture.GestureType.TYPECIRCLE Gesture.GestureType.TYPESWIPE
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPEKEYTAP);
		
		LeapInputEx.GestureDetected += OnGestureDetected; //asdf
		
		//pointables = new GameObject[10];
		pointables = new GameObject[1];
		pointableIDs = new int[pointables.Length];
		for( int i = 0; i < pointables.Length; i++ )
		{
			pointableIDs[i] = -1;	
			pointables[i] = CreatePointable(transform, i);
		}
		
		LeapInputEx.PointableFound += OnPointableFound;
		LeapInputEx.PointableLost += OnPointableLost;
		LeapInputEx.PointableUpdated += OnPointableUpdated;
		
		foreach (GameObject pointable in pointables)
		{
			updatePointable(Leap.Pointable.Invalid, pointable);
		}
	}
		
	public void Update()
	{
		LeapInputEx.Update();
		
		//if (== Gesture.GestureType.TYPEKEYTAP)
		//if (pointables[0].activeSelf){
		//	position = pointables[0].transform.position;
		//}
		
		/*
        InteractionBox interactionBox = LeapInputEx.Frame.InteractionBox;

        foreach(Pointable pointable in LeapInputEx.Frame.Pointables)
        {
			Vector normalizedPosition = interactionBox.NormalizePoint(pointable.StabilizedTipPosition);
            float tx = normalizedPosition.x * 1000;//windowWidth;
            //float ty = windowHeight - normalizedPosition.y * windowHeight;
			float ty = 700 - normalizedPosition.y * 700;
			
            //int alpha = 255;
            if(pointable.TouchDistance > 0 && pointable.TouchZone != Pointable.Zone.ZONENONE)
            {
                //alpha = 255 - (int)(255 * pointable.TouchDistance);
                //touchIndicator.Color = Color.FromArgb((byte)alpha, 0x0, 0xff, 0x0);
            }
            else if(pointable.TouchDistance <= 0)
            {
                //alpha = -(int)(255 * pointable.TouchDistance);
                //touchIndicator.Color = Color.FromArgb((byte)alpha, 0xff, 0x0, 0x0);
            }
		}*/
	}
	
	private void OnGUI()
	{
		/*
		if (skin == null)
		{
			skin = Object.Instantiate(GUI.skin) as GUISkin;
			skin.font = font;
			skin.toggle.fontSize = 10;		
		}
		//GUI.skin = skin;
		//GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
				//showPointers = GUILayout.Toggle(showPointers, " Show Pointers");
			//GUILayout.FlexibleSpace();
			//GUILayout.BeginHorizontal();
			//GUILayout.Label("Experiment #2: Pointers Demo");
			//GUILayout.FlexibleSpace();
			//GUILayout.Label(logo, GUIStyle.none);
			//GUILayout.EndHorizontal();
				//GUILayout.Space(10f); // was 10f
		//GUILayout.EndArea();
		*/
	}
	
	private GameObject CreatePointable(Transform parent, int index)
	{
		GameObject pointable = Instantiate(pointablesTemplate) as GameObject;
		pointable.transform.parent = parent;
		pointable.name = "Pointable " + index;
		
		return pointable;
	}

	void OnPointableUpdated(Pointable p)
	{
		int index = Array.FindIndex(pointableIDs, id => id == p.Id);
		if( index != -1 )
		{
			updatePointable( p, pointables[index] );	
		}
	}
	
	void OnPointableFound( Pointable p )
	{
		int index = Array.FindIndex(pointableIDs, id => id == -1);
		if( index != -1 )
		{
			pointableIDs[index] = p.Id;
			updatePointable( p, pointables[index] );
		}
	}
	
	void OnPointableLost( int lostID )
	{
		int index = Array.FindIndex(pointableIDs, id => id == lostID);
		if( index != -1 )
		{
			updatePointable( Pointable.Invalid, pointables[index] );
			pointableIDs[index] = -1;
		}
	}
	
	void updatePointable( Leap.Pointable pointable, GameObject pointableObject )
	{		
		SetVisible(pointableObject, pointable.IsValid);
		SetCollidable(pointableObject, pointable.IsValid);
		
		if ( pointable.IsValid )
		{
			Vector3 vPointableDir = pointable.Direction.ToUnity();
			Vector3 vPointablePos = pointable.TipPosition.ToUnityTranslated();
						
			pointableObject.transform.localPosition = vPointablePos;
			pointableObject.transform.localRotation = Quaternion.FromToRotation( Vector3.forward, vPointableDir );
		}
	}
		
	void SetCollidable( GameObject obj, bool collidable )
	{
		foreach( Collider component in obj.GetComponents<Collider>() )
			component.enabled = collidable;
	
		foreach( Collider child in obj.GetComponentsInChildren<Collider>() )
			child.enabled = collidable;
	}
	
	void SetVisible( GameObject obj, bool visible )
	{
		foreach( Renderer component in obj.GetComponents<Renderer>() )
			component.enabled = visible && showPointers;
		
		foreach( Renderer child in obj.GetComponentsInChildren<Renderer>() )
			child.enabled = visible && showPointers;
	}
	
	
	
	/*
	private void EnteredTriggerVolume(GameObject go)
	{
		// Trigger a selection
		//selected = go;
		Debug.Log("line ~200. EnteredTriggerVolume");
	}
	
	private void ExitedTriggerVolume(GameObject go)
	{
		if (selected == go)
		{
			selected = null;
		}
		Debug.Log("line ~215. ExitedTriggerVolume");
	}*/
	
	private void OnGestureDetected(GestureList gestures)
	{
		foreach (Gesture g in gestures)
		{
			switch (g.Type)
			{	
			case Gesture.GestureType.TYPEKEYTAP:
				Debug.Log("x: " + position.x.ToString() + " norm: " + position.normalized.x.ToString() + ", " + 
					position.normalized.y.ToString() + ", " + position.normalized.z.ToString());
				
				position = pointables[0].transform.position;
				
				s.shootAttack(shoot.AttackMethod.Fire, position, pointables[0].transform.rotation);
				
				//Instantiate(gameObject.particleSystem);
				
				
				Ray r = Camera.main.ScreenPointToRay(position);
				RaycastHit hit;
				if (Physics.Raycast(r, out hit, 500)){
					Debug.Log(hit.transform.position.ToString() + ", norm: " + hit.normal);
					//Vector3 hitPoint = hit.transform.position + hit.normal;
					//Instantiate(currentBlock, hitPoint, Quaternion.identity);
				}
				
				/*
				if (selectionMethod == SelectionMethod.Tap && g.State == Gesture.GestureState.STATESTOP)
				{
					if (pointables[0].activeSelf){
						position = pointables[0].transform.position;
						Debug.Log("asdf!");
						//if(pointables[0].TouchDistance <= 0){Debug.LogWarning("warning");}
						//Leap.Frame
					}
				}*/
				break;
			default:
				Debug.Log("gesture " + g.Type.ToString() + " detected.");
				break;
			}
		}
	}
	
	/*
	public Rigidbody projecticle;
	public float speed = 5f;
	
	private void shootAttack(AttackMethod attack, Vector3 startPosition, Quaternion angle)
	{
		Rigidbody shot = (Rigidbody)Instantiate(projecticle, startPosition, angle);
		shot.velocity = transform.forward * speed;
		// You can also acccess other components / scripts of the clone
		//shot.GetComponent<MyRocketScript>().DoSomething();
	}
	*/
}
