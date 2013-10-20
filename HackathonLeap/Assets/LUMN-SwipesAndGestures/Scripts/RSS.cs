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
using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("rss")]
public class RSS
{
	public class Content
	{		
		[XmlAttribute]
		public string url;
	}
	
	public class Item
	{
		public string title;
		public string link;
	
		[XmlElement(Namespace="http://search.yahoo.com/mrss/")]
		public Content content;
		
		[XmlElement(Namespace="http://search.yahoo.com/mrss/")]
		public Content thumbnail;
		
		[XmlIgnore]
		public Texture2D large;
		
		[XmlIgnore]
		public Texture2D small;
	}
	
	public class Channel
	{
		[XmlElement("item")]
		public List<Item> items;
	}
	
	public Channel channel;
}