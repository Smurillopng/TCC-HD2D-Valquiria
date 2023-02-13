using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class PersistentSingletonSerialized : IComparable<PersistentSingletonSerialized>
	{
		public const int DefaultDeserializationOrder = 50;

		[NonSerialized]
		public readonly int deserializationOrder = DefaultDeserializationOrder;

		[SerializeField]
		private string typeName;
		[SerializeField]
		private byte[] serializedBytes;

		/// <summary> Initializes PersistentSingletonSerialized based on class instance. </summary>
		/// <param name="target"> PersistentSingleton instance to serialize. If implements IBinarySerializable, state is also serialized using its methods. </param>
		/// <param name="formatter"> The formatter that target can use during serialization if it implements IBinarySerializable. </param>
		public PersistentSingletonSerialized(object target, BinaryFormatter formatter)
		{
			typeName = target.GetType().AssemblyQualifiedName;
			
			var binarySerializable = target as IBinarySerializable;
			if(binarySerializable == null)
			{
				deserializationOrder = DefaultDeserializationOrder;
				return;
			}
			serializedBytes = binarySerializable.Serialize(formatter);
			var setOrder = binarySerializable.DeserializationOrder;
			deserializationOrder = setOrder.HasValue ? setOrder.Value : DefaultDeserializationOrder;
		}

		/*
		[CanBeNull]
		public object Deserialize(BinaryFormatter formatter)
		{
			var type = Type.GetType(typeName, false);
			if(type == null)
			{
				return null;
			}

			var instance = type == Types.ScriptableObject ? ScriptableObject.CreateInstance(type) : Activator.CreateInstance(type);
			if(serializedBytes != null)
			{
				var binarySerializable = instance as IBinarySerializable;
				if(binarySerializable != null)
				{
					using(var stream = new MemoryStream(serializedBytes, false))
					{
						binarySerializable.DeserializeOverride(formatter, stream);
					}
				}
			}
			return instance;
		}
		*/

		public void Deserialize(BinaryFormatter formatter, ref System.Collections.Generic.Dictionary<Type, object> addToDictionary)
		{
			var type = Type.GetType(typeName, false);
			if(type == null)
			{
				return;
			}

			var instance = type == Types.ScriptableObject ? ScriptableObject.CreateInstance(type) : Activator.CreateInstance(type);

			try
			{
				addToDictionary.Add(instance.GetType(), instance);
			}
			catch(ArgumentException) //TEMP
			{
				#if DEV_MODE
				Debug.LogError("PersistentSingletonSerialized.Deserialize - Dictionary already contained instance of "+instance.GetType().Name
				+"\ninstance:\n"+StringUtils.ToString(addToDictionary, "\n"));
				#endif
				return;
			}

			if(serializedBytes != null)
			{
				var binarySerializable = instance as IBinarySerializable;
				if(binarySerializable != null)
				{
					using(var stream = new MemoryStream(serializedBytes, false))
					{
						binarySerializable.DeserializeOverride(formatter, stream);
					}
				}
			}
		}

		public int CompareTo(PersistentSingletonSerialized other)
		{
			if(ReferenceEquals(this, other))
				return 0;
			if(ReferenceEquals(null, other))
				return 1;
			return deserializationOrder.CompareTo(other.deserializationOrder);
		}
	}
}