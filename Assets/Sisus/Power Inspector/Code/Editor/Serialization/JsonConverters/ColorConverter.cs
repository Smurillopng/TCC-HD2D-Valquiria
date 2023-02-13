using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class ColorConverter : JsonConverter
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
			return objectType == Types.Color || objectType == Types.Color32;
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

			if(type == Types.Color32)
			{
				var color = (Color32)value;
				writer.WritePropertyName("r");
				writer.WriteValue(color.r);
				writer.WritePropertyName("g");
				writer.WriteValue(color.g);
				writer.WritePropertyName("b");
				writer.WriteValue(color.b);
				writer.WritePropertyName("a");
				writer.WriteValue(color.a);				
			}
			else
			{
				var color = (Color)value;
				writer.WritePropertyName("r");
				writer.WriteValue(color.r);
				writer.WritePropertyName("g");
				writer.WriteValue(color.g);
				writer.WritePropertyName("b");
				writer.WriteValue(color.b);
				writer.WritePropertyName("a");
				writer.WriteValue(color.a);
			}

			writer.WriteEndObject();
		}
		
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jobject = JObject.Load(reader);
			var enumerator = jobject.GetEnumerator();

			if(objectType == Types.Color32)
			{
				var color = default(Color);
				enumerator.MoveNext();
				color.r = enumerator.Current.Value.Value<float>();
				enumerator.MoveNext();
				color.g = enumerator.Current.Value.Value<float>();
				enumerator.MoveNext();
				color.b = enumerator.Current.Value.Value<float>();
				if(enumerator.MoveNext())
				{
					color.a = enumerator.Current.Value.Value<float>();
				}
				return color;
			}
			else
			{
				var color = default(Color32);
				enumerator.MoveNext();
				color.r = enumerator.Current.Value.Value<byte>();
				enumerator.MoveNext();
				color.g = enumerator.Current.Value.Value<byte>();
				enumerator.MoveNext();
				color.b = enumerator.Current.Value.Value<byte>();
				if(enumerator.MoveNext())
				{
					color.a = enumerator.Current.Value.Value<byte>();
				}
				return color;
			}
		}
	}
}