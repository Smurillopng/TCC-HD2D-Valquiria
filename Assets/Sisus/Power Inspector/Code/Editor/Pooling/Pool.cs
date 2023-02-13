#define SAFE_MODE

using System.Collections.Generic;

namespace Sisus
{
	public class Pool<T> where T : class
	{
		private Stack<T> stack;

		public int Count
		{
			get
			{
				return stack.Count;
			}
		}

		public Pool(int capacity)
		{
			stack = new Stack<T>(capacity);
		}
		
		public void Dispose(ref T disposing)
		{
			#if DEV_MODE || SAFE_MODE
			if(disposing == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("Pool.Dispose was called for null "+typeof(T).Name+"!");
				#endif
				return;
			}

			if(stack.Contains(disposing))
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("Pool.Dispose was called for " + typeof(T).Name + " \"" + StringUtils.ToString(disposing) + "\" but the pool already contained the same item!");
				#endif
				disposing = null;
				return;
			}
			#endif

			stack.Push(disposing);
			disposing = null;
		}

		public void DisposeContent(ref List<T> disposing)
		{
			#if DEV_MODE || SAFE_MODE
			if(disposing == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("Pool.Dispose was called for null List!");
				#endif
				return;
			}
			#endif

			for(int n = disposing.Count - 1; n >= 0; n--)
			{
				var item = disposing[n];
				Dispose(ref item);
			}
			
			disposing.Clear();
		}

		public void DisposeContent(ref Stack<T> disposing)
		{
			#if DEV_MODE || SAFE_MODE
			if(disposing == null)
			{
				#if DEV_MODE
				UnityEngine.Debug.LogError("Pool.Dispose was called for null Stack!");
				#endif
				return;
			}
			#endif

			for(int n = disposing.Count - 1; n >= 0; n--)
			{
				var item = disposing.Pop();
				Dispose(ref item);
			}
			
			disposing.Clear();
		}
		
		public bool TryGet(out T result)
		{
			if(stack.Count > 0)
			{
				result = stack.Pop();
				return true;
			}
			result = null;
			return false;
		}
	}
}