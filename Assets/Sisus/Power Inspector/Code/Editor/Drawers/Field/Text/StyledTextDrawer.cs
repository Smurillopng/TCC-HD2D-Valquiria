using JetBrains.Annotations;
using Sisus.Attributes;
using System;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForAttribute(true, typeof(StyleAttribute), null)]
	public class StyledTextDrawer : TextDrawer
	{
		public GUIStyle guiStyle;

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(string);
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="guiStyle"> The starting cached value of the drawer. </param>
		/// <param name="text"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static StyledTextDrawer Create([NotNull]GUIStyle guiStyle, string text, [CanBeNull]IParentDrawer parent, LinkedMemberInfo memberInfo = null, GUIContent label = null, bool readOnly = false)
		{
			StyledTextDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new StyledTextDrawer();
			}
			result.Setup(guiStyle, text, typeof(string), memberInfo, parent, label, readOnly, memberInfo != null && memberInfo.GetAttribute<TextAreaAttribute>() != null, memberInfo != null && memberInfo.GetAttribute<DelayedAttribute>() != null);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object attribute, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var parametersProvider = attribute as IDrawerSetupDataProvider;
			if(parametersProvider == null)
			{
				parametersProvider = setMemberInfo.GetAttribute<IDrawerSetupDataProvider>();

				#if DEV_MODE
				if(parametersProvider == null)
				{
					throw new NullReferenceException("StyledTextDrawer.Setup MemberInfo did not contain attribute that implements IDrawerSetupDataProvider");
				}
				Debug.LogWarning("StyledTextDrawer.SetupInterface - provided attribute " + (attribute == null ? "null" : attribute.GetType().Name) + " did not implement IDrawerSetupDataProvider, but fetched it manually from LinkedMemberInfo.");
				#endif
			}
			var parameters = parametersProvider.GetSetupParameters();
			var setGuiStyle = Inspector.Preferences.GetStyle((string)parameters[0]);

			string text = setValue as string;
			if(text == null)
			{
				text = StringUtils.ToString(setValue);
			}

			Setup(setGuiStyle, text, typeof(string), setMemberInfo, setParent, setLabel, setReadOnly, setMemberInfo != null && setMemberInfo.GetAttribute<TextAreaAttribute>() != null, setMemberInfo != null && setMemberInfo.GetAttribute<DelayedAttribute>() != null);
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var parametersProvider = setMemberInfo.GetAttribute<IDrawerSetupDataProvider>();
			var parameters = parametersProvider.GetSetupParameters();
			var setGuiStyle = Inspector.Preferences.GetStyle((string)parameters[0]);
		
			string text = setValue as string;
			if(text == null)
			{
				text = StringUtils.ToString(setValue);
			}
			Setup(setGuiStyle, text, setValueType, setMemberInfo, setParent, setLabel, setReadOnly, setMemberInfo != null && setMemberInfo.GetAttribute<TextAreaAttribute>() != null, setMemberInfo != null && setMemberInfo.GetAttribute<DelayedAttribute>() != null);
		}

		/// <inheritdoc />
		protected sealed override void Setup(string setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly, bool setTextArea, bool setDelayed)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		protected virtual void Setup(GUIStyle setGuiStyle, string setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly, bool setTextArea, bool setDelayed)
		{
			#if DEV_MODE
			Debug.Log("setGuiStyle="+ setGuiStyle.name+ ", setValue= "+StringUtils.ToString(setValue)+", setLabel="+StringUtils.ToString(setLabel));
			#endif

			guiStyle = setGuiStyle;
			base.Setup(setValue, setMemberInfo, setParent, setLabel, setReadOnly, setTextArea, setDelayed);
		}

		/// <inheritdoc />
		public override string DrawControlVisuals(Rect position, string inputValue)
		{
			if(inputValue == null)
			{
				if(GUI.Button(position, "null", guiStyle))
				{
					return "";
				}
				return null;
			}

			if(textArea)
			{
				return DrawGUI.Active.TextArea(position, inputValue, guiStyle);
			}
			return DrawGUI.Active.TextField(position, inputValue, guiStyle);
		}
	}
}