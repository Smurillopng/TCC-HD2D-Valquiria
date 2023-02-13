namespace Sisus.Attributes
{
	/// <summary>
	/// Interface that an attribute can implement to make it possible to let Power Inspector know
	/// whether the attribute targets the element that follows it, or the members of that element.
	/// </summary>
	public interface ITargetableAttribute
	{
		/// <summary>
		/// Determines whether the attribute applies the the element that follows it, or the members
		/// of the element that follows it.
		/// 
		/// By default, most attributes apply to the element that follows it.
		/// 
		/// If however a PropertyAttribute is placed before a collection class member,
		/// it applies to the members of the collection by default.
		/// </summary>
		Target Target
		{
			get;
		}
	}
}