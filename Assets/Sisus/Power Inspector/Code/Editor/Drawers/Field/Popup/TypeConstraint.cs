using System;

namespace Sisus
{
	[Flags]
	public enum TypeConstraint
	{
		None = 0,
		Class = (1 << 1),
		Struct = (1 << 2),
		New = (1 << 3)
	}
}