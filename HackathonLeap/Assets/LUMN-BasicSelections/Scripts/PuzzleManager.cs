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
using Leap;
using System;
using Object = UnityEngine.Object;
using Screen = UnityEngine.Screen;

public class PuzzleManager : MonoBehaviour 
{	
	public enum SelectionMethod
	{
		Hover,
		Tap,
		Pinch
	}
	
	public float pointableScale = 0.01f;
	public Material pointableMaterial = null;
	public Font font = null;
	public Texture2D logo = null;
	public float hoverTime = 1.5f;
	public float pinchRadius = 0.02f;	
	
	[HideInInspector]
	public GameObject[] pointables = null;
	
	private SelectionMethod selectionMethod = SelectionMethod.Hover;
	private string[] selectionMethods = System.Enum.GetNames(typeof(SelectionMethod));
	private string[] selectionDescriptions = new string[] {
		"Hover over a piece to select; Drag to a new location and hover over to de-select",
		"Tap on a piece once it is highlighted to select; Tap anywhere to de-select",
		"Pinch a piece once it is highlighted to select; Pinch anywhere to de-select",
	};
	private GameObject selected = null;
	private Piece selectedPiece = null;
	private Vector3 pieceTargetOffset = Vector3.zero;
	private Vector3 pieceTargetPosition = Vector3.zero;
	private Rect selectedRect = Rect.MinMaxRect(0f, 0f, 0f, 0f);
	private float pinchDistance = 0f;
	private float pinchCoolOff = 0.5f;
	private float nextPinch = 0f;
	private float selectedTime = 0f;
	private int[] pointableIDs = null;
	private WebCamPictures webCamPics = null;
	
	private GUISkin skin = null;
	private Texture2D whiteTex = null;	
	private bool showInstructions = true;
	private float snapshotCountdown = -1f;
	private float snapshotWaitTime = 5f;
	
	private void Start()
	{
		whiteTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
		whiteTex.SetPixels(new Color[] { Color.white });
		whiteTex.Apply();
		
		// Set defaults and fix incorrect scaling issue w/ distributed Unity project (units are in mm, not cm)
		Leap.UnityVectorExtension.InputOffset = Vector3.zero;		
		Leap.UnityVectorExtension.InputScale = Vector3.one * 0.001f;
		
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPECIRCLE);
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPEKEYTAP);		
		// Not sure if these are working; Experimenting with these to improve the key tap
