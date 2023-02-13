using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	///	Utility class that holds information about which Types cannot co-exist within the same GameObject instance.
	///	</summary>
	[InitializeOnLoad]
	public static class AddComponentUtility
	{
		private static volatile SetupPhase setupDone = SetupPhase.Unstarted;

		private static Dictionary<Type,Type[]> conflictingTypes;
		private static HashSet<Type> onlyComponentTypes;
		private static HashSet<Type> invalidComponentTypes;

		private static readonly object threadLock = new object();

		/// <summary>
		/// this is initialized on load due to the usage of the InitializeOnLoad attribute
		/// </summary>
		[UsedImplicitly]
		static AddComponentUtility()
		{
			EditorApplication.delayCall += Setup;
		}

		public static bool IsReady
		{
			get
			{
				lock(threadLock)
				{
					return setupDone == SetupPhase.Done;
				}
			}
		}

		private static void Setup()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += Setup;
				return;
			}

			lock(threadLock)
			{
				if(setupDone != SetupPhase.Unstarted)
				{
					return;
				}
				setupDone = SetupPhase.InProgress;
			}

			ThreadPool.QueueUserWorkItem(SetupThreaded, InspectorUtility.Preferences.addComponentMenuConfig);
		}

		private static void SetupThreaded(object state)
        {
			#if DEV_MODE
			var timer = new ExecutionTimeLogger();
			timer.Start("AddComponentUtility.SetupThreaded");
			#endif

			BuildConflictingTypesDictionary((AddComponentMenuConfig)state);

			#if DEV_MODE
			timer.FinishAndLogResults();
			#endif
        }

		/// <summary>
		/// Determines whether or not adding new components to GameObjects
		/// that contain the specified components is allowed.
		/// </summary>
		/// <param name="componentsByTarget"></param>
		/// <returns></returns>
		public static bool CanAddComponents(List<Component[]> componentsByTarget)
		{
			if(!IsReady)
            {
				#if DEV_MODE
				Debug.LogWarning("CanAddComponents called before IsReady was true. Returning true.");
				#endif
				return true;
            }

			#if DEV_MODE && DEBUG_CAN_ADD_COMPONENTS
			Debug.Log("CanAddComponents("+StringUtils.ToString(componentsByTarget)+") checking "+ onlyComponentTypes.Count+ " onlyComponentTypes: " + StringUtils.ToString(onlyComponentTypes));
			#endif

			for(int t = componentsByTarget.Count - 1; t >= 0; t--)
			{
				var components = componentsByTarget[t];
				for(int c = components.Length - 1; c >= 0; c--)
				{
					var component = components[c];
					if(component != null)
					{
						var type = component.GetType();
						if(onlyComponentTypes.Contains(type) || invalidComponentTypes.Contains(type))
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Gets list of existing components drawers on GameObject drawer that prevent component of given type from being added to the target gameobject(s).
		/// </summary>
		/// <param name="type"> Component type to check. </param>
		/// <param name="target"> GameObject drawer. </param>
		/// <param name="conflictingMembers"> Any found conflicting drawers will be added to this list </param>
		public static void GetConflictingMembers(Type type, IGameObjectDrawer target, [NotNull]ref List<IComponentDrawer> conflictingMembers)
		{
			if(!IsReady)
			{
				#if DEV_MODE
				Debug.LogWarning("GetConflictingMembers called before IsReady was true");
				#endif
				return;
			}

			Type[] types;
			if(!conflictingTypes.TryGetValue(type, out types))
			{
				return;
			}

			int count = types.Length;
			if(count == 0)
			{
				return;
			}

			foreach(var componentDrawer in target)
			{
				var memberType = componentDrawer.Type;
				for(int t = count - 1; t >= 0; t--)
				{
					var conflictingType = types[t];
					if(conflictingType.IsAssignableFrom(memberType))
					{
						conflictingMembers.Add(componentDrawer);
						break;
					}
				}
			}
		}

		private static void BuildConflictingTypesDictionary(AddComponentMenuConfig addComponentMenuConfig)
		{
			invalidComponentTypes = TypeExtensions.GetInvalidComponentTypesThreadSafe();

			conflictingTypes = new Dictionary<Type, Type[]>(20);

			onlyComponentTypes = new HashSet<Type>();

			AddConflictPair(typeof(MeshFilter), typeof(TextMesh));

			AddConflictPair(typeof(Rigidbody), typeof(Rigidbody2D));
			AddConflictPair(typeof(Rigidbody), typeof(Collider2D));
			AddConflictPair(typeof(Rigidbody), typeof(Joint2D));
			AddConflictPair(typeof(Rigidbody), typeof(Effector2D));
			AddConflictPair(typeof(Rigidbody), typeof(PhysicsUpdateBehaviour2D));

			AddConflictPair(typeof(Collider), typeof(Rigidbody2D));
			AddConflictPair(typeof(Collider), typeof(Collider2D));
			AddConflictPair(typeof(Collider), typeof(Joint2D));
			AddConflictPair(typeof(Collider), typeof(Effector2D));
			AddConflictPair(typeof(Collider), typeof(PhysicsUpdateBehaviour2D));

			AddConflictPair(typeof(ConstantForce), typeof(Rigidbody2D));
			AddConflictPair(typeof(ConstantForce), typeof(Collider2D));
			AddConflictPair(typeof(ConstantForce), typeof(Joint2D));
			AddConflictPair(typeof(ConstantForce), typeof(Effector2D));
			AddConflictPair(typeof(ConstantForce), typeof(PhysicsUpdateBehaviour2D));

			AddDisallowMultiple(typeof(AudioListener));
			AddDisallowMultiple(typeof(Camera));
			AddDisallowMultiple(typeof(FlareLayer));
			AddDisallowMultiple(typeof(MeshFilter));
			AddDisallowMultiple(typeof(MeshRenderer));
			AddDisallowMultiple(typeof(ParticleSystem));
			AddDisallowMultiple(typeof(Rigidbody));
			AddDisallowMultiple(typeof(Rigidbody2D));
			AddDisallowMultiple(typeof(SkinnedMeshRenderer));
			AddDisallowMultiple(typeof(TextMesh));
			AddDisallowMultiple(typeof(TrailRenderer));
			AddDisallowMultiple(typeof(Light));
			AddDisallowMultiple(typeof(RectTransform));

			foreach(var type in TypeExtensions.GetAllTypesThreadSafe(typeof(Component).Assembly, false, false, true))
			{
				if(!type.IsComponent() || type.IsGenericTypeDefinition || type.IsBaseComponentType())
				{
					continue;
				}

				if(type.IsDefined(Types.DisallowMultipleComponent, false))
				{
					AddDisallowMultiple(type);
				}
				else if(type.IsDefined(typeof(Attributes.OnlyComponentAttribute), true))
				{
					AddOnlyComponent(type);
				}
			}

			lock(threadLock)
			{
				setupDone = SetupPhase.Done;
			}
		}

		public static bool HasConflictingMembers(Type type, IGameObjectDrawer target)
		{
			if(!IsReady)
            {
				#if DEV_MODE
				Debug.LogWarning("HasConflictingMembers called before IsReady was true. Returning false.");
				#endif
				return false;
            }

			if(type == null)
			{
				return false;
			}

			Type[] types;
			if(!conflictingTypes.TryGetValue(type, out types))
			{
				return false;
			}

			int count = types.Length;
			if(count == 0)
            {
				return false;
            }

			foreach(var componentDrawer in target)
			{
				var memberType = componentDrawer.Type;
				for(int t = count - 1; t >= 0; t--)
				{
					var conflictingType = types[t];
					if(conflictingType.IsAssignableFrom(memberType))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// NOTE: Do not call this before first calling BuildConflictingTypesDictionaryIfDoesNotExist
		/// Or you might get a null reference exception
		/// </summary>
		private static void AddDisallowMultiple(Type a)
		{
			AddConflictWithoutExtendingTypes(a, a);
		}

		private static void AddOnlyComponent(Type onlyComponent)
		{
			#if DEV_MODE && DEBUG_CAN_ADD_COMPONENTS
			Debug.Log("AddOnlyComponent: "+onlyComponent);
			#endif
			onlyComponentTypes.Add(onlyComponent);
		}

		private static void AddConflictPair(Type a, Type b)
		{
			AddConflictPairWithoutExtendingTypes(a, b);
			
			var aExtended = a.GetExtendingTypes(false, false);
			var bExtended = b.GetExtendingTypes(false, false);
			
			foreach(var ae in aExtended)
			{
				AddConflictPairWithoutExtendingTypes(ae, b);
				foreach(var be in bExtended)
				{
					AddConflictPairWithoutExtendingTypes(ae, be);
				}
			}

			foreach(var be in bExtended)
			{
				AddConflictPairWithoutExtendingTypes(a, be);
			}
		}

		private static void AddConflictPairWithoutExtendingTypes(Type a, Type b)
		{
			AddConflictWithoutExtendingTypes(a, b);

			#if DEV_MODE
			Debug.Assert(a != b);
			#endif

			AddConflictWithoutExtendingTypes(b, a);
		}

		private static void AddConflictWithoutExtendingTypes(Type a, Type b)
		{
			if(a.IsAbstract || b.IsAbstract)
			{
				return;
			}

			Type[] types;
			if(!conflictingTypes.TryGetValue(a, out types))
			{
				types = new Type[1];
				types[0] = b;
			}
			else
			{
				#if DEV_MODE
				if(Array.IndexOf(types, b) != -1)
				{
					Debug.LogWarning("Type "+b.FullName+" had already been registered to conflict with "+a.FullName);
					return;
				}
				#endif

				int count = types.Length;
				Array.Resize(ref types, count + 1);
				types[count] = b;
			}

			conflictingTypes[a] = types;
		}
	}
}