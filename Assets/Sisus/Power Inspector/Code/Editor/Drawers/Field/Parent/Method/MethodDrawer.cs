#define CALL_ON_VALIDATE
//#define GENERIC_METHODS_NOT_SUPPORTED

//#define DEBUG_SETUP
//#define DEBUG_BUILD_MEMBERS
//#define DEBUG_INVOKE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// The default drawer for methods, used for drawing methods that are preceded by the ShowInInspector attribute.
	/// 
	/// Can handle drawing parameters and return values.
	/// </summary>
	[Serializable]
	public class MethodDrawer : ParentFieldDrawer<object>
	{
		private const float VerticalPadding = 2f;

		/// <summary> Label to display on the button. </summary>
		protected GUIContent buttonLabel;

		/// <summary> Draw position and extends of the invoke button. </summary>
		protected Rect buttonRect;

		/// <summary> Draw position and extends of the background. </summary>
		protected Rect backgroundRect;

		/// <summary> True if method has parameters, false if not. </summary>
		private bool hasParameters;

		/// <summary> True if method is generic, false if not. </summary>
		private bool isGeneric;

		/// <summary> False if method return type is void, false if not. </summary>
		private bool hasReturnValue;

		/// <summary> True if method has been invoked and members containing its results have been generated, false if not. </summary>
		private bool hasResult;

		/// <summary> Holds the return value of the method if the invoke button was pressed. </summary>
		private object result;

		/// <summary> Holds the return values of the method if the invoke button was pressed with multiple selected targets. </summary>
		private object[] results;

		/// <summary> True if cursor is over invoke button at this moment. </summary>
		private bool mouseIsOverButton;

		private bool isCoroutine;

		private InspectorPreferences preferences;

		private MonoBehaviour monoBehaviour;

		#if DEV_MODE
		private bool nowInvoking = false;
		#endif

		private bool drawInSingleRow = true;

		/// <summary>
		/// LinkedMemberHierarchy for the results. This can be important for the functionality of some drawers, like ArrayDrawer.
		/// </summary>
		[CanBeNull]
		private LinkedMemberHierarchy resultMemberHierarchy;

		/// <inheritdoc/>
		public override Part MouseoveredPart
		{
			get
			{
				return mouseIsOverButton ? Part.Button : base.MouseoveredPart;
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

		/// <inheritdoc/>
		public override Rect ClickToSelectArea
		{
			get
			{
				return backgroundRect;
			}
		}

		/// <inheritdoc/>
		public override float HeaderHeight
		{
			get
			{
				return VerticalPadding + DrawGUI.SingleLineHeight + VerticalPadding;
			}
		}

		/// <inheritdoc/>
		protected override bool PrefixLabelClippedToColumnWidth
		{
			get
			{
				return Inspector.Preferences.enableTooltipIcons && label.tooltip.Length > 0;
			}
		}

		/// <summary> Gets or sets the generics drawers. </summary>
		/// <value> The generics drawers. </value>
		private GenericsDrawer GenericsDrawer
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(isGeneric, ToString());
				Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt, ToString());
				#endif
				return members[0] as GenericsDrawer;
			}
		}

		/// <summary> Gets or sets the parameter drawers. </summary>
		/// <value> The parameter drawers. </value>
		private ParameterDrawer ParameterDrawer
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(hasParameters, ToString());
				Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt, ToString());
				#endif

				return members[isGeneric ? 1 : 0] as ParameterDrawer;
			}
		}

		/// <summary> Gets or sets the result drawers. </summary>
		/// <value> The result drawers. </value>
		private IDrawer ResultDrawer
		{
			get
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(hasResult, ToString());
				Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt, ToString());
				#endif

				if(isGeneric)
				{
					if(hasParameters)
					{
						return members[2];
					}
					return members[1];
				}
				if(hasParameters)
				{
					return members[1];
				}
				return members[0];
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetPreferencesUrl("show-methods");
			}
		}


		/// <inheritdoc/>
		public override bool CanReadFromFieldWithoutSideEffects
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return memberInfo.MethodInfo.ReturnType;
			}
		}

		/// <inheritdoc/>
		public override object Value
		{
			get
			{
				#if DEV_MODE
				Debug.LogWarning(Msg(ToString(), ".Value.get called with hasResult=", hasResult, ", hasReturnValue=", hasReturnValue));
				#endif

				if(!hasResult)
				{
					return Type.DefaultValue();
				}
				
				return result;
			}

			set { CantSetValueError(); }
		}

		/// <summary> Gets information describing the method. </summary>
		/// <value> Information describing the method. </value>
		[NotNull]
		private MethodInfo MethodInfo
		{
			get
			{
				return memberInfo.MethodInfo;
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

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="methodInfo"> LinkedMemberInfo of the method that the drawers represent. </param>
		/// <param name="parent"> The parent drawers of this member. Can be null. </param>
		/// <param name="label"> The label. </param>
		/// <param name="setReadOnly"> True if drawer should be read only. </param>
		/// <returns> The newly-created instance. </returns>
		[NotNull]
		public static MethodDrawer Create([NotNull]LinkedMemberInfo methodInfo, [CanBeNull]IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			#if GENERIC_METHODS_NOT_SUPPORTED
			if(fieldInfo.MethodInfo.IsGenericMethod)
			{
				return null;
			}
			#endif
		
			MethodDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MethodDrawer();
			}
			result.Setup(methodInfo, parent, label, null, setReadOnly);
			result.LateSetup();
			return result;
		}
		
		/// <inheritdoc/>
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if UNITY_2019_2_OR_NEWER
			var inspectorName = setMemberInfo.GetAttribute<InspectorNameAttribute>();
			GUIContent setButtonLabel;
			if(inspectorName != null)
			{
				setButtonLabel = GUIContentPool.Create(inspectorName.displayName);
			}
			else
			{
				setButtonLabel = null;
			}
			Setup(setMemberInfo, setParent, setLabel, setButtonLabel, setReadOnly);
			#else
			Setup(setMemberInfo, setParent, setLabel, null, setReadOnly);
			#endif
		}

		/// <inheritdoc />
		protected sealed override void Setup(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setButtonText"> Text to shown on the button. </param>
		/// <param name="setValue"> The initial cached value of the drawers. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setLabel"> The prefix label to precede the button. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		protected virtual void Setup([NotNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setPrefixLabel, [CanBeNull]GUIContent setButtonLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setMemberInfo != null);
			#endif

			result = null;
			hasResult = false;
			
			preferences = setParent != null ? setParent.Inspector.Preferences : InspectorUtility.Preferences;

			if(setMemberInfo == null)
			{
				Debug.LogError("Null fieldInfo detected for \""+(setPrefixLabel != null ? setPrefixLabel.text : "")+"\"");
				return;
			}
			
			var methodInfo = setMemberInfo.MethodInfo;

			if(methodInfo == null)
			{
				Debug.LogError("Null MethodInfo detected for \""+(setPrefixLabel != null ? setPrefixLabel.text : "")+ "\" / " +setMemberInfo.Name);
				return;
			}

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log("MethodDrawer.Setup with setPrefixLabel=" + StringUtils.ToString(setPrefixLabel)+ ", setButtonLabel="+StringUtils.ToString(setButtonLabel) +", Type.ToString()=" + setMemberInfo.Type+", Type.Name="+setMemberInfo.Type.Name+", ToString(Type)="+StringUtils.ToString(setMemberInfo.Type));
			#endif

			memberInfo = setMemberInfo;
			
			hasParameters = methodInfo.GetParameters().Length > 0;
			isGeneric = methodInfo.IsGenericMethod;
			hasReturnValue = methodInfo.ReturnType != Types.Void;
			isCoroutine = methodInfo.ReturnType == Types.IEnumerator;
			monoBehaviour = UnityObject as MonoBehaviour;

			if(setButtonLabel == null)
			{
				setButtonLabel = isCoroutine ? preferences.labels.startCoroutine : preferences.labels.invokeMethod;
			}
			buttonLabel = setButtonLabel;

			if(InvokeMethodUtility.IsDisabled(setMemberInfo.UnityObjects, methodInfo))
			{
				#if DEV_MODE
				Debug.LogWarning("Setting MethodDrawer("+methodInfo.Name+") to ReadOnly because InvokeMethodUtility.IsDisabled returned "+StringUtils.True);
				#endif

				setReadOnly = true;
			}

			base.Setup(null, DrawerUtility.GetType<object>(setMemberInfo, null), setMemberInfo, setParent, setPrefixLabel, setReadOnly);

			// Make count be at least 1, so it works with static classes that have no targets
			int count = Mathf.Max(MemberHierarchy.TargetCount, 1);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(count >= 1);
			#endif

			ArrayPool<object>.Resize(ref results, count);

			for(int n = count - 1; n >= 0; n--)
			{
				results[n] = null;
			}

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(results.Length == count);
			Debug.Assert(results.Length >= 1);
			#endif

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log(ToString()+".Setup()");
			#endif
		}

		/// <inheritdoc/>
		protected override void OnAfterMemberBuildListGenerated()
		{
			UpdateDrawInSingleRow();
			base.OnAfterMemberBuildListGenerated();
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(object setValue, bool applyToField, bool updateMembers)
		{
			CantSetValueError();
			return false;
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc/>
		protected override bool ResetOnDoubleClick()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE && DEBUG_BUILD_MEMBERS
			Debug.Log(Msg(ToString(), ".DoBuildMembers with hasResult=", hasResult, ", hasParameters=", hasParameters, ", isGeneric=", isGeneric));
			#endif

			if(!hasResult && !hasParameters && !isGeneric)
			{
				DrawerArrayPool.Resize(ref members, 0);
			}
			else
			{
				int size = 0;
				if(hasResult)
				{
					size++;
				}
				if(isGeneric)
				{
					size++;
				}
				if(hasParameters)
				{
					size++;
				}
				DrawerArrayPool.Resize(ref members, size);
				
				bool readOnly = ReadOnly;

				int index = 0;
				if(isGeneric)
				{
					members[0] = GenericsDrawer.Create(memberInfo, this, GUIContentPool.Create("Generics"), readOnly);
					index++;
				}

				if(hasParameters)
				{
					members[index] = ParameterDrawer.Create(MethodInfo.GetParameters(), memberInfo, this, GUIContentPool.Create("Parameters"), readOnly);
					index++;
				}

				if(hasResult)
				{
					string tooltip = LinkedMemberInfo.TooltipDatabase.GetTooltipFromParent(MethodInfo.ReturnParameter, memberInfo, "Returns");
					if(tooltip.Length == 0)
					{
						tooltip = "Value returned by method.";
					}

					var resultMemberInfo = resultMemberHierarchy.Get(null, typeof(MethodDrawer).GetField("result", BindingFlags.Instance | BindingFlags.NonPublic), LinkedMemberParent.ClassInstance, null);
					members[index] = DrawerProvider.GetForField(result, Type, resultMemberInfo, this, GUIContentPool.Create("Result", tooltip), readOnly);
				}
			}
		}

		/// <inheritdoc/>
		public override bool SetValue(object newValue)
		{
			CantSetValueError();
			return false;
		}

		/// <inheritdoc/>
		public override object GetValue(int index)
		{
			string error;
			var resultForTarget = GetValue(index, out error);
			if(error.Length > 0)
			{
				InspectorUtility.ActiveInspector.Message("Invoke Method "+error, null, MessageType.Error);
			}
			return resultForTarget;
		}

		/// <summary>
		/// Invokes the Method, returns method return value and outputs possible error descriptions encountered.
		/// </summary>
		/// <param name="index"> Index of target on which to invoke method. </param>
		/// <param name="error"> [out] True if should display message to user about invoking or possible exceptions. </param>
		protected object GetValue(int index, [NotNull]out string error)
		{
			// generics and parameter info is needed from members
			// so build them if they have not yet been built
			// (i.e. if MethodDrawer has yet to be unfolded)
			if(memberBuildState == MemberBuildState.BuildListGenerated)
			{
				BuildMembers();
			}

			bool runAsCoroutine;

			MethodInfo methodInfo;
			if(isGeneric)
			{
				runAsCoroutine = false;

				try
				{
					var genericTypes = GenericsDrawer.Value;

					#if DEV_MODE && DEBUG_INVOKE
					Debug.Log("Making generic method of "+MethodInfo.Name+" from "+genericTypes.Length+" generic types: "+StringUtils.ToString(genericTypes));
					#endif

					//needed?
					if(genericTypes.Length == 1)
					{
						methodInfo = MethodInfo.MakeGenericMethod(genericTypes[0]);
					}
					//needed?
					else if(genericTypes.Length == 2)
					{
						methodInfo = MethodInfo.MakeGenericMethod(genericTypes[0], genericTypes[1]);
					}
					else
					{
						methodInfo = MethodInfo.MakeGenericMethod(genericTypes);
					}
				}
				catch(Exception e)
				{
					if(ExitGUIUtility.ShouldRethrowException(e))
					{
						throw;
					}
					error = e.ToString();
					return DefaultValue();
				}
			}
			else
			{
				try
				{
					methodInfo = MethodInfo;
				}
				catch(Exception e)
				{
					error = e.ToString();
					return DefaultValue();
				}

				if(isCoroutine)
				{
					if(!Inspector.Preferences.askAboutStartingCoroutines)
					{
						runAsCoroutine = true;
					}
					else
					{
						switch(DrawGUI.Active.DisplayDialog("Invoke as coroutine?", "The method return type is IEnumerable. Would you like to start it as a coroutine?", "Start As Coroutine", "Invoke As Normal Method", "Cancel"))
						{
							case 0:
								runAsCoroutine = true;
								break;
							case 1:
								runAsCoroutine = false;
								break;
							case 2:
								ExitGUIUtility.ExitGUI();
								error = "";
								return result;
							default:
								#if DEV_MODE
								throw new IndexOutOfRangeException();
								#else
								runAsCoroutine = false;
								break;
								#endif
						}
					}
				}
				else
				{
					runAsCoroutine = false;
				}

			}

			object returnValue;
			
			var parameters = hasParameters ? ParameterDrawer.Value : null;

			#if DEV_MODE && PI_ASSERTATIONS
			if(hasParameters && (parameters == null || parameters.Length == 0)) { Debug.LogError("hasParameters was "+StringUtils.True+" but params was "+StringUtils.ToString(parameters)); }
			#endif

			try
			{
				#if DEV_MODE && DEBUG_INVOKE
				Debug.Log("Invoking method "+methodInfo.Name+" with "+(parameters == null ? "" : parameters.Length+" ")+"params="+StringUtils.ToString(parameters)+", hasParameters="+StringUtils.ToColorizedString(hasParameters)+ ", runAsCoroutine="+ runAsCoroutine);
				#endif

				// this can be null for static methods
				var fieldOwner = memberInfo.GetFieldOwner(index);

				//get return value by invoking method with current parameter values
				returnValue = methodInfo.Invoke(fieldOwner, parameters);

				if(runAsCoroutine)
				{
					#if UNITY_EDITOR
					if(!Application.isPlaying)
					{
						StaticCoroutine.StartCoroutineInEditMode((IEnumerator)returnValue);
					}
					else if(monoBehaviour != null)
					#else
					if(monoBehaviour != null)
					#endif
					{
						monoBehaviour.StartCoroutine((IEnumerator)returnValue);
					}
					else
					{
						StaticCoroutine.StartCoroutine((IEnumerator)returnValue);
					}
				}

				if(hasParameters)
				{
					//update parameter Drawer with values of parameters so that changes made to parameters
					//(e.g. via ref and out) get shown in the ParameterDrawer
					var paramMembers = ParameterDrawer.Members;
					int paramMembersCount = paramMembers.Length;
					for(int n = ParameterDrawer.parameterInfos.Length - 1; n >= 0; n--)
					{
						var paramInfo = ParameterDrawer.parameterInfos[n];
						if(paramInfo.ParameterType.IsByRef && paramMembersCount > n)
						{
							var memb = ParameterDrawer.Members[n];
							if(memb.Type == paramInfo.ParameterType.GetElementType())
							{
								#if DEV_MODE && DEBUG_INVOKE
								Debug.Log("param #"+n+" \""+memb.Name+"\" value: "+StringUtils.ToString(parameters[n]));
								#endif
								memb.SetValue(parameters[n]);
							}
						}
					}
				}
			}
			catch(Exception e)
			{
				if(Inspector.Preferences.messageDisplayMethod == MessageDisplayMethod.None)
				{
					Debug.LogError(e);
				}
				else
				{
					InspectorUtility.ActiveInspector.Message(e.ToString(), null, MessageType.Error);
				}
				returnValue = DefaultValue();
			}

			#if CALL_ON_VALIDATE
			if(!methodInfo.IsStatic && methodInfo.GetCustomAttributes(typeof(PureAttribute), false).Length == 0)
			{
				OnValidateHandler.CallForTargets(UnityObjects);
			}
			#endif

			error = "";
			return returnValue;
		}

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively() { }

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard() { CantSetValueError(); }

		/// <inheritdoc/>
		protected override void DoReset() { CantSetValueError(); }

		/// <summary> Cant set value error. </summary>
		private void CantSetValueError(){ Debug.LogError("Button value can't be changed"); }
		
		/// <summary>
		/// GUIStyle for the button.
		/// </summary>
		protected virtual GUIStyle Style
		{
			get
			{
				return InspectorPreferences.Styles.MiniButton;
			}
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			lastDrawPosition = position;
			lastDrawPosition.height = HeaderHeight;

			labelLastDrawPosition = lastDrawPosition;
			labelLastDrawPosition.y += VerticalPadding;
			labelLastDrawPosition.height -= VerticalPadding + VerticalPadding;

			DrawGUI.AddMarginsAndIndentation(ref labelLastDrawPosition);

			if(!DrawInSingleRow)
			{
				const float foldoutArrowSize = 12f;
				labelLastDrawPosition.x -= foldoutArrowSize;
				labelLastDrawPosition.width += foldoutArrowSize;
			}
			
			buttonRect = labelLastDrawPosition;
			buttonRect.width = DrawGUI.MinControlFieldWidth - DrawGUI.RightPadding - DrawGUI.MiddlePadding - 1f;
			buttonRect.x = lastDrawPosition.xMax - buttonRect.width - DrawGUI.RightPadding - 2f;
			buttonRect.y += 1f;
			buttonRect.height = 18f;

			labelLastDrawPosition.width = buttonRect.x - labelLastDrawPosition.x;

			backgroundRect = lastDrawPosition;
			backgroundRect.x += DrawGUI.RightPadding;
			backgroundRect.width -= DrawGUI.RightPadding + DrawGUI.RightPadding;
			backgroundRect.y += 1f;
			backgroundRect.height -= 2f;

			if(DrawInSingleRow)
			{
				bodyLastDrawPosition = buttonRect;
			}
			else
			{
				bodyLastDrawPosition = lastDrawPosition;
				bodyLastDrawPosition.y += lastDrawPosition.height;
			}

			localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
		}

		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			if(Event.current.type == EventType.Repaint)
			{
				//hide prefix resizer line behind the field
				EditorGUI.DrawRect(lastDrawPosition, DrawGUI.Active.InspectorBackgroundColor);
			
				if(backgroundRect.width > 0f)
				{
					GUI.Label(backgroundRect, "", InspectorPreferences.Styles.MethodBackground);
				}
			}
			
			bool drawBackgroundBehindFoldoutsWas = DrawGUI.drawBackgroundBehindFoldouts;
			DrawGUI.drawBackgroundBehindFoldouts = false;
			
			bool dirty = base.DrawPrefix(PrefixLabelPosition);
			
			DrawGUI.drawBackgroundBehindFoldouts = drawBackgroundBehindFoldoutsWas;
	
			if(DrawGUI.Active.Button(buttonRect, buttonLabel, Style))
			{
				DrawGUI.UseEvent();
				if(Event.current.button == 0)
				{
					dirty = true;
					Select(ReasonSelectionChanged.ControlClicked);
					Invoke();
				}
				else if(Event.current.button == 1)
				{
					var menu = Menu.Create();

					menu.Add(Menu.Item("Invoke", ()=>Invoke(false, false, false, false)));

					if(hasReturnValue)
					{
						#if !POWER_INSPECTOR_LITE
						menu.AddSeparator();
						menu.Add(Menu.Item("Copy Return Value", ()=>Invoke(false, false, true, false)));
						#endif
						if(UnityObjectExtensions.IsUnityObjectOrUnityObjectCollectionType(Type))
						{
							menu.Add(Menu.Item("Ping Return Value", ()=>Invoke(true, false, false, false)));
							menu.Add(Menu.Item("Select Return Value", ()=>Invoke(false, false, false, true)));
						}
					}

					if(isCoroutine)
					{
						bool addedSeparator = false;

						var monoBehaviour = UnityObject as MonoBehaviour;
						string methodName = MethodInfo.Name;

						if(monoBehaviour != null && Application.isPlaying)
						{
							menu.AddSeparator();
							addedSeparator = true;

							menu.Add("Invoke Repeating/Every Second", ()=>monoBehaviour.InvokeRepeating(methodName, 1f, 1f));
							menu.Add("Invoke Repeating/Every 5 Seconds", ()=>monoBehaviour.InvokeRepeating(methodName, 5f, 5f));
							menu.Add("Invoke Repeating/Every 10 Seconds", ()=>monoBehaviour.InvokeRepeating(methodName, 10f, 10f));
						}

						if(IsInvoking())
						{
							if(!addedSeparator)
							{
								menu.AddSeparator();
							}

							menu.Add("Stop Coroutine", ()=>monoBehaviour.StopCoroutine(methodName));
							menu.Add("Cancel Invoke", ()=>monoBehaviour.CancelInvoke(methodName));
						}
					}

					ContextMenuUtility.Open(menu, this);
				}
				else if(Event.current.button == 2)
				{
					Invoke(true, false, true, false);
				}
			}

			if(IsInvoking())
			{
				var buttonUnderlineRect = buttonRect;
				buttonUnderlineRect.x += 1f;
				buttonUnderlineRect.y += buttonUnderlineRect.height - 3f;
				buttonUnderlineRect.width -= 2f;
				buttonUnderlineRect.height = 2f;
				DrawGUI.DrawRect(buttonUnderlineRect, Color.green);
			}

			return dirty;
		}

		/// <inheritdoc/>
		public override void DrawSelectionRect()
		{
			base.DrawSelectionRect();

			if(IsInvoking())
			{
				return;
			}

			var rect = buttonRect;
			rect.x += 1f;
			rect.width -= 2f;
			rect.height -= 1f;
			DrawGUI.DrawControlSelectionIndicator(rect, localDrawAreaOffset);
		}

		/// <summary>
		/// Returns value indicating whether or not method is a coroutine and is currently invoking (started through InvokeRepeating).
		/// </summary>
		/// <returns> True if method is coroutine that is currently invoking. </returns>
		private bool IsInvoking()
		{
			if(!isCoroutine)
			{
				return false;
			}
			if(monoBehaviour != null)
			{
				return monoBehaviour.IsInvoking(memberInfo.MethodInfo.Name);
			}
			return StaticCoroutine.IsInvoking(memberInfo.MethodInfo.Name);
		}

		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			if(Cursor.CanRequestLocalPosition)
			{
				mouseIsOverButton = buttonRect.MouseIsOver();
			}
			base.OnLayoutEvent(position);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			menu.Add("Invoke", Invoke);
			menu.AddSeparator();
			menu.Add("Copy", CopyToClipboard);
			AddMenuItemsFromAttributes(ref menu);
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);

			menu.Add("Debugging/RebuildIntructionsInChildren", RebuildMembers);
			menu.Add("Debugging/List Members", DebugListMembers);
		}
		#endif

		/// <inheritdoc/>
		public override void OnMouseover()
		{
			if(mouseIsOverButton)
			{
				DrawGUI.DrawMouseoverEffect(buttonRect, localDrawAreaOffset);
				DrawGUI.Active.SetCursor(MouseCursor.Link);
			}
			else if(preferences.mouseoverEffects.prefixLabel)
			{
				var rect = labelLastDrawPosition;
				rect.y += 1f;
				rect.height -= 2f;
				DrawGUI.DrawLeftClickAreaMouseoverEffect(rect, localDrawAreaOffset);
			}
		}

		/// <inheritdoc/>
		public override bool OnClick(Event inputEvent)
		{
			#if DEV_MODE
			Debug.Log(ToString() + ".OnClick with mouseIsOverButton=" + mouseIsOverButton);
			#endif

			if(mouseIsOverButton)
			{
				HandleOnClickSelection(inputEvent, ReasonSelectionChanged.ControlClicked);
				DrawGUI.Use(inputEvent);
				GUI.changed = true;
				Invoke();
				ExitGUIUtility.ExitGUI();
				return true;
			}

			return base.OnClick(inputEvent);
		}

		/// <inheritdoc/>
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(DrawGUI.EditingTextField)
			{
				return false;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					GUI.changed = true;
					DrawGUI.Use(inputEvent);
					Invoke();
					return true;
				default:
					return base.OnKeyboardInputGiven(inputEvent, keys);
			}
		}

		/// <summary>
		/// Invokes the Method, messages the user about it and updates results member if method has a return value.
		/// </summary>
		/// <param name="pingIfUnityObject"> True if should "ping" the return value, if the method has one, and it's of type UnityEngine.Object  or UnityEngine.Object[], and not null or empty. </param>
		/// <param name="displayDialogIfNotUnityObject"> True if should display a popup dialog to the user about the results, if method has a return type, and it's not of type UnityEngine.Object or UnityEngine.Object[]. </param>
		/// <param name="copyToClipboardIfNotPingedOrDialogShown"> True if should copy the method return value to clipboard, if it has one, and if value was not pinged and popup was not displayed. </param>
		///  <param name="pingIfUnityObject"> True if should select the return value(s), if the method has one, and it's of type UnityEngine.Object  or UnityEngine.Object[], and not null or empty. </param>
		private void Invoke(bool pingIfUnityObject, bool displayDialogIfNotUnityObject, bool copyToClipboardIfNotPingedOrDialogShown, bool selectIfUnityObject)
		{
			Inspector.RefreshView(); // make sure repaint gets called immediately

			string error;
			bool suppressMessages = pingIfUnityObject || displayDialogIfNotUnityObject || copyToClipboardIfNotPingedOrDialogShown || selectIfUnityObject;
			Invoke(out error, suppressMessages);

			if(!hasReturnValue)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!suppressMessages);
				#endif
				return;
			}

			if(pingIfUnityObject)
			{
				var list = new List<Object>(0);
				if(UnityObjectExtensions.TryExtractObjectReferences(results, ref list))
				{
					DrawGUI.Ping(list.ToArray());
				}
			}

			if(selectIfUnityObject)
			{
				var list = new List<Object>(0);
				if(UnityObjectExtensions.TryExtractObjectReferences(results, ref list))
				{
					Inspector.Select(list.ToArray());
				}
			}

			if(displayDialogIfNotUnityObject && !isCoroutine)
			{
				TryDisplayDialogForResult();
			}
			else if(copyToClipboardIfNotPingedOrDialogShown)
			{
				Clipboard.Copy(result);
			}
			
			if(Event.current != null)
			{
				ExitGUIUtility.ExitGUI(); // avoid ArgumentException: Getting control 1's position in a group with only 1 controls when doing repaint
			}
		}

		private bool TryDisplayDialogForResult()
		{
			if(!hasReturnValue || isCoroutine)
			{
				return false;
			}

			if(!hasResult)
			{
				Invoke();
			}

			string resultToString = StringUtils.ToStringCompact(result);
			string messageBody = "Method Result:\n"+resultToString;
			if(!DrawGUI.Active.DisplayDialog(string.Concat(Name, " Result"), messageBody, "Ok", "Copy To Clipboard"))
			{
				TryCopyResultToClipboard();
			}
			return true;
		}

		private bool TryPingResult()
		{
			if(!hasReturnValue)
			{
				return false;
			}

			if(!hasResult)
			{
				Invoke();
			}

			var list = new List<Object>(0);
			if(UnityObjectExtensions.TryExtractObjectReferences(results, ref list))
			{
				DrawGUI.Ping(list.ToArray());
			}
			return true;
		}

		private bool TryCopyResultToClipboard()
		{
			if(!hasReturnValue)
			{
				return false;
			}

			if(!hasResult)
			{
				Invoke();
			}

			try
			{
				Clipboard.Copy(result);
				SendCopyToClipboardMessage();
				return true;
			}
			catch
			{
				SendCopyToClipboardMessage();
				return false;
			}
		}

		/// <summary>
		/// Invokes the Method, messages the user about it and updates results member if method has a return value.
		/// </summary>
		private void Invoke()
		{
			string error;
			Invoke(out error, false);
		}

		/// <summary>
		/// Invokes the Method, messages the user about it and updates results member if method has a return value.
		/// </summary>
		/// <param name="error"> [out] True if should display message to user about invoking or possible exceptions. </param>
		/// <param name="suppressMessages"> True if should display message to user about invoking or possible exceptions. </param>
		private bool Invoke(out string error, bool suppressMessages)
		{
			#if DEV_MODE
			if(nowInvoking) { Debug.LogWarning(ToString() + ".Invoke called with nowInvoking already true."); }
			#endif

			error = "";

			// UPDATE: Temporarily lock the view, so that this drawer won't be disposed even if invoked methods e.g. change the selected objects?
			var inspectorState = Inspector.State;
			var viewWasLocked = inspectorState.ViewIsLocked;
			inspectorState.ViewIsLocked = true;

			#if DEV_MODE
			nowInvoking = true;
			#endif

			bool abort = false;

			if(hasParameters)
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					BuildMembers();
				}
				var parameterDrawer = ParameterDrawer;
				if(!parameterDrawer.DataIsValid)
				{
					#if DEV_MODE
					Debug.LogWarning(parameterDrawer+".DataIsValid: "+StringUtils.False);
					#endif
				
					if(!DrawGUI.Active.DisplayDialog("Invalid Parameters", "Method \""+Name+"\" has invalid parameters. Are you sure you still want to invoke it?", "Invoke", "Cancel"))
					{
						error = "Aborted by user.";
						abort = true;
					}
				}
			}

			if(!abort)
			{
				KeyboardControlUtility.KeyboardControl = 0;
				DrawGUI.EditingTextField = false;
			
				if(hasReturnValue && !isCoroutine)
				{
					// Make count be at least 1, so it works with static classes that have no targets
					int count = Mathf.Max(1, MemberHierarchy.TargetCount);

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(count >= 1);
					#endif

					for(int n = count - 1; n >= 0; n--)
					{
						results[n] = GetValue(n, out error);
					}
					result = results[0];

					if(resultMemberHierarchy != null)
					{
						resultMemberHierarchy.DisposeMembers();
					}
					else
					{
						resultMemberHierarchy = LinkedMemberHierarchy.GetForClass(this);
					}

					#if DEV_MODE
					Debug.Log(ToString()+".hasResult = "+StringUtils.True);
					#endif
					hasResult = true;
				
					if(!suppressMessages)
					{
						if(error.Length > 0)
						{
							InspectorUtility.ActiveInspector.Message(string.Concat(Name,  " ", error), null, MessageType.Error, true);
						}
						else
						{
							InspectorUtility.ActiveInspector.Message(MakeMessage(string.Concat(Name,  " result: ", StringUtils.ToString(result))));
						}
					}

					RebuildMembers();
				}
				else
				{
					GetValue(0, out error);
					if(!suppressMessages)
					{
						if(error.Length > 0)
						{
							InspectorUtility.ActiveInspector.Message(string.Concat(Name,  " ", error), null, MessageType.Error, true);
						}
						else
						{
							InspectorUtility.ActiveInspector.Message(MakeMessage(string.Concat(Name,  isCoroutine ? " started." : " invoked.")));
						}
					}
				}
			}

			#if DEV_MODE
			nowInvoking = false;
			#endif

			UpdateDrawInSingleRow();

			// Release inspector view.
			if(inspectorState.ViewIsLocked && !viewWasLocked)
			{
				inspectorState.ViewIsLocked = false;
			}

			return error.Length == 0;
		}

		private GUIContent MakeMessage(string text)
		{
			return new GUIContent(text);
		}

		/// <inheritdoc />
		protected override void UpdateDataValidity(bool evenIfCanHaveSideEffects)
		{
			if(hasResult)
			{
				base.UpdateDataValidity(true);
			}
		}

		/// <inheritdoc />
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			HasUnappliedChanges = true;
			UpdateDataValidity(true);

			// don't send OnMemberValueChanged up the parent chain since
			// value changes in Parameters "don't count"
			// (they are not field backed and we know that changing them will
			//  have zero consequences up from this point)
		}

		/// <inheritdoc/>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}

		/// <inheritdoc cref="IDrawer.CopyToClipboard" />
		public override void CopyToClipboard()
		{
			if(!hasReturnValue)
			{
				InspectorUtility.ActiveInspector.Message(Clipboard.MakeInvalidOperationMessage(Name + " has no return value."), null, MessageType.Info, false);
				return;
			}

			base.CopyToClipboard();
		}

		/// <inheritdoc/>
		protected override string GetCopyToClipboardMessage()
		{
			if(MemberHierarchy.TargetCount > 1)
			{
				return "Copied{0} return values";
			}
			return "Copied{0} return value";
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			drawInSingleRow = true;

			#if DEV_MODE
			if(nowInvoking) { Debug.LogWarning(ToString() + ".Dispose called with nowInvoking true."); }
			#endif

			if(resultMemberHierarchy != null)
			{
				resultMemberHierarchy.DisposeMembers();
			}

			base.Dispose();
		}

		protected void UpdateDrawInSingleRow()
		{
			bool setDrawInSingleRow = !isGeneric && !hasParameters && !hasResult;
			if(setDrawInSingleRow != drawInSingleRow)
			{
				drawInSingleRow = setDrawInSingleRow;
				UpdatePrefixDrawer();
				GUI.changed = true;
				Inspector.InspectorDrawer.Repaint();
				if(setDrawInSingleRow && !Unfolded)
				{
					SetUnfolded(true, false);
				}
			}
		}

		/// <inheritdoc/>
		protected override bool TryGetSingleValueVisualizedInInspector(out object visualizedValue)
		{
			visualizedValue = hasResult ? result : null;
			return true;
		}
	}
}