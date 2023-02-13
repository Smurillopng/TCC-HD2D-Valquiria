using System;

namespace Sisus
{
	[Flags]
	public enum UnityEventFunctions
	{
		None = 0,
		
		/// <summary> Update is called every frame, if the MonoBehaviour is enabled. </summary>
		Update = 1,
		OnApplicationQuit = 2,
		OnApplicationGainedFocus = 4,
		OnApplicationLostFocus = 8
	}
}