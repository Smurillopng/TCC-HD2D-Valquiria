//#define DEBUG_IS_ENABLED

using JetBrains.Annotations;
using System;
using System.Reflection;
using UnityEngine;

namespace Sisus
{
	public static class ComponentExtensions
	{
		/// <summary>
		/// Determines whether or not the component in question has the enabled property.
		/// 
		/// Note that this even if this is true it does not mean that the enabled toggle
		/// control is shown in the inspector! Use HasEnabledFlagInEditor to determines that!
		/// </summary>
		/// <param name="component"> The component to check. </param>
		/// <returns> True if can has enabled property, false if not. </returns>
		public static bool HasEnabledProperty([NotNull]this Component component)
		{
			return component is Behaviour || component is Collider || component is Renderer;
		}

		public static bool IsEnabled([NotNull]this Component target)
		{
			var behaviour = target as Behaviour;
			if(behaviour != null)
			{
				return behaviour.enabled;
			}

			var collider = target as Collider;
			if(collider != null)
			{
				return collider.enabled;
			}

			var renderer = target as Renderer;
			if(renderer != null)
			{
				return renderer.enabled;
			}

			#if DEV_MODE
			if(target == null)
			{
				Debug.LogWarning("ComponentExtensions.IsEnabled was called for null target. Returning default value " + StringUtils.True + ".");
			}
			#if DEBUG_IS_ENABLED
			else { Debug.LogWarning(target+".IsEnabled was called, but Component was not Behaviour, Collider or Renderer. Returning default value "+StringUtils.True+"."); }
			#endif
			#endif
			
			return true;
		}

		public static void SetEnabled([NotNull]this Component target, bool enabled)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(target.HasEnabledProperty(), target+".SetIsEnabled("+StringUtils.ToColorizedString(enabled)+") was called, but HasEnabledProperty was "+StringUtils.False+".");
			#endif
				
			var behaviour = target as Behaviour;
			if(behaviour != null)
			{
				behaviour.enabled = enabled;
				return;
			}
				
			var collider = target as Collider;
			if(collider != null)
			{
				collider.enabled = enabled;
				return;
			}

			var renderer = target as Renderer;
			if(renderer != null)
			{
				renderer.enabled = enabled;
				return;
			}

			#if DEV_MODE
			Debug.LogError(StringUtils.ToColorizedString(target.ToString(), ".Enabled = ", enabled, " called, but Component was not Behaviour, Collider or Renderer!"));
			#endif
		}
		
		/// <summary>
		/// Unity doesn't show the enabled flag control in the Inspector by default
		/// for MonoBehaviours, unless they contain specific event functions.
		/// </summary>
		/// <param name="monoBehaviour"> The monoBehaviour to test. </param>
		/// <returns> True if default Editor shows enabled flag for MonoBehaviour, false if not. </returns>
		public static bool HasEnabledFlagInEditor([NotNull]this MonoBehaviour monoBehaviour)
		{
			return HasEnabledFlagInEditor(monoBehaviour.GetType());
		}

		/// <summary>
		/// Unity doesn't show the enabled flag control in the Inspector by default
		/// for MonoBehaviours, unless they contain specific event functions.
		/// </summary>
		/// <param name="type"> The monoBehaviour type to test. </param>
		/// <returns> True if default Editor shows enabled flag for MonoBehaviour, false if not. </returns>
		private static bool HasEnabledFlagInEditor([NotNull]Type type)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(type == Types.MonoBehaviour || type.IsSubclassOf(Types.MonoBehaviour), "HasEnabledFlagInEditor called for type "+type.Name+" which did not inherit from MonoBehaviour");
			#endif

			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			var method = type.GetMethod("Start", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null)
			{
				var returnType = method.ReturnType;
				if(returnType == Types.Void || returnType == Types.IEnumerator)
				{
					return true;
				}
			}
			method = type.GetMethod("OnEnable", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				return true;
			}
			method = type.GetMethod("Update", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				return true;
			}
			method = type.GetMethod("OnDisable", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				return true;
			}
			method = type.GetMethod("FixedUpdate", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				return true;
			}
			method = type.GetMethod("OnGUI", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				return true;
			}
			method = type.GetMethod("LateUpdate", flags, null, CallingConventions.Any, Types.None, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				return true;
			}

			if(type != Types.MonoBehaviour)
			{
				return HasEnabledFlagInEditor(type.BaseType);
			}

			return false;
		}

		public static bool AnyEnabled([NotNull]this Component[] targets)
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(targets[n].IsEnabled())
				{
					return true;
				}
			}
			return false;
		}

		public static bool AllEnabled([NotNull]this Component[] targets)
		{
			for(int n = targets.Length - 1; n >= 0; n--)
			{
				if(!targets[n].IsEnabled())
				{
					return false;
				}
			}
			return true;
		}

		public static GameObject[] GameObjects([NotNull]this Component[] targets)
		{
			int count = targets.Length;
			var result = ArrayPool<GameObject>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				var target = targets[n];
				result[n] = target == null ? null : target.gameObject;
			}
			return result;
		}

		public static void SetEnabled([NotNull]this Component[] targets, bool enabled)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(targets.Length > 0);
			#endif

			for(int n = targets.Length - 1; n >= 0; n--)
			{
				targets[n].SetEnabled(enabled);
			}
		}
	}
}