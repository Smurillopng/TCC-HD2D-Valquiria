using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Newtonsoft.Json;
using Object = UnityEngine.Object;

namespace Sisus.Serialization
{
	[Serializable]
    public struct SerializedFieldData
    {
		public string name;
		public string serializedState;
		public Object unityObjectValue;
 
        public SerializedFieldData(string propertyName, [NotNull]object propertyValue)
        {
			name = propertyName;
			unityObjectValue = propertyValue as Object;
			if(unityObjectValue == null)
			{
				serializedState = JsonConvert.SerializeObject(propertyValue, InspectorPreferences.jsonSerializerSettings);
			}
			else
			{
				serializedState = "";
			}
		}

		public static List<SerializedFieldData> Get([NotNull]Object target, [CanBeNull]Type ignoreFieldsInBaseType = null)
		{
			var type = target.GetType();

			var serializedFields = new List<SerializedFieldData>();

			while(type != Types.MonoBehaviour && type != Types.UnityObject && type != Types.Component && type != Types.Behaviour && type != ignoreFieldsInBaseType)
			{
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

				for(int n = fields.Length - 1; n >= 0; n--)
				{
					var field = fields[n];

					if(field.IsInitOnly || field.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0)
					{
						continue;
					}

					var value = field.GetValue(target);
					if(value != null)
					{
						serializedFields.Add(new SerializedFieldData(field.Name, value));
					}
				}
				type = type.BaseType;
			}
			

			return serializedFields;
		}

		public static void Apply([NotNull]List<SerializedFieldData> serializedFields, [NotNull]Object target, [CanBeNull]Type ignoreFieldsInBaseType = null)
        {
			int lastSerializedPropertyIndex = serializedFields.Count - 1;
			if(lastSerializedPropertyIndex < 0)
			{
				return;
			}

			var type = target.GetType();

			while(type != Types.MonoBehaviour && type != Types.UnityObject && type != Types.Component && type != Types.Behaviour && type != ignoreFieldsInBaseType)
			{
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
				for(int n = fields.Length - 1; n >= 0; n--)
				{
					var field = fields[n];

					if(field.IsInitOnly || field.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0)
					{
						continue;
					}

					string name = field.Name;

					for(int s = lastSerializedPropertyIndex; s >= 0; s--)
					{
						var serializedProperty = serializedFields[s];
						if(string.Equals(serializedProperty.name, name))
						{
							field.SetValue(target, serializedProperty.Deserialize(field.FieldType));
							break;
						}
					}
				}
				
				type = type.BaseType;
			}
		}

		public object Deserialize(Type type)
		{
			if(!string.IsNullOrEmpty(serializedState))
			{
				return JsonConvert.DeserializeObject(serializedState, type, InspectorPreferences.jsonSerializerSettings);
			}
			return unityObjectValue;
		}

		public override string ToString()
		{
			return name + ":"+(!string.IsNullOrEmpty(serializedState) ? serializedState : unityObjectValue != null ? unityObjectValue.GetType().Name : "null");
		}
	}
}