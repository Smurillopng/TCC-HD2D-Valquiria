using System;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Handles tasks related to UnityEngine.Object selection.
	/// </summary>
	public interface ISelectionManager
	{
		/// <summary>
		/// Delegate callback triggered when currently active/selected item has changed.
		/// </summary>
		/// <value> The delegate. </value>
		Action OnSelectionChanged
		{
			get;
			set;
		}

		/// <summary>
		/// Delegate callback triggered the next time active/selected item changes.
		/// </summary>
		/// <param name="action"> Action to invoke. </param>
		void OnNextSelectionChanged(Action<Object[]> action);

		/// <summary>
		/// Cancel callback that was supposed to be triggered the next time active/selected item change.
		/// </summary>
		/// <param name="action"> Action to cancel. </param>
		void CancelOnNextSelectionChanged(Action<Object[]> action);

		/// <summary> Gets the currently selected UnityEngine.Objects. </summary>
		/// <value> Selected UnityEngine.Objects. </value>
		[NotNull]
		Object[] Selected
		{
			get;
		}

		/// <summary>
		/// Sets the given target as the selected UnityEngine.Object.
		/// </summary>
		/// <param name="target"> UnityEngine.Object to select. </param>
		void Select(Object target);

		/// <summary>
		/// Sets the given targets as the selected UnityEngine.Objects.
		/// </summary>
		/// <param name="targets"> UnityEngine.Objects to select. </param>
		void Select(Object[] targets);
	}
}