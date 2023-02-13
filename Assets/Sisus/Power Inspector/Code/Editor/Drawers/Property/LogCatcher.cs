using System;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	public class LogCatcher : IDisposable
	{
		public bool HasMessage
		{
			get
			{
				return Message != null;
			}
		}

		[CanBeNull]
		public string Message
		{
			get;
			private set;
		}

		public LogType LogType
		{
			get;
			private set;
		}

		public LogCatcher()
		{
			Message = null;
			Application.logMessageReceived += LogMessageReceived;
		}

		private void LogMessageReceived(string text, string stackTrace, LogType type)
		{
			Message = text + "\n" + stackTrace;
			LogType = type;
		}

		public void Dispose()
		{
			Application.logMessageReceived -= LogMessageReceived;
		}
	}
}