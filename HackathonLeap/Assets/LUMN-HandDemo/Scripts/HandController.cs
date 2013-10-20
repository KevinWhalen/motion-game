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

//#define DEBUG_LOG
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Leap;
using Screen = UnityEngine.Screen;

/// <summary>
/// Hand controller demo
/// 
/// NOTE: Turn on "Gizmos" in the Game view to see additional debug info (showDebug must be on)
/// </summary> 
public class HandController : MonoBehaviour 
{
	enum FingerOrdinal
	{
		Pinky,
		Ring,
		Middle,
		Index,
		Thumb
	}
	static readonly int kFingerCount = System.Enum.GetValues(typeof(FingerOrdinal)).Length;
	static readonly float kRotationalSmoothingSpeed = 4f;
	
	public bool showDebug = false;
	public bool requireAllFingers = false;
	public bool useOverheadCamera = false;
	public Font font;
	public Texture2D logo;
	
	public Transform overheadCamera;
	public Transform left;
	public Transform right;
		
	public Transform[] leftJoints = new Transform[kFingerCount];
	public Transform[] rightJoints = new Transform[kFingerCount];
	
	private Transform originalLeft;
	private Transform originalRight;
	private Transform originalCamera;
	
	private Hand leftHand;
	private Hand rightHand;
	
	private Finger[] orderedFingersLeft = new Finger[kFingerCount];
	private Finger[] orderedFingersRight = new Finger[kFingerCount];
	
	private GUISkin skin;
	
	void Start () 
	{
		// Set defaults and fix incorrect scaling issue w/ distributed Unity project (units are in mm, not cm)
		Leap.UnityVectorExtension.InputOffset = Vector3.zero;		
		Leap.UnityVectorExtension.InputScale = Vector3.one * 0.001f;
		
		LeapInputEx.HandUpdated += OnHandUpdated;
		LeapInputEx.HandLost += OnHandLost;
		
		// Keep the original transforms to return back to
		GameObject go = new GameObject("Original Left");
		originalLeft = go.transform;
		originalLeft.position = left.position;
		originalLeft.rotation = left.rotation;
		originalLeft.parent = transform;
		
		go = new GameObject("Original Right");
		originalRight = go.transform;
		originalRight.position = right.position;
		originalRight.rotation = right.rotation;		
		originalRight.parent = transform;
		
		go = new GameObject("Original Camera");
		originalCamera = go.transform;
		originalCamera.position = Camera.main.transform.position;
		originalCamera.rotation = Camera.main.transform.rotation;
	}
		
	void OnHandLost(int Id)
	{
		if (leftHand != null && leftHand.Id == Id)
		{
#if DEBUG_LOG
			Debug.Log("LOST LH: " + Id);
#endif
			leftHand = null;
		}
		
		if (rightHand != null && rightHand.Id == Id)
		{
#if DEBUG_LOG
			Debug.Log("LOST RH: " + Id);
#endif
			rightHand = null;
		}
	}
	
	void OnHandUpdated(Hand hand)
	{
#if DEBUG_LOG		
		StringBuilder sb = new StringBuilder("Hand: ");
#endif
		if (requireAllFingers && hand.Fingers.Count < 5)
		{
			return;
		}

#if DEBUG_LOG
		sb.Append(hand.Id.ToString() + " -- ");
#endif
						
		SortedList<float, Finger> sortedFingers = new SortedList<float, Finger>();
		bool isLeftHand = false;
		bool isRightHand = false;
		for (int i = 0; i < hand.Fingers.Count; i++)
		{
			Finger f = hand.Fingers[i];
			if (!f.IsValid)
			{
				continue;
			}
			
			Vector3 palmRightAxis = Vector3.Cross(hand.Direction.ToUnity(), hand.PalmNormal.ToUnity());
			float fingerDot = Vector3.Dot((f.TipPosition - hand.PalmPosition).ToUnity().normalized, palmRightAxis.normalized);
			sortedFingers.Add(fingerDot, f);
			
			// Find the thumb to determine left or right hand
			if (Mathf.Abs(fingerDot) > 0.9f)
			{					
				isLeftHand = fingerDot > 0f;
				isRightHand = fingerDot < 0f;
			}
		}	
		
		if (!isLeftHand && !isRightHand)
		{
			// Couldn't figure out which hand it is this frame, so let's wait another frame
			return;
		}

#if DEBUG_LOG
		sb.Append(sortedFingers.Count + " " + hand.Fingers.Count);
		if (sortedFingers.Count > 5)
		{
			foreach (KeyValuePair<float, Finger> kvp in sortedFingers)
				sb.AppendLine(kvp.Value.Id.ToString());
		}
#endif
			
		if (isLeftHand && (rightHand == null || rightHand.Id != hand.Id))
		{
			leftHand = hand;
		}
		else if (isRightHand && (leftHand == null || leftHand.Id != hand.Id))
		{
			rightHand = hand;
		}
		
		FingerOrdinal fo = FingerOrdinal.Pinky;
		Finger[] orderedFingers = isLeftHand ? orderedFingersLeft : orderedFingersRight;
		foreach (KeyValuePair<float, Finger> kvp in sortedFingers)
		{
			if ((int)fo < orderedFingers.Length)
			{
				int index = isLeftHand ? (int)fo : orderedFingers.Length - (int)fo - 1;
				orderedFingers[index] = kvp.Value;
				fo++;
			}
		}
		
#if DEBUG_LOG
		Debug.Log(sb.ToString());
#endif
	}
	
