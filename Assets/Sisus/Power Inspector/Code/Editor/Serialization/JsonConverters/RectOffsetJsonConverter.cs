using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class RectOffsetJsonConverter : JsonConverter
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
			return objectType == Types.RectOffset;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var rectOffset = value as RectOffset;
			if(rectOffset == null)
			{
				writer.WriteRawValue(JsonConvert.ToString(null as RectOffset));
				return;
			}
			writer.WriteStartObject();
			writer.WritePropertyName("left");
			writer.WriteValue(rectOffset.left);
			writer.WritePropertyName("right");
			writer.WriteValue(rectOffset.right);
			writer.WritePropertyName("top");
			writer.WriteValue(rectOffset.top);
			writer.WritePropertyName("bottom");
			writer.WriteValue(rectOffset.bottom);
			writer.WriteEndObject();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jobject = JObject.Load(reader);
			var rectOffset = default(RectOffset);
			var enumerator = jobject.GetEnumerator();
			enumerator.MoveNext();
			rectOffset.left = enumerator.Current.Value.Value<int>();
			enumerator.MoveNext();
			rectOffset.right = enumerator.Current.Value.Value<int>();
			enumerator.MoveNext();
			rectOffset.top = enumerator.Current.Value.Value<int>();
			enumerator.MoveNext();
			rectOffset.bottom = enumerator.Current.Value.Value<int>();
			return rectOffset;
		}
	}
}