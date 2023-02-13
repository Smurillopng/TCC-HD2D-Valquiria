using UnityEngine;
using UnityEditor;

namespace Sisus
{
	public interface IEditorWindowMessageDispenser
	{
		/// <summary>
		/// Setups the message dispenser for the given EditorWindow and using the provided preferences.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="preferences"></param>
		void Setup(EditorWindow target, InspectorPreferences preferences);

		/// <summary>
		/// Sends a message to the user of the Inspector.
		/// In the editor the Console can be used.
		/// </summary>
		/// <param name="message"> The message to show. </param>
		/// <param name="context"> (Optional) The UnityEngine.Object context for the message. </param>
		/// <param name="messageType"> (Optional) Type of the message. </param>
		/// <param name="alsoLogToConsole"> (Optional) If true message will also be logged to console, if false it will only be shown as a popup message. </param>
		void Message(string message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true);

		/// <summary>
		/// Sends a message to the user of the Inspector.
		/// In the editor the Console can be used.
		/// </summary>
		/// <param name="message"> The message to show. </param>
		/// <param name="context"> (Optional) The UnityEngine.Object context for the message. </param>
		/// <param name="messageType"> (Optional) Type of the message. </param>
		///  <param name="alsoLogToConsole"> (Optional) If true message will also be logged to console, if false it will only be shown as a popup message. </param>
		void Message(GUIContent message, Object context = null, MessageType messageType = MessageType.Info, bool alsoLogToConsole = true);
	}
}
