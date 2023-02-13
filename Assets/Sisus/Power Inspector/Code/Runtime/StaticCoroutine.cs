#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	/// <summary>
	/// Class for invoking coroutines without a MonoBehaviour instance.
	/// Also supports invoking coroutines in edit mode.
	/// </summary>
	[ExecuteInEditMode, AddComponentMenu("")]
	public class StaticCoroutine : MonoBehaviour
	{
		public static Action<Action, bool> OnNextBeginOnGUI;

		private static StaticCoroutine instance;
		private static MonoBehaviour monoBehaviour;
	 
		private static readonly List<CoroutineInfo> editModeCoroutines = new List<CoroutineInfo>();

		public struct CoroutineInfo
		{
			public readonly string name;
			public readonly object owner;
			public readonly IEnumerator coroutine;

			public CoroutineInfo(IEnumerator methodCoroutine, string methodName)
			{
				coroutine = methodCoroutine;
				name = methodName;
				owner = null;
			}

			public CoroutineInfo(IEnumerator methodCoroutine, string methodName, object methodOwner)
			{
				coroutine = methodCoroutine;
				name = methodName;
				owner = methodOwner;
			}

			public CoroutineInfo(IEnumerator methodCoroutine)
			{
				coroutine = methodCoroutine;
				name = "";
				owner = null;
			}

			public bool Equals(string methodName)
			{
				return string.Equals(name, methodName);
			}

			public bool Equals(string methodName, object methodOwner)
			{
				return ReferenceEquals(owner, methodOwner) && string.Equals(name, methodName);
			}

			public bool Equals(IEnumerator methodCoroutine)
			{
				return coroutine == methodCoroutine;
			}
		}

		private static StaticCoroutine Instance
		{
			get
			{
				if(instance == null)
				{

					instance = FindObjectOfType<StaticCoroutine>();

					if(instance == null)
					{
						var go = new GameObject("StaticCoroutine");
						go.hideFlags = HideFlags.HideAndDontSave; //hide in hierarchy
						if(Application.isPlaying)
						{
							DontDestroyOnLoad(go); //have the object persist from one scene to the next
						}
						instance = go.AddComponent<StaticCoroutine>();
					}
					
					monoBehaviour = instance;
				}
				
				return instance;
			}
		}
		
		private static MonoBehaviour MonoBehavior
		{
			get
			{
				if(monoBehaviour == null)
				{
					instance = FindObjectOfType<StaticCoroutine>();

					if(instance == null)
					{
						var go = new GameObject("StaticCoroutine");
						go.hideFlags = HideFlags.HideAndDontSave; //hide in hierarchy
						if(Application.isPlaying)
						{
							DontDestroyOnLoad(go);//have the object persist from one scene to the next
						}
						instance = go.AddComponent<StaticCoroutine>();
					}
					
					monoBehaviour = instance;
				}

				return monoBehaviour;
			}
		}

		/// <summary>
		/// Generates an invisible temporary MonoBehaviour in the background and starts coroutine using it.
		/// 
		/// This method can only be used in play mode. For starting coroutines in edit mode use
		/// StartCoroutine(IEnumerator, bool) or StartCoroutineInEditMode.
		/// </summary>
		/// <param name="coroutine"> Coroutine to start. </param>
		[NotNull]
		public static new Coroutine StartCoroutine([NotNull]IEnumerator coroutine)
		{
			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				throw new NotSupportedException("StartCoroutine(IEnumerator) cannot be called in edit mode. Please use use StartCoroutine(IEnumerator, bool) or StartCoroutineInEditMode.");
			}
			#endif

			return MonoBehavior.StartCoroutine(Instance.DoCoroutine(coroutine));
		}

		/// <summary>
		/// Determines whether or not coroutine started using StartCoroutine is currently invoking.
		/// 
		/// If methodName is null or empty and this is called in play mode returns false.
		/// 
		/// If coroutine is null and this is called in edit mode returns false.
		/// </summary>
		/// <param name="coroutine"> The coroutine to check. This is used in edit mode only. </param>
		/// <param name="methodName"> The name of the coroutine method. This is used in play mode only. </param>
		/// <returns> True if method is still invoking, false if not. </returns>
		public static bool IsInvoking([CanBeNull]IEnumerator coroutine, [CanBeNull]string methodName)
		{
			if(coroutine != null)
			{
				return IsInvoking(coroutine);
			}

			#if UNITY_EDITOR
			if(!Application.isPlaying)
			{
				for(int n = editModeCoroutines.Count - 1; n >= 0; n--)
				{
					if(editModeCoroutines[n].Equals(methodName))
					{
						return true;
					}
				}
				return false;
			}
			#endif

			if(string.IsNullOrEmpty(methodName))
			{
				return false;
			}
			return monoBehaviour != null && monoBehaviour.IsInvoking(methodName);
		}

		/// <summary>
		/// Determines whether or not coroutine started using StartCoroutine is currently invoking.
		/// 
		/// If methodName is null or empty and this is called in play mode returns false.
		/// 
		/// If coroutine is null and this is called in edit mode returns false.
		/// </summary>
		/// <param name="coroutine"> The coroutine to check. This is used in edit mode only. </param>
		/// <param name="methodName"> The name of the coroutine method. This is used in play mode only. </param>
		/// <returns> True if method is still invoking, false if not. </returns>
		public static bool IsInvoking([NotNull]IEnumerator coroutine)
		{
			if(coroutine == null)
			{
				return false;
			}

			for(int n = editModeCoroutines.Count - 1; n >= 0; n--)
			{
				if(editModeCoroutines[n].Equals(coroutine))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines whether or not coroutine started using StartCoroutine is currently invoking.
		/// 
		/// If methodName is null or empty and this is called in play mode returns false.
		/// 
		/// If coroutine is null and this is called in edit mode returns false.
		/// </summary>
		/// <param name="coroutine"> The coroutine to check. This is used in edit mode only. </param>
		/// <param name="methodName"> The name of the coroutine method. This is used in play mode only. </param>
		/// <returns> True if method is still invoking, false if not. </returns>
		public static new bool IsInvoking(string methodName)
		{
			if(!Application.isPlaying)
			{
				for(int n = editModeCoroutines.Count - 1; n >= 0; n--)
				{
					if(editModeCoroutines[n].Equals(methodName))
					{
						return true;
					}
				}
				return false;
			}
			return false;
		}
	 
		[UsedImplicitly]
		private void Awake()
		{
			if(instance == null)
			{
				instance = this;
			}
			HandleMoveNextForExistingCoroutinesInEditMode();
		}
		
		private static void HandleMoveNextForExistingCoroutinesInEditMode()
		{
			if(Application.isPlaying || editModeCoroutines.Count == 0)
			{
				return;
			}
			UnityEditor.EditorApplication.update -= CallMoveNextForEditModeCoroutines;
			UnityEditor.EditorApplication.update += CallMoveNextForEditModeCoroutines;
		}

		/// <summary>
		/// Starts coroutine If edit mode.
		/// If not in edit mode throws a NotSupportedException exception.
		/// </summary>
		/// <param name="coroutine"> The coroutine to start. </param>
		/// <returns> Yield instructions that determine when coroutine has finished. </returns>
		public static WaitForStaticCoroutine StartCoroutineInEditMode(IEnumerator coroutine)
		{
			return StartCoroutineInEditMode(new CoroutineInfo(coroutine));
		}

		/// <summary>
		/// Starts coroutine If edit mode.
		/// If not in edit mode throws a NotSupportedException exception.
		/// </summary>
		/// <param name="coroutine"> The coroutine to start. </param>
		/// <returns> Yield instructions that determine when coroutine has finished. </returns>
		public static WaitForStaticCoroutine StartCoroutineInEditMode(CoroutineInfo coroutine)
		{
			if(Application.isPlaying)
			{
				throw new NotSupportedException("StartCoroutineInEditMode cannot be called in play mode. Please use use StartCoroutine(IEnumerator) instead.");
			}
			
			editModeCoroutines.Add(coroutine);
			HandleMoveNextForExistingCoroutinesInEditMode();
			
			return new WaitForStaticCoroutine(coroutine.coroutine, null);
		}

		private static void CallMoveNextForEditModeCoroutines()
		{
			int count = editModeCoroutines.Count;
			if(count == 0)
			{
				UnityEditor.EditorApplication.update -= CallMoveNextForEditModeCoroutines;
				return;
			}

			for(int n = count - 1; n >= 0; n--)
			{
				var editorCoroutine = editModeCoroutines[n];
				bool coroutineFinished = !editorCoroutine.coroutine.MoveNext();

				if(coroutineFinished)
				{
					editModeCoroutines.RemoveAt(n);
				}
			}
		}

		private IEnumerator DoCoroutine([NotNull]IEnumerator coroutine)
		{
			if(!Application.isPlaying)
			{
				yield return StartCoroutineInEditMode(coroutine);
			}
			else
			{
				yield return MonoBehavior.StartCoroutine(coroutine);
			}
		}
		
		public static void LaunchDelayed(float delay, [NotNull]Action action, bool allowInEditMode)
		{
			if(!Application.isPlaying)
			{
				if(allowInEditMode)
				{
					LaunchDelayedInEditMode(delay, action);
				}
				else
				{
					throw new NotSupportedException("LaunchDelayed was called in edit mode with allowInEditMode false.");
				}
			}
			else
			{
				LaunchDelayed(delay, action);
			}
		}

		/// <summary> Executes given action after the specified delayed in seconds. </summary>
		/// <param name="delay"> The delay in seconds before action is executed. </param>
		/// <param name="action"> The action to execute. </param>
		public static void LaunchDelayed(float delay, [NotNull]Action action)
		{
			MonoBehavior.StartCoroutine(Instance.DoLaunchDelayed(delay, action));
		}

		private IEnumerator DoCoroutineDelayed(float delay, [NotNull]IEnumerator coroutine)
		{
			yield return new WaitForSeconds(delay);
			yield return MonoBehavior.StartCoroutine(coroutine);
		}

		private IEnumerator DoLaunchDelayed(float delay, [NotNull]Action action)
		{
			yield return new WaitForSeconds(delay);
			action();
		}

		[UsedImplicitly]
		private void OnApplicationQuit()
		{
			instance = null;
		}

		public static void LaunchDelayedInEditMode(int framesToDelay, [NotNull]Action action)
		{
			if(framesToDelay >= 1)
			{
				UnityEditor.EditorApplication.delayCall += ()=>LaunchDelayedInEditMode(framesToDelay - 1, action);
			}
			else
			{
				action();
			}
		}

		public static void LaunchDelayedInEditMode(float secondsToDelay, [NotNull]Action action)
		{
			DoLaunchDelayedInEditMode(UnityEditor.EditorApplication.timeSinceStartup + secondsToDelay, action);
		}

		private static void DoLaunchDelayedInEditMode(double waitDoneTime, [NotNull]Action action)
		{
			if(UnityEditor.EditorApplication.timeSinceStartup < waitDoneTime)
			{
				if(OnNextBeginOnGUI != null)
				{
					OnNextBeginOnGUI(() => DoLaunchDelayedInEditMode(waitDoneTime, action), true);
				}
			}
			else
			{
				action();
			}
		}
	}
}
#endif