using System;
using Sisus.Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class UnityObjectReferenceConverter : JsonConverter
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
			var obj = value as Object;
			string serializedString = obj == null ? JsonConvert.Null : StringUtils.ToString(obj.GetInstanceID());
			writer.WriteRawValue(serializedString);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var value = reader.Value;
			if(value == null)
			{
				return null;
			}
			string stringData = value.ToString();
			if(string.IsNullOrEmpty(stringData) || string.Equals(stringData, "null"))
			{
				return null;
			}

			int instanceId;
			if(!int.TryParse(stringData, out instanceId))
			{
				return false;
			}

			if(objectType == null)
			{
				objectType = Types.UnityObject;
			}

			return InstanceIdUtility.IdToObject(instanceId, objectType);
		}
	}
}