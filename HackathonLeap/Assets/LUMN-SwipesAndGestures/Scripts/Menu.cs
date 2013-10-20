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
using System.Xml.Serialization;
using System.IO;
using Leap;
using Screen = UnityEngine.Screen;

public class Menu : MonoBehaviour 
{
	public Font font;
	public Texture2D logo;
	public Texture2D swipeLeft;
	public Texture2D swipeRight;
	public Texture2D singleTap;
	
	private static readonly float circleWaitTime = 2f;
	private static readonly float scrollSpeedSwipeMultiplier = 2f;
	
	private RSS rss = null;
	private int index = 0;
	private int showIndexLarge = 0;
	
	private int selectorPointableId = -1;
	
	private float showTray = 0f;
	private float trayScrollSpeed = 0f;
	private float trayOffset = 0f;
	private float traySeekVelocity = 0f;
	private float trayOffsetVelocity = 0f;
	private float trayHeight = 0f;
	
	private Vector3 swipeStart = Vector3.zero;
	private Vector3 swipeEnd = Vector3.zero;
	
	private bool showRefreshPrompt = false;
	private float allowCircleGestureTime = 0f;
	
	private Texture2D whiteTex;
	private GUISkin skin;
	
	private void Start () 
	{
		whiteTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
		whiteTex.SetPixels(new Color[] { Color.white, Color.white });
		whiteTex.Apply();
		
		// Set defaults and fix incorrect scaling issue w/ distributed Unity project (units are in mm, not cm)
		Leap.UnityVectorExtension.InputOffset = Vector3.zero;		
		Leap.UnityVectorExtension.InputScale = Vector3.one * 0.001f;
		
		LeapInputEx.PointableUpdated += OnPointableUpdated;
		LeapInputEx.PointableLost += OnPointableLost;
		LeapInputEx.GestureDetected += OnGestureDetected;
		
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPESWIPE);
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPECIRCLE);
		LeapInputEx.Controller.EnableGesture(Gesture.GestureType.TYPESCREENTAP);
		
		StartCoroutine("LoadFlickrFeed");
	}
	
	private IEnumerator LoadFlickrFeed()
	{
		// Flickr's public feed
		WWW www = new WWW("http://api.flickr.com/services/feeds/photos_public.gne?lang=en-us&format=rss_200");
		yield return www;
		if (string.IsNullOrEmpty(www.error))
		{
			XmlSerializer serializer = new XmlSerializer(typeof(RSS));
			rss = serializer.Deserialize(new StringReader(www.text)) as RSS;
//			Debug.Log(rss.channel.items.Count);
			foreach (RSS.Item item in rss.channel.items)
			{
				www = new WWW(item.thumbnail.url);
				yield return www;
				if (string.IsNullOrEmpty(www.error))
				{
					item.small = www.textureNonReadable;					
				}
				else
				{
					Debug.LogError(www.error + " " + www.url);
				}
			}
		}
	}
	
	private void OnGestureDetected(GestureList gestures)
	{
		foreach (Gesture g in gestures)
		{
			if (!g.IsValid)
				continue;
			
			switch (g.Type)
			{
			case Gesture.GestureType.TYPESWIPE:
				allowCircleGestureTime = Time.time + circleWaitTime;
				switch (g.State)
				{
				case Gesture.GestureState.STATESTART:
					// Average the points for the start position
					swipeStart = Vector3.zero;
					foreach (Pointable p in g.Pointables)
					{
						swipeStart += p.TipPosition.ToUnityTranslated();
					}
					swipeStart *= 1f / g.Pointables.Count;
					break;
					
				case Gesture.GestureState.STATEUPDATE:
					// For some reason we sometimes get gestures w/ no pointables
					if (g.Pointables.Count > 0)
					{
						swipeEnd = Vector3.zero;
						foreach (Pointable p in g.Pointables)
						{
							swipeEnd += p.TipPosition.ToUnityTranslated();
						}
						swipeEnd *= 1f / g.Pointables.Count;
					}
					break;
					
				case Gesture.GestureState.STATESTOP:
					// A swipe is used to dismiss the refresh prompt
					if (showRefreshPrompt)
					{
						showRefreshPrompt = false;
						break;
					}
					
					Vector3 startPx = Camera.main.WorldToScreenPoint(swipeStart);
					Vector3 endPx = Camera.main.WorldToScreenPoint(swipeEnd);					
					Vector3 swipe = endPx - startPx;
					// Check major axis to determine if it is a swipe up/down or left/right
					if (Mathf.Abs(Vector3.Dot(swipe.normalized, Vector3.up)) > 0.9f)
					{
						if (showTray >= 1f && !showRefreshPrompt)
						{
							trayScrollSpeed = swipe.y * scrollSpeedSwipeMultiplier;
						}						
					}
					else if (Mathf.Abs(Vector3.Dot(swipe.normalized, Vector3.right)) > 0.9f)
					{						
						if (swipe.x < 0f)
						{
							StartCoroutine("ShowTray");
						}
						else if (swipe.x > 0f)
						{
							StartCoroutine("HideTray");
						}
					}
					break;
				}
				break;
				
			case Gesture.GestureType.TYPESCREENTAP:
//				Debug.Log("TAP");
				trayScrollSpeed = 0f;
				StartCoroutine(ShowLarge(index));
				break;
				
			case Gesture.GestureType.TYPECIRCLE:
				if (Time.time > allowCircleGestureTime && g.State == Gesture.GestureState.STATESTOP)
				{
//					Debug.Log("CIRCLE " + g.Id + " " + g.State + " on frame " + Time.frameCount);
					if (!showRefreshPrompt)
					{
						showRefreshPrompt = true;
						
						// Delay the next circle gesture, so that it doesn't trigger an automatic refresh
						allowCircleGestureTime = Time.time + circleWaitTime;
					}
					else
					{
						if (!IsInvoking("LoadFlickrFeed"))
						{
							StartCoroutine("LoadFlickrFeed");
							
							// Delay the next circle gesture, so that it doesn't trigger another prompt
							allowCircleGestureTime = Time.time + circleWaitTime;
						}
						showRefreshPrompt = false;
					}
				}
				break;
			}
		}
	}
	
	private void OnPointableLost(int id)
	{
		if (selectorPointableId == id)
		{
			selectorPointableId = -1;
		}
	}
	
	private void OnPointableUpdated(Pointable p)
	{		
		if (!showRefreshPrompt && showTray >= 1f && (selectorPointableId == p.Id || selectorPointableId == -1))
		{
			selectorPointableId = p.Id;
			index = Mathf.RoundToInt((trayOffset + Mathf.InverseLerp(500f, 50f, p.TipPosition.ToUnity().y) * Screen.height) / 75f); 
		}
		
		Debug.DrawRay(p.TipPosition.ToUnityTranslated(), p.Direction.ToUnity() * 0.1f);
	}
	
	private IEnumerator ShowLarge(int index)
	{
		if (rss != null && index > 0 && index < rss.channel.items.Count)
		{
			showIndexLarge = index;
			RSS.Item item = rss.channel.items[showIndexLarge];
			if (!item.large)
			{
				WWW www = new WWW(item.content.url);
				yield return www;
				if (string.IsNullOrEmpty(www.error))
				{
					item.large = www.textureNonReadable;					
				}
				else
				{
					Debug.LogError(www.error + " " + www.url);
				}
			}
		}		
	}
	
	private IEnumerator DelayNextGesture(Gesture.GestureType type, float delay)
	{
		LeapInputEx.Controller.EnableGesture(type, false);
		yield return new WaitForSeconds(delay);
		LeapInputEx.Controller.EnableGesture(type);		
	}
	
	private IEnumerator ShowTray()
	{
		StopCoroutine("HideTray");
		while (showTray < 1f)
		{
			yield return null;
			showTray += Time.deltaTime;
		}
		
		showTray = 1f;
	}
	
	private IEnumerator HideTray()
	{
		StopCoroutine("ShowTray");
		while (showTray > 0f)
		{
			yield return null;
			showTray -= Time.deltaTime;
		}
		
		showTray = 0f;
	}
	
	private void Update()
	{
		LeapInputEx.Update();

		trayOffset += trayScrollSpeed * Time.deltaTime;
		
		// Slow the scroll speed over time (similar to touch devices)
		trayScrollSpeed = Mathf.SmoothDamp(trayScrollSpeed, 0f, ref traySeekVelocity, 1f);
		
		// Handle the two cases where we overscroll or underscroll the list
		if (trayOffset < 0f)
		{
			trayOffset = Mathf.SmoothDamp(trayOffset, 0f, ref trayOffsetVelocity, 1f);
		}
		if (trayOffset > (trayHeight - Screen.height))
		{
			trayOffset = Mathf.SmoothDamp(trayOffset, trayHeight - Screen.height, ref trayOffsetVelocity, 1f);
		}		
		
		Debug.DrawLine(swipeStart, swipeEnd, Color.red);
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
		
		Rect fullScreenRect = new Rect(0, 0, Screen.width, Screen.height);
		string title = string.Empty;
		if (rss != null)
		{
			RSS.Item item = rss.channel.items[showIndexLarge];
			title = "Loading...";
			if (item.large)
			{
				title = item.title;
				GUI.DrawTexture(fullScreenRect, item.large, ScaleMode.ScaleToFit);
			}
		}
		
		float traySize = 75f;
		float trayGutter = 10f;
		Rect trayRect = new Rect(Screen.width - traySize + ((1f - showTray) * (traySize - trayGutter)), 0, traySize, Screen.height);
		
		// Swipe instructions
		Rect hintRect = trayRect;
		hintRect.width = swipeLeft.width;
		hintRect.x -= swipeLeft.width;
		GUILayout.BeginArea(hintRect);
		GUILayout.FlexibleSpace();
		Texture2D swipeIcon = showTray < 1f ? swipeLeft : swipeRight;
		GUILayout.Label(swipeIcon, GUIStyle.none);
		GUILayout.FlexibleSpace();
		GUILayout.EndArea();
				
		// Image thumbnails
		GUILayout.BeginArea(trayRect);
		GUILayout.Space(-trayOffset);
		if (rss != null)
		{
			trayHeight = 0f;
			for (int i = 0; i < rss.channel.items.Count; i++)
			{
				RSS.Item item = rss.channel.items[i];
				Texture2D tex = item.small;
				if (tex)
				{
					GUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					GUILayout.Label(tex, GUIStyle.none);
					GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					
					// Draw border for selected image					
					if (index == i)
					{
						Rect rect = GUILayoutUtility.GetLastRect();											
						float borderWidth = 4.0f;
						GUI.color = Color.yellow;
						GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, borderWidth), whiteTex);
						GUI.DrawTexture(new Rect(rect.xMax - borderWidth, rect.yMin, borderWidth, rect.height), whiteTex);
						GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, borderWidth, rect.height), whiteTex);
						GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - borderWidth, rect.width, borderWidth), whiteTex);
						GUI.color = Color.white;
						
						
						if (showTray >= 1f)
						{
							Rect tapRect = rect;
							tapRect.width = singleTap.width;
							tapRect.height = singleTap.height;
							GUI.DrawTexture(tapRect, singleTap);
						}
					}
					
					trayHeight += tex.height;
				}
			}
		}
		GUILayout.EndArea();
		
		GUILayout.BeginArea(fullScreenRect);
		GUILayout.Label("Public Flickr Feed - " + title);
		GUILayout.Space(-10f);
		GUILayout.Label("(Draw a Circle to Refresh)");
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Experiment #3: Swipes and Gestures");
		GUILayout.FlexibleSpace();
		GUILayout.Label(logo, GUIStyle.none);
		GUILayout.EndHorizontal();
		GUILayout.Space(10f);
		GUILayout.EndArea();
		
		if (showRefreshPrompt)
		{
			GUI.color = new Color(0f, 0f, 0f, 0.5f);
			GUI.DrawTexture(fullScreenRect, whiteTex);
			
			GUI.color = Color.white;
			GUILayout.BeginArea(fullScreenRect);
			GUILayout.FlexibleSpace();
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();			
			GUILayout.Label("Draw a circle again to refresh the Flickr feed");			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();			
			GUILayout.Label(swipeLeft, GUIStyle.none);
			GUILayout.Label(swipeRight, GUIStyle.none);
			GUILayout.Label("to Cancel");			
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();			
			
			GUILayout.FlexibleSpace();
			GUILayout.EndArea();
		}
	}
}