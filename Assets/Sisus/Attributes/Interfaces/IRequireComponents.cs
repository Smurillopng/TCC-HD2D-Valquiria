using System;

namespace Sisus.Attributes
{
	/// <summary>
	/// Interface that a component class targeted attribute can implement to let Power Inspector know
	/// what other components should exist on a GameObject that contains the target component.
	/// </summary>
	public interface IRequireComponents
	{
		/// <summary>
		/// The required component types. Either all or one of these are required, depending on the return value of AllRequired.
		/// </summary>
		Type[] RequiredComponents
		{
			get;
		}

		/// <summary>
		/// Specifies whether or not all of the components are required, or just one.
		/// </summary>
		bool AllRequired
		{
			get;
		}
	}
}