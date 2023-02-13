//#define DEBUG_SET_PREVIEW_AREA_HEIGHT

namespace Sisus
{
	/// <summary>
	/// Per-user settings that persist between sessions.
	/// </summary>
	public static class UserSettings
	{
		public static bool PreviewAreaMinimized
		{
			get
			{
				return Platform.Active.GetPrefs("PI.PreviewAreaMinimized", true);
			}

			set
			{
				Platform.Active.SetPrefs("PI.PreviewAreaMinimized", value, true);
			}
		}

		public static ShowPreviewArea ShowPreviewArea
		{
			get
			{
				return (ShowPreviewArea)Platform.Active.GetPrefs("PI.ShowPreviewArea", (int)ShowPreviewArea.Manual);
			}

			set
			{
				Platform.Active.SetPrefs("PI.ShowPreviewArea", (int)value, (int)ShowPreviewArea.Manual);
			}
		}

		public static float PreviewAreaOpenHeight
		{
			get
			{
				return Platform.Active.GetPrefs("PI.PreviewAreaOpenHeight", 79f);
			}

			set
			{
				#if DEV_MODE && DEBUG_SET_PREVIEW_AREA_HEIGHT
				UnityEngine.Debug.Log("PreviewAreaOpenHeight = " + value);
				#endif

				Platform.Active.SetPrefs("PI.PreviewAreaOpenHeight", value, 79f);
			}
		}

		public static bool MergedMultiEditMode
		{
			get
			{
				return Platform.Active.GetPrefs("PI.MergedMultiEditMode", true);
			}

			set
			{
				Platform.Active.SetPrefs("PI.MergedMultiEditMode", value, true);
			}
		}

		public static bool EditComponentsOneAtATime
		{
			get
			{
				return Platform.Active.GetPrefs("PI.EditComponentsOneAtATime", false);
			}

			set
			{
				Platform.Active.SetPrefs("PI.EditComponentsOneAtATime", value, false);
			}
		}

		public static class Snapping
		{
			public static bool Enabled
			{
				get
				{

					return Platform.Active.GetPrefs("PI.EnableSnapping", false);
				}

				set
				{
					Platform.Active.SetPrefs("PI.EnableSnapping", value, false);

					// Disable snapping for all members by default.
					// This is to avoid values being altered the moment that snapping is enabled for the transform.
					if(!value)
					{
						EnabledForMove = false;
						EnabledForRotate = false;
						EnabledForScale = false;
					}
				}
			}

			public static bool EnabledForMove
			{
				get
				{

					return Platform.Active.GetPrefs("PI.MoveSnapOn", false);
				}
				set
				{
					Platform.Active.SetPrefs("PI.MoveSnapOn", value, false);
				}
			}

			public static bool EnabledForRotate
			{
				get
				{

					return Platform.Active.GetPrefs("PI.RotateSnapOn", false);
				}
				set
				{
					Platform.Active.SetPrefs("PI.RotateSnapOn", value, false);
				}
			}

			public static bool EnabledForScale
			{
				get
				{

					return Platform.Active.GetPrefs("PI.ScaleSnapOn", false);
				}
				set
				{
					Platform.Active.SetPrefs("PI.ScaleSnapOn", value, false);
				}
			}

			public static float MoveX
			{
				get
				{
					return Platform.Active.GetPrefs("MoveSnapX", 1f);
				}

				set
				{
					if(value < 0.001f)
					{
						value = 0.001f;
					}
					Platform.Active.SetPrefs("MoveSnapX", value, 1f);
				}
			}

			public static float MoveY
			{
				get
				{
					return Platform.Active.GetPrefs("MoveSnapY", 1f);
				}
				set
				{
					if(value < 0.001f)
					{
						value = 0.001f;
					}
					Platform.Active.SetPrefs("MoveSnapY", value, 1f);
				}
			}

			public static float MoveZ
			{
				get
				{
					return Platform.Active.GetPrefs("MoveSnapZ", 1f);
				}
				set
				{
					if(value < 0.001f)
					{
						value = 0.001f;
					}
					Platform.Active.SetPrefs("MoveSnapZ", value, 1f);
				}
			}

			public static float Rotation
			{
				get
				{
					return Platform.Active.GetPrefs("RotationSnap", 90f);
				}

				set
				{
					if(value < 0.001f)
					{
						value = 0.001f;
					}
					Platform.Active.SetPrefs("RotationSnap", value, 90f);
				}
			}

			public static float Scale
			{
				get
				{
					return Platform.Active.GetPrefs("ScaleSnap", 0.1f);
				}
				set
				{
					if(value < 0.001f)
					{
						value = 0.001f;
					}
					Platform.Active.SetPrefs("ScaleSnap", value, 0.1f);
				}
			}
		}
	}
}