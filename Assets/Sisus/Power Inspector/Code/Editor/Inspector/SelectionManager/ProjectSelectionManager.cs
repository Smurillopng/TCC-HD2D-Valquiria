#define DEBUG_ENABLED

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class ProjectSelectionManager : Singleton<ProjectSelectionManager>, ISelectionManager
	{
		[SerializeField]
		private Action onSelectionChanged;

		[SerializeField]
		private Action<Object[]> onNextSelectionChanged;

		[SerializeField]
		private Object[] lastSelection = new Object[0];

		// Prevents infinite loops if Selected is fetched during OnSelectionChanged event
		private bool autoUpdateSelection = true;

		public Action OnSelectionChanged
		{
			get
			{
				return onSelectionChanged;
			}

			set
			{
				onSelectionChanged = value;
			}
		}

		public Object[] Selected
		{
			get
			{
				if(autoUpdateSelection)
				{
					UpdateSelection();
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!lastSelection.ContainsNullObjects(), "ProjectSelectionManager lastSelection contained nulls.");
				Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "ProjectSelectionManager lastSelection contained transforms (should all be prefab GameObjects or asset type Objects).");
				Debug.Assert(Selection.objects.Length == 0 || Selection.objects[0].IsSceneObject() || lastSelection.ContentsMatch(Selection.objects), "ProjectSelectionManager lastSelection did not match Selection.objects.");
				#endif

				return lastSelection;
			}
		}

		public void OnNextSelectionChanged(Action<Object[]> action)
		{
			onNextSelectionChanged += action;
		}

		/// <inheritdoc />
		public void CancelOnNextSelectionChanged(Action<Object[]> action)
		{
			onNextSelectionChanged -= action;
		}

		private void HandleCallbacks()
		{
			if(onNextSelectionChanged != null)
			{
				var callback = onNextSelectionChanged;
				onNextSelectionChanged = null;
				callback(Selected);
			}

			if(onSelectionChanged != null)
			{
				onSelectionChanged();
			}
		}

		private void UpdateSelection()
		{
			autoUpdateSelection = false;

			int count = Selection.objects.Length;
			if(count == 0)
			{
				var window = EditorWindow.focusedWindow;
				if(window == null)
				{
					window = EditorWindow.mouseOverWindow;
				}
				if(window != null)
				{
					var windowTypeName = window.GetType().Name;
					if(windowTypeName.IndexOf("Project", StringComparison.Ordinal) != -1 || windowTypeName.IndexOf("Asset", StringComparison.Ordinal) != -1)
					{
						lastSelection = ArrayPool<Object>.ZeroSizeArray;

						#if DEV_MODE && DEBUG_ENABLED
						Debug.Log(window.GetType().Name+" selection cleared.");
						#endif

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!lastSelection.ContainsNullObjects(), "ProjectSelectionManager lastSelection contained nulls");
						Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "ProjectSelectionManager lastSelection contained transforms");
						#endif

						HandleCallbacks();
					}
				}
			}
			else if(!Selection.objects[0].IsSceneObject())
			{
				if(!lastSelection.ContentsMatch(Selection.objects))
				{
					lastSelection = Selection.objects;

					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("Project View new selection: "+StringUtils.NamesToString(lastSelection));
					#endif

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!lastSelection.ContainsNullObjects(), "ProjectSelectionManager lastSelection contained null targets.");
					Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "ProjectSelectionManager lastSelection contained transforms. These should be GameObjects.");
					#endif

					HandleCallbacks();
				}
			}

			autoUpdateSelection = true;
		}

		public void Select(Object target)
		{
			bool autoUpdateSelectionWas = autoUpdateSelection;
			autoUpdateSelection = false;
			
			Selection.activeObject = target;

			autoUpdateSelection = autoUpdateSelectionWas;

			UpdateSelection();
		}

		public void Select(Object[] targets)
		{
			bool autoUpdateSelectionWas = autoUpdateSelection;
			autoUpdateSelection = false;
			
			Selection.objects = targets;

			autoUpdateSelection = autoUpdateSelectionWas;

			UpdateSelection();
		}
		
		public ProjectSelectionManager()
		{
			UpdateSelection();
		}
	}
}