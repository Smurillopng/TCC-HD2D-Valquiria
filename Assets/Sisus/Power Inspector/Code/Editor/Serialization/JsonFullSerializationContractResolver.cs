using System;
using System.Collections.Generic;
using System.Reflection;

namespace Sisus.Newtonsoft.Json.Serialization
{
	/// <summary>
	/// Causes all fields to be serialized by default (including property backing fields).
	/// No properties are ever serialized.
	/// </summary>
	public class JsonFullSerializationContractResolver : DefaultContractResolver
	{
		protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var jsonProperties = new List<JsonProperty>(3);

			do
			{
				var jsonObjects = type.GetCustomAttributes(typeof(JsonObjectAttribute), false);
				if(jsonObjects.Length > 0 && (jsonObjects[0] as JsonObjectAttribute).MemberSerialization == MemberSerialization.OptIn)
				{
					return base.CreateProperties(type, memberSerialization);
				}

				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				for(int n = 0, fieldCount = fields.Length; n < fieldCount; n++)
				{
					var field = fields[n];
					if(field.IsInitOnly || field.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0 || field.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Length > 0)
					{
						continue;
					}

					var jsonProperty = CreateProperty(field, memberSerialization);
					jsonProperty.Writable = true;
					jsonProperty.Readable = true;
					jsonProperties.Add(jsonProperty);
				}
				type = type.BaseType;
			}
			while(type != null && type != Types.SystemObject && type != Types.UnityObject && type != Types.Component);

			return jsonProperties;
		}
	}
}