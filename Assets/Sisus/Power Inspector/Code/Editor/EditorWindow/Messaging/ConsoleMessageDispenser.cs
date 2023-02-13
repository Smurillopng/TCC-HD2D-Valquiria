using UnityEngine;
using UnityEditor;

namespace Sisus
{
	public class ConsoleMessageDispenser : IEditorWindowMessageDispenser
	{
		private EditorWindow targetWindow;

		public ConsoleMessageDispenser() { }

		public ConsoleMessageDispenser(EditorWindow setTarget)
		{
			Setup(setTarget);
		}

		/// <inheritdoc/>
		public void Setup(EditorWindow setTarget, InspectorPreferences preferences)
		{
			Setup(setTarget);
		}

		public void Setup(EditorWindow setTarget)
		{
			targetWindow = setTarget;
		}

		/// <inheritdoc/>
		public void Message(string message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true)
		{
			Message(new GUIContent(message, messageType.ToString()), context, messageType, alsoLogToConsole);
		}
		
		/// <inheritdoc/>
		public void Message(GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true)
		{
			if(context == null)
			{
				context = targetWindow;
			}

			string text = message.text;
			
			switch(messageType)
			{
				case MessageType.Error:
					Debug.LogError(text, context);
					return;
				case MessageType.Warning:
					Debug.LogWarning(text, context);
					return;
				default:
					Debug.Log(text, context);
					return;
			}
		}
	}
}