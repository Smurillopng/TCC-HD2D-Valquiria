using System;
using Sisus.Newtonsoft.Json;

namespace Sisus
{
	public class StringJsonConverter : JsonConverter
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
			return objectType == Types.String;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var value = reader.Value;

			string s = value as string;
			if(s != null)
			{
				int count = s.Length;
				if(count < 2)
				{
					return s;
				}
				if(s[0] != '"' || s[count - 1] != '"')
				{
					return s;
				}
				return s.Substring(1, count-2);
			}

			if(value == null)
			{
				return "";
			}
			
			var type = reader.ValueType;
			if(type == null || type == Types.String)
			{
				return "";
			}
			
			return StringUtils.ToString(value);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteRawValue((JsonConvert.ToString(value as string)));
		}
	}
}