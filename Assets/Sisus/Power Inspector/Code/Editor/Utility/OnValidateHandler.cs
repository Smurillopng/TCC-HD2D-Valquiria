using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public static class OnValidateHandler
	{
		private const BindingFlags onValidateBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET || DEV_MODE
		private static HashSet<Object> lastOnValidateCallTargets = new HashSet<Object>();
		private static int lastOnValidateCallTick;
		#endif
		
		public static void CallForTargets(IList<Object> unityObjects)
		{
			#if UNITY_EDITOR
			#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET || DEV_MODE
			int tick = Time.frameCount;
			if(lastOnValidateCallTick != tick)
			{
				lastOnValidateCallTargets.Clear();
				lastOnValidateCallTick = tick;
			}
			#endif

			for(int n = unityObjects.Count - 1; n >= 0; n--)
			{
				var unityObject = unityObjects[n];

				var component = unityObject as Component;
				if(component != null)
				{
					ComponentModifiedCallbackUtility.OnComponentModified(component);
				}
				
				var type = unityObject.GetType();
				var method = type.GetMethod("OnValidate", onValidateBindingFlags, null, Type.EmptyTypes, null);
				if(method != null && method.ReturnType == Types.Void)
				{
					#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET || DEV_MODE
					if(!lastOnValidateCallTargets.Add(unityObject))
					{
						Debug.LogWarning("OnValidateHandler.CallForTargets was called for same target multiple times within one frame: "+unityObject+". Was a value really changed multiple times, or is this a duplicate call?");

						#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET
						continue;
						#endif
					}
					#endif

					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("Calling "+ type.Name + ".OnValidate()");
					#endif
					method.Invoke(unityObject, null);
				}
				#if DEV_MODE && DEBUG_ENABLED
				else { Debug.Log("Not calling "+ type.Name + ".OnValidate() because method not found."); }
				#endif
			}
			#endif
		}

		public static void CallForTarget(Object unityObject)
		{
			#if UNITY_EDITOR
			#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET || DEV_MODE
			int tick = Time.frameCount;
			if(lastOnValidateCallTick != tick)
			{
				lastOnValidateCallTargets.Clear();
				lastOnValidateCallTick = tick;
			}
			#endif

			var component = unityObject as Component;
			if(component != null)
			{
				ComponentModifiedCallbackUtility.OnComponentModified(component);
			}

			var type = unityObject.GetType();
			var method = type.GetMethod("OnValidate", onValidateBindingFlags, null, Type.EmptyTypes, null);
			if(method != null && method.ReturnType == Types.Void)
			{
				#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET || DEV_MODE
				if(lastOnValidateCallTargets.Contains(unityObject))
				{
					#if DEV_MODE
					Debug.LogWarning("OnValidateHandler.CallForTargets was called for same target multiple times within one frame: "+unityObject+". Was a value really changed multiple times, or is this a duplicate call?");
					#endif

					#if SUPPRESS_IF_MULTIPLE_CALLS_WITHIN_ONE_FRAME_FOR_SAME_TARGET
					continue;
					#endif
				}
				lastOnValidateCallTargets.Add(unityObject);
				#endif

				#if DEV_MODE
				Debug.Log("Calling "+ type.Name + ".OnValidate()");
				#endif
				method.Invoke(unityObject, null);
			}
			#endif
		}
	}
}