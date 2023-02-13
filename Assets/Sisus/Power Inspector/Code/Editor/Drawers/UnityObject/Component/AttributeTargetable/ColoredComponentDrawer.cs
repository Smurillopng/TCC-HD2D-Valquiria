using System;
using JetBrains.Annotations;
using UnityEngine;
using Sisus.Attributes;

namespace Sisus
{
	/// <summary>
	/// Draws a component with a custom background color.
	/// </summary>
	/// <example>
	/// <code>
	/// [BackgroundColor(0, 255, 0)]
	/// public class ComponentWithGreenBackground : MonoBehaviour { }
	/// </code>
	/// </example>
	public class ColoredComponentDrawer : ComponentDrawer
	{
		private static readonly Color transparentColor = new Color(0f, 0f, 0f, 0f);

		private Color color;
		private GUIThemeColors.BackgroundColors previousBackgroundColors;

		/// <inheritdoc/>
		protected override Color PrefixBackgroundColor
		{
			get
			{
				return HeaderMouseovered ? Preferences.theme.ComponentMouseoveredHeaderBackground : previousBackgroundColors.componentHeaderBackground;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="targets"> The targets that the drawers represent. </param>
		/// <param name="parent"> The parent drawers of the created drawers. Can be null. </param>
		/// <param name="inspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static ColoredComponentDrawer Create(Color setColor, [NotNull]Component[] targets, [CanBeNull]IParentDrawer parent, [NotNull]IInspector inspector)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(inspector != null, "ColoredComponentDrawer.Create inspector was null for targets " + StringUtils.ToString(targets));
			#endif

			ColoredComponentDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ColoredComponentDrawer();
			}
			result.Setup(setColor, targets, parent, null, inspector);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>  
		public override void SetupInterface(Component[] setTargets, IParentDrawer setParent, IInspector setInspector)
		{
			var useDrawer = (IDrawerSetupDataProvider)setTargets[0].GetType().GetCustomAttributes(typeof(IDrawerSetupDataProvider), false)[0];
			var parameters = useDrawer.GetSetupParameters();
			Color setColor;
			switch(parameters.Length)
			{
				case 1:
					setColor = (Color)parameters[0];
					break;
				case 3:
					if(parameters[0].GetType() == Types.Float)
					{
						setColor = new Color((float)parameters[0], (float)parameters[1], (float)parameters[2], 1f);
					}
					else
					{
						setColor = new Color32((byte)parameters[0], (byte)parameters[1], (byte)parameters[2], 255);
					}
					break;
				case 4:
					if(parameters[0].GetType() == Types.Float)
					{
						setColor = new Color((float)parameters[0], (float)parameters[1], (float)parameters[2], (float)parameters[3]);
					}
					else
					{
						setColor = new Color32((byte)parameters[0], (byte)parameters[1], (byte)parameters[2], (byte)parameters[3]);
					}
					break;
					default:
					throw new NotSupportedException();
			}
			Setup(setColor, setTargets, setParent, null, setInspector);
		}

		/// <inheritdoc/>
		protected override void Setup(Component[] setTargets, IParentDrawer setParent, GUIContent setLabel, IInspector setInspector)
		{
			throw new InvalidOperationException("Please use the other Setup method.");
		}

		/// <summary>
		/// Sets up the Drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setTargets"> The set targets. This cannot be null. </param>
		/// <param name="setParent"> The set parent. This may be null. </param>
		/// <param name="setLabel"> The set label. This may be null. </param>
		/// <param name="setInspector"> The inspector in which the IDrawer are contained. Can not be null. </param>
		private void Setup(Color setColor, [NotNull]Component[] setTargets, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, [NotNull]IInspector setInspector)
		{
			color = setColor;
			base.Setup(setTargets, setParent, setLabel, setInspector);
		}

		/// <inheritdoc />
		public override bool Draw(Rect position)
		{
			// Draw the background color
			DrawGUI.Active.ColorRect(position, color);

			// Also temporarily set all background colors in theme to match this color.
			var theme = inspector.Preferences.theme;

			var backgroundColor = color.a >= 1f ? color : transparentColor;
			theme.SetBackgroundColors(backgroundColor, out previousBackgroundColors);

			bool dirty;
			try
			{
				dirty = base.Draw(position);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(e);
			#else
			catch(Exception)
			{
			#endif
				theme.RestoreBackgroundColors(previousBackgroundColors);
				throw;
			}

			theme.RestoreBackgroundColors(previousBackgroundColors);
			return dirty;
		}
	}
}
