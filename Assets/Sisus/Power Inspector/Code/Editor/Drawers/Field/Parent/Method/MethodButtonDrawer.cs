//#define DEBUG_SETUP

using JetBrains.Annotations;
using Sisus.Attributes;
using System;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Draws a method as a customizable button.
	/// 
	/// This can be used instead of the default MethodDrawer when
	/// </summary>
	[Serializable, DrawerForAttribute(true, typeof(ButtonAttribute), null)]
	public class MethodButtonDrawer : MethodDrawer
	{
		private GUIStyle guiStyle;

		/// <inheritdoc/>
		protected override GUIStyle Style
		{
			get
			{
				return guiStyle;
			}
		}

		/// <inheritdoc/>
		public override Rect ClickToSelectArea
		{
			get
			{
				return lastDrawPosition;
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetAttributeUrl("button");
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="methodInfo"> LinkedMemberInfo of the method that the drawers represent. </param>
		/// <param name="parent"> The parent drawers of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		[NotNull]
		public static new MethodButtonDrawer Create([NotNull]LinkedMemberInfo methodInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			#if GENERIC_METHODS_NOT_SUPPORTED
			if(fieldInfo.MethodInfo.IsGenericMethod)
			{
				return null;
			}
			#endif
		
			MethodButtonDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MethodButtonDrawer();
			}
			result.Setup(null, DrawerUtility.GetType<object>(methodInfo, null), methodInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var button = setMemberInfo.GetAttribute<ButtonAttribute>();
			string setButtonText;
			GUIStyle setGUIStyle;
			if(button != null)
			{
				if(!string.IsNullOrEmpty(button.buttonText))
				{
					setButtonText = button.buttonText;
				}
				else
				{
					if(setLabel != null)
					{
						setButtonText = setLabel.text;
					}
					else
					{
						setButtonText = setMemberInfo.DisplayName;
					}
				}

				GUIContentPool.Replace(ref setLabel, button.prefixLabelText);

				if(!string.IsNullOrEmpty(button.guiStyle))
				{
					setGUIStyle = InspectorUtility.Preferences.GetStyle(button.guiStyle);
					if(setGUIStyle == null)
					{
						setGUIStyle = InspectorPreferences.Styles.Button;
					}
				}
				else
				{
					setGUIStyle = InspectorPreferences.Styles.Button;
				}
			}
			else
			{
				setGUIStyle = InspectorPreferences.Styles.Button;

				if(setLabel != null)
				{
					setButtonText = setLabel.text;
				}
				else
				{
					setButtonText = setMemberInfo.DisplayName;
				}

				GUIContentPool.Replace(ref setLabel, "");
			}

			Setup(setMemberInfo, setParent, setLabel, GUIContentPool.Create(setButtonText), setGUIStyle, setReadOnly);
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setPrefixLabel"> The prefix label to precede the button. Can be null. </param>
		/// <param name="setButtonLabel"> Label to show on the button. </param>
		/// <param name="setGUIStyle"> GUIStyle to use when drawing the button. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		protected virtual void Setup(LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setPrefixLabel, GUIContent setButtonLabel, GUIStyle setGUIStyle, bool setReadOnly)
		{
			#if DEV_MODE && DEBUG_SETUP
			Debug.Log("MethodButton with setPrefixLabel="+StringUtils.ToString(setPrefixLabel)+", setButtonLabel="+StringUtils.ToString(setButtonLabel));
			#endif

			guiStyle = setGUIStyle;

			base.Setup(setMemberInfo, setParent, setPrefixLabel, setButtonLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected sealed override void Setup(LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setPrefixLabel, GUIContent setButtonLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			if(label.text.Length == 0)
			{
				backgroundRect.width = 0f;

				if(!DrawInSingleRow)
				{
					const float spaceForFoldout = foldoutArrowSize + DrawGUI.RightPadding;
					buttonRect.x = labelLastDrawPosition.x + spaceForFoldout;
					buttonRect.width = lastDrawPosition.width - spaceForFoldout - (labelLastDrawPosition.x - lastDrawPosition.x) - DrawGUI.RightPadding;
					labelLastDrawPosition.width = spaceForFoldout;
				}
				else
				{
					buttonRect.x = labelLastDrawPosition.x;
					buttonRect.width = lastDrawPosition.width - (labelLastDrawPosition.x - lastDrawPosition.x) - DrawGUI.RightPadding;
					labelLastDrawPosition = buttonRect;
				}
			}
		}
	}
}