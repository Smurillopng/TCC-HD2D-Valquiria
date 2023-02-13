using System;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public sealed class ResolutionConverter : JsonConverter
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
			return objectType == typeof(Resolution);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var resolution = (Resolution)value;
			writer.WriteStartObject();
			writer.WritePropertyName("height");
			writer.WriteValue((resolution).height);
			writer.WritePropertyName("width");
			writer.WriteValue(resolution.width);
			writer.WritePropertyName("refreshRate");
			#if UNITY_2023_1_OR_NEWER
			writer.WriteValue(resolution.refreshRateRatio);
			#else
			writer.WriteValue(resolution.refreshRate);
			#endif
			writer.WriteEndObject();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jobject = JObject.Load(reader);
			var resolution = default(Resolution);
			var enumerator = jobject.GetEnumerator();
			enumerator.MoveNext();
			resolution.height = enumerator.Current.Value.Value<int>();
			enumerator.MoveNext();
			resolution.width = enumerator.Current.Value.Value<int>();
			enumerator.MoveNext();
			#if UNITY_2023_1_OR_NEWER
			resolution.refreshRateRatio = enumerator.Current.Value.Value<RefreshRate>();
			#else
			resolution.refreshRate = enumerator.Current.Value.Value<int>();
			#endif
			return resolution;
		}
	}
}
