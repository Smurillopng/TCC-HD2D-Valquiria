using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	public static class DrawerListPool
	{
		private const int defaultCapacity = 10;

		private static Stack<List<IDrawer>> pool = new Stack<List<IDrawer>>(3);

		public static List<IDrawer> Create(int capacity = defaultCapacity)
		{
			if(pool.Count > 0)
			{
				return pool.Pop();
			}
			return new List<IDrawer>(capacity);
		}

		/// <summary>
		/// Clears the list, pools it and sets references to null
		/// Will NOT dispose members.
		/// </summary>
		/// <param name="disposing"></param>
		public static void Dispose([NotNull]ref List<IDrawer> disposing)
		{
			disposing.Clear();
			pool.Push(disposing);
			disposing = null;
		}

		/// <summary>
		/// Disposes all members of the list and sets references to them null
		/// </summary>
		/// <param name="disposing"></param>
		public static void DisposeContent([NotNull]ref List<IDrawer> disposing)
		{
			int length = disposing.Count;
			for(int n = length - 1; n >= 0; n--)
			{
				var member = disposing[n];
				if(member != null)
				{
					member.Dispose();
					disposing[n] = null;
				}
			}
		}
	}
}