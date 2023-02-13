using System;

namespace Sisus
{
	[Flags]
	public enum FilterTestType
	{
		None = 0,
		Label = 2,
		Type = 4,
		Value = 8,
		FullClassName = 16,
		Indetermined = 32
	}
}