//#define DEBUG_ENABLED
#define DEBUG_SELECT

using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class EditorSelectionManager : Singleton<EditorSelectionManager>, ISelectionManager
	{
		private Action<Object[]> onNextSelectionChanged;

		public Action OnSelectionChanged
		{
			get
			{
				return Selection.selectionChanged;
			}

			set
			{
				Selection.selectionChanged = value;
			}
		}

		public Object[] Selected
		{
			get
			{
				return Selection.objects;
			}
		}

		public void Select(Object target)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			UnityEngine.Debug.Assert(target == null || !(target is UnityEngine.Component));
			#endif

			#if DEV_MODE && DEBUG_SELECT
			UnityEngine.Debug.Log("Select("+StringUtils.ToString(target)+")");
			#endif

			Selection.activeObject = target;
		}

		public void Select(Object[] targets)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			foreach(var target in targets) { UnityEngine.Debug.Assert(target == null || !(target is UnityEngine.Component)); }
			#endif

			#if DEV_MODE && DEBUG_SELECT
			UnityEngine.Debug.Log("Select("+StringUtils.ToString(targets)+")");
			#endif

			Selection.objects = targets;
		}

		public EditorSelectionManager()
		{
			OnSelectionChanged += HandleOnNextSelectionChanged;
		}

		/// <inheritdoc />
		public void OnNextSelectionChanged(Action<Object[]> action)
		{
			onNextSelectionChanged += action;
		}

		/// <inheritdoc />
		public void CancelOnNextSelectionChanged(Action<Object[]> action)
		{
			onNextSelectionChanged -= action;
		}
		
		private void HandleOnNextSelectionChanged()
		{
			#if DEV_MODE && DEBUG_ENABLED
			UnityEngine.Debug.Log("EditorSelectionManager.OnSelectionChanged");
			#endif

			if(onNextSelectionChanged != null)
			{
				var callback = onNextSelectionChanged;
				onNextSelectionChanged = null;
				callback(Selected);
			}
		}
	}
}