using System;

namespace Sisus
{
	[Flags]
	public enum MessageDisplayMethod
	{
		None = (1 << 0),

		Notification = (1 << 1),
		Console = (1 << 2)
	}
}