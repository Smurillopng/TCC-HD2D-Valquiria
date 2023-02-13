using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class QuaternionJsonConverter : JsonConverter
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
			return objectType == Types.Quaternion;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var quaternion = (Quaternion)value;
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(quaternion.x);
			writer.WritePropertyName("y");
			writer.WriteValue(quaternion.y);
			writer.WritePropertyName("z");
			writer.WriteValue(quaternion.z);
			writer.WritePropertyName("w");
			writer.WriteValue(quaternion.w);
			writer.WriteEndObject();
		}
		
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jobject = JObject.Load(reader);
			var quaternion = default(Quaternion);
			var enumerator = jobject.GetEnumerator();
			enumerator.MoveNext();
			quaternion.x = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			quaternion.y = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			quaternion.z = enumerator.Current.Value.Value<float>();
			if(enumerator.MoveNext())
			{
				quaternion.w = enumerator.Current.Value.Value<float>();
			}
			else
			{
				// UPDATE: Support pasting a Vector3 into a Quaternion field
				// by generating a Quaternion by euler angles
				var vector3 = default(Vector3);
				vector3.x = quaternion.x;
				vector3.y = quaternion.y;
				vector3.z = quaternion.z;
				quaternion = Quaternion.Euler(vector3);
			}
			return quaternion;
		}
	}
}