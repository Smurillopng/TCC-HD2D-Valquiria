using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class RectJsonConverter : JsonConverter
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
			return objectType == Types.Rect;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var color = (Rect)value;
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(color.x);
			writer.WritePropertyName("y");
			writer.WriteValue(color.y);
			writer.WritePropertyName("width");
			writer.WriteValue(color.width);
			writer.WritePropertyName("height");
			writer.WriteValue(color.height);
			writer.WriteEndObject();
		}
		
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jobject = JObject.Load(reader);
			var color = default(Rect);
			var enumerator = jobject.GetEnumerator();
			enumerator.MoveNext();
			color.x = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			color.y = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			color.width = enumerator.Current.Value.Value<float>();
			enumerator.MoveNext();
			color.height = enumerator.Current.Value.Value<float>();
			return color;
		}
	}
}