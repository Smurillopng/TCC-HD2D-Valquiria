using JetBrains.Annotations;

namespace Sisus.Compatibility
{
	/// <summary>
	/// Converts plugin attribute instance to Power Inspector supported attribute instance.
	/// </summary>
	/// <param name="input"> Plugin attribute instance. </param>
	/// <returns> Power Inspector supported attribute instance. </returns>
	[CanBeNull]
	public delegate object AttributeConverter([NotNull]object input);
}