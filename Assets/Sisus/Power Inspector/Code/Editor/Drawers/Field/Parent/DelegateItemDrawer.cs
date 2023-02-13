using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(Delegate), false, true)]
	public class DelegateItemDrawer : ParentFieldDrawer<Delegate>
	{
		private static readonly List<MethodInfo> ReusedMethodsList = new List<MethodInfo>();

		private static bool menuItemsGenerated;
		private static List<PopupMenuItem> generatedMenuItems = new List<PopupMenuItem>(30);
		private static Dictionary<string, PopupMenuItem> generatedGroupsByLabel = new Dictionary<string, PopupMenuItem>(10);
		private static Dictionary<string, PopupMenuItem> generatedItemsByLabel = new Dictionary<string, PopupMenuItem>(20);

		protected Type typeContext;

		private Type delegateType;
		private MethodInfo[] methodOptions;
		private string[] methodOptionNames;
		
		private Rect nullTogglePosition;
		private Rect objectReferenceFieldPosition;
		private Rect instanceTypePopupFieldPosition;
		private Rect methodPopupFieldPosition;

		private int controls;

		/// <inheritdoc/>
		public override bool DrawInSingleRow
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
		
		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return delegateType;
			}
		}

		private bool IsNull
		{
			get
			{
				return Value == null;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="type"> Type of the delegate. Can not be null. </param>
		/// <param name="parent"> The parent drawers of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		public static DelegateItemDrawer Create(Delegate value, [NotNull]Type type, [CanBeNull]IParentDrawer parent, [CanBeNull]GUIContent label, bool setReadOnly)
		{
			DelegateItemDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result =  new DelegateItemDrawer();
			}
			result.Setup(value, type, null, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var delegateValue = (Delegate)setValue;
			Setup(delegateValue, setValueType, null, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup([CanBeNull]Delegate setValue, [NotNull]Type setValueType, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly)
		{
			delegateType = setValueType;
			OnDelegateValueChanged(setValue);
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }
		
		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			var value = Value;
			if(value == null)
			{
				DrawerArrayPool.Resize(ref members, 2);
				members[0] = NullToggleDrawer.Create(OnNullToggleButtonClicked, this, ReadOnly);
				members[1] = ObjectReferenceDrawer.Create(null, Types.UnityObject, this, GUIContent.none, true, false, ReadOnly);
			}
			else
			{
				var target = value.Target;
				bool hasTarget = target != null;
				Object unityObject;
				bool isUnityObject;
				bool isAnonymous;
				string methodName;
				Type targetType;
				int methodIndex;
				var method = value.Method;

				if(hasTarget)
				{
					targetType = target.GetType();

					UpdateMethodOptions(targetType, true);
					
					unityObject = target as Object;
					isUnityObject = unityObject != null;

					methodName = method.Name;
					isAnonymous = methodName[0] == '<';

					if(isAnonymous)
					{
						string methodOrigin = methodName.Substring(1, methodName.IndexOf('>')-1);
						methodName = string.Concat("Anonymous Method (", methodOrigin, ")");
					}

					methodIndex = Array.IndexOf(methodOptionNames, methodName);
					if(methodIndex == -1)
					{
						methodOptions = methodOptions.InsertAt(0, method);
						methodOptionNames = methodOptionNames.InsertAt(0, methodName);
						methodIndex = 0;
					}
				}
				else
				{
					methodIndex = 0;

					if(method == null)
					{
						targetType = null;
						methodName = "{ }";
						unityObject = null;
						isUnityObject = false;
						isAnonymous = false;

						ArrayPool<MethodInfo>.Resize(ref methodOptions, 1);
						methodOptions[0] = method;
						ArrayPool<string>.Resize(ref methodOptionNames, 1);
						methodOptionNames[0] = methodName;
					}
					else
					{
						targetType = method.ReflectedType;

						UpdateMethodOptions(targetType, false);

						methodName = method.Name;
						unityObject = null;
						isUnityObject = false;
						isAnonymous = methodName[0] == '<';

						if(isAnonymous)
						{
							string methodOrigin = methodName.Substring(1, methodName.IndexOf('>')-1);
							methodName = string.Concat("Anonymous Method (", methodOrigin, ")");
						}

						methodIndex = Array.IndexOf(methodOptionNames, methodName);
						if(methodIndex == -1)
						{
							methodOptions = methodOptions.InsertAt(0, method);
							methodOptionNames = methodOptionNames.InsertAt(0, methodName);
							methodIndex = 0;
						}
					}
				}

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(methodOptions.Length == methodOptionNames.Length);
				#endif

				#if DEV_MODE
				Debug.Log(Msg(ToString()+".DoBuildMembers with target=", target, ", type=", targetType, ", isUnityObject=", isUnityObject, ", methodName=", methodName, ", isAnonymous=", isAnonymous+", methodNames=", StringUtils.ToString(methodOptionNames)));
				#endif

				if(isUnityObject)
				{
					DrawerArrayPool.Resize(ref members, 2);
					members[0] = ObjectReferenceDrawer.Create(unityObject, Types.UnityObject, this, GUIContentPool.Empty(), true, false, ReadOnly);
					members[1] = PopupMenuDrawer.Create(methodIndex, methodOptionNames, null, this, GUIContentPool.Empty(), ReadOnly);
				}
				else
				{
					DrawerArrayPool.Resize(ref members, 3);
					members[0] = NullToggleDrawer.Create(OnNullToggleButtonClicked, this, ReadOnly);
					members[1] = TypeDrawer.Create(targetType, null, this, GUIContentPool.Empty(), ReadOnly);
					members[2] = PopupMenuDrawer.Create(methodIndex, methodOptionNames, null, this, GUIContentPool.Empty(), ReadOnly);
				}
			}
		}

		private void OnNullToggleButtonClicked()
		{
			#if DEV_MODE
			Debug.Log(ToString()+".OnNullToggleButtonClicked with IsNull="+StringUtils.ToColorizedString(IsNull));
			#endif

			if(IsNull)
			{
				OpenTypeSelectorMenu();
			}
			else
			{
				Value = null;
			}
		}

		private void OpenTypeSelectorMenu()
		{
			#if DEV_MODE
			Profiler.BeginSample("DelegateItemDrawer.OpenTypeSelectorMenu");
			#endif

			if(!TypeExtensions.IsReady)
			{
				Debug.LogWarning("Can't open menu. Setup still in progress...");
				return;
			}

			if(!menuItemsGenerated)
			{
				menuItemsGenerated = true;
				
				generatedMenuItems.Clear();
				generatedGroupsByLabel.Clear();
				generatedItemsByLabel.Clear();
				GenerateTypeSelectorMenuItems(ref generatedMenuItems, ref generatedGroupsByLabel, ref generatedItemsByLabel);
			}

			PopupMenuManager.Open(Inspector, generatedMenuItems, generatedGroupsByLabel, generatedItemsByLabel, ControlPosition, OnTargetTypeMenuItemClicked, OnTargetTypeMenuClosed, GUIContentPool.Create("Target Class"), this);
		}

		private void GenerateTypeSelectorMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			if(typeContext == null)
			{
				typeContext = PopupMenuUtility.GetTypeContext(memberInfo, parent);
			}

			PopupMenuUtility.BuildTypePopupMenuItemsForContext(ref rootItems, ref groupsByLabel, ref itemsByLabel, typeContext, true);
		}

		private void OnTargetTypeMenuItemClicked(PopupMenuItem item)
		{
			Select(ReasonSelectionChanged.Initialization);
			OnTargetTypeChanged((Type)item.IdentifyingObject, false, null);
		}

		private void OnTargetTypeMenuClosed()
		{
			Select(ReasonSelectionChanged.Initialization);
		}

		private void OnTargetTypeChanged(Type type, bool targetIsInstance, object targetInstance)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnTargetTypeChanged(" + StringUtils.ToString(type)+")");
			#endif

			UpdateMethodOptions(type, targetIsInstance);
			int count = methodOptions.Length;
			MethodInfo method;
			if(count == 0)
			{
				Value = null;
				return;
			}
			
			method = methodOptions[0];
			if(method == null)
			{
				if(count == 1)
				{
					Value = null;
					return;
				}
				method = methodOptions[1];
			}

			#if DEV_MODE
			Debug.Log("CreateDelegate(" + StringUtils.ToString(type)+", null, \"" + method + "\")");
			#endif
			Value = Delegate.CreateDelegate(delegateType, targetInstance, method);
		}

		private void OnUnityObjectTargetChanged(IDrawer changed, object target)
		{
			OnUnityObjectTargetChanged(target as Object);
		}

		private void OnUnityObjectTargetChanged(Object target)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnUnityObjectTargetChanged(" + StringUtils.ToString(target) +")");
			#endif

			if(target == null)
			{
				Value = null;
			}
			else
			{
				var targetType = target.GetType();
				
				UpdateMethodOptions(targetType, true);

				int count = methodOptions.Length;
				if(count == 0)
				{
					Value = null;
				}
				else
				{
					var method = methodOptions[0];
					Value = CreateDelegate(target, method);
				}
			}
		}

		[CanBeNull]
		private Delegate CreateDelegate(object target, MethodInfo method)
		{
			#if DEV_MODE
			Debug.Log("Creating delegate of type "+ delegateType + " from target "+StringUtils.TypeToString(target)+" and method " + method.Name);
			#endif

			return Delegate.CreateDelegate(delegateType, target, method);
		}

		private void OnSelectedMethodChanged(IDrawer changed, object methodIndex)
		{
			OnSelectedMethodChanged((int)methodIndex);
		}

		private void OnSelectedMethodChanged(int methodIndex)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".OnSelectedMethodChanged: #" + StringUtils.ToString(methodIndex) + " out of " + methodOptions.Length+": "+ (methodIndex < 0 ? "n/a" : methodOptionNames[methodIndex])+"\n"+StringUtils.ToString(methodOptionNames));
			#endif

			if(methodIndex < 0 || methodIndex >= methodOptions.Length)
			{
				Value = null;
			}
			else
			{
				var value = Value;
				var target = value.Target;
				Value = CreateDelegate(target, methodOptions[methodIndex]);
			}
		}

		private void UpdateMethodOptions(Type targetType, bool targetIsInstance)
		{
			#if DEV_MODE
			Debug.Log("UpdateMethodOptions for type "+StringUtils.ToStringSansNamespace(targetType)+" with isInstance="+StringUtils.ToColorizedString(targetIsInstance));
			#endif

			GetMethodOptions(targetType, targetIsInstance, delegateType, ref methodOptions, ref methodOptionNames);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(methodOptions.Length == methodOptionNames.Length);
			#endif
		}
		
		private static void GetMethodOptions(Type targetType, bool targetIsInstance, Type delegateType, ref MethodInfo[] methodOptions, ref string[] methodOptionNames)
		{
			if(targetType == null)
			{
				ArrayPool<MethodInfo>.ToZeroSizeArray(ref methodOptions);
				ArrayPool<string>.ToZeroSizeArray(ref methodOptionNames);
				return;
			}

			Type delegateReturnType;
			ParameterInfo[] delegateParameters;
			DelegateUtility.GetDelegateInfo(delegateType, out delegateReturnType, out delegateParameters);
		
			var bindingFlags = targetIsInstance ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic : BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			methodOptions = targetType.GetMethods(bindingFlags);
			int methodCount = methodOptions.Length;
			var validMethods = ReusedMethodsList;
			for(int n = methodCount - 1; n >= 0; n--)
			{
				var method = methodOptions[n];
				if(method.MethodSignatureMatchesDelegate(delegateReturnType, delegateParameters))
				{
					validMethods.Add(method);
				}
			}
			methodOptions = validMethods.ToArray();
			validMethods.Clear();
			methodCount = methodOptions.Length;
			ArrayPool<string>.Resize(ref methodOptionNames, methodCount);
			for(int n = methodCount - 1; n >= 0; n--)
			{
				methodOptionNames[n] = methodOptions[n].Name;
			}

			#if DEV_MODE
			Debug.Log(StringUtils.ToString(targetType) + ".GetMethodOptions results ("+ methodOptionNames.Length+ "):\n"+StringUtils.ToString(methodOptionNames, "\n"));
			#endif
		}

		#if UNITY_EDITOR
		/// <inheritdoc/>
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
		public override bool DrawBodySingleRow(Rect position)
		{
			switch(controls)
			{
				case 0:
					return ParentDrawerUtility.DrawBodySingleRow(this, nullTogglePosition, objectReferenceFieldPosition);
				case 1:
					return ParentDrawerUtility.DrawBodySingleRow(this, objectReferenceFieldPosition, methodPopupFieldPosition);
				case 2:
					if(getValueCausedException)
					{
						return ParentDrawerUtility.DrawBodySingleRow(this, nullTogglePosition, instanceTypePopupFieldPosition, methodPopupFieldPosition);
					}
					return ParentDrawerUtility.DrawBodySingleRow(this, nullTogglePosition, instanceTypePopupFieldPosition, methodPopupFieldPosition);
				default:
					return false;
			}
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			var value = Value;

			bool isNull = IsNull;

			if(isNull)
			{
				controls = 0;

				nullTogglePosition = bodyLastDrawPosition;
				nullTogglePosition.width = 16f;

				objectReferenceFieldPosition = bodyLastDrawPosition;
				objectReferenceFieldPosition.x += 16f;
				objectReferenceFieldPosition.width -= 16f;
			}
			else if(value.Target is Object)
			{
				controls = 1;

				objectReferenceFieldPosition = bodyLastDrawPosition;
				objectReferenceFieldPosition.width = (bodyLastDrawPosition.width - 3f) * 0.5f;

				methodPopupFieldPosition = objectReferenceFieldPosition;
				methodPopupFieldPosition.x = methodPopupFieldPosition.x + 3f + objectReferenceFieldPosition.width;
			}
			else
			{
				controls = 2;

				nullTogglePosition = bodyLastDrawPosition;
				nullTogglePosition.width = 16f;

				instanceTypePopupFieldPosition = bodyLastDrawPosition;
				instanceTypePopupFieldPosition.x += nullTogglePosition.width;
				instanceTypePopupFieldPosition.width = (instanceTypePopupFieldPosition.width - nullTogglePosition.width - 3f) * 0.5f;

				methodPopupFieldPosition = instanceTypePopupFieldPosition;
				methodPopupFieldPosition.x += instanceTypePopupFieldPosition.width;
			}
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			if(!IsNull)
			{
				menu.AddSeparatorIfNotRedundant();
				menu.Add("Invoke", Invoke);
			}
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void Invoke()
		{
			Value.DynamicInvoke(null);
		}

		/// <inheritdoc/>
		public override object DefaultValue(bool _)
		{
			return null;
		}

		/// <inheritdoc/>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			OnDelegateValueChanged(Value);
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		private void OnDelegateValueChanged(Delegate value)
		{
			#if DEV_MODE
			Debug.Log("OnDelegateValueChanged("+StringUtils.ToString(value)+")");
			#endif

			if(value == null)
			{
				UpdateMethodOptions(null, false);
				Label = GUIContentPool.Create("null");
			}
			else
			{
				var target = value.Target;
				var method = value.Method;
				bool hasInstance = target != null;

				UpdateMethodOptions(hasInstance ? target.GetType() : method.ReflectedType, hasInstance);

				// support anonymous methods				
				if(Array.IndexOf(methodOptionNames, method.Name) == -1)
				{
					methodOptions = methodOptions.Add(method);
					methodOptionNames = methodOptionNames.Add(method.Name);
				}
				Label = GUIContentPool.Create("Delegate");
			}
		}


		/// <inheritdoc />
		protected override bool TryUpdateCachedValueFromFieldDuringOnMemberValueChanged()
		{
			return false;
		}

		/// <inheritdoc />
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".TryToManuallyUpdateCachedValueFromMember memberIndex="+memberIndex+ ", memberValue="+StringUtils.ToColorizedString(memberValue));
			#endif

			var currentValue = Value;

			if(currentValue == null)
			{
				if(memberIndex == 1)
				{
					OnUnityObjectTargetChanged(memberValue as Object);
				}
			}
			else if(currentValue.Target is Object)
			{
				if(memberIndex == 0)
				{
					OnUnityObjectTargetChanged(memberValue as Object);
				}
				else if(memberIndex == 1)
				{
					OnSelectedMethodChanged((int)memberValue);
				}
			}
			else
			{
				if(memberIndex == 1)
				{
					var target = currentValue.Target;
					OnTargetTypeChanged(memberValue as Type, target != null, target);
				}
				else if(memberIndex == 2)
				{
					OnSelectedMethodChanged((int)memberValue);
				}
			}
			
			return true;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			methodOptions = null;
			methodOptionNames = null;

			typeContext = null;

			base.Dispose();
		}

		/// <inheritdoc/>
		protected override Delegate GetCopyOfValue(Delegate source)
		{
			return source; // cloning delegates not yet supported
		}
	}
}