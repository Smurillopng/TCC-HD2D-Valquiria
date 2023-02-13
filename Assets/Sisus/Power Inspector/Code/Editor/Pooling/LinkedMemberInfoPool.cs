using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// given a collection, returns its size. If collection is null, return 0.
	/// </summary>
	/// <param name="collection"> Collection whose size to get. </param>
	/// <returns> Collection size, or 0 if collection is null. </returns>
	public delegate object GetSize([CanBeNull]object collection);

	/// <summary>
	/// Resizes collection to given size.
	/// </summary>
	/// <param name="collection"> Collection to resize. </param>
	/// <param name="size"> New size for collection. </param>
	public delegate void SetSize([NotNull]ref object collection, object size);

	//gets property value, utilizing given indexer indexes (that data should be stored elsewhere)
	public delegate object IndexerGet(object propertyOwner, object[] indexParameters);

	//sets property to given value, utilizing given indexer indexes (that data should be stored elsewhere)
	public delegate void IndexerSet(ref object propertyOwner, object[] indexParameters, object value);

	public delegate object GetCollectionMember(object collection, int index);
	public delegate void SetCollectionMember(ref object collection, int index, object value);

	public static class LinkedMemberInfoPool
	{
		private static readonly Pool<LinkedMemberInfo> pool = new Pool<LinkedMemberInfo>(20);

		private static readonly Pool<FieldData> fieldPool = new Pool<FieldData>(10);
		private static readonly Pool<CollectionMemberData> collectionMemberPool = new Pool<CollectionMemberData>(10);
		private static readonly Pool<CollectionResizerData> resizerPool = new Pool<CollectionResizerData>(0);
		private static readonly Pool<PropertyData> propertyPool = new Pool<PropertyData>(0);
		private static readonly Pool<MethodData> methodPool = new Pool<MethodData>(0);
		private static readonly Pool<ParameterData> parameterPool = new Pool<ParameterData>(0);
		private static readonly Pool<GenericTypeArgumentData> genericTypeArgumentPool = new Pool<GenericTypeArgumentData>(0);
		private static readonly Pool<IndexerData> indexerPool = new Pool<IndexerData>(0);
		

		private static LinkedMemberInfo Create([NotNull]LinkedMemberHierarchy hierarchy, [NotNull]MemberData memberData)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(hierarchy != null);
			Debug.Assert(memberData != null);
			#endif

			LinkedMemberInfo created;
			if(!pool.TryGet(out created))
			{
				return new LinkedMemberInfo(hierarchy, memberData);
			}
			created.Hierarchy = hierarchy;
			created.Data = memberData;
			return created;
		}
		
		public static LinkedMemberInfo Create(Object unityObject, MemberInfo[] parents)
		{
			var hierarchy = LinkedMemberHierarchy.Get(unityObject);
			LinkedMemberInfo memberInfo = null;
			for(int n = 0, lastIndex = parents.Length - 1; n <= lastIndex; n++)
			{
				var current = parents[n];
				var fieldInfo = current as FieldInfo;
				if(fieldInfo != null)
				{
					memberInfo = hierarchy.Get(memberInfo, fieldInfo);
					continue;
				}
				var propertyInfo = current as PropertyInfo;
				if(propertyInfo != null)
				{
					memberInfo = hierarchy.Get(memberInfo, propertyInfo);
					continue;
				}

				var methodInfo = current as MethodInfo;
				if(methodInfo != null)
				{
					memberInfo = hierarchy.Get(memberInfo, methodInfo);
					continue;
				}

				Debug.LogError("LinkedMemberInfo.Create - MemberInfo #"+n+" type unsupported: " + StringUtils.ToString(current.GetType()));
			}
			return memberInfo;
		}

		public static LinkedMemberInfo Create(LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parent, MethodInfo getMethodInfo, MethodInfo setMethodInfo, LinkedMemberParent setParentType)
		{
			MethodData memberData;
			if(!methodPool.TryGet(out memberData))
			{
				memberData = new MethodData();
			}
			var created = Create(hierarchy, memberData);
			created.Setup(parent, getMethodInfo, setMethodInfo, setParentType);
			return created;
		}
		
		public static LinkedMemberInfo Create(LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parent, ParameterInfo parameterInfo)
		{
			ParameterData memberData;
			if(!parameterPool.TryGet(out memberData))
			{
				memberData = new ParameterData();
			}
			var created = Create(hierarchy, memberData);
			created.Setup(parent, parameterInfo);
			return created;
		}

		public static LinkedMemberInfo Create(LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parent, Type genericTypeArgument, int argumentIndex)
		{
			GenericTypeArgumentData memberData;
			if(!genericTypeArgumentPool.TryGet(out memberData))
			{
				memberData = new GenericTypeArgumentData();
			}
			var created = Create(hierarchy, memberData);
			created.Setup(parent, genericTypeArgument, argumentIndex);
			return created;
		}

		public static LinkedMemberInfo CreateForCollectionResizer(LinkedMemberHierarchy hierarchy, [NotNull]LinkedMemberInfo parent, [NotNull]Type type, GetSize getSizeDelegate, SetSize setSizeDelegate)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(parent != null, " CreateForCollectionResizer was called with null parent!");
			Debug.Assert(type != null, " CreateForCollectionResizer was called with null type!");
			#endif

			CollectionResizerData memberData;
			if(!resizerPool.TryGet(out memberData))
			{
				memberData = new CollectionResizerData();
			}
			var created = Create(hierarchy, memberData);
			created.SetupCollectionResizer(parent, type, getSizeDelegate, setSizeDelegate);
			return created;
		}
		
		public static LinkedMemberInfo CreateForCollectionMember([NotNull]LinkedMemberHierarchy hierarchy, [NotNull]LinkedMemberInfo parent, Type type, int collectionIndex, GetCollectionMember getDelegate, SetCollectionMember setDelegate)
		{
			CollectionMemberData memberData;
			if(!collectionMemberPool.TryGet(out memberData))
			{
				memberData = new CollectionMemberData();
			}
			var created = Create(hierarchy, memberData);
			created.SetupCollectionMember(parent, type, collectionIndex, getDelegate, setDelegate);
			return created;
		}
		
		public static LinkedMemberInfo Create([NotNull]LinkedMemberHierarchy hierarchy, [CanBeNull]LinkedMemberInfo parent, [NotNull]FieldInfo fieldInfo, LinkedMemberParent parentType, string serializedPropertyPath = null)
		{
			FieldData memberData;
			if(!fieldPool.TryGet(out memberData))
			{
				memberData = new FieldData();
			}
			var created = Create(hierarchy, memberData);
			created.Setup(parent, fieldInfo, parentType, serializedPropertyPath);
			return created;
		}

		public static LinkedMemberInfo Create(LinkedMemberHierarchy hierarchy, LinkedMemberInfo parent, [NotNull]PropertyInfo propertyInfo, LinkedMemberParent parentType, string serializedPropertyPath = null)
		{
			PropertyData memberData;
			if(!propertyPool.TryGet(out memberData))
			{
				memberData = new PropertyData();
			}
			var created = Create(hierarchy, memberData);
			created.Setup(parent, propertyInfo, parentType, serializedPropertyPath);
			return created;
		}

		public static LinkedMemberInfo CreateIndexer(LinkedMemberHierarchy hierarchy, LinkedMemberInfo parent, [NotNull]PropertyInfo propertyInfo, LinkedMemberParent parentType)
		{
			IndexerData memberData;
			if(!indexerPool.TryGet(out memberData))
			{
				memberData = new IndexerData();
			}
			var created = Create(hierarchy, memberData);

			created.SetupIndexer(parent, propertyInfo, parentType);
			return created;
		}
		
		public static void Dispose(LinkedMemberInfo disposing)
		{
			#if DEV_MODE
			Debug.Assert(disposing.Hierarchy == null);
			Debug.Assert(disposing.Data == null);
			#endif
		
			pool.Dispose(ref disposing);
		}

		public static void Dispose(FieldData memberData)
		{
			fieldPool.Dispose(ref memberData);
		}

		public static void Dispose(CollectionMemberData memberData)
		{
			collectionMemberPool.Dispose(ref memberData);
		}

		public static void Dispose(PropertyData memberData)
		{
			#if DEV_MODE
			Debug.Assert(memberData != null);
			#endif

			propertyPool.Dispose(ref memberData);
		}

		public static void Dispose(MethodData memberData)
		{
			methodPool.Dispose(ref memberData);
		}

		public static void Dispose(ParameterData memberData)
		{
			parameterPool.Dispose(ref memberData);
		}

		public static void Dispose(GenericTypeArgumentData memberData)
		{	
			genericTypeArgumentPool.Dispose(ref memberData);
		}

		public static void Dispose(CollectionResizerData memberData)
		{
			resizerPool.Dispose(ref memberData);
		}

		public static void Dispose(IndexerData memberData)
		{
			indexerPool.Dispose(ref memberData);
		}
	}
}