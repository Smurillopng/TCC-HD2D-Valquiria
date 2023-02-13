#if !CSHARP_7_3_OR_NEWER
using System.Collections.Generic;

namespace System.Threading.Tasks
{
	/// <summary>
	/// Class for adding backwards compatibility with Task class in .NET 2.0.
	/// This does not actually use threading but blocks the main thread.
	/// </summary>
	public struct Task
	{
		public Task(Action action)
		{
			if(action != null)
			{
				action();
			}
		}

		public static Task WhenAll(IEnumerable<Task> tasks)
		{
			var enumerator = tasks.GetEnumerator();
			while(enumerator.MoveNext()){ }
			return new Task();
		}

		public static Task Run(Action action)
		{
			if(action != null)
			{
				action();
			}
			return new Task();
		}

		public Task ConfigureAwait(bool await)
		{
			return this;
		}
	}
}
#endif