	void Update()
	{
		// Process the Leap message pump
		LeapInputEx.Update();
		
		Vector3 targetPosition = originalCamera.position;
		Quaternion targetRotation = originalCamera.rotation;
		if (useOverheadCamera)
		{
			// Smoothly move over to the overhead camera if it has been selected
			targetPosition = overheadCamera.position;
			targetRotation = overheadCamera.rotation;
		}
		Transform cam = Camera.main.transform;					
		cam.position = Vector3.Lerp(cam.position, targetPosition, Time.deltaTime);
		cam.rotation = Quaternion.Slerp(cam.rotation, targetRotation, Time.deltaTime * kRotationalSmoothingSpeed);			
	}
	
	void UpdateJoints(Transform[] joints, Finger[] orderedFingers)
	{
		// Seek the finger joints towards the target rotation determined from Leap input
		for (int i = 0; i < joints.Length; i++)
		{			
			Finger finger = orderedFingers[i];
			if (finger != null && finger.IsValid)
			{
				Transform fingerTransform = joints[i];
				Vector3 rotation = fingerTransform.localEulerAngles;
				float angle = Quaternion.FromToRotation(Vector3.forward, finger.Direction.ToUnity()).eulerAngles.x;
				float seekSpeed = Time.deltaTime * kRotationalSmoothingSpeed;
				
				// The thumb has a different rotation axis					
				if (i == (int)FingerOrdinal.Thumb)
				{
					rotation.y = Mathf.LerpAngle(rotation.y, angle, seekSpeed);
				}
				else
				{
					rotation.z = Mathf.LerpAngle(rotation.z, angle, seekSpeed);
				}
				fingerTransform.localEulerAngles = rotation;
			}
		}
	}
	
	void UpdateHand(Hand hand, Transform transform)
	{
		if (showDebug)
		{
			Debug.DrawRay(hand.PalmPosition.ToUnityTranslated(), hand.PalmNormal.ToUnity() * 0.2f, Color.green);
			Debug.DrawRay(hand.PalmPosition.ToUnityTranslated(), hand.Direction.ToUnity() * 0.2f, Color.blue);
			foreach (Finger f in hand.Fingers)
			{
				Vector3 tipPosition = f.TipPosition.ToUnityTranslated();
				Debug.DrawLine(hand.PalmPosition.ToUnityTranslated(), tipPosition);
				Debug.DrawLine(tipPosition, tipPosition - f.Direction.ToUnity() * f.Length * 0.001f, Color.red);
			}
		}
		
		// Smoothly update the orientation and position of the hand
		transform.position = Vector3.Lerp(transform.position, hand.PalmPosition.ToUnityTranslated(), Time.deltaTime);					
		Vector3 normal = -hand.PalmNormal.ToUnity();
		transform.up = Vector3.Slerp(transform.up, normal, Time.deltaTime * kRotationalSmoothingSpeed);
	}
		
	void DockHand(Transform original, Transform current)
	{
		// Return hand to its original position
		current.position = Vector3.Lerp(current.position, original.position, Time.deltaTime);
		current.rotation = Quaternion.Lerp(current.rotation, original.rotation, Time.deltaTime * kRotationalSmoothingSpeed);
	}
	
	void LateUpdate()
	{	
		if (leftHand != null && rightHand != null && (leftHand == rightHand || leftHand.Id == rightHand.Id))
		{
			Debug.LogError("Hands are the same and that just isn't right!");
		}
		
		if (leftHand != null)
		{			
			UpdateHand(leftHand, left);
			UpdateJoints(leftJoints, orderedFingersLeft);
		}
		else
		{
			DockHand(originalLeft, left);
		}
		
		if (rightHand != null)
		{
			UpdateHand(rightHand, right);
			UpdateJoints(rightJoints, orderedFingersRight);
		}
		else
		{
			DockHand(originalRight, right);
		}		
	}
	
	void DrawHand(Finger[] orderedFingers)
	{
		for (int i = 0; i < orderedFingers.Length; i++)
		{
			Finger f = orderedFingers[i];
			if (f != null)
			{				
				Vector3 screenPoint = Camera.main.WorldToScreenPoint(f.TipPosition.ToUnityTranslated());
				GUI.Label(new Rect(screenPoint.x, Screen.height - screenPoint.y, 100f, 100f), 
					string.Format("{0}", (FingerOrdinal)i));
			}
		}
	}
	
	void OnGUI()
	{
		if (showDebug)
		{
			if (leftHand != null)
			{
				DrawHand(orderedFingersLeft);
			}
			
			if (rightHand != null)
			{
				DrawHand(orderedFingersRight);
			}
		}
		
		if (skin == null)
		{
			skin = Object.Instantiate(GUI.skin) as GUISkin;
			skin.font = font;
			skin.toggle.fontSize = 10;		
		}
				
		GUI.skin = skin;
		GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
		showDebug = GUILayout.Toggle(showDebug, " Show Debug");
		requireAllFingers = GUILayout.Toggle(requireAllFingers, " Require all fingers for tracking");		
		useOverheadCamera = GUILayout.Toggle(useOverheadCamera, " Use overhead camera");
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Experiment #1: Hand Demo");
		GUILayout.FlexibleSpace();
		GUILayout.Label(logo, GUIStyle.none);
		GUILayout.EndHorizontal();
		GUILayout.Space(10f);
		GUILayout.EndArea();
	}
}
