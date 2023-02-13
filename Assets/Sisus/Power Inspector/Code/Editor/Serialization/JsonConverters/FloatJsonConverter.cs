using System;
using System.Globalization;
using Sisus.Newtonsoft.Json;

namespace Sisus
{
	public class FloatJsonConverter : JsonConverter
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
				return false;
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == Types.Float;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteRawValue(((float)value).ToString(StringUtils.DoubleFormat, CultureInfo.InvariantCulture));
		}
	}
}