#define SAFE_MODE
#define GET_SERIALIZED_OBJECT_FROM_EDITOR

//#define ENABLE_MEMBER_CACHING //currently has bugs with all collection members re-using LinkedMemberInfo of first member
#define DISPOSE_SERIALIZED_OBJECT

//#define DEBUG_GET_OR_CREATE_TIME
//#define DEBUG_DISPOSE
//#define DEBUG_CREATE
#define DEBUG_PARENT_CHAIN_BROKEN
//#define DEBUG_CREATE_SERIALIZED_OBJECT
//#define DEBUG_SIDE_EFFECTS

//#define DEBUG_MIXED_CONTENT
//#define DEBUG_NOT_MIXED_CONTENT

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

#if DEV_MODE
using UnityEngine;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	public class LinkedMemberHierarchy
	{
		/// <summary> Utilized by GetFieldOwners. </summary>
		private static readonly Pool<Stack<LinkedMemberInfo>> LinkedMemberStackPool = new Pool<Stack<LinkedMemberInfo>>(1);
		/// <summary> Utilized by GetFieldOwners. </summary>
		private static readonly Pool<List<LinkedMemberInfo>> LinkedMemberListPool = new Pool<List<LinkedMemberInfo>>(1);

		private static Dictionary<Object[], LinkedMemberHierarchy> hierarchies = new Dictionary<Object[], LinkedMemberHierarchy>(new TargetEqualityComparer());

		/// <summary> temporary data utilized by OnHierarchyChange. </summary>
		private static Dictionary<Object[], LinkedMemberHierarchy> hierarchiesCleaned = new Dictionary<Object[], LinkedMemberHierarchy>(new TargetEqualityComparer());

		/// <summary> The target UnityObjects of the hierarchy. </summary>
		private Object[] targets;

		/// <summary> The target when targets are not not of type UnityEngine.Object. </summary>
		private object nonUnityObjectTarget;

		private object[] nonUnityObjectTargets;

		/// <summary> List of all LinkedMemberInfos of the hierarchy. </summary>
		private List<LinkedMemberInfo> members = new List<LinkedMemberInfo>(3);

		/// <summary> True if hierarchy has multiple targets. </summary>
		private bool multiField;

		/// <summary> Number of UnityEngine.Object type targets that the hierarchy has. </summary>
		private int targetCount;

		private int nonUnityObjectTargetCount;

		private int unityObjectOrClassTargetCount;

		#if UNITY_EDITOR
		private static readonly string[] IgnoredPrefabModifications = new string[] { "m_Name", "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w", "m_RootOrder", "m_LocalEulerAnglesHint.x", "m_LocalEulerAnglesHint.y", "m_LocalEulerAnglesHint.z" };
		public readonly bool isPrefabInstance;
		private readonly PropertyModification[][] instanceOverrides;
		[CanBeNull]
		private SerializedObject serializedObject;
		#endif

		#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
		private static ExecutionTimeLogger timer = new ExecutionTimeLogger();
		#endif

		#if UNITY_EDITOR
		[CanBeNull]
		public SerializedObject SerializedObject
		{
			get
			{
				if(serializedObject == null)
				{
					// for classes the targets is a zero-length array
					if(Target != null)
					{
						#if DEV_MODE
						Debug.LogWarning("LinkedMemberInfo.SerializedObject was null. rebuilding from targets");
						#endif
						
						#if GET_SERIALIZED_OBJECT_FROM_EDITOR
						// Fetch serialized object from Editor for targets, to ensure data being in sync
						// and to avoid unecessary garbage generation
						Editor editor = null;
						Editors.GetEditor(ref editor, targets);
						
						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(editor != null, "Editor for targets "+StringUtils.TypesToString(targets)+" was null.");
						#endif

						serializedObject = editor.serializedObject;
						#else
						serializedObject = new SerializedObject(targets);
						#endif

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(serializedObject != null);
						#endif
					}
				}
				// TEMP: fixes issue where calling SerializedObject.Update after GUIStyle array was changed caused a crash
				else
				{
					try
					{
						var test = serializedObject.targetObject;
					} 
					catch(ArgumentNullException)
					{
						#if DEV_MODE
						Debug.LogError(ToString()+".serializedObject.targetObject NullReferenceException with serializedProperty.serializedObject="+StringUtils.Null);
						#endif
						if(Target != null)
						{
							// Fetch serialized object from Editor for targets, to ensure data being in sync
							// and to avoid unecessary garbage generation
							Editor editor = null;
							Editors.GetEditor(ref editor, targets);
						
							#if DEV_MODE && PI_ASSERTATIONS
							Debug.Assert(editor != null, "Editor for targets "+StringUtils.TypesToString(targets)+" was null.");
							#endif

							serializedObject = editor.serializedObject;
						}
						else
						{
							serializedObject = null;
						}
					}
				}

				#if DEV_MODE && PI_ASSERTATIONS
				if(serializedObject != null)
				{
					if(serializedObject.targetObjects.ContainsNullObjects())
					{
						Debug.LogWarning("Hierarchy serializedObject.targetObjects had null members\nserializedObject.targetObjects: "+StringUtils.ToString(serializedObject.targetObjects)+"\ntargets:"+StringUtils.ToString(targets));
					}
				}
				if(targets.ContainsNullObjects())
				{
					Debug.LogWarning("Hierarchy targets had null members. This is normal for Missing Components.\ntargets:"+StringUtils.ToString(targets)+"\nserializedObject.targetObjects: "+(serializedObject == null ? "null" : StringUtils.ToString(serializedObject.targetObjects)));
				}
				#endif

				return serializedObject;
			}

			private set
			{
				serializedObject = value;
			}
		}
		#endif
		
		/// <summary>
		/// Returns a flat read-only collection containing all the LinkedMemberInfos generated for the hierarchy.
		/// Does not necessarily contain LinkedMemberInfos of all class members, but only those that are visible in the inspector.
		/// </summary>
		public IList<LinkedMemberInfo> Members
		{
			get
			{
				return members.AsReadOnly();
			}
		}

		public bool MultiField
		{
			get
			{
				return multiField;
			}
		}

		public int TargetCount
		{
			get
			{
				return targetCount;
			}
		}

		public int NonUnityObjectTargetCount
		{
			get
			{
				return nonUnityObjectTargetCount;
			}
		}

		public int UnityObjectOrClassTargetCount
		{
			get
			{
				return unityObjectOrClassTargetCount;
			}
		}

		/// <summary>
		/// Gets the target UnityEngine.Objects of the hierarchy.
		/// If hierarchy has no targets (e.g. if represents a static class),
		/// returns a zero-size array.
		/// </summary>
		/// <value> The targets. </value>
		[NotNull]
		public Object[] Targets
		{
			get
			{
				return targets;
			}
		}

		/// <summary>
		/// Gets the first target UnityEngine.Object of the hierarchy.
		/// If hierarchy has no targets (e.g. if represents a static class),
		/// returns null.
		/// </summary>
		/// <value> The target. </value>
		[CanBeNull]
		public Object Target
		{
			get
			{
				return targets.Length > 0 ? targets[0] : null;
			}
		}

		/// <summary>
		/// Gets the target System.Object of the hierarchy, when not targeting UnityEngine.Objects.
		/// If hierarchy has no targets (e.g. if represents a static class), returns null.
		/// If hierarchy has UnityEngine.Object type targets, returns null.
		/// </summary>
		/// <value> The target, if has one that is not of type UnityEngine.Object. </value>
		[CanBeNull]
		public object NonUnityObjectTarget
		{
			get
			{
				return nonUnityObjectTarget;
			}
		}

		/// <summary>
		/// Gets the target System.Objects of the hierarchy, when not targeting UnityEngine.Objects.
		/// If hierarchy has no targets (e.g. if represents a static class), returns empty array.
		/// If hierarchy has UnityEngine.Object type targets, returns empty array.
		/// </summary>
		/// <value> The targets, if has ones that are not of type UnityEngine.Object. </value>
		[NotNull]
		public object[] NonUnityObjectTargets
		{
			get
			{
				return nonUnityObjectTargets;
			}
		}

		/// <summary>
		/// Gets the target UnityEngine.Object or System.Object or of the hierarchy.
		/// If hierarchy has no targets (e.g. if represents a static class), returns null.
		/// </summary>
		/// <value> The target. </value>
		[CanBeNull]
		public object UnityObjectOrClassTarget
		{
			get
			{
				return targetCount > 0 ? targets[0] : nonUnityObjectTarget;
			}
		}

		public object[] UnityObjectOrClassTargets
		{
			get
			{
				return targetCount > 0 ? targets : nonUnityObjectTargets;
			}
		}

		public static LinkedMemberHierarchy GetForClass([CanBeNull]object target)
		{
			return new LinkedMemberHierarchy(ArrayPool<Object>.ZeroSizeArray, target);
		}

		public static LinkedMemberHierarchy Get(Object target)
		{
			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.Start("LinkedMemberHierarchy.Get("+StringUtils.ToString(target)+")");
			#endif
			
			#if DEV_MODE && !DEBUG_GET_OR_CREATE_TIME && PI_ASSERTATIONS
			Debug.Assert(target != null);
			#endif

			LinkedMemberHierarchy hierarchy;
			var targets = ArrayPool<Object>.CreateWithContent(target);

			if(hierarchies.TryGetValue(targets, out hierarchy))
			{
				#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
				timer.StartInterval("hierarchies.TryGetValue: reuse existing");
				#endif

				ArrayPool<Object>.Dispose(ref targets);

				#if DEV_MODE && !DEBUG_GET_OR_CREATE_TIME && PI_ASSERTATIONS
				Debug.Assert(hierarchy.targets.Length > 0, "hierarchy for " + StringUtils.ToString(target) + " had " + targets.Length + " targets: " + StringUtils.ToString(targets));
				Debug.Assert(hierarchy.targets.Length == hierarchy.targetCount, "hierarchy for " + StringUtils.ToString(target) + " had " + targets.Length + " targets: " + StringUtils.ToString(targets));
				Debug.Assert(hierarchy.Target != null, "hierarchy for " + StringUtils.ToString(target) + " had " + targets.Length + " targets: " + StringUtils.ToString(targets));
				Debug.Assert(hierarchy.Target == target, "hierarchy for "+StringUtils.ToString(target)+" had " + targets.Length + " targets: " + StringUtils.ToString(targets));
				#endif
				
				#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
				timer.FinishInterval();
				timer.FinishAndLogResults();
				#endif

				return hierarchy;
			}

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.StartInterval("hierarchies.TryGetValue: create new");
			#endif

			hierarchy = new LinkedMemberHierarchy(targets, null);
			//hierarchies.Add(type, hierarchy);
			hierarchies.Add(targets, hierarchy);

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.FinishInterval();
			timer.FinishAndLogResults();
			#endif

			return hierarchy;
		}

		[NotNull]
		public static LinkedMemberHierarchy Get([NotNull]Object[] targets)
		{
			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.Start("LinkedMemberHierarchy.Get("+StringUtils.ToString(targets)+")");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!targets.ContainsNullObjects());
			#endif

			LinkedMemberHierarchy hierarchy;
			if(!hierarchies.TryGetValue(targets, out hierarchy))
			{
				// because LinkedMemberHierarchies can persist even through selection changes,
				// and we can't control what outside systems do with the given targets array reference,
				// we make a copy of the array
				int count = targets.Length;
				var copyArray = ArrayPool<Object>.Create(count);
				Array.Copy(targets, copyArray, count);
				targets = copyArray;

				#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
				timer.StartInterval("hierarchies.TryGetValue: create new");
				#endif

				hierarchy = new LinkedMemberHierarchy(targets, null);
				hierarchies.Add(targets, hierarchy);

				#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
				timer.FinishInterval();
				#endif
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			//Debug.Assert(hierarchy.targets.Length > 0, "hierarchy for " + StringUtils.ToString(targets) + " had " + hierarchy.Targets.Length+" targets: "+StringUtils.ToString(hierarchy.Targets) +". This is okay for a static class.");
			Debug.Assert(hierarchy.targets.Length == hierarchy.targetCount, "hierarchy for " + StringUtils.ToString(targets) + " had " + hierarchy.Targets.Length + " targets: " + StringUtils.ToString(hierarchy.Targets));
			Debug.Assert(hierarchy.Targets.ContentsMatch(targets), "hierarchy for "+StringUtils.ToString(targets) +" had " + hierarchy.Targets.Length + " targets: " + StringUtils.ToString(hierarchy.Targets));
			//Debug.Assert(hierarchy.Target != null, "hierarchy for " + StringUtils.ToString(targets) + " had " + targets.Length + " targets: " + StringUtils.ToString(targets)); //UPDATE: This is normal for MissingScriptDrawer, ClassDrawer
			#endif

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.FinishAndLogResults();
			#endif

			return hierarchy;
		}

		public static LinkedMemberParent GuessParentType([CanBeNull]LinkedMemberInfo parent, [NotNull]FieldInfo fieldInfo)
		{
			if(fieldInfo.IsStatic)
			{
				return LinkedMemberParent.Static;
			}
			else if(parent == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GuessParentType("+ fieldInfo + ") called with null parent. Returning Missing");
				#endif
				return LinkedMemberParent.Missing;
			}
			return LinkedMemberParent.LinkedMemberInfo;
		}

		public static LinkedMemberParent GuessParentType([CanBeNull]LinkedMemberInfo parent, [NotNull]PropertyInfo propertyInfo)
		{
			if(propertyInfo.IsStatic())
			{
				return LinkedMemberParent.Static;
			}
			else if(parent == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GuessParentType("+propertyInfo+") called with null parent. Returning Missing");
				#endif
				return LinkedMemberParent.Missing;
			}
			return LinkedMemberParent.LinkedMemberInfo;
		}

		public static LinkedMemberParent GuessParentType([CanBeNull]LinkedMemberInfo parent, [NotNull]MethodInfo methodInfo)
		{
			if(methodInfo.IsStatic)
			{
				return LinkedMemberParent.Static;
			}
			else if(parent == null)
			{
				#if DEV_MODE
				Debug.LogWarning("GuessParentType("+ methodInfo + ") called with null parent. Returning Missing");
				#endif
				return LinkedMemberParent.Missing;
			}
			return LinkedMemberParent.LinkedMemberInfo;
		}
		
		public static void Dispose(ref LinkedMemberHierarchy hierarchy)
		{
			hierarchy.DisposeMembers();

			#if UNITY_EDITOR && DISPOSE_SERIALIZED_OBJECT
			if(hierarchy.serializedObject != null)
			{
				#if DEV_MODE && DEBUG_DISPOSE
				Debug.Log("hierarchy.SerializedObject.Dispose() ");
				#endif
				hierarchy.serializedObject.Dispose();
				hierarchy.serializedObject = null;
			}
			#endif

			hierarchy = null;
		}

		private LinkedMemberHierarchy([NotNull]Object[] setTargets, [CanBeNull]object setNonUnityObjectClassTarget)
		{
			#if DEV_MODE && DEBUG_CREATE
			Debug.Log("LinkedMemberHierarchy created for targets "+setTargets);
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setTargets != null);
			Debug.Assert(setNonUnityObjectClassTarget == null || !(setNonUnityObjectClassTarget as Object));
			#endif

			targets = setTargets;
			targetCount = setTargets.Length;
			if(targetCount > 0)
			{
				unityObjectOrClassTargetCount = targetCount;
				nonUnityObjectTarget = null;
				nonUnityObjectTargets = ArrayPool<object>.ZeroSizeArray;
				#if UNITY_EDITOR
				isPrefabInstance = false;
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(setNonUnityObjectClassTarget == null);
				#endif
			}
			else
			{
				nonUnityObjectTarget = setNonUnityObjectClassTarget;
				if(setNonUnityObjectClassTarget == null)
				{
					nonUnityObjectTargets = ArrayPool<object>.ZeroSizeArray;
					nonUnityObjectTargetCount = 0;
				}
				else
				{
					nonUnityObjectTargets = ArrayPool<object>.CreateWithContent(setNonUnityObjectClassTarget);
					nonUnityObjectTargetCount = 1;
				}
				unityObjectOrClassTargetCount = nonUnityObjectTargetCount;
			}

			multiField = targetCount > 1;
			#if UNITY_EDITOR
			if(targetCount > 0 && setTargets[0] != null)
			{
				if(!setTargets.AllSameType())
				{
					#if DEV_MODE
					Debug.Log("Won't create SerializedObject from " + setTargets.Length + " targets because of type mismatch.\ntargets: "+StringUtils.TypesToString(setTargets, ", "));
					#endif
					return;
				}

				#if DEV_MODE && DEBUG_CREATE_SERIALIZED_OBJECT
				if(setTargets.Length > 1) Debug.Log("Creating SerializedObject from "+setTargets.Length+" targets: "+StringUtils.TypesToString(setTargets));
				#endif

				SerializedObject = new SerializedObject(setTargets);

				#if UNITY_EDITOR
				isPrefabInstance = setTargets[0].IsPrefabInstance();
				if(isPrefabInstance)
				{
					instanceOverrides = new PropertyModification[targetCount][];
					for(int n = targetCount - 1; n >= 0; n--)
					{
						var target = targets[n];
						instanceOverrides[n] = PrefabUtility.GetPropertyModifications(target);
					}
				}
				#endif

				#if DEV_MODE && DEBUG_CREATE_SERIALIZED_OBJECT
				if(setTargets.Length > 1) Debug.Log("SerializedObject successfully created from "+setTargets.Length+" targets: "+StringUtils.TypesToString(setTargets));
				#endif
			}
			#endif
		}

		public void DisposeMembers()
		{
			for(int n = members.Count - 1; n >= 0; n--)
			{
				var member = members[n];

				#if ENABLE_MEMBER_CACHING
				if(member.IsPersistent)
				{
					continue;
				}
				#endif

				#if DEV_MODE && DEBUG_DISPOSE
				Debug.Log("LinkedMemberHierarchy("+hierarchy.Target.GetType().Name+").Dispose #"+n+": "+member);
				#endif

				members.RemoveAt(n);
				member.Dispose();
			}

			#if !ENABLE_MEMBER_CACHING
			members.Clear();
			#endif
		}

		#if UNITY_EDITOR
		public void UpdatePrefabOverrides()
		{
			if(!isPrefabInstance)
			{
				return;
			}

			SerializedObject.Update();

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				var target = targets[n];
				var modifications = PrefabUtility.GetPropertyModifications(target);
				var previousModifications = instanceOverrides[n];
				instanceOverrides[n] = modifications;

				int count = modifications.Length;
				if(count == previousModifications.Length)
				{
					continue;
				}

				for(int l = members.Count - 1; l >= 0; l--)
				{
					var member = members[l];
					var serializedProperty = member.SerializedProperty;
					if(serializedProperty == null)
					{
						continue;
					}
					
					bool hasBeenModified = false;
					for(int m = count - 1; m >= 0; m--)
					{
						var modification = modifications[m];
						if(string.Equals(modification.propertyPath, serializedProperty.propertyPath))
						{
							hasBeenModified = true;
							break;
						}
					}

					if(serializedProperty.prefabOverride != hasBeenModified)
					{
						#if DEV_MODE
						Debug.Log("Rebuilding SerializedProperty of member "+member+ " because prefab override state has changed.");
						#endif
						member.RebuildSerializedProperty();
					}
				}
			}
		}
		#endif

		public static void OnHierarchyChanged(out bool hadNullTargets)
		{
			hadNullTargets = false;

			foreach(var item in hierarchies)
			{
				var targets = item.Key;
				var hierarchy = item.Value;

				if(!targets.ContainsNullObjects())
				{
					// for classes the targets is a zero-length array
					if(targets.Length > 0)
					{
						#if UNITY_EDITOR
						if(hierarchy.SerializedObject == null || hierarchy.SerializedObject.targetObjects.ContainsNullObjects())
						{
							hierarchy.SerializedObject = new SerializedObject(targets);
						}
						#endif
					}
					
					hierarchiesCleaned[targets] = hierarchy;
				}
				else
				{
					hadNullTargets = true;

					Dispose(ref hierarchy);
				}
			}

			var temp = hierarchies;
			hierarchies = hierarchiesCleaned;
			hierarchiesCleaned = temp;
			hierarchiesCleaned.Clear();
		}

		public Object GetTarget(int index)
		{
			try
			{
				return targets[index];
			}
			catch(IndexOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogWarning("LinkedMemberHierarchy.GetTarget("+index+ ") IndexOutOfRangeException with "+ targets.Length+" targets: "+StringUtils.ToString(targets));
				#endif
				return null;
			}
		}

		public object GetNonUnityObjectTarget(int index)
		{
			try
			{
				return nonUnityObjectTargets[index];
			}
			catch(IndexOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogWarning("LinkedMemberHierarchy.GetTarget("+index+ ") IndexOutOfRangeException with "+ targets.Length+" targets: "+StringUtils.ToString(targets));
				#endif
				return null;
			}
		}

		public object[] GetValues(LinkedMemberInfo member)
		{
			if(member.IsStatic)
			{
				return ArrayPool<object>.CreateWithContent(member.GetStaticValue());
			}

			var results = ArrayPool<object>.Create(unityObjectOrClassTargetCount);
			for(int n = unityObjectOrClassTargetCount - 1; n >= 0; n--)
			{
				results[n] = GetValue(member, n);
			}
			return results;
		}
		
		public T[] GetValues<T>(LinkedMemberInfo member)
		{
			if(member.IsStatic)
			{
				return ArrayPool<T>.CreateWithContent(member.GetStaticValue<T>());
			}

			var results = ArrayPool<T>.Create(unityObjectOrClassTargetCount);
			for(int n = unityObjectOrClassTargetCount - 1; n >= 0; n--)
			{	
				results[n] = (T)GetValue(member, n);
			}
			return results;
		}

		public object GetValue([NotNull]LinkedMemberInfo member, [CanBeNull]object targetUnityObjectOrClass)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member != null);
			Debug.Assert(!member.ParentChainIsBroken, "LinkedMemberHierarchy.GetValue(" + member + ") called with ParentChainIsBroken="+StringUtils.True);
			#endif

			object fieldOwner;
			GetFieldOwner(member, targetUnityObjectOrClass, out fieldOwner);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member.OwnerCanBeNull || fieldOwner != null, "LinkedMemberHierarchy(" + StringUtils.ToString(targets) + ").GetValue(" + member + ", " + StringUtils.ToString(targetUnityObjectOrClass) + "): returned fieldOwner was null. parent="+(member.Parent == null ? "null" : member.Parent.ToString())+", ParentChainIsBroken="+member.ParentChainIsBroken);
			#endif

			return member.GetValue(fieldOwner);
		}

		public object GetValue(LinkedMemberInfo member, int index)
		{
			if(member.IsStatic)
			{
				return member.GetStaticValue();
			}
		
			if(targets.Length == 0)
			{
				if(nonUnityObjectTargets.Length <= index)
				{
					#if DEV_MODE
					Debug.LogWarning("LinkedMemberHierarchy.GetValue(" + member + ", " + index + ") called. member.IsStatic was false but nonUnityObjectTargets.Length="+ nonUnityObjectTargets.Length);
					#endif

					return GetValue(member, null);
				}

				return GetValue(member, nonUnityObjectTargets[index]);
			}

			try
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(targets.Length > index, "LinkedMemberHierarchy("+StringUtils.ToString(targets)+ ").GetValue("+member+", "+index+"): targets.Length "+targets.Length+" < index "+index+"!");
				Debug.Assert(targets[index] != null, "LinkedMemberHierarchy("+StringUtils.ToString(targets)+ ").GetValue("+member+", "+index+"): targets["+index+"] was null!");
				Debug.Assert(member.CanRead, "LinkedMemberHierarchy("+StringUtils.ToString(targets)+ ").GetValue("+member+", "+index+"): ReadOnly was "+StringUtils.False);
				#endif
				return GetValue(member, targets[index]);
			}
			catch(IndexOutOfRangeException)
			{
				#if DEV_MODE
				Debug.LogWarning("LinkedMemberHierarchy.GetValue("+ member+", "+ index + ") IndexOutOfRangeException with "+ targets.Length+" targets: "+StringUtils.ToString(targets));
				#endif
				return GetValue(member, null);
			}
		}

		public bool SetValue(LinkedMemberInfo member, object value)
		{
			//empty array if parameter type?
			var fieldOwners = GetFieldOwners(member);

			return member.SetValues(targets, ref fieldOwners, value);
		}

		public bool SetValues(LinkedMemberInfo member, object[] values)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(values.Length == unityObjectOrClassTargetCount);
			#endif

			var fieldOwners = GetFieldOwners(member);
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member.IsStatic || fieldOwners.Length == values.Length);
			#endif

			return member.SetValues(targets, ref fieldOwners, values);
		}

		/// <summary>
		/// Gets instances of objects that contain the member.
		/// </summary>
		/// <param name="member"> The member whose owners to get. </param>
		/// <returns> An array of object. If member is static or a parameter, returns an empty array. </returns>
		[NotNull]
		public object[] GetFieldOwners(LinkedMemberInfo member)
		{
			if(member.IsStatic)
			{
				return ArrayPool<object>.ZeroSizeArray;
			}

			if(member.ParentChainIsBroken)
			{
				#if DEV_MODE
				Debug.LogWarning(member+".GetFieldOwner returning null because ParentChainIsBroken.");
				#endif
				return ArrayPool<object>.ZeroSizeArray;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member.LinkedMemberType != LinkedMemberType.Parameter, member+" - ParameterDrawer are expected to return true for IsStatic!");
			#endif
			
			var fieldOwners = ArrayPool<object>.Create(unityObjectOrClassTargetCount);

			List<LinkedMemberInfo> parentList;
			if(!LinkedMemberListPool.TryGet(out parentList))
			{
				parentList = new List<LinkedMemberInfo>(3);
			}

			var current = member;
			do
			{
				var parent = current.Parent;
				if(ReferenceEquals(parent, null))
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(current.IsRootMember);
					#endif
					break;
				}
				parentList.Add(parent);
				current = parent;
			}
			while(true);
			
			var getTargets = UnityObjectOrClassTargets;

			for(int t = unityObjectOrClassTargetCount - 1; t >= 0; t--)
			{
				object fieldOwner = getTargets[t];
				for(int p = parentList.Count - 1; p >= 0; p--)
				{
					var parent = parentList[p];
					parent.GetValue(fieldOwner, out fieldOwner);
				}
				fieldOwners[t] = fieldOwner;
			}
			
			parentList.Clear();
			LinkedMemberListPool.Dispose(ref parentList);
			
			return fieldOwners;
		}

		public void GetFieldOwner([NotNull]LinkedMemberInfo member, int index, [CanBeNull]out object fieldOwner)
		{
			GetFieldOwner(member, targets.Length == 0 ? nonUnityObjectTarget : targets[index], out fieldOwner);
		}
		
		public void GetFieldOwner([NotNull]LinkedMemberInfo member, [CanBeNull]object targetUnityObjectOrClass, [CanBeNull]out object fieldOwner)
		{
			if(member.IsStatic)
			{
				fieldOwner = null;
				return;
			}

			if(member.ParentChainIsBroken)
			{
				#if DEV_MODE
				Debug.LogWarning(member+".GetFieldOwner returning null because ParentChainIsBroken.");
				#endif
				fieldOwner = null;
				return;
			}

			Stack<LinkedMemberInfo> parentStack;
			if(!LinkedMemberStackPool.TryGet(out parentStack))
			{
				parentStack = new Stack<LinkedMemberInfo>(3);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(parentStack.Count == 0);
			#endif

			var current = member;
			do
			{
				var parent = current.Parent;
				if(ReferenceEquals(parent, null))
				{
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(current.IsRootMember, current+" parent="+StringUtils.Null+" but IsRootMember="+StringUtils.False);
					#endif
					break;
				}
				parentStack.Push(parent);
				current = parent;
			}
			while(true);

			fieldOwner = targetUnityObjectOrClass;
			for(int n = parentStack.Count - 1; n >= 0; n--)
			{
				var parent = parentStack.Pop();
				fieldOwner = parent.GetValue(fieldOwner);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(parentStack.Count == 0);
			#endif

			LinkedMemberStackPool.Dispose(ref parentStack);
		}		
		
		public bool GetHasMixedContentUpdated(LinkedMemberInfo member)
		{
			if(!multiField)
			{
				#if DEV_MODE && DEBUG_NOT_MIXED_CONTENT
				Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentUpdated(", member.ToString(), "): ", false, " because multiField=", false));
				#endif

				return false;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member.CanRead, "GetHasMixedContentUpdated called for member with !CanRead: "+member);
			#endif
			#if DEV_MODE && DEBUG_SIDE_EFFECTS //currently spams false warnings for transform members. should implement a blacklist for them.
			if(!member.CanReadWithoutSideEffects) { Debug.LogWarning("GetHasMixedContentUpdated called for "+member+" with !CanReadWithoutSideEffects: "+member+". This might be dangerous unless explicitly requested by the user or done with class members we know to be safe?"); }
			#endif

			#if SAFE_MODE
			if(!member.CanRead)
			{
				#if DEV_MODE && DEBUG_NOT_MIXED_CONTENT
				Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentUpdated(", member.ToString(), "): ", false, " because CanRead=", false));
				#endif
				return false;
			}
			#endif

			object firstValue = GetValue(member, 0);
			if(firstValue == null)
			{
				for(int n = unityObjectOrClassTargetCount - 1; n >= 1; n--)
				{
					if(GetValue(member, n) != null)
					{
						#if DEV_MODE && DEBUG_MIXED_CONTENT
						Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentUpdated(", member.ToString(), "): ", true, " because values[" + n + "] != null"));
						#endif
						return true;
					}
				}
				return false;
			}

			for(int n = unityObjectOrClassTargetCount - 1; n >= 1; n--)
			{
				var otherValue = GetValue(member, n);
				if(!firstValue.Equals(otherValue))
				{
					#if DEV_MODE && DEBUG_MIXED_CONTENT
					Debug.Log(StringUtils.ToColorizedString("GetHasMixedContentUpdated(", member.ToString(), "): ", true, " because values["+n+"] ("+ StringUtils.ToString(otherValue) + ") != values[0] (" + StringUtils.ToString(firstValue) + ")"));
					#endif

					return true;
				}
			}
			return false;
		}

		/// <summary> Gets LinkedMemberInfo for field serialized by Unity. </summary>
		/// <param name="parent"> The parent. This may be null. </param>
		/// <param name="field"> The field. This cannot be null. </param>
		/// <param name="serializedPropertyRelativePath"> (Optional) SerializedProperty path relative to parent. </param>
		/// <returns> A LinkedMemberInfo. </returns>
		public LinkedMemberInfo Get([CanBeNull]LinkedMemberInfo parent, [NotNull]FieldInfo field, string serializedPropertyRelativePath)
		{
			return Get(parent, field, GuessParentType(parent, field), serializedPropertyRelativePath);
		}

		/// <summary> Gets LinkedMemberInfo for field. </summary>
		/// <param name="parent"> The parent. This may be null. </param>
		/// <param name="field"> The field. This cannot be null. </param>
		/// <param name="parentType"> (Optional) Type of the parent. </param>
		/// <param name="serializedPropertyRelativePath"> (Optional) SerializedProperty path relative to parent. </param>
		/// <returns> A LinkedMemberInfo. </returns>
		public LinkedMemberInfo Get([CanBeNull]LinkedMemberInfo parent, [NotNull]FieldInfo field, LinkedMemberParent parentType = LinkedMemberParent.LinkedMemberInfo, string serializedPropertyRelativePath = null)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(parentType == LinkedMemberParent.LinkedMemberInfo || parentType == LinkedMemberParent.UnityObject || string.IsNullOrEmpty(serializedPropertyRelativePath), "parentType was "+ parentType+" but serializedPropertyRelativePath was \"" + serializedPropertyRelativePath+"\"");
			#endif

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.Start("LinkedMemberHierarchy.Get("+StringUtils.ToString(parent)+", "+field+")");
			#endif

			#if ENABLE_MEMBER_CACHING
			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.StartInterval("try find cached instance");
			#endif
			foreach(var member in members)
			{
				if(member.Represents(parent, field))
				{
					//Debug.Assert(member.Parent == parent, "member "+member+" Represents "+field+" but member.Parent ("+StringUtils.ToString(member.Parent)+") != "+ StringUtils.ToString(parent));
					
					//don't reuse existing instances if not persistent!
					if(!member.IsPersistent)
					{
						break;
					}
				
					#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
					timer.FinishInterval();
					timer.FinishAndLogResults();
					#endif

					return member;
				}
			}
			#endif

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.FinishInterval();
			timer.StartInterval("create new instance");
			#endif
		
			if(parentType == LinkedMemberParent.Undetermined)
			{
				parentType = LinkedMemberParent.LinkedMemberInfo;
			}

			if(parentType == LinkedMemberParent.LinkedMemberInfo)
			{
				if(field.IsStatic)
				{
					#if DEV_MODE
					Debug.LogWarning(field.Name + " parentType was " + parentType + " but field was static. Changing type to Static.");
					#endif
					parentType = LinkedMemberParent.Static;
				}
				else if(parent == null)
				{
					#if DEV_MODE
					Debug.LogWarning(field.Name + " parentType was " + parentType + " but field parent was null. Changing type to Missing.");
					#endif
					parentType = LinkedMemberParent.Missing;
				}
			}
			else if(field.IsStatic && parentType != LinkedMemberParent.Static)
			{
				#if DEV_MODE
				Debug.LogWarning(field.Name + " parentType was " + parentType + " but field was static. Changing type to Static.");
				#endif
				parentType = LinkedMemberParent.Static;
			}

			#if DEV_MODE || SAFE_MODE
			if(Target == null)
			{
				#if DEV_MODE
				if(serializedPropertyRelativePath != null) { Debug.LogWarning("Setting serializedPropertyRelativePath to "+StringUtils.Null+" because hierarchy Target was "+StringUtils.Null); }
				#endif
				serializedPropertyRelativePath = null;
			}
			#endif

			var created = LinkedMemberInfoPool.Create(this, parent, field, parentType, serializedPropertyRelativePath);
			
			members.Add(created);

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.FinishInterval();
			timer.FinishAndLogResults();
			#endif

			return created;
		}

		/// <summary> Gets LinkedMemberInfo for a property that is serialized by Unity. </summary>
		/// <param name="parent"> The parent. This may be null. </param>
		/// <param name="property"> The property. This cannot be null. </param>
		/// <param name="serializedPropertyRelativePath"> SerializedProperty path relative to parent. </param>
		/// <returns> A LinkedMemberInfo. </returns>
		public LinkedMemberInfo Get([CanBeNull]LinkedMemberInfo parent, [NotNull]PropertyInfo property, string serializedPropertyRelativePath)
		{
			return Get(parent, property, GuessParentType(parent, property), serializedPropertyRelativePath);
		}

		/// <summary> Gets LinkedMemberInfo for a property. </summary>
		/// <param name="parent"> The parent. This may be null. </param>
		/// <param name="property"> The property. This cannot be null. </param>
		/// <param name="parentType"> (Optional) Type of the parent. </param>
		/// <param name="serializedPropertyRelativePath"> (Optional) SerializedProperty path relative to parent, or null if isn't serialized by Unity. </param>
		/// <returns> A LinkedMemberInfo. </returns>
		public LinkedMemberInfo Get([CanBeNull]LinkedMemberInfo parent, [NotNull]PropertyInfo property, LinkedMemberParent parentType = LinkedMemberParent.LinkedMemberInfo, string serializedPropertyRelativePath = null)
		{
			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.Start("LinkedMemberHierarchy.Get("+StringUtils.ToString(parent)+", "+ property + ")");
			#endif

			#if ENABLE_MEMBER_CACHING
			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.StartInterval("try find cached instance");
			#endif
			foreach(var member in members)
			{
				if(member.Represents(parent, property))
				{
					//don't reuse existing instances if not persistent!
					if(!member.IsPersistent)
					{
						break;
					}

					#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
					timer.FinishInterval();
					timer.FinishAndLogResults();
					#endif

					//Debug.Assert(member.Parent == parent);
					return member;
				}
			}
			#endif

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.FinishInterval();
			timer.StartInterval("create new instance");
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(property != null, "LinkedMemberHierarchy.Get(parent="+StringUtils.ToString(parent)+", serializedPropertyPath="+StringUtils.ToString(serializedPropertyRelativePath)+") called with null PropertyInfo!");
			#endif

			#if DEV_MODE || SAFE_MODE
			if(Target == null)
			{
				#if DEV_MODE
				if(serializedPropertyRelativePath != null) { Debug.LogWarning("Setting serializedPropertyRelativePath to "+StringUtils.Null+" because hierarchy Target was "+StringUtils.Null); }
				#endif
				serializedPropertyRelativePath = null;
			}
			#endif

			if(parentType == LinkedMemberParent.Undetermined)
			{
				parentType = LinkedMemberParent.LinkedMemberInfo;
			}

			if(parentType == LinkedMemberParent.LinkedMemberInfo)
			{
				if(property.IsStatic())
				{
					#if DEV_MODE
					Debug.LogWarning(property.Name + " parentType was " + parentType+" but property was static. Changing type to Static.");
					#endif
					parentType = LinkedMemberParent.Static;
				}
				else if(parent == null)
				{
					#if DEV_MODE
					Debug.LogWarning(property.Name + " parentType was " + parentType+ " but property parent was null. Changing type to Missing.");
					#endif
					parentType = LinkedMemberParent.Missing;
				}
			}

			#if DEV_MODE && DEBUG_PARENT_CHAIN_BROKEN
			if(parentType == LinkedMemberParent.Missing) { Debug.Log(StringUtils.ToColorizedString("LinkedMemberHierarchy.Get(parent=", parent, ", property=", property, ") parentType was ", parentType)); }
			#endif

			LinkedMemberInfo created;
			if(property.GetIndexParameters().Length > 0)
			{
				created = LinkedMemberInfoPool.CreateIndexer(this, parent, property, parentType);
			}
			else
			{
				created = LinkedMemberInfoPool.Create(this, parent, property, parentType, serializedPropertyRelativePath);
			}
			members.Add(created);

			#if DEV_MODE && DEBUG_GET_OR_CREATE_TIME
			timer.FinishInterval();
			timer.FinishAndLogResults();
			#endif

			return created;
		}

		/// <summary> Gets LinkedMemberInfo for method. </summary>
		/// <param name="parent"> The parent. </param>
		/// <param name="method"> The method. </param>
		/// <param name="parentType"> (Optional) Type of the parent. </param>
		/// <returns> A LinkedMemberInfo. </returns>
		public LinkedMemberInfo Get(LinkedMemberInfo parent, MethodInfo method, LinkedMemberParent parentType = LinkedMemberParent.LinkedMemberInfo)
		{
			#if ENABLE_MEMBER_CACHING
			foreach(var member in members)
			{
				if(member.Represents(parent, method))
				{
					//don't reuse existing instances if not persistent!
					if(!member.IsPersistent)
					{
						break;
					}
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(member.Parent == parent);
					#endif
					return member;
				}
			}
			#endif
		
			if(parent == null && parentType == LinkedMemberParent.LinkedMemberInfo)
			{
				#if DEV_MODE
				Debug.LogWarning("parentType was "+ parentType+" but parent was null");
				#endif
				parentType = LinkedMemberParent.Missing;
			}

			var created = LinkedMemberInfoPool.Create(this, parent, method, null, parentType);

			if(parentType == LinkedMemberParent.LinkedMemberInfo)
			{
				if(method.IsStatic)
				{
					#if DEV_MODE
					Debug.LogWarning(method.Name + "parentType was " + parentType + " but method was static. Changing type to Static.");
					#endif
					parentType = LinkedMemberParent.Static;
				}
				else if(parent == null)
				{
					#if DEV_MODE
					Debug.LogWarning(method.Name+" parentType was " + parentType + " but parent was null. Changing type to Missing.");
					#endif
					parentType = LinkedMemberParent.Missing;
				}
			}

			members.Add(created);
			return created;
		}

		public LinkedMemberInfo Get(LinkedMemberInfo parent, ParameterInfo parameter)
		{
			#if ENABLE_MEMBER_CACHING
			foreach(var member in members)
			{
				if(member.Represents(parent, parameter))
				{
					//don't reuse existing instances if not persistent!
					if(!member.IsPersistent)
					{
						break;
					}
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(member.Parent == parent);
					#endif
					return member;
				}
			}
			#endif
			var created = LinkedMemberInfoPool.Create(this, parent, parameter);
			members.Add(created);
			return created;
		}

		public LinkedMemberInfo Get(LinkedMemberInfo parent, Type genericTypeArgument, int argumentIndex)
		{
			#if ENABLE_MEMBER_CACHING
			foreach(var member in members)
			{
				if(member.Represents(parent, parameter))
				{
					//don't reuse existing instances if not persistent!
					if(!member.IsPersistent)
					{
						break;
					}
					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(member.Parent == parent);
					#endif
					return member;
				}
			}
			#endif
			var created = LinkedMemberInfoPool.Create(this, parent, genericTypeArgument, argumentIndex);
			members.Add(created);
			return created;
		}
		
		public LinkedMemberInfo GetCollectionMember(LinkedMemberInfo parent, [NotNull]Type type, int collectionIndex, [NotNull]GetCollectionMember get, SetCollectionMember set)
		{
			#if ENABLE_MEMBER_CACHING
			foreach(var member in members)
			{
				if(member.Represents(parent, collectionIndex))
				{
					return member;
				}
			}
			#endif

			var created = LinkedMemberInfoPool.CreateForCollectionMember(this, parent, type, collectionIndex, get, set);
			members.Add(created);
			
			return created;
		}
		
		public LinkedMemberInfo GetCollectionResizer([NotNull]LinkedMemberInfo parent, Type type, GetSize getSizeDelegate, SetSize setSizeDelegate)
		{
			#if ENABLE_MEMBER_CACHING
			var method = getDelegate.Method;
			foreach(var member in members)
			{
				if(member.Represents(parent, method))
				{
					return member;
				}
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(parent != null, " CreateForCollectionResizer was called with null parent!");
			Debug.Assert(type != null, " CreateForCollectionResizer was called with null type!");
			#endif

			var created = LinkedMemberInfoPool.CreateForCollectionResizer(this, parent, type, getSizeDelegate, setSizeDelegate);
			members.Add(created);
			return created;
		}
		
		public void Dispose([NotNull]ref LinkedMemberInfo member)
		{
			#if ENABLE_MEMBER_CACHING
			if(member.IsPersistent)
			{
				return;
			}
			#endif
			if(members.Remove(member))
			{
				member.Dispose();
			}
			#if DEV_MODE
			else
			{
				Debug.LogWarning("LinkedMemberHierarchy("+Target.name+").Dispose - member not found in members list: "+member);
			}
			#endif
		}

		public bool HasMissingTargets()
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(targets[n] == null)
				{
					return true;
				}
			}
			return false;
		}

		public static bool AnyHierarchyHasMissingTargets()
		{
			foreach(var item in hierarchies)
			{
				var targets = item.Key;
				for(int n = targets.Length - 1; n >= 0; n--)
				{
					if(targets[n] == null)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool AnyHierarchyTargetsArrayEquals(Object[] test)
		{
			foreach(var item in hierarchies)
			{
				if(ReferenceEquals(item.Key, test))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasUnappliedChanges()
		{
			if(!isPrefabInstance)
			{
				return false;
			}

			for(int t = targetCount - 1; t >= 0; t--)
			{
				var overrides = instanceOverrides[t];
				int overrideCount = overrides.Length;
				int ignoredCount = IgnoredPrefabModifications.Length;
				if(overrideCount <= ignoredCount)
				{
					continue;
				}

				var serializedProperty = serializedObject.GetIterator();
				if(serializedProperty.Next(true))
				{
					while(serializedProperty.Next(false))
					{
						for(int n = overrideCount - 1; n >= 0; n--)
						{
							if(string.Equals(overrides[n].propertyPath, serializedProperty.propertyPath) && !string.Equals(serializedProperty.propertyPath, "m_Name"))
							{
								#if DEV_MODE
								Debug.Log("Property has unapplied changes: "+ serializedProperty.propertyPath);
								#endif
								return true;
							}
						}
					}
				}

				for(int n = overrideCount - 1; n >= 0; n--)
				{
					if(string.Equals(overrides[n].propertyPath, "m_Enabled"))
					{
						#if DEV_MODE
						Debug.Log("Enabled flag has unapplied changes: "+ overrides[n].propertyPath);
						#endif
						return true;
					}
				}
			}

			return false;
		}

		private class TargetEqualityComparer : IEqualityComparer<Object[]>
		{
			public bool Equals([NotNull]Object[] a, [NotNull]Object[] b)
			{
				int count = a.Length;
				if(count != b.Length)
				{
					return false;
				}
				for(int i = count - 1; i >= 0; i--)
				{
					if(a[i] != b[i])
					{
						return false;
					}
				}
				return true;
			}

			public int GetHashCode([NotNull]Object[] obj)
			{
				int hash = 17;
				for(int i = obj.Length - 1; i >= 0; i--)
				{
					unchecked
					{
						hash = hash * 101 + obj[i].GetHashCode();
					}
				}
				return hash;
			}
		}
	}
}