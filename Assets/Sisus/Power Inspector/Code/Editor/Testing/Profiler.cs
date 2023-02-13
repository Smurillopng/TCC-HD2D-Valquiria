//#define PROFILE_POWER_INSPECTOR

using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace Sisus
{
	/// <summary>
	/// Wrapper class for Unity's profiler which makes it
	/// easy to disable profiling when outside of DEV_MODE
	/// </summary>
	public static class Profiler
	{
		public static void BeginSample(string name)
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			UnityProfiler.BeginSample(name);
			#endif
		}

		public static void EndSample()
		{
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			UnityProfiler.EndSample();
			#endif
		}
	}
}