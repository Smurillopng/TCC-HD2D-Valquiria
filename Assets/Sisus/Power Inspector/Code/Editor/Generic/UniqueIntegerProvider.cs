using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class UniqueIntegerProvider
	{
		private readonly ThreadLock locked = new ThreadLock();

		[SerializeField]
		private int next;
	
		public int NextInt()
		{
			lock(locked)
			{
				unchecked
				{
					next++;
				}
				return next;
			}
		}
	}
}