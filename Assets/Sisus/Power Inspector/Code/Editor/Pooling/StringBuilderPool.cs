#define THREAD_SAFE

using System.Text;

namespace Sisus
{
	public static class StringBuilderPool
	{
		private const int InitialCapacityForCreatedBuilders = 1000;

		#if !THREAD_SAFE
		private static Pool<StringBuilder> pool = new Pool<StringBuilder>(2);
		#elif !NET_2_0 && !NET_2_0_SUBSET && !NET_STANDARD_2_0
		private static System.Collections.Concurrent.ConcurrentBag<StringBuilder> pool = new System.Collections.Concurrent.ConcurrentBag<StringBuilder>();
		#endif

		public static StringBuilder Create()
		{
			#if THREAD_SAFE && (NET_2_0 || NET_2_0_SUBSET || NET_STANDARD_2_0)
			return new StringBuilder(InitialCapacityForCreatedBuilders);
			#else

			StringBuilder result;
			#if THREAD_SAFE
			if(!pool.TryTake(out result))
			#else
			if(!pool.TryGet(out result))
			#endif
			{
				return new StringBuilder(InitialCapacityForCreatedBuilders);
			}
			return result;

			#endif
		}

		public static void Dispose(ref StringBuilder disposing)
		{
			#if THREAD_SAFE && (NET_2_0 || NET_2_0_SUBSET || NET_STANDARD_2_0)
			disposing.Length = 0;
			disposing = null;
			#else

			disposing.Length = 0;

			#if THREAD_SAFE
			pool.Add(disposing);
			disposing = null;
			#else
			pool.Dispose(ref disposing);
			#endif

			#endif
		}

		public static string ToStringAndDispose(ref StringBuilder disposing)
		{
			#if THREAD_SAFE && (NET_2_0 || NET_2_0_SUBSET || NET_STANDARD_2_0)
			string result = disposing.ToString();
			disposing.Length = 0;
			disposing = null;
			return result;
			#else

			var result = disposing.ToString();
			disposing.Length = 0;

			#if THREAD_SAFE
			pool.Add(disposing);
			disposing = null;
			#else
			pool.Dispose(ref disposing);
			#endif

			return result;

			#endif
		}
	}
}