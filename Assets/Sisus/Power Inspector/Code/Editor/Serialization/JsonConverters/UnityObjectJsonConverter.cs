using System;
using System.Collections.Generic;
using Sisus.Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace Sisus.Serialization
{
	public class UnityObjectJsonConverter : JsonConverter
	{
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType.IsUnityObject();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			string serializedString;
			var unityObject = value as Object;
			if(unityObject == null)
			{
				serializedString = "null";
			}
			else
			{
				serializedString = JsonConvert.SerializeObject(new SerializedUnityObjectData(unityObject));
				//List<Object> objectReferences = null;
				//serializedString = PrettySerializer.SerializeUnityObject(value as Object, ref objectReferences);
			}

			writer.WriteRawValue(serializedString);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
			{
				#if DEV_MODE
				UnityEngine.Debug.Log("Null token");
				#endif
				return objectType.DefaultValue();
			}

			string stringData = reader.Value as string;
			if(string.IsNullOrEmpty(stringData) || string.Equals(stringData, "null"))
			{
				#if DEV_MODE
				UnityEngine.Debug.Log("reader.Value as string was null");
				#endif
				return null;
			}

			var unityObjectToOverwrite = existingValue as Object;
			if(unityObjectToOverwrite == null)
			{
				throw new NullReferenceException();
			}

			//PrettySerializer.DeserializeUnityObject(stringData, unityObjectToOverwrite);

			var unityObjectData = JsonConvert.DeserializeObject<SerializedUnityObjectData>(stringData);

			#if DEV_MODE
			UnityEngine.Debug.Log("unityObjectData: "+(unityObjectData == null ? "null" : unityObjectData.ToString()));
			#endif

			unityObjectData.Apply(unityObjectToOverwrite);
			
			return unityObjectToOverwrite;
		}
	}
}