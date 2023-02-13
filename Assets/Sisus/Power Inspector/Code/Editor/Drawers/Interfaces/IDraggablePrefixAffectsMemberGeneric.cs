namespace Sisus
{
	/// <summary>
	/// Generic interface implemented by drawers whose prefix label can sometimes be be dragged to adjust the value of one or more of their members.
	/// 
	/// TValue
	/// <typeparam name="TValue"> Value type of the drawer that implements the interface (does not need to be the same as the value type of the members). </typeparam>
	public interface IDraggablePrefixAffectsMember<TValue> : IDraggablePrefix<TValue>, IDraggablePrefixAffectsMember { }
}