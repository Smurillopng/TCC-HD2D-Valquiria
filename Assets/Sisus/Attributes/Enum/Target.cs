namespace Sisus.Attributes
{
	/// <summary>
	/// Indicates whether an attribute applies the the element that follows it,
	/// or the members of the element that follows it.
	/// </summary>
	public enum Target
	{
		/// <summary>
		/// The attribute target is selected based on default behaviour for the given attribute type.
		/// 
		/// By default, most attributes apply to the element that follows it.
		/// 
		/// If however a PropertyAttribute is placed before a collection class member,
		/// it applies to the members of the collection by default.
		/// </summary>
		Default = 0,

		/// <summary>
		/// The attribute targets the element that follows it.
		/// </summary>
		This = 1,

		/// <summary>
		/// The attribute targets the collection that follows it.
		/// </summary>
		Collection = 1,

		/// <summary>
		/// The attribute targets the members of the element that follows it.
		/// </summary>
		Members = 2
	}
}