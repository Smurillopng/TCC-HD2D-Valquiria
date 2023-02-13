using JetBrains.Annotations;

namespace Sisus.Attributes
{
	public interface IValueValidator
	{
		/// <summary>
		/// Validate the values of a drawer.
		/// For multi-selection
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		bool Validate([NotNull]object[] values);
	}
}