#define ENABLE_PREFIX_HIGHLIGHTING

//#define DEBUG_NOT_HIGHLIGHTING

using UnityEngine;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable]
	public abstract class PrefixDrawer : IDisposable
	{
		protected static PolymorphicPool<PrefixDrawer> pool = new PolymorphicPool<PrefixDrawer>(5, 250);
		private static readonly List<Rect> highlightRects = new List<Rect>();

		protected PrefixDrawer() { }

		protected PrefixDrawer(GUIContent setLabel)
		{
			Setup(setLabel);
		}

		protected PrefixDrawer(string text, string tooltip, Texture image)
		{
			Setup(text, tooltip, image);
		}

		protected void Setup(GUIContent setLabel)
		{
			Setup(setLabel.text, setLabel.tooltip, setLabel.image);
		}

		protected void Setup(string text, string tooltip, Texture image)
		{
			label.text = text;
			label.image = image;
			label.tooltip = tooltip;
		}

		public static void ClearCache()
		{
			pool.Clear();
		}

		public readonly GUIContent label = new GUIContent("");

		public abstract bool Unfolded { get; set; }
		
		public static PrefixDrawer CreateLabel([CanBeNull]string text, bool selected, bool mouseovered, bool unappliedChanges)
		{
			if(text == null)
			{
				return EmptyPrefixDrawer.Create();
			}

			if(selected)
			{
				if(unappliedChanges)
				{
					return SelectedModifiedPrefixDrawer.Create(text, "", null);
				}
				return SelectedPrefixDrawer.Create(text, "", null);
			}

			if(mouseovered)
			{
				if(unappliedChanges)
				{
					return MouseoveredModifiedPrefixDrawer.Create(text, "", null);
				}
				return MouseoveredPrefixDrawer.Create(text, "", null);
			}

			if(unappliedChanges)
			{
				return ModifiedPrefixDrawer.Create(text, "", null);
			}
			return IdlePrefixDrawer.Create(text, "", null);
		}

		public static PrefixDrawer CreateLabel([CanBeNull]GUIContent label, bool selected, bool mouseovered, bool unappliedChanges)
		{
			if(label == null)
			{
				return EmptyPrefixDrawer.Create();
			}

			if(selected)
			{
				if(unappliedChanges)
				{
					return SelectedModifiedPrefixDrawer.Create(label);
				}
				return SelectedPrefixDrawer.Create(label);
			}

			if(mouseovered)
			{
				if(unappliedChanges)
				{
					return MouseoveredModifiedPrefixDrawer.Create(label);
				}
				return MouseoveredPrefixDrawer.Create(label);
			}

			if(unappliedChanges)
			{
				return ModifiedPrefixDrawer.Create(label);
			}
			return IdlePrefixDrawer.Create(label);
		}

		public static FoldoutDrawer CreateFoldout([NotNull]GUIContent label, bool unfolded, bool textClipping, [NotNull]GUIStyle guiStyle)
		{
			var result = FoldoutDrawer.Create(label, textClipping, guiStyle);
			result.Unfolded = unfolded;
			return result;
		}

		public static FoldoutDrawer CreateFoldout([NotNull]GUIContent label, bool selected, bool mouseovered, bool unappliedChanges, bool unfolded, bool textClipping)
		{
			var result = FoldoutDrawer.Create(label, selected, mouseovered, unappliedChanges, textClipping);
			result.Unfolded = unfolded;
			return result;
		}

		/// <summary>
		/// Draw the prefix GUI at the given position.
		/// </summary>
		/// <param name="position"></param>
		public abstract void Draw(Rect position);

		/// <summary>
		/// Draw the prefix GUI at the given position.
		/// 
		/// This variant of the Draw method should be called when there's a filter present.
		/// It handles highlighting characters in the prefix label based on the filter.
		/// </summary>
		public virtual void Draw(Rect position, [CanBeNull]SearchFilter filter, string fullClassName, FilterTestType passedTestMethod)
		{
			HighlightTextForFilter(position, filter, fullClassName, passedTestMethod, -3f, -6f);
			Draw(position);
		}
		
		public void HighlightTextForFilter(Rect position, SearchFilter filter, string fullClassName, FilterTestType passedTestMethod, float offsetX, float adjustWidth)
		{
			if(TryGetTextHighlightRectsForFilter(label.text, position, DrawGUI.prefixLabel, filter, fullClassName, passedTestMethod, offsetX, 0f, adjustWidth, highlightRects))
			{
				var color = InspectorUtility.Preferences.theme.FilterHighlight;
				for(int n = highlightRects.Count - 1; n >= 0; n--)
				{
					Platform.Active.GUI.ColorRect(highlightRects[n], color);
				}
				highlightRects.Clear();
			}
			#if DEV_MODE && DEBUG_NOT_HIGHLIGHTING
			else { Debug.LogWarning("HighlightTextForFilter(\""+label.text+"\" / \""+fullClassName+") with passedTestMethod="+passedTestMethod+" Highlighting: "+StringUtils.False); }
			#endif
		}

		public static Rect? GetTextHighlightRectForFilter(string text, Rect position, GUIStyle style, SearchFilter filter, string fullClassName, FilterTestType passedTestMethod, float offsetX, float offsetY, float adjustWidth)
		{
			#if ENABLE_PREFIX_HIGHLIGHTING
		
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(filter.HasFilterAffectingInspectedTargetContent, "!HasFilterAffectingInspectedTargetContent");
			Debug.Assert(passedTestMethod != FilterTestType.None, passedTestMethod.ToString());
			Debug.Assert(fullClassName.Length >= text.Length, "\""+fullClassName+"\"");
			#endif

			string filterText = filter.FilterFieldLabel;
			if(filterText.Length == 0)
			{
				filterText = filter.FilterGeneric;
				if(filterText.Length == 0)
				{
					return null;
				}
			}

			int highlightStart;
			int highlightCount;
			switch(passedTestMethod)
			{
				case FilterTestType.Label:
					if(!TryGetHighlightStartIndexAndCharCount(text, filterText, out highlightStart, out highlightCount))
					{
						return null;
					}
					break;
				case FilterTestType.FullClassName:
				case FilterTestType.Indetermined:
					if(fullClassName.Length > text.Length && filterText.IndexOf('.') != -1)
					{
						highlightStart = GetHighlightStartIndex(text, filterText, out filterText);
						highlightCount = filterText.Length;
					}
					else
					{
						if(!TryGetHighlightStartIndexAndCharCount(text, filterText, out highlightStart, out highlightCount))
						{
							return null;
						}
					}
					break;
				default:
					return null;
			}

			if(highlightStart == -1)
			{
				return null;
			}

			var highlightRect = position;
			highlightRect.x += style.contentOffset.x + style.margin.left - 2f;
			
			var size = style.CalcSize(GUIContentPool.Temp(text.Substring(0, highlightStart)));
			highlightRect.x += size.x;

			size = style.CalcSize(GUIContentPool.Temp(text.Substring(highlightStart, highlightCount)));
			highlightRect.width = size.x;
			
			highlightRect.x += offsetX;
			highlightRect.y += offsetY;
			highlightRect.width += adjustWidth;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(highlightRect.x >= 0f);
			Debug.Assert(highlightRect.width > 0f);
			Debug.Assert(highlightRect.height > 0f);
			#endif
			
			return highlightRect;
			#else
			return null;
			#endif
		}

		public static bool TryGetTextHighlightRectsForFilter(string text, Rect position, GUIStyle style, SearchFilter filter, string fullClassName, FilterTestType passedTestMethod, float offsetX, float offsetY, float adjustWidth, List<Rect> results)
		{
			var genericFilters = filter.FiltersGeneric;
			int genericFiltersCount = genericFilters.Count;
			int genericFilterIndex = 0;

			string filterText = filter.FilterFieldLabel;
			do
			{
				if(filterText.Length > 0)	
				{
					int highlightStart;
					int highlightCount;
					switch(passedTestMethod)
					{
						case FilterTestType.Label:
							if(TryGetHighlightStartIndexAndCharCount(text, filterText, out highlightStart, out highlightCount))
							{
								results.Add(GetTextHighlightRectForFilter(text, position, style, highlightStart, highlightCount, offsetX, offsetY, adjustWidth));
							}
							break;
						case FilterTestType.FullClassName:
						case FilterTestType.Indetermined:
							if(fullClassName.Length > text.Length && filterText.IndexOf('.') != -1)
							{
								highlightStart = GetHighlightStartIndex(text, filterText, out filterText);
								if(highlightStart != -1)
								{
									highlightCount = filterText.Length;
									results.Add(GetTextHighlightRectForFilter(text, position, style, highlightStart, highlightCount, offsetX, offsetY, adjustWidth));
								}
							}
							else if(TryGetHighlightStartIndexAndCharCount(text, filterText, out highlightStart, out highlightCount))
							{
								results.Add(GetTextHighlightRectForFilter(text, position, style, highlightStart, highlightCount, offsetX, offsetY, adjustWidth));
							}
							break;
					}
				}

				if(genericFilterIndex >= genericFiltersCount)
				{
					break;
				}

				filterText = genericFilters[genericFilterIndex];
				genericFilterIndex++;

			}
			while(true);

			return results.Count > 0;
		}

		public static Rect GetTextHighlightRectForFilter(string text, Rect position, GUIStyle style, int highlightStart, int highlightCount, float offsetX, float offsetY, float adjustWidth)
		{
			#if ENABLE_PREFIX_HIGHLIGHTING
			var highlightRect = position;
			highlightRect.x += style.contentOffset.x + style.margin.left - 2f;
			
			var size = style.CalcSize(GUIContentPool.Temp(text.Substring(0, highlightStart)));
			highlightRect.x += size.x;

			size = style.CalcSize(GUIContentPool.Temp(text.Substring(highlightStart, highlightCount)));
			highlightRect.width = size.x;
			
			highlightRect.x += offsetX;
			highlightRect.y += offsetY;
			highlightRect.width += adjustWidth;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(highlightRect.x >= 0f, highlightRect);
			Debug.Assert(highlightRect.width > 0f, highlightRect);
			Debug.Assert(highlightRect.height > 0f, highlightRect);
			#endif
			
			return highlightRect;
			#else
			return null;
			#endif
		}

		private static bool TryGetHighlightStartIndexAndCharCount(string text, string filterText, out int startIndex, out int charCount)
		{
			startIndex = StringUtils.IndexOfIgnoringSpaces(text, filterText, out charCount);
			if(startIndex == -1)
			{
				return false;
			}
			charCount += filterText.Length;
			return true;
		}

		private static int GetHighlightStartIndex(string text, string filterText, out string partialFilterText)
		{
			int start = 0;
			int count = filterText.Length;
			for(int n = 0; n < count; n++)
			{
				if(filterText[n] == '.')
				{
					var part = filterText.Substring(start, n - start);
					if(part.Length > 0)
					{
						int highlightStart = text.IndexOf(part, StringComparison.OrdinalIgnoreCase);
						if(highlightStart != -1)
						{
							partialFilterText = part;
							return highlightStart;
						}
					}
					start = n + 1;
				}
			}

			if(start > 0 && start < count)
			{
				partialFilterText = filterText.Substring(start, count - start);
				return text.IndexOf(partialFilterText, StringComparison.OrdinalIgnoreCase);
			}

			partialFilterText = "";
			return -1;
		}

		public virtual void Dispose()
		{
			var disposing = this;
			pool.Pool(ref disposing);
		}
	}

	[Serializable]
	public sealed class IdlePrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; }  set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }

		public static IdlePrefixDrawer Create(string text, string tooltip, Texture image)
		{
			IdlePrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(text, tooltip, image);
				return result;
			}
			return new IdlePrefixDrawer(text, tooltip, image);
		}

		public static IdlePrefixDrawer Create(GUIContent label)
		{
			IdlePrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label);
				return result;
			}
			return new IdlePrefixDrawer(label);
		}
		
		private IdlePrefixDrawer(GUIContent setLabel) : base(setLabel) { }

		private IdlePrefixDrawer(string text, string tooltip, Texture image) : base(text, tooltip, image) { }

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			DrawGUI.Active.InlinedPrefixLabel(position, label);
		}
	}

	[Serializable]
	public sealed class SelectedPrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; }  set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }

		public static SelectedPrefixDrawer Create(string text, string tooltip, Texture image)
		{
			SelectedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(text, tooltip, image);
				return result;
			}
			return new SelectedPrefixDrawer(text, tooltip, image);
		}

		public static SelectedPrefixDrawer Create(GUIContent label)
		{
			SelectedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label);
				return result;
			}
			return new SelectedPrefixDrawer(label);
		}
		
		private SelectedPrefixDrawer(GUIContent setLabel) : base(setLabel) { }

		private SelectedPrefixDrawer(string text, string tooltip, Texture image) : base(text, tooltip, image) { }

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			DrawGUI.Active.InlinedSelectedPrefixLabel(position, label);
		}
	}

	[Serializable]
	public sealed class MouseoveredPrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; } set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }

		public static MouseoveredPrefixDrawer Create(string text, string tooltip, Texture image)
		{
			MouseoveredPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(text, tooltip, image);
				return result;
			}
			return new MouseoveredPrefixDrawer(text, tooltip, image);
		}

		public static MouseoveredPrefixDrawer Create(GUIContent label)
		{
			MouseoveredPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label);
				return result;
			}
			return new MouseoveredPrefixDrawer(label);
		}
		
		private MouseoveredPrefixDrawer(GUIContent setLabel) : base(setLabel) { }

		private MouseoveredPrefixDrawer(string text, string tooltip, Texture image) : base(text, tooltip, image) { }

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			DrawGUI.Active.InlinedMouseoveredPrefixLabel(position, label);
		}
	}

	[Serializable]
	public sealed class ModifiedPrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; }  set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }
		
		public static ModifiedPrefixDrawer Create(string text, string tooltip, Texture image)
		{
			ModifiedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(text, tooltip, image);
				return result;
			}
			return new ModifiedPrefixDrawer(text, tooltip, image);
		}

		public static ModifiedPrefixDrawer Create(GUIContent label)
		{
			ModifiedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label);
				return result;
			}
			return new ModifiedPrefixDrawer(label);
		}

		private ModifiedPrefixDrawer(GUIContent setLabel) : base(setLabel) { }

		private ModifiedPrefixDrawer(string text, string tooltip, Texture image) : base(text, tooltip, image) { }

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			DrawGUI.Active.InlinedModifiedPrefixLabel(position, label);
		}
	}

	public sealed class SelectedModifiedPrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; }  set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }

		public static SelectedModifiedPrefixDrawer Create(string text, string tooltip, Texture image)
		{
			SelectedModifiedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(text, tooltip, image);
				return result;
			}
			return new SelectedModifiedPrefixDrawer(text, tooltip, image);
		}

		public static SelectedModifiedPrefixDrawer Create(GUIContent label)
		{
			SelectedModifiedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label);
				return result;
			}
			return new SelectedModifiedPrefixDrawer(label);
		}
		
		private SelectedModifiedPrefixDrawer(GUIContent setLabel) : base(setLabel) { }

		private SelectedModifiedPrefixDrawer(string text, string tooltip, Texture image) : base(text, tooltip, image) { }

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			DrawGUI.Active.InlinedSelectedModifiedPrefixLabel(position, label);
		}
	}

	public sealed class MouseoveredModifiedPrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; } set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }

		public static MouseoveredModifiedPrefixDrawer Create(string text, string tooltip, Texture image)
		{
			MouseoveredModifiedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(text, tooltip, image);
				return result;
			}
			return new MouseoveredModifiedPrefixDrawer(text, tooltip, image);
		}

		public static MouseoveredModifiedPrefixDrawer Create(GUIContent label)
		{
			MouseoveredModifiedPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label);
				return result;
			}
			return new MouseoveredModifiedPrefixDrawer(label);
		}

		private MouseoveredModifiedPrefixDrawer(GUIContent setLabel) : base(setLabel) { }

		private MouseoveredModifiedPrefixDrawer(string text, string tooltip, Texture image) : base(text, tooltip, image) { }

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			DrawGUI.Active.InlinedMouseoveredModifiedPrefixLabel(position, label);
		}
	}
	
	[Serializable]
	public sealed class FoldoutDrawer : PrefixDrawer
	{
		[SerializeField]
		private bool unfolded;

		private GUIStyle style;
		private bool clipping;

		public override bool Unfolded
		{
			get { return unfolded; }
			set
			{
				unfolded = value;
			}
		}

		public GUIStyle GUIStyle
		{
			get
			{
				return style;
			}
		}

		public static FoldoutDrawer Create(GUIContent label, bool selected, bool mouseovered, bool unappliedChanges, bool textClipping, GUIStyle guiStyle = null)
		{
			FoldoutDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label, selected, mouseovered, unappliedChanges, textClipping, guiStyle);
				return result;
			}
			return new FoldoutDrawer(label, selected, mouseovered, unappliedChanges, textClipping, guiStyle);
		}

		public static FoldoutDrawer Create([NotNull]GUIContent label, bool textClipping, [NotNull]GUIStyle guiStyle)
		{
			FoldoutDrawer result;
			if(pool.TryGet(out result))
			{
				result.Setup(label, false, false, false, textClipping, guiStyle);
				return result;
			}
			return new FoldoutDrawer(label, false, false, false, textClipping, guiStyle);
		}

		private FoldoutDrawer([NotNull]GUIContent setLabel, bool selected, bool mouseovered, bool unappliedChanges, bool textClipping, [CanBeNull]GUIStyle setStyle)
		{
			Setup(setLabel, selected, mouseovered, unappliedChanges, textClipping, setStyle);
		}

		private void Setup([NotNull]GUIContent setLabel, bool selected, bool mouseovered, bool unappliedChanges, bool textClipping, [CanBeNull]GUIStyle setStyle)
		{
			Setup(setLabel);

			clipping = textClipping;

			if(setStyle == null)
			{
				style = DrawGUI.GetFoldoutStyle(selected, mouseovered, unappliedChanges, textClipping);
			}
			else
			{
				style = setStyle;
			}
		}

		/// <inheritdoc/>
		public override void Draw(Rect position, SearchFilter filter, string fullClassName, FilterTestType passedTestMethod)
		{
			var highlightRect = GetTextHighlightRectForFilter(label.text, position, style, filter, fullClassName, passedTestMethod, -4f, 0f, -17f);
			if(highlightRect.HasValue)
			{
				if(clipping)
				{
					bool drawBackgroundBehindFoldoutsWas = DrawGUI.drawBackgroundBehindFoldouts;
					DrawGUI.drawBackgroundBehindFoldouts = false;
					DrawGUI.Active.Foldout(position, label, unfolded, style, highlightRect);
					DrawGUI.drawBackgroundBehindFoldouts = drawBackgroundBehindFoldoutsWas;
				}
				else
				{
					DrawGUI.Active.Foldout(position, label, unfolded, style, highlightRect);
				}
				return;
			}
			Draw(position);
		}

		/// <inheritdoc/>
		public override void Draw(Rect position)
		{
			if(clipping)
			{
				bool drawBackgroundBehindFoldoutsWas = DrawGUI.drawBackgroundBehindFoldouts;
				DrawGUI.drawBackgroundBehindFoldouts = false;
				DrawGUI.Active.Foldout(position, label, unfolded, style);
				DrawGUI.drawBackgroundBehindFoldouts = drawBackgroundBehindFoldoutsWas;
			}
			else
			{
				DrawGUI.Active.Foldout(position, label, unfolded, style);
			}
		}

		public override void Dispose()
		{
			unfolded = false;
			base.Dispose();
		}
	}

	[Serializable]
	public sealed class EmptyPrefixDrawer : PrefixDrawer
	{
		public override bool Unfolded { get { return true; }  set { throw new InvalidOperationException("Can't set unfolded state of PrefixDrawer"); } }

		public static EmptyPrefixDrawer Create()
		{
			EmptyPrefixDrawer result;
			if(pool.TryGet(out result))
			{
				return result;
			}
			return new EmptyPrefixDrawer();
		}

		private EmptyPrefixDrawer() { }

		/// <inheritdoc/>
		public override void Draw(Rect position) { }

		/// <inheritdoc/>
		public override void Draw(Rect position, SearchFilter filter, string fullClassName, FilterTestType passedTestMethod) { }
	}
}