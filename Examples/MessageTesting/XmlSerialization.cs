﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SimpleSockets.Messaging.MessageContract;

namespace MessageTesting
{
	public class XmlSerialization: IObjectSerializer
	{
		public byte[] SerializeObjectToBytes(object anySerializableObject)
		{
			try
			{
				XmlSerializer xmlSer = new XmlSerializer(anySerializableObject.GetType());

				using (var sww = new StringWriter())
				{
					using (XmlWriter writer = XmlWriter.Create(sww))
					{
						xmlSer.Serialize(writer, anySerializableObject);
						return Encoding.UTF8.GetBytes(sww.ToString());
					}
				}
			}
			catch (Exception)
			{
				throw new Exception("Unable to serialize the object of type " + anySerializableObject.GetType() + " to an xml string.");
			}
		}

		public object DeserializeBytesToObject(byte[] bytes, Type objType)
		{
			try
			{
				var xmlSer = new XmlSerializer(objType);
				var xml = Encoding.UTF8.GetString(bytes);
				var stringReader = new StringReader(xml);
				return xmlSer.Deserialize(stringReader);
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to convert xml string back to an object of type " + objType + ".\n" + ex.ToString());
			}
		}
	}
}
