using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class Matrix4x4Converter : JsonConverter
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
			return objectType == typeof(Matrix4x4);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if(value == null)
			{
				writer.WriteNull();
				return;
			}

			var matrix4x4 = (Matrix4x4)value;
			writer.WriteStartObject();
			writer.WritePropertyName("m00");
			writer.WriteValue(matrix4x4.m00);
			writer.WritePropertyName("m01");
			writer.WriteValue(matrix4x4.m01);
			writer.WritePropertyName("m02");
			writer.WriteValue(matrix4x4.m02);
			writer.WritePropertyName("m03");
			writer.WriteValue(matrix4x4.m03);
			writer.WritePropertyName("m10");
			writer.WriteValue(matrix4x4.m10);
			writer.WritePropertyName("m11");
			writer.WriteValue(matrix4x4.m11);
			writer.WritePropertyName("m12");
			writer.WriteValue(matrix4x4.m12);
			writer.WritePropertyName("m13");
			writer.WriteValue(matrix4x4.m13);
			writer.WritePropertyName("m20");
			writer.WriteValue(matrix4x4.m20);
			writer.WritePropertyName("m21");
			writer.WriteValue(matrix4x4.m21);
			writer.WritePropertyName("m22");
			writer.WriteValue(matrix4x4.m22);
			writer.WritePropertyName("m23");
			writer.WriteValue(matrix4x4.m23);
			writer.WritePropertyName("m30");
			writer.WriteValue(matrix4x4.m30);
			writer.WritePropertyName("m31");
			writer.WriteValue(matrix4x4.m31);
			writer.WritePropertyName("m32");
			writer.WriteValue(matrix4x4.m32);
			writer.WritePropertyName("m33");
			writer.WriteValue(matrix4x4.m33);
			writer.WriteEnd();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
			{
				return default(Matrix4x4);
			}

			var jobject = JObject.Load(reader);
			var matrix4x4 = default(Matrix4x4);
			var enumerator = jobject.GetEnumerator();
			enumerator.MoveNext();
			matrix4x4.m00 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m01 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m02 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m03 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m20 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m21 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m22 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m23 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m30 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m31 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m32 = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			matrix4x4.m33 = enumerator.Current.Value.Value<float>();
			return matrix4x4;
		}
	}
}
