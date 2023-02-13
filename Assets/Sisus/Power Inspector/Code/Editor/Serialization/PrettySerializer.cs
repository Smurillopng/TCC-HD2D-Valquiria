#define DEBUG_SERIALIZE
//#define DEBUG_DESERIALIZE
#define DEBUG_DESERIALIZE_FAIL

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using Sisus.Newtonsoft.Json;
using Object = UnityEngine.Object;
#if !DONT_USE_ODIN_SERIALIZER
using Sisus.OdinSerializer;
#endif

namespace Sisus
{
	/// <summary>
	/// Uses a combination of Json.Net, JsonUtility and OdinSerializer to
	/// serialize (theoretically) any object to json string and back.
	/// Focuses on trying to make the serialized data as human-readable
	/// as possible, as well as maximizing cross-type conversion support (i.e.
	/// serializing a value to string and then deserializing it into a value
	/// of a completely different type)
	/// </summary>
	public static class PrettySerializer
	{
		private static List<Object> reusableObjectList = new List<Object>();

		/// <summary>
		/// Serializes any given object supported by Unity serialization into a simple and readable form
		/// </summary>
		public static string Serialize(object target)
		{
			var result = Serialize(target, ref reusableObjectList);
			reusableObjectList.Clear();
			return result;
		}

		/// <summary>
		/// Serializes any given object supported by Unity serialization into a simple and readable form
		/// </summary>
		public static string Serialize([CanBeNull]object target, ref List<Object> objectReferences)
		{
			string stringData;
			if(target == null)
			{
				stringData = "null";
			}
			else
			{
				var unityObject = target as Object;
				if(unityObject != null)
				{
					stringData = SerializeUnityObject(unityObject, ref objectReferences);
				}
				else
				{
					stringData = SerializeNonUnityObject(target, ref objectReferences);
				}
			}
			return stringData;
		}

		/// <summary>
		/// Converts given target to json. Target cannot be of type UnityEngine.Object.
		/// </summary>
		/// <param name="target"> Target to serialize. Cannot be of type UnityEngine.Object. </param>
		/// <param name="objectReferences"> UnityEngine.Object references contained in target will be added to this list. </param>
		/// <returns>serialized string representation of target</returns>
		public static string SerializeNonUnityObject([NotNull]object target, [CanBeNull]ref List<Object> objectReferences)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(target != null);
			#endif

			string result;
			#if !DONT_USE_ODIN_SERIALIZER
			if(JsonDotNetCanHandlePropertySerialization(target.GetType()))
			#endif
			{
				#if DEV_MODE && DEBUG_SERIALIZE
				Debug.Log("SerializeNonUnityObject("+StringUtils.TypeToString(target)+") using Json.NET");
				#endif
				
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(InspectorPreferences.jsonSerializerSettings != null);
				#endif

				result = JsonConvert.SerializeObject(target, InspectorPreferences.jsonSerializerSettings);
				return result;
			}
			#if !DONT_USE_ODIN_SERIALIZER
			#if DEV_MODE && DEBUG_SERIALIZE
			Debug.Log("SerializeNonUnityObject("+StringUtils.TypeToString(target)+") using OdinSerializer");
			#endif
			var byteData = SerializationUtility.SerializeValueWeak(target, DataFormat.JSON, out objectReferences);
			result = Encoding.UTF8.GetString(byteData);
			return result;
			#endif
		}

