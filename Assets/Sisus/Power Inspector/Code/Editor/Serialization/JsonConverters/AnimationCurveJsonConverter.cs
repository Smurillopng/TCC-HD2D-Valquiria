using System;
using System.Collections.Generic;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class AnimationCurveJsonConverter : JsonConverter
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
			return objectType == Types.AnimationCurve;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var animationCurve = value as AnimationCurve;
			if(animationCurve == null)
			{
				writer.WriteNull();
				return;
			}

			writer.Formatting = Formatting.Indented;

			writer.WriteStartObject();

			writer.WritePropertyName("keys");
			writer.WriteStartArray();
			foreach(var key in animationCurve.keys)
			{
				writer.WriteStartObject();

				writer.WritePropertyName("time");
				writer.WriteValue(key.time);
				writer.WritePropertyName("value");
				writer.WriteValue(key.value);
				writer.WritePropertyName("inTangent");
				writer.WriteValue(key.inTangent);
				writer.WritePropertyName("outTangent");
				writer.WriteValue(key.outTangent);
				#if UNITY_2018_1_OR_NEWER
				writer.WritePropertyName("inWeight");
				writer.WriteValue(key.inWeight);
				writer.WritePropertyName("outWeight");
				writer.WriteValue(key.outWeight);
				writer.WritePropertyName("weightedMode");
				writer.WriteValue((int)key.weightedMode);
				#endif
				writer.WriteEndObject();
			}
			writer.WriteEndArray();

			writer.WritePropertyName("preWrapMode");
			writer.WriteValue((int)animationCurve.preWrapMode);
			writer.WritePropertyName("postWrapMode");
			writer.WriteValue((int)animationCurve.postWrapMode);

			writer.WriteEndObject();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			var jobject = JObject.Load(reader);

			var keys = new List<Keyframe>(2);
			foreach(var keyframeToken in jobject["keys"].Children())
			{
				var time = keyframeToken["time"].Value<float>();
				var value = keyframeToken["value"].Value<float>();
				var inTangent = keyframeToken["inTangent"].Value<float>();
				var outTangent = keyframeToken["outTangent"].Value<float>();
				#if UNITY_2018_1_OR_NEWER
				var inWeight = keyframeToken["inWeight"].Value<float>();
				var outWeight = keyframeToken["outWeight"].Value<float>();
				var weightedMode = (WeightedMode)keyframeToken["weightedMode"].Value<int>();
				keys.Add(new Keyframe(time, value, inTangent, outTangent, inWeight, outWeight) { weightedMode = weightedMode });
				#else
				keys.Add(new Keyframe(time, value, inTangent, outTangent));
				#endif
			}

			var animationCurve = new AnimationCurve(keys.ToArray());
			animationCurve.preWrapMode = (WrapMode)jobject["preWrapMode"].Value<int>();
			animationCurve.postWrapMode = (WrapMode)jobject["postWrapMode"].Value<int>();

			return animationCurve;
		}
	}
}