#define DEBUG_ENABLED

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class HierarchySelectionManager : Singleton<HierarchySelectionManager>, ISelectionManager
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
				Debug.Assert(!lastSelection.ContainsNullObjects(), "HierarchySelectionManager lastSelection contained nulls.");
				Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "HierarchySelectionManager lastSelection contained transforms (should all be GameObjects).");
				Debug.Assert(Selection.objects.Length == 0 || !Selection.objects[0].IsSceneObject() || lastSelection.ContentsMatch(Selection.objects), "HierarchySelectionManager lastSelection did not match Selection.objects.");
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
					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("active window type="+window.GetType()+", name="+window.name);
					#endif

					var windowTypeName = window.GetType().Name;
					if(windowTypeName.IndexOf("Scene", StringComparison.Ordinal) != -1 || windowTypeName.IndexOf("Hierarchy", StringComparison.Ordinal) != -1)
					{
						lastSelection = ArrayPool<Object>.ZeroSizeArray;

						#if DEV_MODE && DEBUG_ENABLED
						Debug.Log(window.GetType().Name+" selection cleared.");
						#endif

						#if DEV_MODE && PI_ASSERTATIONS
						Debug.Assert(!lastSelection.ContainsNullObjects(), "HierarchySelectionManager lastSelection contained nulls");
						Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "HierarchySelectionManager lastSelection contained transforms");
						#endif

						HandleCallbacks();
					}
				}
			}
			else if(Selection.objects[0].IsSceneObject())
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(Selection.objects.ContentsMatch(Selection.gameObjects));
				#endif

				if(!lastSelection.ContentsMatch(Selection.gameObjects))
				{
					lastSelection = Selection.gameObjects;

					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("Hierarchy View new selection: "+StringUtils.NamesToString(lastSelection));
					#endif

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!lastSelection.ContainsNullObjects(), "HierarchySelectionManager lastSelection contained nulls");
					Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "HierarchySelectionManager lastSelection contained transforms");
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
		
		public HierarchySelectionManager()
		{
			UpdateSelection();
		}
	}
}