using UnityEngine;
using UnityEditor;

namespace Sisus
{
	public class NotificationMessageDispenser : IEditorWindowMessageDispenser
	{
		private EditorWindow targetWindow;

		#if UNITY_2019_1_OR_NEWER // EditorWindow.ShowNotification only supports display time in later Unity versions
		private float displayDurationPerWord;
		private float minDisplayDuration;
		private float maxDisplayDuration;
		#endif
		private bool canAlsoLogToConsole = true;

		public NotificationMessageDispenser() { }

		public NotificationMessageDispenser(EditorWindow setTarget, InspectorPreferences preferences)
		{
			Setup(setTarget, preferences);
		}

		/// <inheritdoc/>
		public void Setup(EditorWindow setTarget, InspectorPreferences preferences)
		{
			Setup(setTarget, preferences.displayDurationPerWord, preferences.minDisplayDuration, preferences.maxDisplayDuration, preferences.messageDisplayMethod.HasFlag(MessageDisplayMethod.Console));
		}

		public void Setup(EditorWindow setTarget, float setDisplayDurationPerWord, float setMinDisplayDuration, float setMaxDisplayDuration, bool setCanAlsoLogToConsole)
		{
			targetWindow = setTarget;
			#if UNITY_2019_1_OR_NEWER
			displayDurationPerWord = setDisplayDurationPerWord;
			minDisplayDuration = setMinDisplayDuration;
			maxDisplayDuration = setMaxDisplayDuration;
			#endif
			canAlsoLogToConsole = setCanAlsoLogToConsole;
		}

		/// <inheritdoc/>
		public void Message(string message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true)
		{
			Message(new GUIContent(message, messageType.ToString()), context, messageType, alsoLogToConsole);
		}
		
		/// <inheritdoc/>
		public void Message(GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true)
		{
			string text = message.text;
			
			switch(messageType)
			{
				case MessageType.Error:
					if(alsoLogToConsole && canAlsoLogToConsole)
					{
						Debug.LogError(text, context);
					}
					if(message.image == null)
					{
						message.image = InspectorUtility.Preferences.graphics.errorMessage;
					}
					break;
				case MessageType.Warning:
					if(alsoLogToConsole && canAlsoLogToConsole)
					{
						Debug.LogWarning(text, context);
					}
					if(message.image == null)
					{
						message.image = InspectorUtility.Preferences.graphics.warningMessage;
					}
					break;
				default:
					if(alsoLogToConsole && canAlsoLogToConsole)
					{
						Debug.Log(text, context);
					}
					if(message.image == null)
					{
						message.image = InspectorUtility.Preferences.graphics.infoMessage;
					}
					break;
			}

			#if UNITY_2019_1_OR_NEWER
			float duration = GetDisplayDuration(text);
			if(duration > 0f)
			{
				targetWindow.ShowNotification(AutoSplitLongTextIntoRows(message), duration);
			}
			#else
			targetWindow.ShowNotification(AutoSplitLongTextIntoRows(message));
			#endif
		}

		#if UNITY_2019_1_OR_NEWER
		private float GetDisplayDuration(string text)
		{
			float displayDuration = 0f;
			for(int n = text.Length - 1; n >= 0; n--)
			{
				switch(text[n])
				{
					case ' ':
					case '\r':
					case '\n':
					case ':':
					case '.':
					case '/':
					case '-':
					case '\\':
						displayDuration += displayDurationPerWord;
						break;
				}
			}

			return Mathf.Clamp(displayDuration, minDisplayDuration, maxDisplayDuration);
		}
		#endif

		private GUIContent AutoSplitLongTextIntoRows(GUIContent message)
		{
			float windowWidth = targetWindow.position.width;
			
			string text = message.text;
			int letterCount = text.Length;

			int wordWrapAfterLetters = Mathf.RoundToInt(windowWidth * 0.05f);

			if(letterCount <= wordWrapAfterLetters)
			{
				return message;
			}
						
			for(int n = wordWrapAfterLetters; n < letterCount; n++)
			{
				switch(text[n])
				{
					case '\r':
					case '\n':
						n += wordWrapAfterLetters;
						break;
					case ':':
					case '-':
						int next = n +  1;
						if(next + 1 >= letterCount)
						{
							n = letterCount;
							break;
						}
						if(text[next].IsLineEnd())
						{
							break;
						}
						text = text.Substring(0, next) + "\n" + text.Substring(next);
						n += wordWrapAfterLetters;
						break;
					case ' ':
						next = n +  1;
						if(next + 1 >= letterCount)
						{
							n = letterCount;
							break;
						}
						if(text[next].IsLineEnd())
						{
							break;
						}
						text = text.Substring(0, n) + "\n" + text.Substring(next);
						n += wordWrapAfterLetters;
						break;
				}
			}

			message.text = text;
			return message;
		}
	}
}