		public static byte[] ToBytes([NotNull]object target)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!(target is Object));
			#endif

			#if DONT_USE_ODIN_SERIALIZER
			var jsonString = JsonUtility.ToJson(target);
			var bytes = Encoding.UTF8.GetBytes(jsonString);
			return bytes;
			#else
			return SerializationUtility.SerializeValueWeak(target, DataFormat.Binary);
			#endif
		}

		public static byte[] ToBytes([NotNull]object target, out List<Object> objectReferences)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!(target is Object));
			#endif

			#if DONT_USE_ODIN_SERIALIZER
			objectReferences = null;
			return ToBytes(target);
			#else
			return SerializationUtility.SerializeValueWeak(target, DataFormat.Binary, out objectReferences);
			#endif
		}

		public static T FromBytes<T>(byte[] bytes)
		{
			#if DONT_USE_ODIN_SERIALIZER
			var jsonString = Encoding.UTF8.GetString(bytes);
			return JsonUtility.FromJson<T>(jsonString);
			#else
			return SerializationUtility.DeserializeValue<T>(bytes, DataFormat.Binary);
			#endif
		}

		private static bool JsonDotNetCanHandlePropertySerialization([NotNull]Type type)
		{
			return true;
		}

		private static bool ShouldUseJsonUtilityForTargetSerialization(Object target)
		{
			return target as MonoBehaviour == null && target as ScriptableObject == null;
		}

		public static void SerializeUnityObject(Object target, ref byte[] bytes, ref List<Object> objectReferences)
		{
			if(ShouldUseJsonUtilityForTargetSerialization(target))
			{
				#if UNITY_EDITOR
				var jsonString = UnityEditor.EditorJsonUtility.ToJson(target);
				#else
				var jsonString = JsonUtility.ToJson(target);
				#endif
				bytes = Encoding.UTF8.GetBytes(jsonString);
				if(objectReferences == null)
				{
					objectReferences = new List<Object>();
				}
				else
				{
					objectReferences.Clear();
				}
			}
			else
			{
				#if DONT_USE_ODIN_SERIALIZER
				var jsonString = JsonConvert.SerializeObject(target, InspectorPreferences.jsonSerializerSettings);
				#if DEV_MODE
				Debug.Log("Target Serialized with Json.NET:\n"+jsonString, target);
				#endif
				bytes = Encoding.UTF8.GetBytes(jsonString);
				if(objectReferences == null)
				{
					objectReferences = new List<Object>();
				}
				else
				{
					objectReferences.Clear();
				}
				#else
				UnitySerializationUtility.SerializeUnityObject(target, ref bytes, ref objectReferences, DataFormat.Binary, true);
				#endif
			}
		}

		public static string SerializeUnityObject(Object target, ref List<Object> objectReferences)
		{
			#if !DONT_USE_ODIN_SERIALIZER
			if(ShouldUseJsonUtilityForTargetSerialization(target))
			#endif
			{
				#if UNITY_EDITOR
				return UnityEditor.EditorJsonUtility.ToJson(target);
				#else
				return JsonUtility.ToJson(target);
				#endif
			}

			#if DONT_USE_ODIN_SERIALIZER
			var jsonString = JsonConvert.SerializeObject(target, InspectorPreferences.jsonSerializerSettings);
			#if DEV_MODE
			Debug.Log("Target Serialized with Json.NET:\n"+jsonString, target);
			#endif
			return jsonString;
			#else
			byte[] byteData = null;
			UnitySerializationUtility.SerializeUnityObject(target, ref byteData, ref objectReferences, DataFormat.JSON, true);

			#if DEV_MODE
			Debug.Log("SerializeUnityObject: " + StringUtils.ToString(Encoding.UTF8.GetString(byteData)));
			#endif

			return Encoding.UTF8.GetString(byteData);
			#endif
		}

		public static object Deserialize(string stringData, Type type)
		{
			var result = Deserialize(stringData, type, reusableObjectList);
			reusableObjectList.Clear();
			return result;
		}

		public static object Deserialize(string stringData, Type type, List<Object> objectReferences)
		{
			object result = null;
			if(!DeserializeOverride(stringData, type, ref result, objectReferences))
			{
				throw new InvalidCastException("Cannot convert Clipboard contents to type "+type.Name+": "+stringData);
			}
			return result;
		}

		public static bool DeserializeOverride(string stringData, Type type, ref object objectToOverwrite)
		{
			var result = DeserializeOverride(stringData, type, ref objectToOverwrite, reusableObjectList);
			reusableObjectList.Clear();
			return result;
		}

		public static bool DeserializeOverride(string stringData, Type type, ref object objectToOverwrite, List<Object> objectReferences)
		{
			try
			{
				var unityObject = objectToOverwrite as Object;
				if(unityObject != null)
				{
					DeserializeUnityObject(stringData, unityObject, ref objectReferences);
				}
				else
				{
					return DeserializeOverrideNonUnityObject(stringData, type, ref objectToOverwrite, objectReferences);
				}
				return true;
			}
			#if DEV_MODE && DEBUG_DESERIALIZE_FAIL
			catch(Exception e)
			{
				Debug.Log("DeserializeOverride failed deserializing to "+StringUtils.ToStringSansNamespace(type)+": "+e);
				return false;
			}
			#else
			catch
			{
				return false;
			}
			#endif
		}

		public static bool DeserializeOverrideNonUnityObject(string stringData, Type type, ref object objectToOverwrite, List<Object> objectReferences)
		{
			if(type == Types.String)
			{
				objectToOverwrite = stringData;
				return true;
			}

			#if !DONT_USE_ODIN_SERIALIZER
			if(JsonDotNetCanHandlePropertySerialization(type))
			#endif
			{
				try
				{
					objectToOverwrite = JsonConvert.DeserializeObject(stringData, type, InspectorPreferences.jsonSerializerSettings);
					return true;
				}
				#if DEV_MODE && DEBUG_DESERIALIZE_FAIL
				catch(Exception e)
				{
					Debug.Log("JsonConvert failed deserializing to "+StringUtils.ToStringSansNamespace(type)+": "+e);
				#else
				catch
				{
				#endif
					return false;
				}
			}
			
			#if !DONT_USE_ODIN_SERIALIZER
			try
			{
				var bytes = Encoding.UTF8.GetBytes(stringData);
				objectToOverwrite = SerializationUtility.DeserializeValueWeak(bytes, DataFormat.JSON, objectReferences);
				#if DEV_MODE && DEBUG_DESERIALIZE
				Debug.Log("OdinSerializer deserialized to " + StringUtils.ToString(type) + ": " + StringUtils.ToString(objectToOverwrite));
				#endif
				return true;
			}
			#if DEV_MODE && DEBUG_DESERIALIZE_FAIL
			catch(Exception e)
			{
				Debug.Log("OdinSerializer failed deserializing to " + StringUtils.ToString(type)+": "+e);
				return false;
			}
			#else
			catch
			{
				return false;
			}
			#endif
			#endif
		}

		public static void DeserializeUnityObject(string stringData, [NotNull]Object objectToOverwrite)
		{
			DeserializeUnityObject(stringData, objectToOverwrite, ref reusableObjectList);
			reusableObjectList.Clear();
		}
		
		public static void DeserializeUnityObject(string stringData, [NotNull]Object objectToOverwrite, ref List<Object> objectReferences)
		{
			//Use Unity's own JsonUtility for serializing built-in UnityObject types
			//because it's more efficient and because OdinSerializer seems to have problems
			//serializing some built-in Components like Transform.
			if(ShouldUseJsonUtilityForTargetSerialization(objectToOverwrite))
			{
				#if UNITY_EDITOR
				UnityEditor.EditorJsonUtility.FromJsonOverwrite(stringData, objectToOverwrite);
				#else
				JsonUtility.FromJsonOverwrite(stringData, objectToOverwrite);
				#endif
			}
			//for user-created Components (MonoBehaviours and ScriptableObjects) use OdinSerializer
			//so that also fields that JsonUtility cannot serialize also get copied.
			else
			{
				#if DONT_USE_ODIN_SERIALIZER
				var jsonString = JsonConvert.SerializeObject(target, InspectorPreferences.jsonSerializerSettings);
				#if DEV_MODE
				Debug.Log("Target Serialized with Json.NET:\n"+jsonString, target);
				#endif
				return jsonString;
				#else
				string deserializeFromString = stringData;
				byte[] bytes = Encoding.UTF8.GetBytes(deserializeFromString);
				UnitySerializationUtility.DeserializeUnityObject(objectToOverwrite, ref bytes, ref objectReferences, DataFormat.JSON);
				#endif
			}
		}

		public static void DeserializeUnityObject(byte[] bytes, [NotNull]Object objectToOverwrite, ref List<Object> objectReferences)
		{
			//Use Unity's own JsonUtility for serializing built-in UnityObject types
			//because it's more efficient and because OdinSerializer seems to have problems
			//serializing some built-in Components like Transform.
			#if !DONT_USE_ODIN_SERIALIZER
			if(ShouldUseJsonUtilityForTargetSerialization(objectToOverwrite))
			#endif
			{
				var jsonString = Encoding.UTF8.GetString(bytes);

				#if UNITY_EDITOR
				UnityEditor.EditorJsonUtility.FromJsonOverwrite(jsonString, objectToOverwrite);
				#else
				JsonUtility.FromJsonOverwrite(jsonString, objectToOverwrite);
				#endif
			}
			#if !DONT_USE_ODIN_SERIALIZER
			//for user-created Components (MonoBehaviours and ScriptableObjects) use OdinSerializer
			//so that also fields that JsonUtility cannot serialize also get copied.
			else
			{
				UnitySerializationUtility.DeserializeUnityObject(objectToOverwrite, ref bytes, ref objectReferences, DataFormat.JSON);
			}
			#endif
		}
		
		/// <summary> Generates and returns a deep copy of class member value. </summary>
		/// <param name="subject"> Subject to copy. </param>
		/// <returns> A deep copy of value. For null returns null. For UnityEngine.Objects, returns source as is. </returns>
		public static object Copy([CanBeNull]object subject)
		{
			if(subject == null)
			{
				return null;
			}

			if(subject is Object)
			{
				return subject;
			}

			#if DONT_USE_ODIN_SERIALIZER
			using(var stream = new System.IO.MemoryStream())
			{
				var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				formatter.Serialize(stream, subject);
				stream.Position = 0;
				return formatter.Deserialize(stream);
			}
			#else
			return SerializationUtility.CreateCopy(subject);
			#endif
		}

		public static bool GuessIfUnityWillSerialize([NotNull]System.Reflection.MemberInfo memberInfo)
		{
			return GuessIfUnityWillSerialize(memberInfo, null);
		}

		public static bool GuessIfUnityWillSerialize([NotNull]System.Reflection.MemberInfo memberInfo, [CanBeNull]object value)
		{
			#if DONT_USE_ODIN_SERIALIZER
			if(memberInfo.IsStatic())
			{
				return false;
			}
			var fieldType = memberInfo.DeclaringType;

			if(!(memberInfo is System.Reflection.FieldInfo))
			{
				return false;
			}

			if(!fieldType.IsSerializable)
			{
				return false;
			}

			#if !UNITY_2020_1_OR_NEWER
			// Generic types are not supported, even with the SerializeReference attribute - with the exception of List<>.
			// UPDATE: No longer true since 2020.1 !
			if(fieldType.IsGenericType)
			{
				return fieldType.GetGenericTypeDefinition() == Types.List;
			}
			#endif

			return fieldType.IsSerializable && memberInfo is System.Reflection.FieldInfo;
			#else

			var field = memberInfo as System.Reflection.FieldInfo;
			if(field != null && field.IsInitOnly)
			{
				return false;
			}

			var fieldType = memberInfo.DeclaringType;

			#if !UNITY_2020_1_OR_NEWER
			// Generic types are not supported, even with the SerializeReference attribute - with the exception of List<>.
			// UPDATE: No longer true since 2020.1 !
			if(fieldType.IsGenericType)
			{
				return fieldType.GetGenericTypeDefinition() == Types.List;
			}
			#endif

			#if UNITY_2019_3_OR_NEWER
			// With SerializeReference attribute Unity can serialize interface values if value does not derive from UnityEngine.Object
			if(memberInfo.GetCustomAttributes(typeof(SerializeReference), false).Length > 0)
			{
				return value == null || !Types.UnityObject.IsAssignableFrom(value.GetType());
			}
			#endif

			if(fieldType.IsAbstract)
			{
				return false;
			}

			if(fieldType == Types.SystemObject)
			{
				return false;
			}

			return UnitySerializationUtility.GuessIfUnityWillSerialize(memberInfo);
			#endif
		}

		public static string SerializeReference([CanBeNull]Object target)
		{
			if(target == null)
			{
				return "null";
			}
			
			#if UNITY_EDITOR
			if(UnityEditor.AssetDatabase.IsMainAsset(target))
			{
				return UnityEditor.AssetDatabase.GetAssetPath(target);
			}

			var pathAndId = new AssetPathAndLocalId(target);
			if(pathAndId.HasPath())
			{
				return pathAndId.Serialize();
			}
			#endif

			return StringUtils.ToString(target.GetInstanceID());
		}

		[CanBeNull]
		public static Object DeserializeReference(string content, bool throwErrorIfFailed)
		{
			// support Object ID
			// (non-asset reference copied from an Object field)
			int objectId;
			if(int.TryParse(content, out objectId))
			{
				return InstanceIdUtility.IdToObject(objectId, Types.UnityObject);
			}

			#if UNITY_EDITOR
			// support pasting by path and local file identifier
			// (asset reference copied from an Object field,
			// where target is not the main asset)
			object pathAndIdObject = new AssetPathAndLocalId();
			if(DeserializeOverride(content, typeof(AssetPathAndLocalId), ref pathAndIdObject))
			{
				var pathAndId = (AssetPathAndLocalId)pathAndIdObject;
				if(pathAndId.HasPath())
				{
					return pathAndId.Load();
				}
			}

			// support pasting using asset path
			var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(content);
			if(asset != null)
			{
				return asset;
			}
			#endif

			// support pasting GameObject using hierarchy path
			var go = HierarchyUtility.FindByHierarchyPath(content);
			if(go != null)
			{
				return go;
			}
			// support pasting Component using hierarchy path
			var comp = HierarchyUtility.FindComponentByHierarchyPath(content, Types.Component);
			if(comp != null)
			{
				return comp;
			}

			if(throwErrorIfFailed)
			{
				throw new InvalidOperationException();
			}

			return null;
		}

		public static bool TryDeserializeReference(string stringData, out Object result)
		{
			// support Object ID
			// (non-asset reference copied from an Object field)
			int objectId;
			if(int.TryParse(stringData, out objectId))
			{
				result = InstanceIdUtility.IdToObject(objectId, Types.UnityObject);
				if(result != null)
				{
					return true;
				}
			}

			#if UNITY_EDITOR
			// support pasting by path and local file identifier
			// (asset reference copied from an Object field,
			// where target is not the main asset)
			object pathAndIdObject = new AssetPathAndLocalId();
			if(DeserializeOverride(stringData, typeof(AssetPathAndLocalId), ref pathAndIdObject))
			{
				var pathAndId = (AssetPathAndLocalId)pathAndIdObject;
				if(pathAndId.HasPath())
				{
					result = pathAndId.Load();
					return true;
				}
			}

			// support pasting using asset path
			result = UnityEditor.AssetDatabase.LoadMainAssetAtPath(stringData);
			if(result != null)
			{
				return true;
			}
			#endif

			// support pasting GameObject using hierarchy path
			result = HierarchyUtility.FindByHierarchyPath(stringData);
			if(result != null)
			{
				return true;
			}
			// support pasting Component using hierarchy path
			result = HierarchyUtility.FindComponentByHierarchyPath(stringData, Types.Component);
			if(result != null)
			{
				return result;
			}

			return false;
		}

		public static bool IsSerializedReference(string stringData)
		{
			#if UNITY_EDITOR
			// support reference by guid and local file identifier
			// (asset reference copied from an Object field)
			object pathAndId = new AssetPathAndLocalId();
			if(DeserializeOverride(stringData, typeof(AssetPathAndLocalId), ref pathAndId))
			{
				var guidAndId = (AssetPathAndLocalId)pathAndId;
				if(guidAndId.HasPath())
				{
					return true;
				}
			}
			#endif
				
			// support reference by Object ID
			// (non-asset reference copied from an Object field)
			int objectId;
			if(int.TryParse(stringData, out objectId))
			{
				if(InstanceIdUtility.IdToObject(objectId, Types.UnityObject) != null)
				{
					return true;
				}
			}

			#if UNITY_EDITOR
			// support preference by asset path			
			if(UnityEditor.AssetDatabase.LoadMainAssetAtPath(stringData) != null)
			{
				return true;
			}
			#endif

			// support GameObject reference by hierarchy path
			if(HierarchyUtility.FindByHierarchyPath(stringData) != null)
			{
				return true;
			}

			// support pasting Component using hierarchy path
			if(HierarchyUtility.FindComponentByHierarchyPath(stringData, Types.Component) != null)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Parse color from text string.
		/// Default format: "0.7688679;1;0.9791414;1"
		/// </summary>
		/// <param name="colorAsString"> Color r, g, b and optional a components in text form. </param>
		/// <param name="separator"> Character used to separate the different color components. </param>
		/// <returns> Color </returns>
		public static bool TryParseColor(string colorAsString, out Color color, char separator = ';')
		{
			try
			{
				color = ParseColor(colorAsString, separator);
				return true;
			}
			catch
			{
				color = default(Color);
				return false;
			}
		}

		/// <summary>
		/// Parse color from text string.
		/// Default format: "0.7688679;1;0.9791414;1"
		/// </summary>
		/// <param name="colorAsString"> Color r, g, b and optional a components in text form. </param>
		/// <param name="separator"> Character used to separate the different color components. </param>
		/// <returns> Color </returns>
		public static Color ParseColor(string colorAsString, char separator = ';')
		{
			int i = colorAsString.IndexOf(separator);
			float r = float.Parse(colorAsString.Substring(0, i));
			
			i++;
			int i2 = colorAsString.IndexOf(separator, i);
			float g = float.Parse(colorAsString.Substring(i, i2 - i));

			i++;
			i2 = colorAsString.IndexOf(separator, i);
			float b = float.Parse(colorAsString.Substring(i, i2 - i));

			var color = new Color(r, g, b);

			i++;
			i2 = colorAsString.IndexOf(separator, i);

			// alpha value is optional
			float a;
			if(float.TryParse(colorAsString.Substring(i, i2 - i), out a))
			{
				color.a = a;
			}

			return color;
		}
	}
}