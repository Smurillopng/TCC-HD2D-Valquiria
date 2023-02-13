using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class LockedSelectionManager : ISelectionManager
	{
		[SerializeField]
		private Action onSelectionChanged;

		[SerializeField]
		private Action<Object[]> onNextSelectionChanged;

		[SerializeField]
		private Object[] lockedSelection = new Object[0];

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
				return lockedSelection;
			}
		}

		public void Select(Object target)
		{
			if(lockedSelection.Length == 0)
			{
				lockedSelection = ArrayPool<Object>.CreateWithContent(target);
				HandleCallbacks();
			}
		}

		public void Select(Object[] targets)
		{
			if(lockedSelection.Length == 0)
			{
				lockedSelection = targets;
				HandleCallbacks();
			}
		}

		public void ReleaseLockedSelection()
		{
			lockedSelection = ArrayPool<Object>.ZeroSizeArray;
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

		public LockedSelectionManager() { }

		public LockedSelectionManager(Object target)
		{
			Select(target);
		}

		public LockedSelectionManager(Object[] targets)
		{
			Select(targets);
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
	}
}