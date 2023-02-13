using System;
using Sisus.Newtonsoft.Json;

namespace Sisus
{
	public class TypeJsonConverter : JsonConverter
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
			return objectType == Types.Type || objectType == Types.String;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if(value != null)
			{
				var type = value as Type;
				if(type != null)
				{
					writer.WriteRawValue(JsonConvert.ToString(type));
				}

				string str = value as string;
				if(str != null)
				{
					writer.WriteRawValue(JsonConvert.ToString(TypeExtensions.GetType(str)));
					return;
				}
			}
			writer.WriteRawValue(JsonConvert.ToString(null as Type));
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			string s = reader.Value as string;
			return TypeExtensions.GetType(s);
		}
	}
}