//#define DEBUG_ENABLED

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable]
	public class PrefixColumnWidths : ISerializationCallbackReceiver
	{
		[NonSerialized]
		private readonly Dictionary<Type, float> dictionary = new Dictionary<Type, float>();
		
		[SerializeField]
		private string[] typesSerialized;
		[SerializeField]
		private float[] widthsSerialized;

		public float Get([NotNull]Type type, float defaultWidth)
		{
			float savedWidth;
			if(dictionary.TryGetValue(type, out savedWidth))
			{
				return savedWidth;
			}
			return defaultWidth;
		}

		public bool TryGet([NotNull]Type type, out float savedWidth)
		{
			if(type == null)
			{
				#if DEV_MODE
				Debug.LogWarning("PrefixColumnWidths.TryGet called with null type.");
				#endif
				savedWidth = 0f;
				return false;
			}

			if(dictionary.TryGetValue(type, out savedWidth))
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("Returning cached column width for " + type.Name + ": " + savedWidth);
				#endif

				return true;
			}
			return false;
		}

		public void Save([NotNull]Type type, float width)
		{
			if(type == null)
			{
				#if DEV_MODE
				Debug.LogWarning("PrefixColumnWidths.Save called with null type.");
				#endif
				return;
			}

			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("Saving column width for " + type.Name + ": " + width);
			#endif

			dictionary[type] = width;
		}

		public void Clear([NotNull]Type type)
		{
			#if DEV_MODE && DEBUG_ENABLED
			Debug.Log("Clearing column width for " + type.Name + ".");
			#endif

			if(type == null)
			{
				#if DEV_MODE
				Debug.LogWarning("PrefixColumnWidths.Clear called with null type.");
				#endif
				return;
			}

			dictionary.Remove(type);
		}

		public void OnBeforeSerialize()
		{
			typesSerialized = dictionary.Keys.Select((type)=>type.AssemblyQualifiedName).ToArray();
			widthsSerialized = dictionary.Values.ToArray();
		}

		public void OnAfterDeserialize()
		{
			if(typesSerialized == null || widthsSerialized == null)
			{
				return;
			}

			for(int n = typesSerialized.Length - 1; n >= 0; n--)
			{
				var type = Type.GetType(typesSerialized[n], false);
				if(type != null)
				{
					dictionary[type] = widthsSerialized[n];
				}
			}
		}
	}
}