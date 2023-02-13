using System;

namespace Sisus
{
	[Flags]
	public enum MenuItems
	{
		None = (1 << 0),
		Peek = (1 << 5),
		Reset = (1 << 10),
		//Folders = (1 << 15),
	}
}