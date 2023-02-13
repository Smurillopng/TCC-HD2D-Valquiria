using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class VectorsJsonConverter : JsonConverter
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
			return objectType == Types.Vector3 || objectType == Types.Vector2 || objectType == Types.Vector4;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
			{
				return objectType.DefaultValue();
			}

			var jobject = JObject.Load(reader);
			var enumerator = jobject.GetEnumerator();

			if(objectType == Types.Vector2)
			{
				var vector2 = default(Vector2);
				enumerator.MoveNext();
				vector2.x = enumerator.Current.Value.Value<float>();
				enumerator.MoveNext();
				vector2.y = enumerator.Current.Value.Value<float>();
				return vector2;
			}
			if(objectType == Types.Vector3)
			{
				var vector3 = default(Vector3);
				enumerator.MoveNext();
				vector3.x = enumerator.Current.Value.Value<float>();
				enumerator.MoveNext();
				vector3.y = enumerator.Current.Value.Value<float>();
				if(enumerator.MoveNext())
				{
					vector3.z = enumerator.Current.Value.Value<float>();
				}

				// UPDATE: Support pasting a Quaternion into a Vector3 field
				// by using the Quaternion's eulerAngles representation
				if(enumerator.MoveNext())
				{
					var quaternion = default(Quaternion);
					quaternion.x = vector3.x;
					quaternion.y = vector3.y;
					quaternion.z = vector3.z;
					quaternion.w = enumerator.Current.Value.Value<float>();
					return quaternion.eulerAngles;
				}

				return vector3;
			}
			if(objectType == Types.Vector4)
			{
				var vector4 = default(Vector4);
				enumerator.MoveNext();
				vector4.x = enumerator.Current.Value.Value<float>();
				enumerator.MoveNext();
				vector4.y = enumerator.Current.Value.Value<float>();
				if(enumerator.MoveNext())
				{
					vector4.z = enumerator.Current.Value.Value<float>();
					if(enumerator.MoveNext())
					{
						vector4.w = enumerator.Current.Value.Value<float>();
					}
				}
				return vector4;
			}
			return objectType.DefaultValue();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if(value == null)
			{
				writer.WriteNull();
				return;
			}

			var type = value.GetType();

			writer.WriteStartObject();

			if(type == Types.Vector3)
			{
				var vector3 = (Vector3)value;
				writer.WritePropertyName("x");
				writer.WriteValue(vector3.x);
				writer.WritePropertyName("y");
				writer.WriteValue(vector3.y);
				writer.WritePropertyName("z");
				writer.WriteValue(vector3.z);
			}
			else if(type == Types.Vector2)
			{
				var vector2 = (Vector2)value;
				writer.WritePropertyName("x");
				writer.WriteValue(vector2.x);
				writer.WritePropertyName("y");
				writer.WriteValue(vector2.y);
			}
			else if(type == Types.Vector4)
			{
				var vector4 = (Vector4)value;
				writer.WritePropertyName("x");
				writer.WriteValue(vector4.x);
				writer.WritePropertyName("y");
				writer.WriteValue(vector4.y);
				writer.WritePropertyName("z");
				writer.WriteValue(vector4.z);
				writer.WritePropertyName("w");
				writer.WriteValue(vector4.w);
			}

			writer.WriteEndObject();
		}
	}
}