//		Debug.Log(LeapInputEx.Controller.Config.SetFloat("Gesture.KeyTap.MinDownVelocity", 0f));
//		Debug.Log(LeapInputEx.Controller.Config.SetFloat("Gesture.KeyTap.MinDistance", 0f));		
//		Debug.Log(LeapInputEx.Controller.Config.Save());
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
		LeapInputEx.GestureDetected += OnGestureDetected;
		
		pointables = new GameObject[10];
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
		
		webCamPics = GetComponentInChildren<WebCamPictures>();
	}
	
	private void Update () 
	{
		LeapInputEx.Update();
		
		if (webCamPics.Ready)
		{
			snapshotCountdown -= Time.deltaTime;
			if (!webCamPics.snapshotTaken && snapshotCountdown < 0f)
			{
				webCamPics.TakeSnapshot();			
			}
		}
		
		if (selectedPiece)
		{
			Vector3 position = Vector3.zero;
			int activePointables = 0;
			foreach (GameObject pointable in pointables)
			{
				if (pointable.active)
				{
					position += pointable.transform.position;
					activePointables++;
				}
			}
			
			if (activePointables > 0)
			{
				position = position * 1f / activePointables;
				position.y = selectedPiece.transform.position.y;
				position += pieceTargetOffset;
				if (!collider.bounds.Contains(position))
				{
					position = collider.ClosestPointOnBounds(position);
				}
				pieceTargetPosition = position;
			}
			
			selectedPiece.transform.position = Vector3.Lerp(selectedPiece.transform.position, pieceTargetPosition, Time.deltaTime);			
			
			if (selected)
			{
				// If the object is also selected, then we need to update the outline rect
				GenerateSelectedRect();
			}
		}
		
		if (selected)
		{
			// The switch is a special case where we only want to handle a tap				
			if (selected.GetComponent<Switch>())
			{
				selectedTime = hoverTime;
			}
			else
			{
				switch (selectionMethod)
				{
				case SelectionMethod.Hover:
					selectedTime = Mathf.Clamp(selectedTime + Time.deltaTime, 0f, hoverTime);
					if (selectedTime >= hoverTime)
					{
						TriggerSelected();
					}
					break;
										
				default:
					selectedTime = hoverTime;
					break;
				}
			}
		}
		
		// Pinch is handled separately as a piece can be de-selected even when the pointables are not inside the volume
		if (selectionMethod == SelectionMethod.Pinch)
		{
			selectedTime = hoverTime;
			GameObject firstPointable = null;
			if (Time.time >= nextPinch)
			{
				foreach (GameObject p in pointables)
				{
					if (!p.active)
						continue;
					
					if (firstPointable)
					{
						pinchDistance = Vector3.Distance(firstPointable.transform.position, p.transform.position);
						if (pinchDistance <= pinchRadius)
						{
							if (selected)
							{
								TriggerSelected();
							}
							else if (selectedPiece)
							{
								// Allow for a selected piece to be de-selected even if we are not inside the collision volume
								selectedPiece = null;
							}
							nextPinch = Time.time + pinchCoolOff;
						}
						break;
					}
					else
					{
						firstPointable = p;
					}
				}
			}
		}	
	}	
	
	private void TriggerSelected()
	{
		// De-select the selected piece if this new selection is the same one
		if (selectedPiece && selectedPiece.gameObject == selected)
		{
			selectedPiece = null;
		}
		else
		{
			selectedPiece = selected.GetComponent<Piece>();
			if (selectedPiece)
			{
				// All pointables are averaged, since we don't know which pointable is necessarily the "right" one to
				// track in the case of multiple fingers
				int activePointables = 0;
				Vector3 position = Vector3.zero;
				foreach (GameObject pointable in pointables)
				{
					if (pointable.active)
					{
						position += pointable.transform.position;
						activePointables++;
					}
				}
			
				if (activePointables > 0)
				{
					position = position * 1f / activePointables;
					position.y = selectedPiece.transform.position.y;
				}
				
				// Calculate an offset from the lower-left corner of the piece, so that the offset is preserved when
				// tracking the finger; Otherwise it will snap to the lower-left corner
				if (selectedPiece.collider.bounds.Contains(position))
				{
					pieceTargetOffset = selectedPiece.transform.position - position;
				}
			}
			else
			{
				selected.SendMessage("OnSelected");
			}
		}
		
		selected = null;		
	}
	
	private void EnteredTriggerVolume(GameObject go)
	{
		// Only allow selection after the image has been scattered
		if (!webCamPics.scattered)
			return;

		// Only set a selection if we don't have a piece selected (i.e. switch) or if this object is the same
		// as the piece that is already selected (necessary for de-selection)
		if (!selectedPiece || selectedPiece.gameObject == go)
		{
			selected = go;
			selectedTime = 0f;
			GenerateSelectedRect();
		}
	}
	
	private void ExitedTriggerVolume(GameObject go)
	{
		if (selected == go)
		{
			selected = null;
		}
	}
	
	private void OnGestureDetected(GestureList gestures)
	{
		foreach (Gesture g in gestures)
		{
			switch (g.Type)
			{
			case Gesture.GestureType.TYPECIRCLE:
				if (g.State == Gesture.GestureState.STATESTOP)
				{
					if (showInstructions)
					{
						StartCoroutine(webCamPics.StartPlayback());
						snapshotCountdown = snapshotWaitTime;
						LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPECIRCLE, false);
						showInstructions = false;
					}
				}
				break;
				
			case Gesture.GestureType.TYPESWIPE:
				if (g.State == Gesture.GestureState.STATESTOP)
				{
					if (webCamPics.snapshotTaken && !webCamPics.scattered)
					{
						StartCoroutine(webCamPics.Scatter());
					}
				}
				break;
				
			case Gesture.GestureType.TYPEKEYTAP:
				if ((selected && selected.GetComponent<Switch>())
					|| (selectionMethod == SelectionMethod.Tap && g.State == Gesture.GestureState.STATESTOP))
				{
					if (selected)
					{
						TriggerSelected();
					}
					else if (selectedPiece)
					{
						// Allow for a selected piece to be de-selected even if we are not inside the collision volume
						selectedPiece = null;
					}
				}
				break;
			}
		}
	}
	
	private GameObject CreatePointable(Transform parent, int index)
	{
		GameObject pointable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		pointable.AddComponent<Rigidbody>();
		pointable.rigidbody.useGravity = false;
		pointable.rigidbody.isKinematic = true;
		pointable.transform.localScale = Vector3.one * pointableScale;
		pointable.transform.parent = parent;
		pointable.renderer.sharedMaterial = pointableMaterial;
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
	
	private void updatePointable( Leap.Pointable pointable, GameObject pointableObject )
	{		
		pointableObject.active = pointable.IsValid;
		
		if ( pointable.IsValid )
		{
			Vector3 vPointableDir = pointable.Direction.ToUnity();
			Vector3 vPointablePos = pointable.TipPosition.ToUnityTranslated();

			Debug.DrawRay(vPointablePos, vPointableDir * 0.1f, Color.red);
									
			pointableObject.transform.position = vPointablePos;
			pointableObject.transform.localRotation = Quaternion.FromToRotation( Vector3.forward, vPointableDir );					
		} 
	}
		
	private void DrawOutline(Rect selectedRect, float t, float borderWidth, Color color)
	{
		GUI.color = color;
		
		// Calculate the lerp value for each section using the actual circumference traversed in a period of time
		float circumference = 2f * (selectedRect.width + selectedRect.height);		
		float tCircumference = t * circumference;
		
		float traceSum = selectedRect.width;
		float section = Mathf.Min(tCircumference, traceSum) / selectedRect.width;
		GUI.DrawTexture(new Rect(selectedRect.xMin, selectedRect.yMin, 
			Mathf.Lerp(0f, selectedRect.width, section), borderWidth), whiteTex);
		
		float tracePrev = traceSum;
		traceSum += selectedRect.height;
		section = (Mathf.Min(tCircumference, traceSum) - tracePrev) / selectedRect.height;
		GUI.DrawTexture(new Rect(selectedRect.xMax - borderWidth, selectedRect.yMin, borderWidth, 
			Mathf.Lerp(0f, selectedRect.height, section)), whiteTex);

		tracePrev = traceSum;
		traceSum += selectedRect.width;
		section = (Mathf.Min(tCircumference, traceSum) - tracePrev) / selectedRect.width;
		GUI.DrawTexture(new Rect(selectedRect.xMax, selectedRect.yMax - borderWidth, 
			-Mathf.Lerp(0f, selectedRect.width, section), borderWidth), whiteTex);

		tracePrev = traceSum;
		traceSum += selectedRect.height;
		section = (Mathf.Min(tCircumference, traceSum) - tracePrev) / selectedRect.height;
		GUI.DrawTexture(new Rect(selectedRect.xMin, selectedRect.yMax, borderWidth,
			-Mathf.Lerp(0f, selectedRect.height, section)), whiteTex);

		GUI.color = Color.white;	
	}
	
	private void OnGUI()
	{
		if (skin == null)
		{
			skin = Object.Instantiate(GUI.skin) as GUISkin;
			skin.font = font;
			skin.toggle.fontSize = 10;		
		}
				
		GUI.skin = skin;		
		
		if (selected)
		{
			DrawOutline(selectedRect, selectedTime / hoverTime, 4f, Color.yellow);						
		}
		
		Rect fullScreenRect = new Rect(0, 0, Screen.width, Screen.height);
		
		GUILayout.FlexibleSpace();
		Rect backgroundRect = fullScreenRect;
		backgroundRect.y = Screen.height - 50f;
		GUI.color = new Color(1f, 1f, 1f, 0.25f);
		GUI.DrawTexture(backgroundRect, whiteTex);
		GUI.color = Color.white;
		
		GUILayout.BeginArea(fullScreenRect);
		selectionMethod = (SelectionMethod)GUILayout.Toolbar((int)selectionMethod, selectionMethods);
		GUILayout.Label(selectionDescriptions[(int)selectionMethod]);
//		GUILayout.Label(pinchDistance.ToString());
				
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.Space(10f);
		GUILayout.Label("Experiment #4: Basic Selections");
		GUILayout.FlexibleSpace();
		GUILayout.Label(logo, GUIStyle.none);
		GUILayout.Space(10f);
		GUILayout.EndHorizontal();
		GUILayout.Space(10f);
		GUILayout.EndArea();	
		
		if (snapshotCountdown > 0f)
		{
			GUIStyle s = new GUIStyle();
			s.fontSize = 50;
			
			GUILayout.BeginArea(fullScreenRect);
			GUILayout.FlexibleSpace();
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(Mathf.CeilToInt(snapshotCountdown).ToString(), s);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
						
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();			
		}
		
		if (showInstructions)
		{
			GUI.color = new Color(0f, 0f, 0f, 0.5f);
			GUI.DrawTexture(fullScreenRect, whiteTex);
			
			GUI.color = Color.white;
			GUILayout.BeginArea(fullScreenRect);
			GUILayout.FlexibleSpace();
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Draw a circle to take a picture\nAfterwards, swipe to scatter");
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
						
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();
		}
	}
	
	private void GenerateSelectedRect()
	{
		if (selected)
		{
			Bounds? selectedBounds = null;
		
			// Collect the bounds of all of the meshes for this object in local space as an optimization,
			// since we can then transform that bounding box by the selected object's position and orientation.
			Renderer[] renderers = selected.GetComponentsInChildren<Renderer>();
			foreach (Renderer r in renderers)
			{
				MeshFilter mf = r.GetComponent<MeshFilter>();
				if (mf.sharedMesh)
				{
					if (!selectedBounds.HasValue)
						selectedBounds = mf.sharedMesh.bounds;
					else
						selectedBounds.Value.Encapsulate(mf.sharedMesh.bounds);
				}
			}

			// Collect all corners of the AABB
			Vector3[] corners = new Vector3[8];
			Vector3 min = selectedBounds.Value.min;
			Vector3 size = selectedBounds.Value.size;
			corners[0] = min;
			corners[1] = min + Vector3.right * size.x;
			corners[2] = min + Vector3.forward * size.z;
			corners[3] = min + Vector3.right * size.x + Vector3.forward * size.z;
			corners[4] = min + Vector3.up * size.y;
			corners[5] = min + Vector3.right * size.x + Vector3.up * size.y;
			corners[6] = min + Vector3.forward * size.z + Vector3.up * size.y;
			corners[7] = min + size;
			
			// Find the min and max coords in screen space by projecting each of the corners
			selectedRect.xMin = Screen.width;
			selectedRect.xMax = 0;
			selectedRect.yMin = Screen.height;
			selectedRect.yMax = 0;
			
			selectedRect.xMin = float.MaxValue;
			selectedRect.xMax = float.MinValue;
			selectedRect.yMin = float.MaxValue;
			selectedRect.yMax = float.MinValue;
			
			Vector3[] screenPts = new Vector3[8];
			for (int i = 0; i < corners.Length; i++)
			{
				Vector3 screenPt = Camera.main.WorldToScreenPoint(selected.transform.TransformPoint(corners[i]));
				screenPts[i] = screenPt;
				if (screenPt.x < selectedRect.xMin)
				{
					selectedRect.xMin = screenPt.x;
					//Debug.Log("xMin " + i.ToString());
				}
			
				if (screenPt.y < selectedRect.yMin)
				{
					selectedRect.yMin = screenPt.y;
					//Debug.Log("yMin " + i.ToString());
				}
				
				if (screenPt.x > selectedRect.xMax)
				{
					selectedRect.xMax = screenPt.x;
					//Debug.Log("xMax " + i.ToString());
				}
					
				
				if (screenPt.y > selectedRect.yMax)
				{
					selectedRect.yMax = screenPt.y;
					//Debug.Log("yMax " + i.ToString());
				}
					
			}		
		
			// Flip for GUI coords				
			selectedRect.yMin = Screen.height - selectedRect.yMin;
			selectedRect.yMax = Screen.height - selectedRect.yMax;
			float swap = selectedRect.yMin;
			selectedRect.yMin = selectedRect.yMax;
			selectedRect.yMax = swap;
		}
	}
}