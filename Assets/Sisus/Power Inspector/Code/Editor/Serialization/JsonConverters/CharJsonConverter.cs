using System;
using Sisus.Newtonsoft.Json;

namespace Sisus
{
	public class CharJsonConverter : JsonConverter
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
			return objectType == Types.Char;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteRawValue((JsonConvert.ToString((char)value)));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string s = reader.Value as string;

			if(string.IsNullOrEmpty(s))
			{
				return '\0';
			}

			if(s.Length == 1)
			{
				return s[0];
			}

			if(s[0] != '"')
			{
				return JsonConvert.DeserializeObject(string.Concat("\"", s, "\""));
			}

			return JsonConvert.DeserializeObject(s);
		}
	}
}