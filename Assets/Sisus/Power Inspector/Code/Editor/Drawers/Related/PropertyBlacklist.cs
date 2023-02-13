using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	[InitializeOnLoad]
	public static class PropertyBlacklist
	{
		private static readonly Dictionary<Type, HashSet<string>> blacklist = new Dictionary<Type, HashSet<string>>(20)
		{
			{ typeof(Renderer), new HashSet<string>()
			{
				"material", "materials" // Calling this instantiates a new copy of the material(s) and causes memory leaking in the Editor.
			}},
			{ typeof(MeshFilter), new HashSet<string>()
			{
				"mesh" // Calling this instantiates a new copy of the mesh and causes memory leaking in the Editor.
			}},
			{ typeof(Collider), new HashSet<string>()
			{
				"material" // Calling this creates a new copy of the material and causes memory leaking in the Editor.
			}},
			{ typeof(Animator), new HashSet<string>()
			{
				"GetPlaybackTime", // Can't call GetPlaybackTime while not in playback mode. You must call StartPlayback before.
				"playbackTime", // Can't call GetPlaybackTime while not in playback mode. You must call StartPlayback before.
				"bodyRotation", // Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK
				"bodyPosition", // Setting and getting Body Position/Rotation, IK Goals, Lookat and BoneLocalRotation should only be done in OnAnimatorIK or OnStateIK
				"layerCount", // Animator is not playing an AnimatorController
				"parameters",  // Animator is not playing an AnimatorController
				"parameterCount"  // Animator is not playing an AnimatorController
			}},
		};

		static PropertyBlacklist()
		{
			EditorApplication.delayCall += ApplyPreferences;
		}

		#if !UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod]
		#endif
		private static void ApplyPreferences()
		{
			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += ApplyPreferences;
				return;
			}

			if(InspectorUtility.ActiveManager == null || !InspectorUtility.Preferences.SetupDone)
			{
				EditorApplication.delayCall += ApplyPreferences;
				return;
			}

			var addProperties = InspectorUtility.Preferences.propertyBlacklist;
			for(int n = addProperties.Length - 1; n >= 0; n--)
			{
				if(!Add(addProperties[n].ownerTypeName, addProperties[n].propertyName))
				{
					Debug.LogWarning("Blacklisted property " + addProperties[n].ownerTypeName+"." + addProperties[n].propertyName + " does not exist.");
				}
			}
		}

		public static void Add([NotNull]PropertyInfo property)
		{
			var ownerType = property.DeclaringType;
			string propertyName = property.Name;
			AddSingle(ownerType, propertyName);

			if(ownerType.IsValueType || ownerType.IsSealed)
			{
				return;
			}

			foreach(var extendingType in ownerType.GetExtendingTypes(true, false))
			{
				AddSingle(extendingType, propertyName);
			}
		}

		public static bool Add([NotNull]string ownerTypeName, [NotNull]string propertyName)
		{
			var ownerType = TypeExtensions.GetType(ownerTypeName);
			if(ownerType == null)
			{
				return false;
			}

			var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
			if(property != null)
			{
				Add(property);
				return true;
			}

			for(var baseType = ownerType.BaseType; baseType != null; baseType = baseType.BaseType)
			{
				property = baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				if(property != null)
				{
					Add(property);
					return true;
				}
			}
			return false;
		}

		public static bool Add([NotNull]Type ownerType, [NotNull]string propertyName)
		{
			var property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
			if(property != null)
			{
				Add(property);
				return true;
			}

			for(var baseType = ownerType.BaseType; baseType != null; baseType = baseType.BaseType)
			{
				property = baseType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
				if(property != null)
				{
					Add(property);
					return true;
				}
			}
			return false;
		}

		private static void AddSingle([NotNull]Type ownerType, [NotNull]string propertyName)
		{
			HashSet<string> set;
			if(!blacklist.TryGetValue(ownerType, out set))
			{
				blacklist.Add(ownerType, new HashSet<string>() { propertyName });
				return;
			}
			set.Add(propertyName);
		}

		public static bool IsBlacklisted([NotNull]PropertyInfo property)
		{
			HashSet<string> set;
			if(!blacklist.TryGetValue(property.DeclaringType, out set))
			{
				return false;
			}
			return set.Contains(property.Name);
		}

		public static bool IsBlacklisted([NotNull]Type ownerType, [NotNull]string propertyName)
		{
			HashSet<string> set;
			if(!blacklist.TryGetValue(ownerType, out set))
			{
				return false;
			}
			return set.Contains(propertyName);
		}

		public static bool TryGetBlacklist([NotNull]Type ownerType, [CanBeNull]out HashSet<string> blacklistedProperties)
		{
			return blacklist.TryGetValue(ownerType, out blacklistedProperties);
		}

		[CanBeNull]
		public static HashSet<string> GetBlacklist([NotNull]Type ownerType)
		{
			HashSet<string> set;
			if(blacklist.TryGetValue(ownerType, out set))
			{
				return set;
			}
			return null;
		}
	}
}