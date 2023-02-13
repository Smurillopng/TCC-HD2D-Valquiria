using System;
using Sisus.Newtonsoft.Json;
using Sisus.Newtonsoft.Json.Linq;

namespace Sisus
{
	public class DelegateJsonConverter : JsonConverter
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
			return typeof(Delegate).IsAssignableFrom(objectType);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			string serializedString;
			if(value == null)
			{
				serializedString = "null";
			}
			else
			{
				var settings = new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.All,
					ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
					{
						IgnoreSerializableInterface = false,
						IgnoreSerializableAttribute = false,
					},
					Formatting = Formatting.Indented,
				};
				serializedString = JsonConvert.SerializeObject(value, settings);
			}

			writer.WriteRawValue(serializedString);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var token = JToken.ReadFrom(reader);
			string stringData = token.Value<string>();

			#if DEV_MODE
			UnityEngine.Debug.Log(GetType().Name+".ReadJson with stringData="+StringUtils.ToString(stringData)+ ", reader.ReadAsString()="+StringUtils.ToString(reader.ReadAsString())+ ", token.Value<string>()="+StringUtils.ToString(token.Value<string>()));
			#endif

			if(string.IsNullOrEmpty(stringData) || string.Equals(stringData, "null"))
			{
				return null;
			}

			var settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All,
				ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
				{
					IgnoreSerializableInterface = false,
					IgnoreSerializableAttribute = false,
				},
				Formatting = Formatting.Indented,
			};
			return JsonConvert.DeserializeObject(stringData, settings);
		}
	}
}