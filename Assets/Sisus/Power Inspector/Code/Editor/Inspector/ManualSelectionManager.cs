using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	public class ManualSelectionManager : ISelectionManager
	{
		[SerializeField]
		private Action onSelectionChanged;

		[SerializeField]
		private Action<Object[]> onNextSelectionChanged;

		[SerializeField]
		private Object[] lastSelection = new Object[0];

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
				return lastSelection;
			}
		}

		public void Select(Object target)
		{
			if(lastSelection.Length != 1 || lastSelection[0] != target)
			{
				lastSelection = ArrayPool<Object>.CreateWithContent(target);
				HandleCallbacks();
			}
		}

		public void Select(Object[] targets)
		{
			if(!lastSelection.ContentsMatch(targets))
			{
				lastSelection = targets;

				HandleCallbacks();
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

		public ManualSelectionManager() { }

		public ManualSelectionManager(Object target)
		{
			Select(target);
		}

		public ManualSelectionManager(Object[] targets)
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