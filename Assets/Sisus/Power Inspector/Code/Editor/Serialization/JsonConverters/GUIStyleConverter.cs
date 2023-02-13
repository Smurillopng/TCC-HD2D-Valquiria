using System;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;
using UnityEngine;

namespace Sisus
{
	public class GUIStyleConverter : JsonConverter
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
			return objectType == Types.GUIStyle;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var style = value as GUIStyle;
			if(style == null)
			{
				writer.WriteRawValue("null");
				return;
			}
			writer.WriteStartObject();
			writer.WritePropertyName("name");
			writer.WriteValue(style.name); //this can cause an ArgumentNullException!
			writer.WritePropertyName("font");
			serializer.Serialize(writer, style.font, typeof(Font));
			writer.WritePropertyName("imagePosition");
			writer.WriteValue(style.imagePosition);
			writer.WritePropertyName("alignment");
			writer.WriteValue(style.alignment);
			writer.WritePropertyName("wordWrap");
			writer.WriteValue(style.wordWrap);
			writer.WritePropertyName("clipping");
			writer.WriteValue(style.clipping);
			writer.WritePropertyName("contentOffset");
			serializer.Serialize(writer, style.contentOffset, Types.Vector2);
			writer.WritePropertyName("fixedWidth");
			writer.WriteValue(style.fixedWidth);
			writer.WritePropertyName("fixedHeight");
			writer.WriteValue(style.fixedHeight);
			writer.WritePropertyName("stretchWidth");
			writer.WriteValue(style.stretchWidth);
			writer.WritePropertyName("stretchHeight");
			writer.WriteValue(style.stretchHeight);
			writer.WritePropertyName("fontSize");
			writer.WriteValue(style.fontSize);
			writer.WritePropertyName("fontStyle");
			writer.WriteValue(style.fontStyle);
			writer.WritePropertyName("richText");
			writer.WriteValue(style.richText);
			writer.WritePropertyName("clipOffset");
			#pragma warning disable 0618 //disable warning CS0618: `UnityEngine.GUIStyle.clipOffset' is obsolete: `Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.'
			serializer.Serialize(writer, style.clipOffset, Types.Vector2);
			#pragma warning restore 0618
			writer.WriteEndObject();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var jobject = JObject.Load(reader);
			var style = new GUIStyle();
			var enumerator = jobject.GetEnumerator();
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(enumerator != null);
			#endif
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(enumerator.MoveNext());
			#else
			enumerator.MoveNext();
			#endif

			style.name = Convert.ToString(enumerator.Current.Value, System.Globalization.CultureInfo.InvariantCulture);
			enumerator.MoveNext();
			style.font = enumerator.Current.Value.ToObject<Font>(serializer);
			enumerator.MoveNext();
			style.imagePosition = enumerator.Current.Value.ToObject<ImagePosition>(serializer);
			enumerator.MoveNext();
			style.alignment = enumerator.Current.Value.ToObject<TextAnchor>(serializer);
			enumerator.MoveNext();
			style.wordWrap = enumerator.Current.Value.ToObject<bool>(serializer);
			enumerator.MoveNext();
			style.clipping = enumerator.Current.Value.ToObject<TextClipping>(serializer);
			enumerator.MoveNext();
			style.contentOffset = (Vector2)enumerator.Current.Value.ToObject(Types.Vector2, serializer);
			enumerator.MoveNext();
			style.fixedWidth = enumerator.Current.Value.ToObject<float>(serializer);
			enumerator.MoveNext();
			style.fixedHeight = enumerator.Current.Value.ToObject<float>(serializer);
			enumerator.MoveNext();
			style.stretchWidth = enumerator.Current.Value.ToObject<bool>(serializer);
			enumerator.MoveNext();
			style.stretchHeight = enumerator.Current.Value.ToObject<bool>(serializer);
			enumerator.MoveNext();
			style.fontSize = enumerator.Current.Value.ToObject<int>(serializer);
			enumerator.MoveNext();
			style.fontStyle = enumerator.Current.Value.ToObject<FontStyle>(serializer);
			enumerator.MoveNext();
			style.richText = enumerator.Current.Value.ToObject<bool>(serializer);
			enumerator.MoveNext();
			#pragma warning disable 0618 //disable warning CS0618: `UnityEngine.GUIStyle.clipOffset' is obsolete: `Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.'
			style.clipOffset = (Vector2)enumerator.Current.Value.ToObject(Types.Vector2, serializer);
			#pragma warning restore 0618
			return style;
		}
	}
}