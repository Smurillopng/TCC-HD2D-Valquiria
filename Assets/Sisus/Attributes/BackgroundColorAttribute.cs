using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus.Attributes
{
	/// <summary>
	/// Attribute that can be added to a Component class to have it be drawn with a specified background color.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public class BackgroundColorAttribute : Attribute, IUseDrawer, IDrawerSetupDataProvider
	{
		private static readonly object[] parameterWrapper = new object[1];

		private static Type customEditorComponentDrawerInterface;
		private static Type coloredCustomEditorComponentDrawer;
		private static Type coloredComponentDrawer;

		private readonly string html;
		private readonly float r;
		private readonly float g;
		private readonly float b;
		private readonly float a;

		public object color
		{
			get
			{
				if(!string.IsNullOrEmpty(html))
				{
					Color result;
					if(!ColorUtility.TryParseHtmlString(html, out result))
					{
						return color;
					}
					Debug.LogError("BackgroundColorAttribute failed to parse color in format rrggbbaa: " + html);
				}
				return new Color(r, g, b, a);
			}
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified background color.
		/// </summary>
		/// <param name="colorHtml">
		/// Case insensitive html string to be converted into a color.
		/// 
		/// Supported formats are #RRGGBB, #RRGGBBAA and literal colors with the following supported:
		/// red, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta.
		/// 
		/// When not specified alpha will default to FF (fully opaque).
		/// </param>
		/// <example>
		/// <code>
		/// [BackgroundColor("#00FF00")]
		/// public class ComponentWithGreenBackground : MonoBehaviour { }
		/// 
		/// [BackgroundColor("yellow")]
		/// public class ComponentWithYellowBackground : MonoBehaviour { }
		/// </code>
		/// </example>
		public BackgroundColorAttribute([NotNull]string colorHtml)
		{
			if(colorHtml[0] != '#')
			{
				html = "#" + colorHtml;
			}
			else
			{
				html = colorHtml;
			}
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified background color.
		/// </summary>
		/// <param name="r"> Color32 red component intensity. </param>
		/// <param name="g"> Color32 green component intensity. </param>
		/// <param name="b"> Color32 alpha component intensity. 0 is fully transparent and 255 is fully opaque. </param>
		/// <example>
		/// <code>
		/// [BackgroundColor(0, 255, 0)]
		/// public class ComponentWithGreenBackground : MonoBehaviour { }
		/// </code>
		/// </example>
		public BackgroundColorAttribute(byte r, byte g, byte b) : base()
		{
			this.r = r / 255f;
			this.g = g / 255f;
			this.b = b / 255f;
			a = 1f;
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified background color.
		/// </summary>
		/// <param name="r"> Color red component intensity between 0 (min) and 255 (max). </param>
		/// <param name="g"> Color green component intensity between 0 (min) and 255 (max). </param>
		/// <param name="b"> Color blue component intensity between 0 (min) and 255 (max). </param>
		/// <param name="a"> Color32 alpha component intensity between 0 (fully transparent) and 255 (fully opaque). </param>
		/// <example>
		/// <code>
		/// [BackgroundColor(0, 255, 0, 255)]
		/// public class ComponentWithGreenBackground : MonoBehaviour { }
		/// </code>
		/// </example>
		public BackgroundColorAttribute(byte r, byte g, byte b, byte a) : base()
		{
			this.r = r / 255f;
			this.g = g / 255f;
			this.b = b / 255f;
			this.a = a / 255f;
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified background color.
		/// </summary>
		/// <param name="r"> Color red component intensity between 0f (min) and 1f (max). </param>
		/// <param name="g"> Color green component intensity between 0f (min) and 1f (max). </param>
		/// <param name="b"> Color blue component intensity between 0f (min) and 1f (max). </param>
		/// <example>
		/// <code>
		/// [BackgroundColor(0f, 1f, 0f)]
		/// public class ComponentWithGreenBackground : MonoBehaviour { }
		/// </code>
		/// </example>
		public BackgroundColorAttribute(float r, float g, float b) : base()
		{
			this.r = r;
			this.g = g;
			this.b = b;
			a = 1f;
		}

		/// <summary>
		/// Specifies that the attribute holder should be drawn with the specified background color.
		/// </summary>
		/// <param name="r"> Color red component intensity between 0f (min) and 1f (max). </param>
		/// <param name="g"> Color green component intensity between 0f (min) and 1f (max). </param>
		/// <param name="b"> Color blue component intensity between 0f (min) and 1f (max). </param>
		/// <param name="a"> Color alpha component intensity betwen 0f (fully transparent) and 1f (fully opaque). </param>
		/// <example>
		/// <code>
		/// [BackgroundColor(0f, 1f, 0f, 1f)]
		/// public class ComponentWithGreenBackground : MonoBehaviour { }
		/// </code>
		/// </example>
		public BackgroundColorAttribute(float r, float g, float b, float a) : base()
		{
			this.r = r;
			this.g = g;
			this.b = b;
			this.a = a;
		}

		/// <inheritdoc />
		public object[] GetSetupParameters()
		{
			parameterWrapper[0] = color;
			return parameterWrapper;
		}

		/// <inheritdoc/>
		public Type GetDrawerType(Type attributeHolderType, Type defaultDrawerTypeForAttributeHolder, IDrawerByNameProvider drawerByNameProvider)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(attributeHolderType != null);
			Debug.Assert(defaultDrawerTypeForAttributeHolder != null);
			Debug.Assert(drawerByNameProvider != null);
			#endif

			#if POWER_INSPECTOR // if power inspector is installed
			#if UNITY_EDITOR
			if(customEditorComponentDrawerInterface == null)
			{
				customEditorComponentDrawerInterface = drawerByNameProvider.GetComponentDrawerTypeByName("ICustomEditorComponentDrawer");
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(customEditorComponentDrawerInterface != null);
				#endif
			}

			if(customEditorComponentDrawerInterface.IsAssignableFrom(defaultDrawerTypeForAttributeHolder))
			{
				if(coloredCustomEditorComponentDrawer == null)
				{
					coloredCustomEditorComponentDrawer = drawerByNameProvider.GetComponentDrawerTypeByName("ColoredCustomEditorComponentDrawer");
				}
				return coloredCustomEditorComponentDrawer;
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(drawerByNameProvider.GetComponentDrawerTypeByName("IEditorlessComponentDrawer").IsAssignableFrom(defaultDrawerTypeForAttributeHolder), "IEditorlessComponentDrawer not assignable from "+defaultDrawerTypeForAttributeHolder.Name);
			#endif

			if(coloredComponentDrawer == null)
			{
				coloredComponentDrawer = drawerByNameProvider.GetComponentDrawerTypeByName("ColoredComponentDrawer");
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(coloredComponentDrawer != null);
				#endif
			}

			return coloredComponentDrawer;
			#else  // if power inspector is not installed
			throw new NotSupportedException("BackgroundColorAttribute.GetDrawerType is not supported because Power Inspector is not installed.");
			#endif
		}
	}
}