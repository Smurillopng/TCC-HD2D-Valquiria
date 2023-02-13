using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable]
	public class SerializableMemberInfo : IDisposable
	{
		private readonly Stack<Data> memberStack;
		private readonly int[] targetReferences;
		
		private SerializableMemberInfo(LinkedMemberInfo memberInfo)
		{
			int parentCount = 0;
			for(var parent = memberInfo.Parent; parent != null; parent = parent.Parent)
			{
				parentCount++;
			}
			memberStack = new Stack<Data>(parentCount + 1);
			var next = memberInfo;
			for(int n = parentCount; n >= 0; n--)
			{
				memberStack.Push(new Data(next));
				next = next.Parent;
			}

			var targets = memberInfo.UnityObjects;
			int targetCount = targets.Length;
			targetReferences = new int[targetCount];
			for(int n = targetCount - 1; n >= 0; n--)
			{
				var target = targets[n];
				targetReferences[n] = ObjectIds.Get(target);
			}
		}
		
		public static byte[] Serialize([NotNull]LinkedMemberInfo memberInfo)
		{
			using(var serialize = new SerializableMemberInfo(memberInfo))
			{
				return serialize.Serialize();
			}
		}

		[CanBeNull]
		public static LinkedMemberInfo Deserialize(byte[] bytes)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(bytes != null);
			#endif

			try
			{
				using(var stream = new MemoryStream())
				{
					var formatter = new BinaryFormatter();
					stream.Write(bytes, 0, bytes.Length);
					stream.Seek(0, SeekOrigin.Begin);
					SerializableMemberInfo deserialized;
					deserialized = formatter.Deserialize(stream) as SerializableMemberInfo;
					int count = deserialized.targetReferences.Length;
					var targets = ArrayPool<Object>.Create(count);
					for(int n = count - 1; n >= 0; n--)
					{
						targets[n] = ObjectIds.GetTarget(deserialized.targetReferences[n]);
					}

					var hierarchy = LinkedMemberHierarchy.Get(targets);
				
					LinkedMemberInfo result = null;
					var memberStack = deserialized.memberStack;

					// TO DO: serialize parent info better for more reliable fetching

					for(int n = memberStack.Count - 1; n >= 0; n--)
					{
						var member = memberStack.Pop();
						result = member.Deserialize(hierarchy);
					}
					return result;
				}
			}
			#if DEV_MODE
			catch(Exception e)
			{
				UnityEngine.Debug.LogWarning(e);
			#else
			catch
			{
			#endif
				return null;
			}
		}

		private byte[] Serialize()
		{
			var formatter = new BinaryFormatter();
			using(var stream = new MemoryStream())
			{
				formatter.Serialize(stream, this);
				return stream.ToArray();
			}
		}
		
		public void Dispose()
		{
			for(int n = memberStack.Count - 1; n >= 0; n--)
			{
				var member = memberStack.Pop();
				member.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		[Serializable]
		private class Data : IDisposable
		{
			// name of parent LinkedMemberInfo, or null if has no parent
			[CanBeNull]
			private readonly string parentName;
			private readonly LinkedMemberType linkedMemberType;
			private readonly bool parentChainIsBroken;
			private readonly Type type;
			private readonly ICustomAttributeProvider attributeProvider;
			private readonly int collectionIndex;
			private readonly SerializedDelegate getDelegateSerialized;
			private readonly SerializedDelegate setDelegateSerialized;
			
			#if UNITY_EDITOR
			private readonly string serializedPropertyFullPath;
			#endif

			public Data(LinkedMemberInfo member)
			{
				linkedMemberType = member.LinkedMemberType;
				parentChainIsBroken = member.ParentChainIsBroken;
				
				var parent = member.Parent;
				parentName = parent == null ? null : parent.Name;

				// UPDATE: Only serializing type when it's necessary to both improve performance
				// and reduce risk of exceptions
				switch(linkedMemberType)
				{
					case LinkedMemberType.CollectionResizer:
						type = member.Type;
						var getDelegate = member.GetDelegate;
						if(getDelegate != null)
						{
							getDelegateSerialized = new SerializedDelegate(getDelegate);
						}
						var setDelegate = member.SetDelegate;
						if(setDelegate != null)
						{
							setDelegateSerialized = new SerializedDelegate(setDelegate);
						}
						break;
					case LinkedMemberType.CollectionMember:
						type = member.Type;
						collectionIndex = member.CollectionIndex;
						getDelegate = member.GetDelegate;
						if(getDelegate != null)
						{
							getDelegateSerialized = new SerializedDelegate(getDelegate);
						}
						setDelegate = member.SetDelegate;
						if(setDelegate != null)
						{
							setDelegateSerialized = new SerializedDelegate(setDelegate);
						}
						break;
					case LinkedMemberType.GenericTypeArgument:
						collectionIndex = member.CollectionIndex;
						break;
				}
				
				attributeProvider = member.AttributeProvider;

				#if UNITY_EDITOR
				serializedPropertyFullPath = member.SerializedProperty != null ? member.SerializedProperty.propertyPath : null;
				#endif
			}
			
			public LinkedMemberInfo Deserialize(LinkedMemberHierarchy hierarchy)
			{
				LinkedMemberInfo parent = null;
				var parentType = parentChainIsBroken ? LinkedMemberParent.Missing : LinkedMemberParent.UnityObject;
				if(parentName != null)
				{
					var allInfos = hierarchy.Members;
					for(int n = allInfos.Count - 1; n >= 0; n--)
					{
						if(string.Equals(allInfos[n].Name, parentName))
						{
							parent = allInfos[n];
							if(parent.IsStatic)
							{
								parentType = LinkedMemberParent.Static;
							}
							else
							{
								parentType = LinkedMemberParent.LinkedMemberInfo;
							}
							break;
						}
					}

					#if DEV_MODE && PI_ASSERTATIONS
					UnityEngine.Debug.Assert(parent != null, "SerializableMemberInfo.Deserialize: unable to find parent by name \""+parentName+"\"");
					#endif
				}

				LinkedMemberInfo result;

				#if UNITY_EDITOR
				string serializedPropertyRelativePath = serializedPropertyFullPath;
				if(!string.IsNullOrEmpty(serializedPropertyRelativePath))
				{
					int i = serializedPropertyRelativePath.LastIndexOf('.');
					if(i != -1)
					{
						serializedPropertyRelativePath = serializedPropertyRelativePath.Substring(i + 1);
					}
				}
				#else
				string serializedPropertyRelativePath = null;
				#endif

				switch(linkedMemberType)
				{
					case LinkedMemberType.Field:
						var fieldInfo = attributeProvider as FieldInfo;
						if(fieldInfo.IsStatic)
						{
							parentType = LinkedMemberParent.Static;
						}
						result = hierarchy.Get(parent, fieldInfo, parentType, serializedPropertyRelativePath);
						break;
					case LinkedMemberType.CollectionMember:
						var getMember = getDelegateSerialized.Deserialize<GetCollectionMember>();
						var setMember = setDelegateSerialized.Deserialize<SetCollectionMember>();
						#if DEV_MODE && PI_ASSERTATIONS
						UnityEngine.Debug.Assert(getMember != null || setMember != null, "CollectionMember with type="+StringUtils.ToStringSansNamespace(type)+ " and parentName=\""+ parentName+ "\" get and set were null with getDelegateSerialized="+getDelegateSerialized);
						#endif
						result = hierarchy.GetCollectionMember(parent, type, collectionIndex, getMember, setMember);
						break;
					case LinkedMemberType.Property:
					case LinkedMemberType.Indexer:
						var propertyInfo = attributeProvider as PropertyInfo;
						if(propertyInfo.IsStatic())
						{
							parentType = LinkedMemberParent.Static;
						}
						result = hierarchy.Get(parent, propertyInfo, parentType, serializedPropertyRelativePath);
						break;
						case LinkedMemberType.Method:
						var methodInfo = attributeProvider as MethodInfo;
						if(methodInfo.IsStatic)
						{
							parentType = LinkedMemberParent.Static;
						}
						result = hierarchy.Get(parent, methodInfo, parentType);
						break;
					case LinkedMemberType.Parameter:
						result = hierarchy.Get(parent, attributeProvider as ParameterInfo);
						break;
					case LinkedMemberType.GenericTypeArgument:
						result = hierarchy.Get(parent, attributeProvider as Type, collectionIndex);
						break;
					case LinkedMemberType.CollectionResizer:
						var get = getDelegateSerialized.Deserialize<GetSize>();
						var set = setDelegateSerialized.Deserialize<SetSize>();
						result = hierarchy.GetCollectionResizer(parent, type, get, set);
						break;
					default:
						throw new NotSupportedException("Deserializing LinkedMemberInfo of type " + linkedMemberType);
				}

				#if UNITY_EDITOR
				if(serializedPropertyFullPath != null)
				{
					result.SetSerializedProperty(hierarchy.SerializedObject.FindProperty(serializedPropertyFullPath));
				}
				#endif

				return result;
			}

			public void Dispose() { }
		}
	}
}