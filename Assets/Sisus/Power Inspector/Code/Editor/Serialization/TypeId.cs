using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Class representing a type in a format that can be serialized and deserialized easily.
	/// 
	/// Deserialization does not necessarily work after assembly reloads. Especially if available types in any assemblies are changed.
	/// </summary>
	[Serializable]
	public class TypeId : IDisposable
	{
		[SerializeField]
		private int baseId;
		[SerializeField, NotNull]
		private TypeId[] genericTypeIds;
	
		public TypeId()
		{
			baseId = -1;
			genericTypeIds = ArrayPool<TypeId>.ZeroSizeArray;
		}

		public TypeId(Type type)
		{
			if(type.IsGenericType)
			{
				baseId = TypeExtensions.GetTypeId(type.GetGenericTypeDefinition());

				var genericTypes = type.GetGenericArguments();
				int genericTypeCount = genericTypes.Length;
				genericTypeIds = ArrayPool<TypeId>.Create(genericTypeCount);
				for(int n = 0; n < genericTypeCount; n++)
				{
					genericTypeIds[n] = new TypeId(genericTypes[n]);
				}
			}
			else
			{
				baseId = TypeExtensions.GetTypeId(type);
				genericTypeIds = ArrayPool<TypeId>.ZeroSizeArray;
			}
		}
		
		[CanBeNull]
		public Type RepresentedType()
		{
			var baseType = TypeExtensions.GetTypeById(baseId);

			if(baseType == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+" failed to fetch baseType");
				#endif
				return null;
			}

			int genericTypeCount = genericTypeIds.Length;
			if(genericTypeCount == 0)
			{
				return baseType;
			}

			if(!baseType.IsGenericTypeDefinition)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+" had generic types but fetched baseType "+baseType.Name+" was not generic");
				#endif
				return null;
			}
			
			var genericTypes = new Type[genericTypeCount];
			for(int n = 0; n < genericTypeCount; n++)
			{
				var genericType = genericTypeIds[n].RepresentedType();
				if(genericType == null)
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+" failed to fetch generic argument "+(n+1)+"/"+genericTypeCount+" for base type"+baseType.Name);
					#endif
					return null;
				}
				genericTypes[n] = genericType;
			}
			
			var type = baseType.MakeGenericType(genericTypes);
			return type;
		}

		public override string ToString()
		{
			int count = genericTypeIds.Length;
			if(count > 0)
			{
				return StringUtils.Concat("Type<", count,">(", baseId, ")");
			}
			return StringUtils.Concat("Type(", baseId, ")");
		}

		public void Dispose()
		{
			int genericTypeCount = genericTypeIds.Length;
			if(genericTypeCount > 0)
			{
				for(int n = 0; n < genericTypeCount; n++)
				{
					var genericTypeId = genericTypeIds[n];
					genericTypeId.Dispose();
				}
				ArrayPool<TypeId>.Dispose(ref genericTypeIds);
			}
		}
	}
}