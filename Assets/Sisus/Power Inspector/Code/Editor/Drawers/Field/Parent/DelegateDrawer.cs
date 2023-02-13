//#define DEBUG_UPDATE_INVOCATION_LIST
#define DEBUG_BUILD_MEMBERS

using System;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(MulticastDelegate), true, true)]
	public class DelegateDrawer : ParentFieldDrawer<MulticastDelegate>
	{
		private ParameterInfo[] parameterInfos;
		private Delegate[] invocationList;

		private bool drawInSingleRow;

		/// <summary>
		/// Allows specifying the type of the delegate in situations where both LinkedMemberInfo and value are null.
		/// </summary>
		[CanBeNull]
		private Type delegateType;

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				if(delegateType != null)
				{
					return delegateType;
				}
				return base.Type;
			}
		}

		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <inheritdoc />
		protected override bool CanBeNull
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return RebuildingMembersAllowed;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="methodInfo"> LinkedMemberInfo of the method that the drawer represent. If represents an anonymous method, leave null. </param>
		/// <param name="parent"> The parent drawer of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		public static DelegateDrawer Create([CanBeNull]MulticastDelegate value, [CanBeNull]LinkedMemberInfo methodInfo, Type delegateType, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			DelegateDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DelegateDrawer();
			}

			#if DEV_MODE
			if(delegateType != null)
			{
				Debug.Assert(delegateType != typeof(Delegate), delegateType.FullName + " not supported by DelegateDrawer. Should use AnyDelegateDrawer instead.");
				Debug.Assert(delegateType != typeof(MulticastDelegate), delegateType.FullName + " not supported by DelegateDrawer. Should use AnyDelegateDrawer instead.");
			}
			#endif

			result.Setup(value, delegateType, methodInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="methodInfo"> LinkedMemberInfo of the method that the drawer represent. If represents an anonymous method, leave null. </param>
		/// <param name="parent"> The parent drawer of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		public static DelegateDrawer Create([CanBeNull]MulticastDelegate value, [CanBeNull]LinkedMemberInfo methodInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			DelegateDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result =  new DelegateDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(methodInfo, value), methodInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((MulticastDelegate)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected sealed override void Setup(MulticastDelegate setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(setValue == null)
			{
				if(setMemberInfo != null)
				{
					setValue = setMemberInfo.GetValue(0) as MulticastDelegate;
				}
				else if(setValueType == null)
				{
					#if DEV_MODE
					Debug.LogError("Value and fieldInfo and setDelegateType were all null for DelegateDrawer!");
					#endif

					return;
				}
			}

			UpdateInvocationList(setValue);

			delegateType = setValueType;
			var type = setValueType != null ? setValueType : setMemberInfo != null ? setMemberInfo.Type : setValue.GetType();

			#if DEV_MODE
			Debug.Assert(typeof(Delegate).IsAssignableFrom(type), type.FullName);
			Debug.Assert(type != typeof(Delegate), type.FullName + " not supported by DelegateDrawer. Should use AnyDelegateDrawer instead.");
			Debug.Assert(type != typeof(MulticastDelegate), type.FullName + " not supported by DelegateDrawer. Should use AnyDelegateDrawer instead.");
			#endif

			var invokeMethod = type.GetMethod("Invoke");
			if(invokeMethod == null)
			{
				#if DEV_MODE
				Debug.LogError("DelegateDrawer - Could not find \"Invoke\" method in type "+type.Name+"! memberInfo.Type="+(setMemberInfo == null ? "n/a" : setMemberInfo.Type.Name));
				#endif

				parameterInfos = invokeMethod.GetParameters();
			}
			else
			{
				parameterInfos = invokeMethod.GetParameters();
			}
			
			if(setLabel == null)
			{
				setLabel = GUIContentPool.Create(setMemberInfo == null ? "()=>" : setMemberInfo.Name, GetTooltip(parameterInfos));
			}
			else
			{
				if(setLabel.tooltip.Length == 0)
				{
					setLabel.tooltip = GetTooltip(parameterInfos);
				}
			}
			
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && DEBUG_BUILD_MEMBERS
			Debug.Log(ToString()+ ".DoBuildMembers called with invocationList "+StringUtils.ToString(invocationList));
			#endif

			int count = invocationList.Length;

			if(count == 0)
			{
				DrawerArrayPool.Resize(ref members, 1);
				members[0] = NullToggleDrawer.Create(AddNewItemToInvocationList, InspectorPreferences.Styles.AddButton, this, ReadOnly);
			}
			else
			{
				DrawerArrayPool.Resize(ref members, count);
			
				var type = Type;
				for(int n = count - 1; n >= 0; n--)
				{
					var invocationMember = invocationList[n];
					members[n] = DelegateItemDrawer.Create(invocationMember, type, this, GUIContentPool.Create("Delegate #"+StringUtils.ToString(n + 1)), ReadOnly);
					members[n].OnKeyboardInputBeingGiven += OnMemberKeyboardInputBeingGiven;
				}
			}
		}

		/// <inheritdoc />
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE
			Debug.Log("TryToManuallyUpdateCachedValueFromMember("+memberIndex+", "+StringUtils.ToString(memberValue)+ ") with invocationList="+ invocationList);
			#endif

			if(memberValue != null)
			{
				invocationList[memberIndex] = memberValue as Delegate;
				SetValue(Delegate.Combine(invocationList.RemoveNullMembers()) as MulticastDelegate, true, false);
			}
			return true;
		}

		private void AddNewItemToInvocationList()
		{
			int index = invocationList.Length;
			ArrayPool<Delegate>.Resize(ref invocationList, index + 1);
			RebuildMembers();
		}

		#if UNITY_EDITOR
		public override bool Draw(Rect position)
		{
			var backgroundRect = position;
			backgroundRect.x = DrawGUI.InspectorWidth - DrawGUI.MinControlFieldWidth + DrawGUI.MiddlePadding - 1f;
			backgroundRect.width = DrawGUI.MinControlFieldWidth - DrawGUI.RightPadding - DrawGUI.MiddlePadding - 1f;
			backgroundRect.y += 1f;
			backgroundRect.height -= 2f;
			return base.Draw(position);
		}
		#endif
		
		private static string GetTooltip(ParameterInfo[] parameterInfos)
		{
			int count = parameterInfos.Length;
			if(count <= 0)
			{
				return "";
			}

			var sb = StringBuilderPool.Create();
			sb.Append('<');
			for(int n = 0; n < count; n++)
			{
				if(n != 0)
				{
					sb.Append(',');
				}
				sb.Append(parameterInfos[n].ParameterType.Name);
			}
			sb.Append('>');

			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(parameterInfos.Length == 0)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Invoke", Invoke);
				menu.Add("Add", Invoke);
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}
		
		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
			return null;
		}

		/// <inheritdoc />
		protected override bool OnKeyboardActivate(Event inputEvent)
		{
			DrawGUI.Use(inputEvent);
			AddNewItemToInvocationList();
			return true;
		}

		/// <summary>
		/// Called before keyboard input is sent to a selected member of the collection.
		///  </summary>
		/// <param name="member"> The member that is selected. </param>
		/// <param name="inputEvent"> Iput data. </param>
		/// <param name="keys"> Key configuration data. </param>
		/// <returns> True if input event should be consumed, i.e. not sent to the selected member, otherwise false. </returns>
		private bool OnMemberKeyboardInputBeingGiven(IDrawer member, Event inputEvent, KeyConfigs keys)
		{
			if(keys.duplicate.DetectAndUseInput(inputEvent))
			{
				DuplicateMember(member);
				return true;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Home:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						var firstVisibleMember = visibleMembers[0];
						if(member != firstVisibleMember)
						{
							firstVisibleMember.Select(ReasonSelectionChanged.KeyPressShortcut);
							return true;
						}
						
						//select collection itself if first member already selected?
						Select(ReasonSelectionChanged.KeyPressShortcut);
						return true;
					}
					return false;
				case KeyCode.Delete:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);

						if(ReadOnly)
						{
							InspectorUtility.ActiveInspector.Message("Can't delete member of a read-only collection");
							return false;
						}
						var manager = InspectorUtility.ActiveManager;
						if(manager.HasMultiSelectedControls)
						{
							DeleteMembers(manager.MultiSelectedControls);
						}
						else
						{
							DeleteMember(member);
						}
						return true;
					}
					return false;
				case KeyCode.End:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						var lastVisibleMember = visibleMembers[visibleMembers.Length - 1];
						if(member != lastVisibleMember)
						{
							lastVisibleMember.Select(ReasonSelectionChanged.KeyPressShortcut);
							return true;
						}

						//select next control if last member already selected?
						var next = GetNextSelectableDrawerDown(GetSelectedRowIndex(), this);
						if(next != null)
						{
							next.Select(ReasonSelectionChanged.KeyPressShortcut);
							return true;
						}
					}
					return false;

			}
			return false;
		}

		private void DuplicateMember(IDrawer drawer)
		{
			int index = Array.IndexOf(members, drawer);
			if(index != -1)
			{
				invocationList = invocationList.InsertAt(index, invocationList[index]);
				SetValue(Delegate.Combine(invocationList.RemoveNullMembers()) as MulticastDelegate, true, false);
			}
		}

		private void DeleteMember(IDrawer drawer)
		{
			int index = Array.IndexOf(members, drawer);
			if(index != -1)
			{
				invocationList = invocationList.RemoveAt(index);
				SetValue(Delegate.Combine(invocationList.RemoveNullMembers()) as MulticastDelegate, true, false);
			}
		}

		private void DeleteMembers(System.Collections.Generic.List<IDrawer> drawers)
		{
			bool changed = false;
			for(int n = members.Length - 1; n >= 0; n--)
			{
				if(drawers.Contains(members[n]))
				{
					changed = true;
					invocationList = invocationList.RemoveAt(n);
				}
			}
			if(changed)
			{
				SetValue(Delegate.Combine(invocationList.RemoveNullMembers()) as MulticastDelegate, true, false);
			}
		}


		/// <inheritdoc />
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}

		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			UpdateInvocationList();
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		private void Invoke()
		{
			Value.DynamicInvoke(null);
		}

		private void UpdateInvocationList()
		{
			UpdateInvocationList(Value);
		}

		private void UpdateInvocationList(MulticastDelegate value)
		{
			#if DEV_MODE && DEBUG_UPDATE_INVOCATION_LIST
			Debug.Log(ToString()+ ".UpdateInvocationList called with value="+StringUtils.ToString(value));
			#endif	

			if(value != null)
			{
				invocationList = value.GetInvocationList();
				if(invocationList == null)
				{
					ArrayExtensions.ArrayToZeroSize(ref invocationList);
				}
			}
			else
			{
				ArrayExtensions.ArrayToZeroSize(ref invocationList);
			}

			bool setDrawInSingleRow = invocationList.Length == 0;
			if(setDrawInSingleRow != drawInSingleRow)
			{
				drawInSingleRow = setDrawInSingleRow;

				if(!inactive && DrawInSingleRow && !Unfolded)
				{
					SetUnfolded(true, false);
				}
			}
		}
	}
}