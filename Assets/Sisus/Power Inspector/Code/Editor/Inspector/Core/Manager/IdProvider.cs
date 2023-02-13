using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class IdProvider
	{
		[SerializeField]
		private int current = 0;

		public void Reset()
		{
			current = 0;
		}

		public int Next()
		{
			unchecked
			{
				current++;
			}
			return current;
		}
	}
}