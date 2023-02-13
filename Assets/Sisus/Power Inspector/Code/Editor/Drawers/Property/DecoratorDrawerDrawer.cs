//#define ENABLE_INDENT_FIX_HACK
#define ALWAYS_USE_WIDEMODE
//#define DRAW_LABEL_RESIZE_CONTROL
#define SAFE_MODE
#define DEBUG_NULL_PROPERTY

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing a property attribute with a DectoratorDrawer.
	/// </summary>
	[Serializable]
	public sealed class DecoratorDrawerDrawer : BaseDrawer, IDecoratorDrawerDrawer
	{
		private DecoratorDrawer decoratorDrawer;
		private bool targetDrawerShownInInspector = true;

		/// <inheritdoc/>
		public bool RequiresDecoratorDrawerType
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc cref="IDrawer.ReadOnly" />
		public override bool ReadOnly
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc cref="IDrawer.Type" />
		public override Type Type
		{
			get
			{
				return decoratorDrawer.GetType();
			}
		}

		/// <inheritdoc cref="IDrawer.PrefixResizingEnabledOverControl" />
		public override bool PrefixResizingEnabledOverControl
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				#if ALWAYS_USE_WIDEMODE
				bool wideModeWas = EditorGUIUtility.wideMode;
				EditorGUIUtility.wideMode = true;
				#endif

				float height = decoratorDrawer.GetHeight();
				if(height <= 0f)
				{
					height = 16f;
				}

				#if ALWAYS_USE_WIDEMODE
				EditorGUIUtility.wideMode = wideModeWas;
				#endif

				return height + 2f;
			}
		}

		/// <inheritdoc/>
		public override bool ShouldShowInInspector
		{
			get
			{
				return base.ShouldShowInInspector && targetDrawerShownInInspector;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="attribute"> The PropertyAttribute that these drawer represent and whose DectoratorDrawer is used when drawing the control. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="drawerType"> Type of the DecoratorDrawer that is used for drawing the control. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DecoratorDrawerDrawer Create([NotNull]PropertyAttribute attribute, [NotNull]Type drawerType, [CanBeNull]IParentDrawer parent)
		{
			DecoratorDrawerDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DecoratorDrawerDrawer();
			}
			result.Setup(attribute, drawerType, null, parent);
			result.LateSetup();
			return result;
		}
		
		public static IDrawer GetTargetClassMemberDrawer([NotNull]IDrawer decoratorDrawerDrawer)
		{
			var parent = decoratorDrawerDrawer.Parent;

			if(parent == null)
			{
				#if DEV_MODE
				Debug.LogWarning(decoratorDrawerDrawer.ToString()+" parent was null.");
				#endif
				return null;
			}

			var members = parent.Members;

			int thisIndex = Array.IndexOf(members, decoratorDrawerDrawer);

			if(thisIndex == -1)
			{
				#if DEV_MODE
				Debug.LogWarning(decoratorDrawerDrawer.ToString() + " not found among "+ members.Length + " members of parent "+ parent + ": "+StringUtils.TypesToString(members));
				#endif
				return null;
			}

			for(int n = thisIndex + 1, count = members.Length; n < count; n++)
			{
				var member = members[n];
				if(!(member is IDecoratorDrawerDrawer))
				{
					return member;
				}
			}
			return null;
		}

		/// <inheritdoc/>
		public override void OnSiblingValueChanged(int memberIndex, object memberValue, [CanBeNull] LinkedMemberInfo memberLinkedMemberInfo)
		{
			UpdateTargetDrawerShowInInspector();
			base.OnSiblingValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
		}

		private void UpdateTargetDrawerShowInInspector()
		{
			var targetedDrawer = GetTargetClassMemberDrawer(this);
			if(targetedDrawer != null)
			{
				bool set = targetedDrawer.ShouldShowInInspector;
				if(set != targetDrawerShownInInspector)
				{
					targetDrawerShownInInspector = set;
					Inspector.InspectorDrawer.Repaint();
				}
			}
			else
			{
				targetDrawerShownInInspector = true;
			}
		}

		/// <inheritdoc/>
		protected override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		/// <inheritdoc />
		public void SetupInterface(PropertyAttribute propertyAttribute, Type decoratorDrawerType, IParentDrawer setParent, LinkedMemberInfo attributeTarget)
		{
			if(decoratorDrawerType == null)
			{
				throw new NotSupportedException(GetType().Name + " requires a decoratorDrawerType to be provided.");
			}
			Setup(propertyAttribute, decoratorDrawerType, null, setParent);
		}

		private void Setup(PropertyAttribute attribute, [NotNull]Type decoratorDrawerType, [CanBeNull]DecoratorDrawer decoratorDrawerInstance, [CanBeNull]IParentDrawer setParent)
		{
			if(decoratorDrawerInstance == null)
			{
				decoratorDrawerInstance = decoratorDrawerType.CreateInstance() as DecoratorDrawer;
			}

			decoratorDrawer = decoratorDrawerInstance;
			
			var attField = decoratorDrawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
			if(attField == null)
			{
				Debug.LogError("Failed to get field \"m_Attribute\" from DecoratorDrawer of type "+StringUtils.ToString(decoratorDrawerType));
			}
			else
			{
				attField.SetValue(decoratorDrawer, attribute);
			}
			
			base.Setup(setParent, GUIContent.none);
		}

		/// <inheritdoc/>
		protected override bool ShouldConstantlyUpdateCachedValues()
		{
			return false;
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			var positionWithoutMargins = position;

			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			
			#if !DRAW_LABEL_RESIZE_CONTROL
			DrawGUI.Active.ColorRect(position, DrawGUI.Active.InspectorBackgroundColor);
			#endif
			
			position.height -= 2f;
			position.y += 1f;
			position.width -= DrawGUI.RightPadding;
			
			float labelWidthWas = EditorGUIUtility.labelWidth;
			float fieldWidthWas = EditorGUIUtility.fieldWidth;

			float leftPadding = DrawGUI.LeftPadding;
			int labelRightPadding = (int)(DrawGUI.MiddlePadding + DrawGUI.MiddlePadding);
			
			position.x += leftPadding;
			position.width -= leftPadding;
			
			//always use wide mode for properties because it works better with the prefix width control
			#if ALWAYS_USE_WIDEMODE
			bool wideModeWas = EditorGUIUtility.wideMode;
			EditorGUIUtility.wideMode = true;
			#endif
			
			EditorStyles.label.padding.right = labelRightPadding;

			GUILayout.BeginArea(positionWithoutMargins);
			{
				position.y = position.y - positionWithoutMargins.y;
				position.x = position.x - positionWithoutMargins.x;

				try
				{
					decoratorDrawer.OnGUI(position);
				}
				catch(Exception e)
				{
					if(ExitGUIUtility.ShouldRethrowException(e))
					{
						throw;
					}
					#if DEV_MODE
					Debug.LogWarning(ToString()+" "+e);
					#endif
				}
			}
			GUILayout.EndArea();

			#if ALWAYS_USE_WIDEMODE
			EditorGUIUtility.wideMode = wideModeWas;
			#endif

			EditorStyles.label.padding.right = 2;
			EditorGUIUtility.labelWidth = labelWidthWas;
			EditorGUIUtility.fieldWidth = fieldWidthWas;

			return false;
		}
		
		/// <summary>
		/// fill out lastDrawPosition for Draw
		/// </summary>
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = Height;

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			targetDrawerShownInInspector = true;
			base.Dispose();
		}
	}
}