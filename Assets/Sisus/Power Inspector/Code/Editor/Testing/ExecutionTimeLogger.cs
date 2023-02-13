using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sisus
{
	public class ExecutionTimeLogger
	{
		private readonly Stack<TimedPart> timerStack = new Stack<TimedPart>(5);
		private readonly Stack<string> intervalTimes = new Stack<string>(5);
		private readonly StringBuilder sb = new StringBuilder();
		private bool logResultsCalled;

		public bool HasResultsToReport
		{
			get
			{
				return timerStack.Count == 0 && intervalTimes.Count > 0;
			}
		}

		/// <summary> Clears the existing timer stack and starts a new timer with the given label. </summary>
		/// <param name="label"> The label for the timer. </param>
		public void Start(string label)
		{
			if(!logResultsCalled && timerStack.Count > 0)
			{
				UnityEngine.Debug.LogWarning("ExecutionTimerLogger.Start was called with timerStack.Count "+timerStack.Count+ " and logResultsCalled false. Did you mean to use StartInterval?");
			}
			Reset();
			
			StartInterval(label);
		}

		/// <summary> Adds a new timer to the stack with the given label. </summary>
		/// <param name="label"> The label for the timer. </param>
		public void StartInterval(string label)
		{
			var timer = new TimedPart(label);
			timer.Start();
			timerStack.Push(timer);
		}

		public void FinishInterval()
		{
			try
			{
				var intervalTimer = timerStack.Pop();
				intervalTimer.Stop();
				intervalTimes.Push(intervalTimer.Label);
			}
			catch(InvalidOperationException)
			{
				UnityEngine.Debug.LogWarning("FinishInterval was called with timerStack.Count " + timerStack.Count+". Make sure that number of StartInterval and FinishInterval calls are the same.");
			}
		}
		
		public void Finish()
		{
			FinishInterval();
		}

		public void FinishAndLogResults()
		{
			FinishInterval();
			LogResults();
		}

		public string Results()
		{
			sb.Append(intervalTimes.Pop());

			for(int n = intervalTimes.Count; n >= 1; n--)
			{
				sb.Append("\n   ");
				sb.Append(intervalTimes.Pop());
			}

			var result = sb.ToString();
			sb.Length = 0;

			Reset();
			return result;
		}

		public void LogResults()
		{
			#if DEV_MODE
			Debug.Assert(!logResultsCalled);
			Debug.Assert(HasResultsToReport);
			#endif

			logResultsCalled = true;
			string results = Results();
			
			#if DEV_MODE
			Debug.Assert(results.Length > 0, "ExecutionTimerLogger.LogResults called but results.Length was zero");
			#endif

			UnityEngine.Debug.Log(results);
		}

		public void Reset()
		{
			logResultsCalled = false;
			timerStack.Clear();
			intervalTimes.Clear();
		}

		private class TimedPart
		{
			private readonly string label;
			private readonly Stopwatch timer;

			private string TimeSecondsAsString
			{
				get
				{
					int timeInteger = UnityEngine.Mathf.RoundToInt(timer.ElapsedMilliseconds * 10L);
					return (timeInteger / 10000f).ToString(CultureInfo.CurrentCulture);
				}
			}

			public string Label
			{
				get
				{
					return string.Concat(label.Length > 0 ? label : "Interval", " - Time: ", TimeSecondsAsString);
				}
			}

			public TimedPart(string setLabel)
			{
				label = setLabel;
				timer = new Stopwatch();
			}

			public void Start()
			{
				timer.Start();
			}

			public void Stop()
			{
				timer.Stop();
			}
		}
	}
}