#define SAFE_MODE
#define ENABLE_UNFOLD_ANIMATIONS
#define DRAW_MEMBERS_INLINED

#define DEBUG_KEYBOARD_INPUT
//#define DEBUG_INVOKE
//#define DEBUG_UPDATE_DRAW_IN_SINGLE_ROW
//#define DEBUG_DRAW_IN_SINGLE_ROW
//#define DEBUG_UPDATE_VISIBLE_MEMBERS
//#define DEBUG_SETUP
//#define DEBUG_GENERATE_MEMBER_BUILD_LIST
//#define GENERIC_METHODS_NOT_SUPPORTED
#define DEBUG_BUILD_MEMBERS

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// Represents Drawer that can be assigned multiple different classes of different forms.
	/// Examples include abstract classes, interfaces, System.Object and Nullable fields.
	/// Allows selecting value from popup menu dynamically popuplated with possible values.
	/// If any UnityEngine.Object classes values are possible, then an object reference field can
	/// also be displayed.
	/// </summary>
	[Serializable]
	public abstract class PolymorphicDrawer : ParentFieldDrawer<object>, IDraggablePrefixAffectsMember
	{
		private const float NullButtonWidth = DrawGUI.SingleLineHeight;

		private static List<PopupMenuItem> generatedMenuItems = new List<PopupMenuItem>();
		private static Dictionary<string, PopupMenuItem> generatedGroupsByLabel = new Dictionary<string, PopupMenuItem>();
		private static Dictionary<string, PopupMenuItem> generatedItemsByLabel = new Dictionary<string, PopupMenuItem>();
		private static bool menuItemsGenerated;
		private static Type menuItemsGeneratedForType;

		private bool canBeNonUnityObject;
		private bool drawInSingleRow;
		private bool drawToggleNullButton;
		private Type userSelectedType;
		private Type instanceType;
		private Rect valueFieldPosition;
		private Rect toggleNullButtonPosition;
		#if DRAW_MEMBERS_INLINED
		private bool? canDrawMembersInlined;
		#endif

		/// <inheritdoc/>
		public bool DraggingPrefix
		{
			get
			{
				if(InspectorUtility.ActiveManager.MouseDownInfo.MouseDownOverDrawer == this)
				{
					var draggableMember = ValueDrawer as IDraggablePrefix;
					if(draggableMember != null && draggableMember.ShouldShowInInspector)
					{
						return true;
					}
				}
				return false;
			}
		}

		public bool DrawMembersInlined
		{
			get
			{
				#if DRAW_MEMBERS_INLINED
				return canDrawMembersInlined.HasValue ? canDrawMembersInlined.Value : true;
				#else
				return false;
				#endif
			}
		}

		/// <inheritdoc cref="IDrawer.Type" />
		public sealed override Type Type
		{
			get
			{
				var typeForValue = TypeForValue;
				return typeForValue != null ? typeForValue : base.Type;
			}
		}

		/// <summary>
		/// Returns interface or base class type which is assigned to the field.
		/// </summary>
		/// <returns></returns>
		protected override Type GetAssignableType()
		{
			if(memberInfo != null)
			{
				return memberInfo.Type;
			}
			return base.Type;
		}

		/// <summary>
		/// Type of the current value that the drwaer has. If value is null, this is also null.
		/// </summary>
		[CanBeNull]
		protected Type InstanceType
		{
			get
			{
				return instanceType;
			}
		}

		/// <summary>
		/// Specific type that user explicitly selected from popup to be used for the value of this drawer, or the only possible type for values of this field if has only one option.
		/// </summary>
		[CanBeNull]
		protected Type UserSelectedType
		{
			get
			{
				return userSelectedType;
			}
		}

		/// <summary>
		/// Type of current value or specific type that user explicitly selected from popup to be used for the value of this drawer,
		/// or the only possible type for values of this field if has only one option.
		/// Otherwise null.
		/// </summary>
		[CanBeNull]
		protected Type TypeForValue
		{
			get
			{
				if(instanceType != null)
				{
					return instanceType;
				}
				if(userSelectedType != null)
				{
					return userSelectedType;
				}
				return null;
			}
		}

		/// <inheritdoc />
		public sealed override bool MembersAreVisible
		{
			get
			{
				return base.MembersAreVisible || DrawInSingleRow || Value == null || !DrawMembersInlined;
			}
		}

		/// <inheritdoc />
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				if(DrawMembersInlined && Value != null)
				{
					return base.Height;
				}

				if(Foldable && Unfolded && DrawToggleNullButton && visibleMembers.Length >= 2 && members[0] == visibleMembers[0])
				{
					return base.Height - DrawGUI.SingleLineHeight;
				}
				return base.Height;
			}
		}

		/// <inheritdoc/>
		protected override bool CanBeNull
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		protected override bool PrefixLabelClippedToColumnWidth
		{
			get
			{
				return base.PrefixLabelClippedToColumnWidth || DrawToggleNullButton;
			}
		}

		[CanBeNull]
		protected ObjectReferenceDrawer ObjectDrawer
		{
			get
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					return null;
				}
				if(!CanBeUnityObject)
				{
					return null;
				}
				return members[members.Length - 1] as ObjectReferenceDrawer;
			}
		}

		/// <summary>
		/// Can the value of this field by a UnityEngine.Object reference?
		/// </summary>
		/// <value>
		/// True if value can be UnityEngine.Object, false if not
		/// </value>
		protected abstract bool CanBeUnityObject
		{
			get;
		}

		/// <summary>
		/// Gets a list of types of the non unity objects that implement / inherit from the base type
		/// </summary>
		/// <value>
		/// A list of types that are not of the type UnityEngine.Object
		/// </value>
		protected abstract IEnumerable<Type> NonUnityObjectTypes
		{
			get;
		}

		/// <inheritdoc />
		protected sealed override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return false; // only rebuilding when specifically instructed
			}
		}

		/// <summary>
		/// Returns true if should currently drwa the the toggle null button.
		/// </summary>
		protected bool DrawToggleNullButton
		{
			get
			{
				return drawToggleNullButton;
			}
		}

		/// <summary>
		/// Returns true if current value is null.
		/// </summary>
		protected bool IsNull
		{
			get
			{
				return Value == null;
			}
		}

		/// <summary>
		/// Returns true if an explicit non-abstract type has been specified for the value of this drawer.
		/// If drawer has a non-null value, or user has selected a specific type from the popup list, then this returns true.
		/// </summary>
		protected bool HasExplicitType
		{
			get
			{
				return TypeForValue != null && !TypeForValue.IsGenericTypeDefinition;
			}
		}

		/// <summary>
		/// Returns member drawer responsible drawing the current value instance.
		/// Null if currently has no drawer for value.
		/// </summary>
		[CanBeNull]
		protected IFieldDrawer ValueDrawer
		{
			get
			{
				if(DrawMembersInlined)
				{
					return null;
				}

				if(IsNull || memberBuildState != MemberBuildState.MembersBuilt)
				{
					return null;
				}

				int index = DrawToggleNullButton ? 1 : 0;
				if(members.Length <= index)
				{
					return null;
				}

				return members[index] as IFieldDrawer;
			}
		}

		protected NullToggleDrawer ToggleNullButton
		{
			get
			{
				return DrawToggleNullButton && memberBuildState == MemberBuildState.MembersBuilt && members.Length > 0 ? members[0] as NullToggleDrawer : null;
			}
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			instanceType = setValue == null ? null : setValue.GetType();
			canBeNonUnityObject = NonUnityObjectTypes.Any();

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			UpdateDrawToggleNullButton();

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log(Msg(ToString(), ".Setup with DrawToggleNullButton=", DrawToggleNullButton, ", IsNull=", IsNull, ", CanBeUnityObject = ", CanBeUnityObject,", NonUnityObjectTypes=", StringUtils.ToString(NonUnityObjectTypes.Length), ", DrawInSingleRow=", DrawInSingleRow, ", Unfolded=", Unfolded));
			#endif
		}

		private void UpdateDrawToggleNullButton()
		{
			if(DrawMembersInlined && Value != null)
			{
				drawToggleNullButton = false;
				return;
			}

			drawToggleNullButton = canBeNonUnityObject && (IsNull || !instanceType.IsUnityObject());
		}
		
		/// <inheritdoc />
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Assert(members[memberIndex].GetType() != typeof(NullToggleDrawer), ToString(), ".OnMemberValueChanged: value of NullToggleDrawer changed somehow.");
			#endif

			if(DrawMembersInlined && Value != null)
			{
				base.OnMemberValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
				return;
			}

			var referenceValue = memberValue as Object;
			if(referenceValue != null)
			{
				if(IsValidUnityObjectValue(referenceValue))
				{
					InspectorUtility.ActiveInspector.OnNextLayout(()=>DoSetValue(referenceValue, false, true));
				}
			}
			else
			{
				// don't apply to field, because should already be applied by the member if can be applied
				DoSetValue(memberValue, false, false);

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(memberLinkedMemberInfo != null || memberInfo == null);
				#endif
			}

			if(parent != null)
			{
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), memberValue, memberLinkedMemberInfo);
			}

			UpdateDataValidity(true);
			HasUnappliedChanges = GetHasUnappliedChangesUpdated();
		}
		
		/// <inheritdoc />
		public override bool DrawBody(Rect position)
		{
			if(DrawMembersInlined && Value != null)
			{
				return ParentDrawerUtility.DrawBody(this, position);
			}

			bool dirty = false;
			IDrawer draw;
			float unfoldedness = Unfoldedness;

			if(DrawToggleNullButton)
			{
				if(unfoldedness > 0f)
				{
					if(CanBeUnityObject || !IsNull)
					{
						if(DrawInSingleRow)
						{
							#if DEV_MODE && PI_ASSERTATIONS
							if(valueFieldPosition.width <= 0f) { Debug.LogError(ToString()+".valueFieldPosition <= 0f: "+valueFieldPosition); }
							#endif

							return ParentDrawerUtility.DrawBodySingleRow(this, toggleNullButtonPosition, valueFieldPosition);
						}

						ParentDrawerUtility.HandleTooltipBeforeControl(label, toggleNullButtonPosition);
						draw = members[0];
						if(draw.ShouldShowInInspector && draw.Draw(toggleNullButtonPosition))
						{
							if(members.Length == 1)
							{
								return true;
							}
							dirty = true;
						}
						
						using(new MemberScaler(position.min, unfoldedness))
						{
							DrawGUI.IndentLevel += 1;
							{
								draw = members[1];

								#if DEV_MODE && PI_ASSERTATIONS
								if(draw.ShouldShowInInspector && valueFieldPosition.width <= 0f) { Debug.LogError(ToString()+".valueFieldPosition <= 0f: "+valueFieldPosition); }
								#endif

								if(draw.ShouldShowInInspector && draw.Draw(valueFieldPosition))
								{
									dirty = true;
								}
							}
							DrawGUI.IndentLevel -= 1;
						}

						return dirty;
					}
				}
				#if DEV_MODE && PI_ASSERTATIONS
				else { if(DrawInSingleRow) { Debug.LogError(Msg(ToString()," - DrawInSingleRow was ", true, " but Unfolded was ", false, "!")); } }
				#endif

				ParentDrawerUtility.HandleTooltipBeforeControl(label, toggleNullButtonPosition);
				draw = members[0];
				return draw.ShouldShowInInspector && draw.Draw(toggleNullButtonPosition);
			}

			ParentDrawerUtility.HandleTooltipBeforeControl(label, toggleNullButtonPosition);
			
			if(unfoldedness > 0f)
			{
				draw = members[0];
				using(new MemberScaler(position.min, unfoldedness))
				{
					DrawGUI.IndentLevel += 1;
					{
						dirty =  draw.ShouldShowInInspector && draw.Draw(valueFieldPosition);
					}
					DrawGUI.IndentLevel -= 1;
				}
			}
			return dirty;
		}
		
		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			UpdateDrawToggleNullButton();

			var instanceTypeWas = instanceType;

			base.OnCachedValueChanged(applyToField, false);

			var cachedValue = Value;
			instanceType = cachedValue == null ? null : cachedValue.GetType();
			userSelectedType = instanceType;

			#if DEV_MODE
			Debug.Log(Msg(ToString() + ".OnCachedValueChanged with instanceTypeWas=", instanceTypeWas, ", instanceType=", instanceType, ", ObjectDrawer=", ObjectDrawer, ", GetTypeForObjectReferenceField=", GetTypeForObjectReferenceField(), ", Value=", cachedValue));
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Assert(!RebuildDrawersIfValueChanged);
			#endif
			
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnCachedValueChanged(applyToField=", applyToField, ", updateMembers=", updateMembers, ") typeWas=", instanceTypeWas, ", instanceType=", instanceType));
			#endif

			bool rebuildMembers;
			if(instanceTypeWas != instanceType)
			{
				rebuildMembers = true;
			}
			else
			{
				var objectDrawer = ObjectDrawer;
				rebuildMembers = objectDrawer != null && objectDrawer.Type != GetTypeForObjectReferenceField();
			}

			if(rebuildMembers)
			{
				var focusedControl = InspectorUtility.ActiveManager.FocusedDrawer;
				var selectedIndexPath = focusedControl == null ? null : focusedControl.GenerateMemberIndexPath(this);

				RebuildMemberBuildListAndMembers();

				// todo: fix bug where this is not null even though no member of this drawer should be selected.
				if(selectedIndexPath != null)
				{
					SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.Initialization);
				}
			}
			else if(updateMembers)
			{
				base.OnCachedValueChanged(applyToField, true);
			}
		}

		/// <summary>
		/// Query if 'test' is valid UnityEngine.Object value for the field
		/// </summary>
		/// <param name="test">
		/// UnityEngine.Object to test. This cannot be null. </param>
		/// <returns>
		/// True if valid value, false if not.
		/// </returns>
		protected abstract bool IsValidUnityObjectValue([NotNull]Object test);

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			#if SAFE_MODE
			// Temp fix for strange issue with LinkedMemberInfo.MemberData being null after interface or object field value is set to be null.
			// UPDATE: This should be fixed now, but leaving this check here just to be sure.
			if(memberInfo != null && memberInfo.Data == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+" memberInfo.Data was null! Calling parent.RebuildMemberBuildListAndMembers()");
				#endif

				parent.RebuildMemberBuildListAndMembers();
				ExitGUIUtility.ExitGUI();
			}
			#endif

			#if DEV_MODE && DEBUG_GENERATE_MEMBER_BUILD_LIST
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".DoGenerateMemberBuildList called with IsNull=", IsNull, ", memberInfo=", memberInfo, ", HasExplicitType=", HasExplicitType, ", TypeForValue=", TypeForValue, ", InstanceType=", InstanceType, ", UserSelectedType=", UserSelectedType));
			#endif

			if(HasExplicitType)
			{
				#if DRAW_MEMBERS_INLINED
				var typeForValue = TypeForValue;
				var drawerType = DrawerProvider.GetDrawerTypeForField(typeForValue);
				if(drawerType == typeof(DataSetDrawer) && (memberInfo == null || !memberInfo.IsDuplicateReference(false))) // This also ignores UnityEngine.Object which we do want ignored.
				{
					canDrawMembersInlined = true;
					ParentDrawerUtility.GetMemberBuildList(TypeForValue, this, MemberHierarchy, ref memberBuildList, DebugMode);
				}
				else
				{
					canDrawMembersInlined = false;
					memberBuildList.Add(memberInfo);
				}
				#else
				memberBuildList.Add(memberInfo);
				#endif

				UpdateDrawToggleNullButton();
			}
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && DEBUG_BUILD_MEMBERS
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".DoBuildMembers called with memberBuildList=", memberBuildList, ", IsNull = ", IsNull, ", CanBeUnityObject=", CanBeUnityObject, ", DrawToggleNullButton=", DrawToggleNullButton, ", memberInfo=", memberInfo, ", memberInfo.Data=", (memberInfo == null ? "n/a" : StringUtils.ToString(memberInfo.Data))+ ", TypeForValue="+ StringUtils.ToStringSansNamespace(TypeForValue)));
			#endif

			var typeForValue = TypeForValue;

			DrawerArrayPool.DisposeContent(ref members);

			if(typeForValue == null)
			{
				if(CanBeUnityObject)
				{
					var referenceField = ObjectReferenceDrawer.Create(null, memberInfo, GetTypeForObjectReferenceField(), this, GUIContent.none, AllowSceneObjects(), false, ReadOnly);
					if(DrawToggleNullButton)
					{
						DrawerArrayPool.Resize(ref members, 2);
						members[0] = NullToggleDrawer.Create(OnNullToggleButtonClicked, this, ReadOnly);
						members[1] = referenceField;
					}
					else
					{
						DrawerArrayPool.Resize(ref members, 1);
						members[0] = referenceField;
					}
				}
				else if(DrawToggleNullButton)
				{
					DrawerArrayPool.Resize(ref members, 1);
					members[0] = NullToggleDrawer.Create(OnNullToggleButtonClicked, this, ReadOnly);
				}
				else
				{
					DrawerArrayPool.Resize(ref members, 1);
					members[0] = ReadOnlyTextDrawer.Create("null", null, this, GUIContent.none);
				}
				return;
			}

			var isGenericTypeDefinition = typeForValue.IsGenericTypeDefinition;
			if(isGenericTypeDefinition)
			{
				DrawerArrayPool.Resize(ref members, 1);
				members[0] = GenericTypeArgumentDrawer.Create(typeForValue, memberInfo, this, null, ReadOnly);
				return;
			}

			if(DrawMembersInlined && memberBuildList.Count > 0 && (memberInfo == null || memberBuildList[0].MemberInfo != memberInfo.MemberInfo))
			{
				#if DEV_MODE
				Debug.Log(memberBuildList[0].MemberInfo + " != " + memberInfo.MemberInfo);
				int depth = 0;
				for(var p = parent; p != null; p = p.Parent)
				{
					depth++;
				}
				Debug.Log(ToString() + " DrawMembersInlined and " + memberBuildList[0].MemberInfo.ReflectedType.Name + "." + memberBuildList[0].MemberInfo + " != " + memberInfo.MemberInfo.ReflectedType.Name + "." + memberInfo.MemberInfo+" with depth = "+depth);
				#endif

				base.DoBuildMembers(); // here
				return;
			}
			
			#if DEV_MODE && PI_ASSERTATIONS
			Assert(typeForValue != null, ToString(), ".BuildMembers was called with isNull=false but with userSelectedType=", null, ".\nDrawInSingleRow=", DrawInSingleRow, ", Value=", Value, ", Value.Type=", StringUtils.TypeToString(Value));
			Assert(!typeForValue.IsAbstract, ToString(), ".BuildMembers was called with isNull=false but with userSelectedType ", userSelectedType, " IsAbstract="+true+".\nDrawInSingleRow=", DrawInSingleRow, ", Value=", Value, ", Value.Type=", StringUtils.TypeToString(Value));
			#endif
			
			var valueDrawer = BuildDrawerForValue(typeForValue);

			#if DEV_MODE && PI_ASSERTATIONS
			Assert(valueDrawer.GetType() != GetType());
			#endif

			valueDrawer.OnValueChanged += (changed, setValue) => DoSetValue(setValue, false, false);

			if(DrawToggleNullButton)
			{
				DrawerArrayPool.Resize(ref members, 2);
				members[0] = NullToggleDrawer.Create(OnNullToggleButtonClicked, this, ReadOnly);
				members[1] = valueDrawer;
			}
			else
			{
				DrawerArrayPool.Resize(ref members, 1);
				members[0] = valueDrawer;
			}

			#if DRAW_VALUE_IN_SINGLE_ROW_IF_POSSIBLE
			if(DrawerUtility.CanDrawInSingleRow(valueDrawer))
			{
				valueDrawer.Label = GUIContentPool.Empty();
			}
			#endif
		}

		/// <summary>
		/// Given a non-abstract explicitly chosen type, returns the drawer for the value of the drawer using said type.
		/// </summary>
		/// <param name="typeForValue"> Type of value. </param>
		/// <returns> Drawer instance to use for the value. </returns>
		protected virtual IDrawer BuildDrawerForValue([NotNull]Type typeForValue)
		{
			#if DEV_MODE && DEBUG_DRAW_IN_SINGLE_ROW
			Debug.Log("BuildDrawerForValue("+typeForValue.Name+") called with DrawInSingleRow="+DrawInSingleRow+ ", CanDrawInSingleRow(" + typeForValue.Name + ")=" + DrawerUtility.CanDrawInSingleRow(typeForValue, DebugMode)+ ", CanDrawMultipleControlsOfTypeInSingleRow(" + typeForValue.Name + ")=" + DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(typeForValue));
			#endif

			#if DEV_MODE
			Debug.Assert(typeForValue != null);
			Debug.Assert(!typeForValue.IsGenericTypeDefinition);
			#endif

			if(typeForValue.IsUnityObject())
			{
				return ObjectReferenceDrawer.Create(Value as Object, memberInfo, GetTypeForObjectReferenceField(), this, GUIContent.none, AllowSceneObjects(), false, ReadOnly);
			}

			var label = GUIContentPool.Create(StringUtils.SplitPascalCaseToWords(StringUtils.ToStringSansNamespace(typeForValue)));

			// Possible infinite loop danger.
			if(memberInfo != null && typeForValue == memberInfo.Type)
			{
				// UPDATE: This should be safe when using e.g. the SerializeReference attribute to allow for null value.
				if(!CanBeNull)
				{
					#if DEV_MODE
					Debug.LogError("Infinite loop! typeForValue " + StringUtils.ToString(typeForValue) + " same as Type! instanceType=" + StringUtils.ToString(instanceType)+", userSelectedType="+ StringUtils.ToString(userSelectedType)+", CanBeNull="+CanBeNull);
					#endif
					return ReadOnlyTextDrawer.Create(StringUtils.ToString(Value), memberInfo, this, label);
				}
				#if DEV_MODE
				Debug.LogWarning(ToString() + " infinite loop danger because typeForValue " + StringUtils.ToString(typeForValue) + " same as Type. Still calling BuildDrawerForValue because CanBeNull="+StringUtils.True+". instanceType=" + StringUtils.ToString(instanceType)+", userSelectedType="+ StringUtils.ToString(userSelectedType)+", CanBeNull="+CanBeNull);
				#endif
			}

			return BuildDrawerForValue(typeForValue, Value, memberInfo, this, label, ReadOnly);
		}

		/// <summary>
		/// Given a non-abstract explicitly chosen type, returns the drawer for the value of the drawer using said type.
		/// </summary>
		/// <param name="setType"> Type of value. </param>
		/// <param name="setValue"> Initial value. Can be null. </param>
		/// <param name="setMemberInfo"> Linked member info for class member which the drawer represents. </param>
		/// <param name="setParent"> Parent for the drawer. </param>
		/// <param name="setLabel"> Label for the drawer. </param>
		/// <param name="setReadOnly"> If true drawer should be greted out and its value should not be editable. </param>
		/// <returns> Drawer instance to use for the value. </returns>
		protected virtual IDrawer BuildDrawerForValue(Type setType, [CanBeNull]object setValue, [CanBeNull]LinkedMemberInfo setMemberInfo, [NotNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setType != null || setValue != null || setMemberInfo != null);
			Debug.Assert(setMemberInfo == memberInfo);
			Debug.Assert(setParent == this);
			Debug.Assert(setType != base.Type, ToString()+" - setType "+ StringUtils.ToString(setType)+ " equals base.Type " + StringUtils.ToString(base.Type));
			#endif

			return DrawerProvider.GetForField(setValue, setType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		private Type GetTypeForObjectReferenceField()
		{
			if(memberInfo == null)
			{
				if(instanceType != null)
				{
					return instanceType;
				}
				return Types.UnityObject;
			}

			var type = memberInfo.Type;
			return !type.IsAbstract && !type.IsUnityObject() ? Types.UnityObject : type;
		}
		
		/// <inheritdoc />
		public override void OnAfterMembersBuilt()
		{
			UpdateDrawInSingleRow();

			base.OnAfterMembersBuilt();
			
			if(!DrawMembersInlined && !IsNull)
			{
				var valueDrawer = ValueDrawer;
				if(drawInSingleRow)
				{
					valueDrawer.Label = GUIContentPool.Empty();
				}
				
				#if DEV_MODE && PI_ASSERTATIONS
				if(!ValueEquals(valueDrawer.GetValue())) { Debug.LogError(Msg("Value ", Value, " != valueDrawer.Value ", valueDrawer.GetValue())); }
				#endif
			}
		}

		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
			return null;
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			if(DrawInSingleRow)
			{
				if(DrawToggleNullButton)
				{
					toggleNullButtonPosition = bodyLastDrawPosition;

					if(CanBeUnityObject || !IsNull)
					{
						toggleNullButtonPosition.width = NullButtonWidth;
						toggleNullButtonPosition.y += 3f;
						toggleNullButtonPosition.height -= 3f;

						valueFieldPosition = bodyLastDrawPosition;
						valueFieldPosition.x += toggleNullButtonPosition.width;
						valueFieldPosition.width -= NullButtonWidth;

						#if DEV_MODE && PI_ASSERTATIONS
						if(valueFieldPosition.width <= 0f) { Debug.LogError(Msg(ToString(), ".valueFieldPosition <= 0f: ", valueFieldPosition, " with DrawInSingleRow=", true, ", position=", position, ", bodyLastDrawPosition=", bodyLastDrawPosition, ", toggleNullButtonPosition=", toggleNullButtonPosition, ", NullButtonWidth=", NullButtonWidth, ", Event=", Event.current)); }
						#endif
					}
				}
				else
				{
					valueFieldPosition = bodyLastDrawPosition;

					#if DEV_MODE && PI_ASSERTATIONS
					if(valueFieldPosition.width <= 0f) { Debug.LogError(Msg(ToString(), ".valueFieldPosition <= 0f: ", valueFieldPosition, " with position=", position, ", bodyLastDrawPosition=", bodyLastDrawPosition, ", Event=", Event.current)); }
					#endif
				}
			}
			else
			{
				valueFieldPosition = bodyLastDrawPosition;

				#if DEV_MODE && PI_ASSERTATIONS
				if(valueFieldPosition.width <= 0f) { Debug.LogError(Msg(ToString(), ".valueFieldPosition <= 0f: ", valueFieldPosition, " with position=", position, ", bodyLastDrawPosition=", bodyLastDrawPosition, ", Event=", Event.current)); }
				#endif
				
				if(DrawToggleNullButton)
				{
					Rect ignoredLabelRect;
					position.GetLabelAndControlRects(label, out ignoredLabelRect, out toggleNullButtonPosition);
				}
			}
		}
		
		private void OnCreateInstanceButtonClicked()
		{
			DrawGUI.Use(Event.current);

			var nonUnityObjectTypes = NonUnityObjectTypes;

			if(!nonUnityObjectTypes.Any())
            {
				return;
            }

			if(!menuItemsGenerated || menuItemsGeneratedForType != Type)
			{
				generatedMenuItems.Clear();
				generatedGroupsByLabel.Clear();
				generatedItemsByLabel.Clear();
				foreach(var nonUnityObjectType in nonUnityObjectTypes)
				{
					PopupMenuUtility.BuildPopupMenuItemForType(generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, nonUnityObjectType);
				}
				menuItemsGenerated = true;
				menuItemsGeneratedForType = Type;
			}

			if(generatedMenuItems.Count == 1)
			{
				SetValueFromType(nonUnityObjectTypes.First());
				return;
			}

			var inspector = InspectorUtility.ActiveInspector;
			var openPosition = ControlPosition;
			openPosition.y += -2f;
			PopupMenuManager.Open(inspector, generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, openPosition, OnPopupMenuItemClicked, OnPopupMenuClosed, GUIContentPool.Create("Type"), this);

			if(!IsNull)
			{
				var selectItemOfType = Value.GetType();
				if(selectItemOfType != null)
				{
					PopupMenuManager.SelectItem(PopupMenuUtility.GetFullLabel(selectItemOfType));
				}
			}
		}

		private void OnPopupMenuItemClicked([NotNull]PopupMenuItem item)
		{
			SetValueFromType(item.type);
		}

		private void SetValueFromType([NotNull]Type type)
		{
			userSelectedType = type;

			object setValue;
			try
			{
				setValue = type.DefaultValue();
			}
			catch(Exception e)
			{
				Debug.LogError("Failed to generate default value for type "+StringUtils.ToString(type) +"\n"+e);
				return;
			}

			DoSetValue(setValue, true, false);
			
			if(setValue == null)
			{
				RebuildMemberBuildListAndMembers();
			}
			else if(members.Length > 0)
			{
				// Unfold all contents for convenience.
				var valueDrawer = ValueDrawer as IParentDrawer;
				if(valueDrawer != null)
				{
					#if DEV_MODE
					Debug.Log("Setting valueDrawer unfolded: " + valueDrawer);
					#endif
					valueDrawer.SetUnfolded(true, true);
				}
				else if(!DrawInSingleRow)
				{
					SetUnfolded(true, true);
				}
			}
			
		}

		private void OnPopupMenuClosed()
		{
			Select(ReasonSelectionChanged.Initialization);
		}
		
		/// <summary> Determine if we allow scene objects to be set as the field value. </summary>
		/// <returns> True if we allow scene object values, false if not. </returns>
		protected abstract bool AllowSceneObjects();

		private void OnNullToggleButtonClicked()
		{
			#if DEV_MODE
			Debug.Log(StringUtils.ToColorizedString(ToString()+".OnNullToggleButtonClicked with IsNull=", IsNull, ", NonUnityObjectTypes.Count=", NonUnityObjectTypes.Count(), ", Unfolded=", Unfolded));
			#endif

			if(IsNull)
			{
				OnCreateInstanceButtonClicked();
			}
			else
			{
				userSelectedType = null;
				Value = null;

				if(!drawInSingleRow)
				{
					var parentMember = members[0] as IParentDrawer;
					if(parentMember != null)
					{
						parentMember.Unfolded = true;
					}
				}
			}
		}

		private void UpdateDrawInSingleRow()
		{
			bool setDrawInSingleRow;

			if(IsNull)
			{
				setDrawInSingleRow = true;
			}
			#if DRAW_VALUE_IN_SINGLE_ROW_IF_POSSIBLE
			else if(memberBuildState == MemberBuildState.MembersBuilt)
			{
				setDrawInSingleRow = DrawerUtility.CanDrawInSingleRow(ValueDrawer);
			}
			else if(memberBuildState == MemberBuildState.BuildListGenerated)
			{
				setDrawInSingleRow = DrawerUtility.CanDrawInSingleRow(instanceType, DebugMode);
			}
			#else
			else if(memberBuildState != MemberBuildState.Unstarted)
			{
				setDrawInSingleRow = DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(instanceType);
			}
			#endif
			else
			{
				setDrawInSingleRow = true;
			}
			
			if(setDrawInSingleRow != drawInSingleRow)
			{
				#if DEV_MODE && DEBUG_UPDATE_DRAW_IN_SINGLE_ROW
				Debug.Log(Msg(ToString(), ".drawInSingleRow = ", setDrawInSingleRow, " (was: ", drawInSingleRow, ") with IsNull=", IsNull,", memberBuildState = ", memberBuildState, ", instanceType=", instanceType));
				#endif

				drawInSingleRow = setDrawInSingleRow;

				UpdatePrefixDrawer();

				if(DrawInSingleRow)
				{
					if(inactive)
					{
						SetUnfoldedInstantly(true);
					}
					else
					{
						SetUnfolded(true);
					}
				}

				UpdateVisibleMembers();
			}
			else if(prefixLabelDrawer == null)
			{
				#if DEV_MODE
				Debug.LogError("This null check was needed!");
				#endif
				UpdatePrefixDrawer();
			}
		}

		/// <inheritdoc />
		public override void UpdateVisibleMembers()
		{
			if(!DrawInSingleRow && !Unfolded)
			{
				if(DrawToggleNullButton && members[0].ShouldShowInInspector)
				{
					if(visibleMembers.Length != 1)
					{
						DrawerArrayPool.Resize(ref visibleMembers, 1);
						visibleMembers[0] = members[0];
						OnVisibleMembersChanged();
						OnChildLayoutChanged();
					}
					#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
					Debug.Log(ToString()+".UpdateVisibleMembers() - "+visibleMembers.Length+" of " + members.Length + " now visible\nvisibleMembers=" + StringUtils.ToString(visibleMembers) + "\nmembers=" + StringUtils.ToString(members) + "\nUnfolded=" + Unfolded+ ", MembersAreVisible="+ MembersAreVisible);
					#endif
					return;
				}

				if(visibleMembers.Length != 0)
				{
					DrawerArrayPool.Resize(ref visibleMembers, 0);
					OnVisibleMembersChanged();
					OnChildLayoutChanged();
				}
				#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
				Debug.Log(ToString()+".UpdateVisibleMembers() - "+visibleMembers.Length+" of " + members.Length + " now visible\nvisibleMembers=" + StringUtils.ToString(visibleMembers) + "\nmembers=" + StringUtils.ToString(members) + "\nUnfolded=" + Unfolded+ ", MembersAreVisible="+ MembersAreVisible);
				#endif
				return;
			}

			base.UpdateVisibleMembers();

			#if DEV_MODE && DEBUG_UPDATE_VISIBLE_MEMBERS
			Debug.Log(ToString()+".UpdateVisibleMembers() - "+visibleMembers.Length+" of " + members.Length + " now visible\nvisibleMembers=" + StringUtils.ToString(visibleMembers) + "\nmembers=" + StringUtils.ToString(members) + "\nUnfolded=" + Unfolded+ ", MembersAreVisible="+ MembersAreVisible+ ", DrawInSingleRow="+ DrawInSingleRow+ ", DrawToggleNullButton="+ DrawToggleNullButton);
			#endif
		}

		/// <inheritdoc />
		public override void OnVisibleMembersChanged()
		{
			UpdateDrawInSingleRow();
			base.OnVisibleMembersChanged();
		}

		#if !DRAW_MEMBERS_INLINED
		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			var draggableMember = ValueDrawer as IDraggablePrefix;
			if(draggableMember != null && draggableMember.ShouldShowInInspector)
			{
				DrawGUI.Active.SetCursor(MouseCursor.SlideArrow);
				DrawGUI.DrawMouseoverEffect(draggableMember.ControlPosition, Inspector.Preferences.theme.CanDragPrefixToAdjustValueTint, localDrawAreaOffset);
				return;
			}
			base.OnMouseover();
		}
		#endif

		/// <inheritdoc />
		public void OnPrefixDragStart(Event inputEvent) { }

		/// <inheritdoc/>
		public void OnPrefixDragged(Event inputEvent)
		{
			var draggableMember = ValueDrawer as IDraggablePrefix;
			if(draggableMember != null && draggableMember.ShouldShowInInspector)
			{
				draggableMember.OnPrefixDragged(inputEvent);
			}
		}

		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			#if DEV_MODE && DEBUG_KEYBOARD_INPUT
			Debug.Log(ToString()+".OnKeyboardInputGiven("+StringUtils.ToString(inputEvent)+")");
			#endif

			switch(inputEvent.keyCode)
			{
				#if !DRAW_MEMBERS_INLINED
				case KeyCode.F2:
					var textFieldMember = ValueDrawer as ITextFieldDrawer;
					if(textFieldMember != null)
					{
						DrawGUI.Use(inputEvent);
						textFieldMember.StartEditingField();
						return true;
					}
					return false;
				#endif
				case KeyCode.Return:
				case KeyCode.Space:
				case KeyCode.KeypadEnter:
					if(inputEvent.modifiers == EventModifiers.None && DrawToggleNullButton)
					{
						OnNullToggleButtonClicked();
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						return true;
					}
					break;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc/>
		public bool DraggingPrefixAffectsMember(IDrawer member)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member != null);
			Debug.Assert(Array.IndexOf(members, member) != -1);
			Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt);
			#endif

			return member is IDraggablePrefix;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			#if DRAW_MEMBERS_INLINED
			canDrawMembersInlined = null;
			#endif

			userSelectedType = null;
			instanceType = null;

			canBeNonUnityObject = false;
			drawInSingleRow = false;
			drawToggleNullButton = false;
		
			valueFieldPosition.width = 0f;
			toggleNullButtonPosition.width = 0f;

			base.Dispose();
		}

		/// <inheritdoc/>
		public override void CopyToClipboard()
		{
			var unityObjectValue = Value as Object;
			if(unityObjectValue != null)
			{
				try
				{
					Clipboard.CopyObjectReference(unityObjectValue, unityObjectValue.GetType());
					SendCopyToClipboardMessage();
				}
				#if DEV_MODE
				catch(Exception e)
				{
					Debug.LogWarning(e);
				#else
				catch
				{
				#endif
					SendCopyToClipboardMessage();
				}
				return;
			}

			base.CopyToClipboard();
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			if(MixedContent || !CanBeUnityObject || !Clipboard.HasObjectReference())
			{
				base.DoPasteFromClipboard();
				return;
			}

			var assignableType = GetAssignableType();

			#if DEV_MODE
			Debug.Log(ToString()+".DoPasteFromClipboard as Object reference with Type="+StringUtils.ToString(Type)+ ", assignableType=" + StringUtils.ToString(assignableType));
			#endif

			Value = Clipboard.PasteObjectReference(assignableType);
		}
	}
}