#define DEBUG_NEXT_FIELD

using System;
using System.Reflection;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(GUIStyle), false, true)]
	public class GUIStyleDrawer : ParentFieldDrawer<GUIStyle>
	{
		private static bool fieldInfosGenerated;
		private static PropertyInfo name;
		private static PropertyInfo normal;
		private static PropertyInfo hover;
		private static PropertyInfo active;
		private static PropertyInfo focused;
		private static PropertyInfo onNormal;
		private static PropertyInfo onHover;
		private static PropertyInfo onActive;
		private static PropertyInfo onFocused;
		private static PropertyInfo border;
		private static PropertyInfo margin;
		private static PropertyInfo padding;
		private static PropertyInfo overflow;
		private static PropertyInfo font;
		private static PropertyInfo fontSize;
		private static PropertyInfo fontStyle;
		private static PropertyInfo alignment;
		private static PropertyInfo wordWrap;
		private static PropertyInfo richText;
		private static PropertyInfo clipping;
		private static PropertyInfo imagePosition;
		private static PropertyInfo contentOffset;
		private static PropertyInfo fixedWidth;
		private static PropertyInfo fixedHeight;
		private static PropertyInfo stretchWidth;
		private static PropertyInfo stretchHeight;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get { return false; }
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static GUIStyleDrawer Create(GUIStyle value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			GUIStyleDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GUIStyleDrawer();
			}
			result.Setup(value, typeof(GUIStyle), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((GUIStyle)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			if(!fieldInfosGenerated)
			{
				fieldInfosGenerated = true;
				name = Types.GUIStyle.GetProperty("name"); //NOTE: This didn't have a PropertyInfo earlier for some reason. did it cause problems?
				normal = Types.GUIStyle.GetProperty("normal");
				hover = Types.GUIStyle.GetProperty("hover");
				active = Types.GUIStyle.GetProperty("active");
				focused = Types.GUIStyle.GetProperty("focused");
				onNormal = Types.GUIStyle.GetProperty("onNormal");
				onHover = Types.GUIStyle.GetProperty("onHover");
				onActive = Types.GUIStyle.GetProperty("onActive");
				onFocused = Types.GUIStyle.GetProperty("onFocused");
				border = Types.GUIStyle.GetProperty("border");
				margin = Types.GUIStyle.GetProperty("margin");
				padding = Types.GUIStyle.GetProperty("padding");
				overflow = Types.GUIStyle.GetProperty("overflow");
				font = Types.GUIStyle.GetProperty("font");
				fontSize = Types.GUIStyle.GetProperty("fontSize");
				fontStyle = Types.GUIStyle.GetProperty("fontStyle");
				alignment = Types.GUIStyle.GetProperty("alignment");
				wordWrap = Types.GUIStyle.GetProperty("wordWrap");
				richText = Types.GUIStyle.GetProperty("richText");
				clipping = Types.GUIStyle.GetProperty("clipping");
				imagePosition = Types.GUIStyle.GetProperty("imagePosition");
				contentOffset = Types.GUIStyle.GetProperty("contentOffset");
				fixedWidth = Types.GUIStyle.GetProperty("fixedWidth");
				fixedHeight = Types.GUIStyle.GetProperty("fixedHeight");
				stretchWidth = Types.GUIStyle.GetProperty("stretchWidth");
				stretchHeight = Types.GUIStyle.GetProperty("stretchHeight");
			}

			if(memberInfo == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString() + " memberInfo was null.");
				#endif

				for(int n = 0; n < 26; n++)
				{
					memberBuildList.Add(null);
				}
				return;
			}

			var hierarchy = MemberHierarchy;
			if(hierarchy.Target == null)
			{
				memberBuildList.Add(hierarchy.Get(memberInfo, name));
				memberBuildList.Add(hierarchy.Get(memberInfo, normal));
				memberBuildList.Add(hierarchy.Get(memberInfo, hover));
				memberBuildList.Add(hierarchy.Get(memberInfo, active));
				memberBuildList.Add(hierarchy.Get(memberInfo, focused));
				memberBuildList.Add(hierarchy.Get(memberInfo, onNormal));
				memberBuildList.Add(hierarchy.Get(memberInfo, onHover));
				memberBuildList.Add(hierarchy.Get(memberInfo, onActive));
				memberBuildList.Add(hierarchy.Get(memberInfo, onFocused));
				memberBuildList.Add(hierarchy.Get(memberInfo, border));
				memberBuildList.Add(hierarchy.Get(memberInfo, margin));
				memberBuildList.Add(hierarchy.Get(memberInfo, padding));
				memberBuildList.Add(hierarchy.Get(memberInfo, overflow));
				memberBuildList.Add(hierarchy.Get(memberInfo, font));
				memberBuildList.Add(hierarchy.Get(memberInfo, fontSize));
				memberBuildList.Add(hierarchy.Get(memberInfo, fontStyle));
				memberBuildList.Add(hierarchy.Get(memberInfo, alignment));
				memberBuildList.Add(hierarchy.Get(memberInfo, wordWrap));
				memberBuildList.Add(hierarchy.Get(memberInfo, richText));
				memberBuildList.Add(hierarchy.Get(memberInfo, clipping));
			
				memberBuildList.Add(hierarchy.Get(memberInfo, imagePosition));
				memberBuildList.Add(hierarchy.Get(memberInfo, contentOffset));
				memberBuildList.Add(hierarchy.Get(memberInfo, fixedWidth));
				memberBuildList.Add(hierarchy.Get(memberInfo, fixedHeight));
				memberBuildList.Add(hierarchy.Get(memberInfo, stretchWidth));
				memberBuildList.Add(hierarchy.Get(memberInfo, stretchHeight));
			}
			else
			{
				memberBuildList.Add(hierarchy.Get(memberInfo, name));
				memberBuildList.Add(hierarchy.Get(memberInfo, normal, "m_Normal"));
				memberBuildList.Add(hierarchy.Get(memberInfo, hover, "m_Hover"));
				memberBuildList.Add(hierarchy.Get(memberInfo, active, "m_Active"));
				memberBuildList.Add(hierarchy.Get(memberInfo, focused, "m_Focused"));
				memberBuildList.Add(hierarchy.Get(memberInfo, onNormal, "m_OnNormal"));
				memberBuildList.Add(hierarchy.Get(memberInfo, onHover, "m_OnHover"));
				memberBuildList.Add(hierarchy.Get(memberInfo, onActive, "m_OnActive"));
				memberBuildList.Add(hierarchy.Get(memberInfo, onFocused, "m_OnFocused"));
				memberBuildList.Add(hierarchy.Get(memberInfo, border, "m_Border"));
				memberBuildList.Add(hierarchy.Get(memberInfo, margin, "m_Margin"));
				memberBuildList.Add(hierarchy.Get(memberInfo, padding, "m_Padding"));
				memberBuildList.Add(hierarchy.Get(memberInfo, overflow, "m_Overflow"));
				memberBuildList.Add(hierarchy.Get(memberInfo, font, "m_Font"));
				memberBuildList.Add(hierarchy.Get(memberInfo, fontSize, "m_FontSize"));
				memberBuildList.Add(hierarchy.Get(memberInfo, fontStyle, "m_FontStyle"));
				memberBuildList.Add(hierarchy.Get(memberInfo, alignment, "m_Alignment"));
				memberBuildList.Add(hierarchy.Get(memberInfo, wordWrap, "m_WordWrap"));
				memberBuildList.Add(hierarchy.Get(memberInfo, richText, "m_RichText"));
				memberBuildList.Add(hierarchy.Get(memberInfo, clipping/*, "m_Clipping"*/));
			
				memberBuildList.Add(hierarchy.Get(memberInfo, imagePosition, "m_ImagePosition"));
				memberBuildList.Add(hierarchy.Get(memberInfo, contentOffset, "m_ContentOffset"));
				memberBuildList.Add(hierarchy.Get(memberInfo, fixedWidth, "m_FixedWidth"));
				memberBuildList.Add(hierarchy.Get(memberInfo, fixedHeight, "m_FixedHeight"));
				memberBuildList.Add(hierarchy.Get(memberInfo, stretchWidth, "m_StretchWidth"));
				memberBuildList.Add(hierarchy.Get(memberInfo, stretchHeight, "m_StretchHeight"));
			}

			#if DEV_MODE
			Debug.Assert(memberBuildList.Count == 26);
			#endif
		}

		protected override void DoBuildMembers()
		{
			Array.Resize(ref members, 26);

			var first = Value;

			bool readOnly = ReadOnly;

			//NOTE: This didn't have a LinkedMemberInfo earlier for some reason. Did it cause problems?
			members[0] = TextDrawer.Create(first.name, memberBuildList[0], this, GUIContentPool.Create("Name"), readOnly, false);

			members[1] = GUIStyleStateDrawer.Create(first.normal, memberBuildList[1], this, GUIContentPool.Create("Normal"), readOnly);
			members[2] = GUIStyleStateDrawer.Create(first.hover, memberBuildList[2], this, GUIContentPool.Create("Hover"), readOnly);
			members[3] = GUIStyleStateDrawer.Create(first.active, memberBuildList[3], this, GUIContentPool.Create("Active"), readOnly);
			members[4] = GUIStyleStateDrawer.Create(first.focused, memberBuildList[4], this, GUIContentPool.Create("Focused"), readOnly);

			members[5] = GUIStyleStateDrawer.Create(first.onNormal, memberBuildList[5], this, GUIContentPool.Create("On Normal"), readOnly);
			members[6] = GUIStyleStateDrawer.Create(first.onHover, memberBuildList[6], this, GUIContentPool.Create("On Hover"), readOnly);
			members[7] = GUIStyleStateDrawer.Create(first.onActive, memberBuildList[7], this, GUIContentPool.Create("On Active"), readOnly);
			members[8] = GUIStyleStateDrawer.Create(first.onFocused, memberBuildList[8], this, GUIContentPool.Create("On Focused"), readOnly);
			
			members[9] = RectOffsetDrawer.Create(first.border, memberBuildList[9], this, GUIContentPool.Create("Border"), readOnly);
			members[10] = RectOffsetDrawer.Create(first.margin, memberBuildList[10], this, GUIContentPool.Create("Margin"), readOnly);
			members[11] = RectOffsetDrawer.Create(first.padding, memberBuildList[11], this, GUIContentPool.Create("Padding"), readOnly);
			members[12] = RectOffsetDrawer.Create(first.overflow, memberBuildList[12], this, GUIContentPool.Create("Overflow"), readOnly);

			members[13] = ObjectReferenceDrawer.Create(first.font, memberBuildList[13], this, GUIContentPool.Create("Font"), true, false, readOnly);
			members[14] = IntDrawer.Create(first.fontSize, memberBuildList[14], this, GUIContentPool.Create("Font Size"), readOnly);
			members[15] = EnumDrawer.Create(first.fontStyle, memberBuildList[15], this, GUIContentPool.Create("Font Style"), readOnly);

			members[16] = EnumDrawer.Create(first.alignment, memberBuildList[16], this, GUIContentPool.Create("Alignment"), readOnly);
			members[17] = ToggleDrawer.Create(first.wordWrap, memberBuildList[17], this, GUIContentPool.Create("Word Wrap"), readOnly);
			members[18] = ToggleDrawer.Create(first.richText, memberBuildList[18], this, GUIContentPool.Create("Rich Text"), readOnly);
			members[19] = EnumDrawer.Create(first.clipping, memberBuildList[19], this, GUIContentPool.Create("Text Clipping"), readOnly);

			members[20] = EnumDrawer.Create(first.imagePosition, memberBuildList[20], this, GUIContentPool.Create("Image Position"), readOnly);
			members[21] = Vector2Drawer.Create(first.contentOffset, memberBuildList[21], this, GUIContentPool.Create("Content Offset"), readOnly);
			members[22] = FloatDrawer.Create(first.fixedWidth, memberBuildList[22], this, GUIContentPool.Create("Fixed Width"), readOnly);
			members[23] = FloatDrawer.Create(first.fixedHeight, memberBuildList[23], this, GUIContentPool.Create("Fixed Height"), readOnly);
			members[24] = ToggleDrawer.Create(first.stretchWidth, memberBuildList[24], this, GUIContentPool.Create("Strech Width"), readOnly);
			members[25] = ToggleDrawer.Create(first.stretchHeight, memberBuildList[25], this, GUIContentPool.Create("Strech Height"), readOnly);

			#if DEV_MODE
			Debug.Assert(memberBuildList.Count == members.Length);
			#endif
		}

		/// <inheritdoc/>
		public override object DefaultValue(bool _)
		{
			return new GUIStyle();
		}
		
		/// <inheritdoc/>
		protected override GUIStyle GetCopyOfValue(GUIStyle source)
		{
			return source == null ? null : new GUIStyle(source);
		}
	}
}