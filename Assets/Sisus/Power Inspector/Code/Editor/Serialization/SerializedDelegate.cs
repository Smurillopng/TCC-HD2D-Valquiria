using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class representing a delegate in a format that can be serialized and deserialized easily.
	/// 
	/// Only supports delegates representing static methods.
	/// 
	/// Deserialization does not necessarily work after assembly reloads. Especially if available types in any assemblies are changed.
	/// </summary>
	[Serializable]
	public class SerializedDelegate : IDisposable
	{
		[SerializeField]
		private byte[] bytes;
		
		public SerializedDelegate()
		{
			bytes = ArrayPool<byte>.ZeroSizeArray;
		}

		public SerializedDelegate(Delegate serialize)
		{
			if(serialize == null)
			{
				bytes = ArrayPool<byte>.ZeroSizeArray;
				return;
			}

			var formatter = new BinaryFormatter();
			using(var stream = new MemoryStream())
			{
				formatter.Serialize(stream, serialize);
				bytes = stream.ToArray();
			}
		}

		[CanBeNull]
		public T Deserialize<T>() where T : class
		{
			using (var stream = new MemoryStream(bytes))
			{
				var formatter = new BinaryFormatter();
				return formatter.Deserialize(stream) as T;
			}
		}

		public void Dispose()
		{
			
		}

		public override string ToString()
		{
			return "SerializedDelegate";
		}
	}
}