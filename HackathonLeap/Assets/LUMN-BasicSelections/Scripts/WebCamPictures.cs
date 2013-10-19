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

public class WebCamPictures : MonoBehaviour 
{
	public bool Ready
	{
		get
		{
			return webcamTexture && webcamTexture.isPlaying;
		}
	}
	
	//Define the size of the puzzle in rows and columns
	public Vector2 size = new Vector2(6f, 8f);
	public GameObject planePrefab = null;
	public bool snapshotTaken = false;
	public bool scattered = false;
		
	private WebCamTexture webcamTexture = null;
	private Texture2D snapshot = null;
	private bool scattering = false;
	
	private GameObject[] pieces; // Puzzle pieces
	private Transform[] targetLocations;
	
	public IEnumerator StartPlayback() 
	{
		if (!webcamTexture)
		{
			// Check if the user have given authorization for using the web camera
			yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
			if (Application.HasUserAuthorization(UserAuthorization.WebCam))
	        {
				//Get and activate the WebCam
				WebCamDevice[] webcams = WebCamTexture.devices;
				webcamTexture = new WebCamTexture(webcams[0].name);
				webcamTexture.Play();
				Vector3 localScale = transform.localScale;
				localScale.z = localScale.x * webcamTexture.height / (float)webcamTexture.width;
				transform.localScale = localScale;			
				renderer.material.mainTexture = webcamTexture;
			} 
			else
			{
				Debug.LogWarning("User did not give an authorizations to use a WebCam ");
			}	
		}
	}
	
	public void TakeSnapshot()
	{
		
		//Create a Texture2D as a contet for the sanpshot
		snapshot = new Texture2D(webcamTexture.width, webcamTexture.height);
		snapshot.SetPixels(webcamTexture.GetPixels());
		snapshot.Apply();
		webcamTexture.Stop();
		renderer.material.mainTexture = snapshot;
		renderer.enabled = false;		
				
		CreatePuzzle();

		snapshotTaken = true;
	}
	
	public IEnumerator Scatter()
	{
		if (!scattering)
		{
			scattering = true;
			
			Vector3[] targets = new Vector3[pieces.Length];
			
			for (int i = 0; i < pieces.Length; i++)
			{
				Transform trans = pieces[i].transform;
				targets[i] = new Vector3(Random.Range(-0.2f, 0.8f), trans.localPosition.y, Random.Range(-0f, 1.5f));
			}
			
			float scatterTime = 3f;
			float timeLeft = scatterTime + 1f; // Give a little buffer, since deltaTime isn't exactly correct for a co-routine
			Vector3[] velocities = new Vector3[pieces.Length];
			while (timeLeft > 0f)
			{
				for (int i = 0; i < pieces.Length; i++)
				{
					pieces[i].transform.localPosition = Vector3.SmoothDamp(pieces[i].transform.localPosition, 
						targets[i], ref velocities[i], scatterTime);
				}
				
				timeLeft -= Time.deltaTime;
				yield return null;				
			}
			
			scattered = true;
		}
	}
	
	private void CreatePuzzle()
	{	
		//Instantiate an array pieces  
		pieces = new GameObject[(int)size.x*(int)size.y];
		
		//Normalize size
		Vector2 s = new Vector2(1f/size.x, 1f/size.y);
		
		int index = 0;
		
		for(int i = 0; i < size.x; i++)
		{
			for(int j = 0; j < size.y; j++)
			{
				//Create a piece of the puzzle
				GameObject go = Instantiate(planePrefab) as GameObject;
				go.name = "Piece["+i.ToString()+"]["+j.ToString()+"]";
				
				// Set up collider
				Destroy(go.collider);
				BoxCollider box = go.AddComponent<BoxCollider>();
				box.isTrigger = true;
				Vector3 boxSize = box.size;
				boxSize.y = 1f;
				box.size = boxSize;				
				go.AddComponent<Piece>();
								
				// Set up parent, size, position, and rotation
				go.transform.parent = transform;
				go.transform.localScale = new Vector3(s.x, 1f, s.y);
				go.transform.localRotation = Quaternion.identity;
				go.transform.position = transform.TransformPoint(Vector3.right * i * s.x + Vector3.forward * j * s.y
					+ Vector3.up * 0.00001f * index);
				
				// Set material, shader and texture that came from the webcam
				go.renderer.material = new Material(Shader.Find("Diffuse"));
				go.renderer.material.renderQueue = index;
				go.renderer.material.mainTexture = snapshot;
				
				// Dissect in pieces the given texture and assign the correct UVs
				go.renderer.material.mainTextureScale = s;
				go.renderer.material.mainTextureOffset = new Vector2(i*s.x, j*s.y);
				
				pieces[index] = go;
				index++;
			}
		}
	}
}