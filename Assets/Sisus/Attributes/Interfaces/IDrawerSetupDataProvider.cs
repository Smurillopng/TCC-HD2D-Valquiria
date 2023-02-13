using System;
using JetBrains.Annotations;

namespace Sisus.Attributes
{
	/// <summary>
	/// Interface that an attribute can implement when it might be able to provide a drawer some parameters to use during the Setup phase.
	/// </summary>
	public interface IDrawerSetupDataProvider
	{
		/// <summary>
		/// Gets parameters that can be used during Setup phase of a drawer that was selected using this attribute.
		/// </summary>
		/// <returns>
		/// Array of parameter values for the Setup method. If no additional parameters are provided for the given drawer, returns a zero-size array.
		/// </returns>
		[NotNull]
		object[] GetSetupParameters();
	}
}