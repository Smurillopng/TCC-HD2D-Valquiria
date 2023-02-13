#define SAFE_MODE
//#define DEBUG_POOLING
//#define DISABLE_POOLING //TEMP FOR TESTING

using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	public static class GUIContentPool
	{
		private static readonly Pool<GUIContent> Pool = new Pool<GUIContent>(50);
		private static readonly GUIContent ReusableTempLabel = new GUIContent();

		public static GUIContent Temp()
		{
			ReusableTempLabel.text = "";
			ReusableTempLabel.tooltip = "";
			return ReusableTempLabel;
		}

		public static GUIContent Temp(string text)
		{
			ReusableTempLabel.text = text;
			ReusableTempLabel.tooltip = "";
			return ReusableTempLabel;
		}

		public static GUIContent Temp(string text, string tooltip)
		{
			ReusableTempLabel.text = text;
			ReusableTempLabel.tooltip = tooltip;
			return ReusableTempLabel;
		}

		public static GUIContent Empty()
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent();
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result != GUIContent.none);
			#endif

			#if DEV_MODE && DEBUG_POOLING
			if(!string.IsNullOrEmpty(result.text)) { Debug.Log("Empty() reusing existing: "+StringUtils.ToString(result)); }
			#endif

			result.text = "";
			result.image = null;
			result.tooltip = "";
			return result;
		}

		public static GUIContent Create([NotNull]string text)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(text);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result != GUIContent.none);
			#endif

			#if DEV_MODE && DEBUG_POOLING
			if(!string.IsNullOrEmpty(result.text)) { Debug.Log("Create("+StringUtils.ToString(text)+") reusing existing: "+StringUtils.ToString(result)); }
			#endif

			result.text = text;
			result.image = null;
			result.tooltip = "";
			return result;
		}

		public static GUIContent Create([CanBeNull]Texture image)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(image);
			}
			result.text = "";
			result.image = image;
			result.tooltip = "";
			return result;
		}

		public static GUIContent Create([CanBeNull]Texture image, string tooltip)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(image, tooltip);
			}
			result.text = "";
			result.image = image;
			result.tooltip = tooltip;
			return result;
		}

		public static GUIContent Create(string text, string tooltip)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(text, tooltip);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result != GUIContent.none);
			#endif

			#if DEV_MODE && DEBUG_POOLING
			if(!string.IsNullOrEmpty(result.text)) { Debug.Log("Create("+StringUtils.ToString(text)+") reusing existing: "+StringUtils.ToString(result)); }
			#endif

			result.text = text;
			result.image = null;
			result.tooltip = tooltip;
			return result;
		}

		public static GUIContent Create(string text, [CanBeNull]Texture image)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(text, image);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result != GUIContent.none);
			#endif
			
			result.text = text;
			result.image = image;
			result.tooltip = "";
			return result;
		}

		public static GUIContent Create(string text, [CanBeNull]Texture image, string tooltip)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(text, image, tooltip);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result != GUIContent.none);
			#endif

			result.text = text;
			result.image = image;
			result.tooltip = tooltip;
			return result;
		}

		[NotNull]
		public static GUIContent Create([NotNull]GUIContent clone)
		{
			GUIContent result;
			if(!Pool.TryGet(out result))
			{
				return new GUIContent(clone.text, clone.image, clone.tooltip);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(result != GUIContent.none);
			#endif
			
			result.image = clone.image;
			result.text = clone.text;
			result.tooltip = clone.tooltip;
			return result;
		}

		public static void Replace([CanBeNull]ref GUIContent replace, string text, string tooltip = "")
		{
			if(replace == null || replace == GUIContent.none)
			{
				replace = Empty();
			}
			replace.image = null;

			replace.text = text;
			replace.tooltip = tooltip;
		}

		/// <summary>
		/// Disposes "replace" and replaces it with a new GUIContent instance that is a copy of "copySource".
		/// </summary>
		/// <param name="replace"> [in,out] The target to override. </param>
		/// <param name="copySource"> The source to copy. This cannot be null. </param>
		public static void CopyOver([CanBeNull]ref GUIContent replace, [NotNull]GUIContent copySource)
		{
			if(replace != null && replace != GUIContent.none && replace != copySource)
			{
				Dispose(ref replace);
			}
			replace = Create(copySource);
		}

		public static void Dispose([NotNull]ref GUIContent disposing)
		{
			#if DEV_MODE && DEBUG_POOLING
			/*if(disposing != null && !string.IsNullOrEmpty(disposing.text))*/ { Debug.Log("Dispose("+StringUtils.ToString(disposing)+")"); }
			#endif
		
			#if DEV_MODE && PI_ASSERTATIONS
			if(InspectorUtility.ActiveInspector != null && InspectorUtility.ActiveInspector.Preferences != null && InspectorUtility.ActiveInspector.Preferences.labels != null)
			{
				InspectorUtility.ActiveInspector.Preferences.labels.AssertDoesNotContain(disposing);
			}
			#endif

			#if DISABLE_POOLING
			disposing = null;
			#else
			if(disposing == GUIContent.none)
			{
				#if DEV_MODE
				Debug.LogWarning("Tried to dispose GUIContent.none!");
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(disposing.text.Length == 0 && disposing.tooltip.Length == 0 && disposing.image == null, "Dispose() was called for GUIContent.none and it had content: "+disposing);
				#endif

				disposing = null;
				return;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(disposing != ReusableTempLabel);
			#endif
			
			Pool.Dispose(ref disposing);
			disposing = null;
			#endif
		}
	}
}