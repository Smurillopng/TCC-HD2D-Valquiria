#define DEBUG_ENABLED

using JetBrains.Annotations;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class TargetWindowSelectionManager : ISelectionManager
	{
		[SerializeField]
		private EditorWindow targetWindow;

		[SerializeField]
		private Action onSelectionChanged;

		[SerializeField]
		private Action<Object[]> onNextSelectionChanged;

		[SerializeField]
		private Object[] lastSelection = new Object[0];

		// Prevents infinite loops if Selected is fetched during OnSelectionChanged event
		private bool autoUpdateSelection = true;

		public EditorWindow TargetWindow
        {
			get
            {
				return targetWindow;
            }
        }

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
				Debug.Assert(!lastSelection.ContainsNullObjects(), "TargetWindowSelectionManager lastSelection contained nulls.");
				Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "TargetWindowSelectionManager lastSelection contained transforms (should most likely all be GameObjects).");
				#endif

				return lastSelection;
			}
		}

		public TargetWindowSelectionManager()
		{
			UpdateSelection();
		}

		public TargetWindowSelectionManager(EditorWindow setTargetWindow)
		{
			targetWindow = setTargetWindow;
			UpdateSelection();
		}

		public void SetTargetWindow([NotNull]EditorWindow setTargetWindow)
		{
			targetWindow = setTargetWindow;
			UpdateSelection();
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

			var window = EditorWindow.focusedWindow;
			if(window == null)
			{
				window = EditorWindow.mouseOverWindow;
			}
			if(window == targetWindow)
			{
				#if DEV_MODE && DEBUG_ENABLED
				Debug.Log("active window type="+window.GetType()+", name="+window.name);
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(Selection.objects.ContentsMatch(Selection.gameObjects));
				#endif

				if(!lastSelection.ContentsMatch(Selection.gameObjects))
				{
					lastSelection = Selection.gameObjects;

					#if DEV_MODE && DEBUG_ENABLED
					Debug.Log("TargetWindowSelectionManager new selection: "+StringUtils.NamesToString(lastSelection));
					#endif

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(!lastSelection.ContainsNullObjects(), "TargetWindowSelectionManager lastSelection contained nulls.");
					Debug.Assert(!lastSelection.ContainsObjectsOfType(typeof(Transform)), "TargetWindowSelectionManager lastSelection contained transforms (should most likely all be GameObjects).");
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
	}
}