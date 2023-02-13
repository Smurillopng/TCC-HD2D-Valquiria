#if UNITY_EDITOR
using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForAsset(typeof(SceneAsset), false, true)]
	public class SceneDrawer : CustomEditorAssetDrawer
	{
		[CanBeNull]
		private Code yaml;
		private float yamlContentHeight;
		private Vector2 yamlViewScrollPos;
		private float yamlViewportHeight;
		private bool yamlViewHasScrollBar;

		/// <summary>
		/// 
		/// </summary>
		public override float Height
		{
			get
			{
				return ViewingYaml() ? yamlViewportHeight : base.Height;
			}
		}

		private float YamlContentHeight
		{
			get
			{
				return yamlContentHeight;
			}
		}

		/// <inheritdoc />
		protected override void OnInspectorWidthChanged()
		{
			base.OnChildLayoutChanged();

			if(yaml != null)
			{
				yamlContentHeight = yaml.LineCount * DrawGUI.SingleLineHeight;
				yamlViewportHeight = CalculateViewportHeight();
				yamlViewHasScrollBar = yamlContentHeight > yamlViewportHeight;
			}
		}

		/// <inheritdoc />
		protected override void OnDebugModeChanged(bool nowEnabled)
		{
			if(nowEnabled)
			{
				RebuildYamlContent();
				UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
			}
			else
			{
				UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
				if(yaml != null)
				{
					yaml.Dispose();
					yaml = null;
				}

				yamlContentHeight = 0f;
				yamlViewScrollPos.x = 0f;
				yamlViewScrollPos.y = 0f;
				yamlViewportHeight = 0f;
				yamlViewHasScrollBar = false;
			}

			base.OnDebugModeChanged(nowEnabled);
		}

		/// <inheritdoc />
		public override bool DrawBody(Rect position)
		{
			if(ViewingYaml())
			{
				var contentRect = position;
				contentRect.x = 0f;
				contentRect.y = 0f;
				contentRect.height = yamlContentHeight;
				if(yamlViewHasScrollBar)
				{
					contentRect.width -= DrawGUI.ScrollBarWidth;
				}

				var viewportRect = position;
				viewportRect.height = yamlViewportHeight;

				DrawGUI.BeginScrollView(viewportRect, contentRect, ref yamlViewScrollPos);
				{
					var style = InspectorPreferences.Styles.formattedText;
					
					var linePosition = contentRect;
					linePosition.height = DrawGUI.SingleLineHeight;

					int firstIndex = Mathf.FloorToInt(yamlViewScrollPos.y / DrawGUI.SingleLineHeight);
					int lastIndex = firstIndex + Mathf.FloorToInt(yamlViewportHeight / DrawGUI.SingleLineHeight);

					int lineCount = yaml.LineCount;
					if(lastIndex >= lineCount)
					{
						lastIndex = lineCount - 1;
					}

					linePosition.y = yamlViewScrollPos.y - yamlViewScrollPos.y % DrawGUI.SingleLineHeight;

					for(int n = firstIndex; n <= lastIndex; n++)
					{
						EditorGUI.SelectableLabel(linePosition, yaml[n], style);

						linePosition.y += DrawGUI.SingleLineHeight;
					}
				}
				DrawGUI.EndScrollView();

				return false;
			}

			return base.DrawBody(position);
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			if(yaml != null)
			{
				yaml.Dispose();
				yaml = null;

				yamlContentHeight = 0f;
				yamlViewScrollPos.x = 0f;
				yamlViewScrollPos.y = 0f;
				yamlViewportHeight = 0f;
				yamlViewHasScrollBar = false;

				UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
			}

			base.Dispose();
		}

		private void OnSceneSaved(UnityEngine.SceneManagement.Scene savedScene)
		{
			var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(LocalPath);
			if(savedScene == scene)
			{
				RebuildYamlContent();
			}
		}

		private void RebuildYamlContent()
		{
			var yamlText = System.IO.File.ReadAllText(FullPath);

			if(yaml != null)
			{
				if(string.Equals(yaml.TextUnformatted, yamlText))
				{
					return;
				}

				yaml.Dispose();
				yaml = null;
			}

			var builder = CreateSyntaxFormatter();
			yaml = new Code(builder);
			builder.SetCode(yamlText);
			builder.BuildAllBlocks();
			builder.GeneratedBlocks.ToCode(ref yaml, builder, Fonts.NormalSizes);
			yamlContentHeight = yaml.LineCount * DrawGUI.SingleLineHeight;
			yamlViewportHeight = CalculateViewportHeight();
			yamlViewHasScrollBar = yamlContentHeight > yamlViewportHeight;
		}

		private bool ViewingYaml()
		{
			return DebugMode && targets.Length == 1;
		}

		private float CalculateViewportHeight()
		{
			float height = inspector.State.WindowRect.height - inspector.ToolbarHeight - inspector.PreviewAreaHeight;

			if(height < 0f)
			{
				return 0f;
			}

			if(HasHorizontalScrollBar())
			{
				height -= DrawGUI.ScrollBarWidth;
			}

			if(!UserSettings.MergedMultiEditMode && inspector.State.inspected.Length > 1)
			{
				height = Mathf.Min(yamlContentHeight, height);
			}

			return height;
		}

		private bool HasHorizontalScrollBar()
		{
			return yaml.width > Width;
		}

		private YamlSyntaxFormatter CreateSyntaxFormatter()
		{
			return YamlSyntaxFormatterPool.Pop();
		}
	}
}
#endif