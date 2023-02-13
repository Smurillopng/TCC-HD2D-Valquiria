//#define DEBUG_FOCUS_CONTROL
//#define DEBUG_CAN_REQUEST_CURSOR_POSITION

using UnityEngine;
using JetBrains.Annotations;
using UnityEditor;

namespace Sisus
{
	/// <summary>
	/// Class that tracks current cursor position and broadcasts OnPositionChanged
	///	event whenever it changes. It can also be polled for the current cursor position
	///	in local GUILayout space as well as screen space.
	///	
	///	Relies on DrawGUI.OnBeginOnGUI which only gets called if there's at least one
	///	class that has an OnGUI method and calls DrawGUI.BeginOnGUI at the start of said method.
	/// </summary>
	[InitializeOnLoad]
	public static class Cursor
	{
		public delegate void CursorPositionChangedEvent(Vector2 cursorScreenPosition);

		private static readonly Vector2 InvalidPosition = new Vector2(-10000f, -10000f);

		public static CursorPositionChangedEvent OnPositionChanged;

		private static bool initialized;
		private static Vector2 screenPosition = Vector2.zero;
		private static Vector2 localPosition;
		private static bool canRequestLocalPosition;

		/// <summary>
		/// Can cursor position in local GUILayout space be requested from current context?
		/// Returns true if called from inside OnGUI, otherwise returns false.
		/// </summary>
		/// <value>
		/// True if we can request local position, false if not.
		/// </value>
		public static bool CanRequestLocalPosition
		{
			get
			{
				return Event.current != null && Event.current.type != EventType.Ignore && canRequestLocalPosition;
			}

			set
			{
				#if DEV_MODE && DEBUG_CAN_REQUEST_CURSOR_POSITION
				if(canRequestLocalPosition != value) { Debug.Log("CanRequestLocalPosition = "+StringUtils.ToColorizedString(value)); }
				#endif

				canRequestLocalPosition = value;
			}
		}
		
		/// <summary>
		/// Gets the current position in local GUILayout space. Same as calling Event.current.mousePosition, except
		/// doesn't throw a NullReferenceException if Event.current is null but prints a descriptive error to log instead.
		/// </summary>
		/// <value> Cursor position in local GUILayout space. </value>
		public static Vector2 LocalPosition
		{
			get
			{
				var e = Event.current;
				if(e == null)
				{
					Debug.LogError("Cursor.LocalPosition was requested outside of OnGUI! Did you mean to use ScreenPosition instead?");
					return InvalidPosition;
				}
				return e.mousePosition;
			}
		}

		/// <summary>
		/// Gets the current cursor position in Screen space.
		/// This can called safely even outside of OnGUI.
		/// </summary>
		/// <value> Cursor position in screen space.
		/// </value>
		public static Vector2 ScreenPosition
		{
			get
			{
				return screenPosition;
			}
		}

		/// <summary>
		/// If can request cursor local GUILayout position from current context then
		/// sets parameter cursorLocalPosition to the local position and returns true.
		/// If can't request it now, sets cursorLocalPosition to Cursor.InvalidPosition
		/// and returns false;
		/// </summary>
		/// <param name="cursorLocalPosition">
		/// [out] The result. </param>
		/// <returns>
		/// True if could get cursor local GUILayout position from current context.
		/// </returns>
		public static bool TryGetLocalPosition(out Vector2 cursorLocalPosition)
		{
			if(CanRequestLocalPosition)
			{
				cursorLocalPosition = localPosition;
				return true;
			}
			cursorLocalPosition = InvalidPosition;
			return false;
		}

		/// <summary>
		/// This is called on editor load because of usage of the InitializeOnLoad attribute.
		/// </summary>
		[UsedImplicitly]
		static Cursor()
		{
			EditorApplication.delayCall += Initialize;
		}

		/// <summary>
		/// This is called when entering play mode or when the game is loaded
		/// </summary>
		[RuntimeInitializeOnLoadMethod, UsedImplicitly]
		private static void RuntimeInitializeOnLoad()
		{
			EditorApplication.delayCall += Initialize;
		}

		private static void Initialize()
		{
			if(initialized)
			{
				return;
			}

			if(!ApplicationUtility.IsReady())
			{
				EditorApplication.delayCall += Initialize;
				return;
			}

			initialized = true;
			
			DrawGUI.OnEveryBeginOnGUI(UpdateMousePosition, false);
		}
		
		private static void UpdateMousePosition()
		{
			var newPosition = Event.current.mousePosition;
			var newPositionScreenSpace = GUIUtility.GUIToScreenPoint(newPosition);
			if(newPositionScreenSpace != screenPosition)
			{
				localPosition = newPosition;
				screenPosition = newPositionScreenSpace;
				
				if(OnPositionChanged != null)
				{
					OnPositionChanged(newPositionScreenSpace);
				}
			}
		}
	}
}