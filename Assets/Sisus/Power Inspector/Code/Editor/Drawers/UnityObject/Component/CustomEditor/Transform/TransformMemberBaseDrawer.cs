//#define ENABLE_WORLD_SPACE_TOOLTIPS
//#define DEBUG_PREFIX_DRAGGED
//#define DEBUG_SNAPPING
#define DEBUG_UPDATE_CACHED_VALUES

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Sisus
{
	[Serializable]
	public abstract class TransformMemberBaseDrawer : ParentFieldDrawer<Vector3>, ISnappable, IDraggablePrefixAffectsMember<Vector3>
	{
		/// <summary>
		/// True if field infos generated.
		/// </summary>
		private static bool fieldInfosGenerated;

		/// <summary>
		/// The field information for the x field.
		/// </summary>
		private static FieldInfo fieldInfoX;

		/// <summary>
		/// The field information for the y field.
		/// </summary>
		private static FieldInfo fieldInfoY;

		/// <summary>
		/// The field information for the z field.
		/// </summary>
		private static FieldInfo fieldInfoZ;

		/// <summary> True if any draggable members are currently visible. </summary>
		private bool draggableMemberVisible;

		/// <inheritdoc cref="IParentDrawer.DrawInSingleRow" />
		public sealed override bool DrawInSingleRow
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc />
		public bool DraggingPrefix
		{
			get
			{
				return this == InspectorUtility.ActiveManager.MouseDownInfo.MouseDownOverDrawer && draggableMemberVisible;
			}
		}

		/// <inheritdoc />
		public abstract bool SnappingEnabled { get; set; }

		/// <inheritdoc />
		public abstract float GetSnapStep(int memberIndex = -1);

		private bool DrawSnapIcon
		{
			get
			{
				return UserSettings.Snapping.Enabled;
			}
		}
		
		/// <summary>
		/// Gets the zero-based index of the members whose values are modified by prefix dragging action.
		/// </summary>
		/// <value> Indexes in members array. </value>
		protected abstract int[] DraggingTargetsMembers { get; }

		/// <inheritdoc/>
		protected sealed override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return false;
			}
		}

		private Rect SnappingIconPosition
		{
			get
			{
				var rect = labelLastDrawPosition;
				rect.x += rect.width + DrawGUI.MiddlePadding + DrawGUI.MiddlePadding;
				rect.width = 18f;
				rect.height = 18f;
				return rect;
			}
		}

		/// <inheritdoc/>
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("transform-drawer");
			}
		}
		
		#if ENABLE_WORLD_SPACE_TOOLTIPS
		protected override void Setup(Vector3 setValue, LinkedMemberInfo setFieldInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			base.Setup(setValue, setFieldInfo, setParent, setLabel, setReadOnly);
			if(Mouseovered)
			{
				UpdateTooltips();
			}
		}
		#elif DEV_MODE && PI_ASSERTATIONS
		protected override void Setup(Vector3 setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			// setMemberInfo should only be null for ScaleDrawer when UsingLocalSpace is false
			if(setMemberInfo == null)
			{
				if((Inspector.State.usingLocalSpace || GetType() != typeof(ScaleDrawer)))
				{
					Debug.LogError(ToString()+".DoGenerateMemberBuildList called with setMemberInfo "+StringUtils.Null+" with UsingLocalSpace "+StringUtils.ToColorizedString(Inspector.State.usingLocalSpace));
				}
			}
			else
			{
				Debug.Assert(setMemberInfo.SerializedProperty != null);

				if(!Inspector.State.usingLocalSpace && GetType() == typeof(ScaleDrawer))
				{
					Debug.LogError(ToString()+".DoGenerateMemberBuildList called with setMemberInfo "+setMemberInfo+" with UsingLocalSpace "+StringUtils.ToColorizedString(Inspector.State.usingLocalSpace));
				}
			}
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}
		#endif
		
		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			if(!ReadOnly)
			{
				menu.AddSeparatorIfNotRedundant();
				
				menu.Add("Snapping/Off", DisableSnapping, SnappingEnabled);
				menu.Add("Snapping/On", EnableSnapping, SnappingEnabled);
				
				menu.Add("Snap", Snap);
			}
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			var resetIndex = menu.IndexOf("Reset");
			if(resetIndex != -1)
			{
				menu.Insert(resetIndex + 1, "Reset Without Affecting Children", ResetWithoutAffectingChildren);
			}
		}

		private void ResetWithoutAffectingChildren()
		{
			var transform = Transform;
			var temp = (new GameObject()).transform;
			temp.parent = transform.parent;
			temp.localPosition = transform.localPosition;
			temp.localEulerAngles = transform.localEulerAngles;
			temp.localScale = transform.localScale;

			int childCount = transform.childCount;
			var children = ArrayPool<Transform>.Create(childCount);
			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = transform.GetChild(n);
				children[n] = transform.GetChild(n);
				child.parent = temp;
			}

			DoReset();
			
			for(int n = childCount - 1; n >= 0; n--)
			{
				var child = temp.GetChild(n);
				child.parent = transform;
			}

			ArrayPool<Transform>.Dispose(ref children);

			Platform.Active.Destroy(temp.gameObject);
		}

		/// <inheritdoc/>
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use Create method.");
		}

		/// <summary>
		/// Generates FieldInfos for members.
		/// This should be called at least once before DoGenerateMemberBuildList is called.
		/// </summary>
		private static void GenerateMemberInfos()
		{
			if(!fieldInfosGenerated)
			{
				fieldInfosGenerated = true;
				fieldInfoX = Types.Vector3.GetField("x", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoY = Types.Vector3.GetField("y", BindingFlags.Instance | BindingFlags.Public);
				fieldInfoZ = Types.Vector3.GetField("z", BindingFlags.Instance | BindingFlags.Public);
			}
		}

		/// <inheritdoc />
		protected sealed override void DoGenerateMemberBuildList()
		{
			GenerateMemberInfos();
			
			var hierarchy = MemberHierarchy;
			
			//UPDATE: Don't add LinkedMemberInfos if memberInfo is missing, to avoid lossy scale fields being readonly!
			if(memberInfo != null)
			{
				memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoX, LinkedMemberParent.LinkedMemberInfo, "x"));
				memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoY, LinkedMemberParent.LinkedMemberInfo, "y"));
				memberBuildList.Add(hierarchy.Get(memberInfo, fieldInfoZ, LinkedMemberParent.LinkedMemberInfo, "z"));
			}

			#if DEV_MODE && PI_ASSERTATIONS
			// memberInfo should only be null for ScaleDrawer when UsingLocalSpace is false
			if(memberInfo == null)
			{
				if((Inspector.State.usingLocalSpace || GetType() != typeof(ScaleDrawer)))
				{
					Debug.LogError(ToString()+".DoGenerateMemberBuildList called with memberInfo "+StringUtils.Null+" with UsingLocalSpace "+StringUtils.ToColorizedString(Inspector.State.usingLocalSpace));
				}
				Debug.Assert(memberBuildList.Count == 0);
			}
			else
			{
				if(!Inspector.State.usingLocalSpace && GetType() == typeof(ScaleDrawer))
				{
					Debug.LogError(ToString()+".DoGenerateMemberBuildList called with memberInfo "+memberInfo+" with UsingLocalSpace "+StringUtils.ToColorizedString(Inspector.State.usingLocalSpace));
				}
				Debug.Assert(memberBuildList.Count == 3);
			}
			Debug.Assert(fieldInfoX != null);
			Debug.Assert(fieldInfoY != null);
			Debug.Assert(fieldInfoZ != null);
			#endif
		}

		/// <inheritdoc />
		protected sealed override void DoBuildMembers()
		{
			var first = Value;
			Array.Resize(ref members, 3);

			var labels = Preferences.labels;
			TransformFloatDrawer x, y, z;

			bool readOnly = ReadOnly;

			if(memberBuildList.Count == 3)
			{
				x = TransformFloatDrawer.Create(first.x, memberBuildList[0], this, labels.X, readOnly);
				y = TransformFloatDrawer.Create(first.y, memberBuildList[1], this, labels.Y, readOnly);
				z = TransformFloatDrawer.Create(first.z, memberBuildList[2], this, labels.Z, readOnly);
			}
			else
			{
				x = TransformFloatDrawer.Create(first.x, null, this, labels.X, readOnly);
				y = TransformFloatDrawer.Create(first.y, null, this, labels.Y, readOnly);
				z = TransformFloatDrawer.Create(first.z, null, this, labels.Z, readOnly);
			}
			
			var firstTransform = Transform;
			if(firstTransform != null && firstTransform.IsPrefabInstance())
			{
				Func<bool> hasUnappliedChanges = ()=>
				{
					var mods = PropertyModifications;
					if(mods != null)
					{
						for(int n = mods.Length - 1; n >= 0; n--)
						{
							if(string.Equals(mods[n].propertyPath, XPropertyPath()))
							{
								return true;
							}
						}
					}
					return false;
				};
				x.OverrideHasUnappliedChanges = hasUnappliedChanges;

				hasUnappliedChanges = ()=>
				{
					var mods = PropertyModifications;
					if(mods != null)
					{
						for(int n = mods.Length - 1; n >= 0; n--)
						{
							if(string.Equals(mods[n].propertyPath, YPropertyPath()))
							{
								return true;
							}
						}
					}
					return false;
				};
				y.OverrideHasUnappliedChanges = hasUnappliedChanges;

				hasUnappliedChanges = ()=>
				{
					var mods = PropertyModifications;
					if(mods != null)
					{
						for(int n = mods.Length - 1; n >= 0; n--)
						{
							if(string.Equals(mods[n].propertyPath, ZPropertyPath()))
							{
								return true;
							}
						}
					}
					return false;
				};
				z.OverrideHasUnappliedChanges = hasUnappliedChanges;
			}

			members[0] = x;
			members[1] = y;
			members[2] = z;
		}

		/// <summary> Gets serialized property path to "x" member. </summary>
		/// <returns> Relative property path. </returns>
		protected abstract string XPropertyPath();

		/// <summary> Gets serialized property path to "y" member. </summary>
		/// <returns> Relative property path. </returns>
		protected abstract string YPropertyPath();

		/// <summary> Gets serialized property path to "z" member. </summary>
		/// <returns> Relative property path. </returns>
		protected abstract string ZPropertyPath();
		
		/// <inheritdoc />
		public sealed override bool DrawPrefix(Rect position)
		{
			bool dirty = base.DrawPrefix(position);

			if(DrawSnapIcon)
			{
				var snapButtonRect = SnappingIconPosition;
				var settings = InspectorUtility.Preferences;
				var graphics = settings.graphics;

				if(Event.current.type == EventType.Repaint)
				{
					if(SnappingEnabled)
					{
						GUI.DrawTexture(snapButtonRect, graphics.SnappingOnIcon, ScaleMode.StretchToFill);
					}
					else
					{
						var guiColorWas = GUI.color;
						var setGuiColor = guiColorWas;
						setGuiColor.a = 0.5f;
						GUI.color = setGuiColor;
						GUI.DrawTexture(snapButtonRect, graphics.SnappingOffIcon, ScaleMode.StretchToFill);
						GUI.color = guiColorWas;
					}
				}

				if(DrawGUI.Active.Button(snapButtonRect, GUIContent.none, DrawGUI.prefixLabel))
				{
					if(Event.current.button == 0)
					{
						ToggleSnapping();
						return true;
					}
					if(Event.current.button == 1)
					{
						var menu = Menu.Create();
						menu.Insert(0, "Edit Snap Settings...", () => DrawGUI.ExecuteMenuItem("Edit/Snap Settings..."));
						ContextMenuUtility.OpenAt(menu, snapButtonRect, this, (Part)TransformHeaderPart.ToggleSnapToGridButton);
						return true;
					}
				}
			}

			return dirty;
		}

		protected void ToggleSnapping()
		{
			SnappingEnabled = !SnappingEnabled;
		}

		protected void EnableSnapping()
		{
			SnappingEnabled = true;
		}

		protected void DisableSnapping()
		{
			SnappingEnabled = false;
		}

		/// <inheritdoc />
		protected sealed override void GetDrawPositions(Rect position)
		{
			if(DrawSnapIcon)
			{
				lastDrawPosition = position;
				lastDrawPosition.height = HeaderHeight;

				lastDrawPosition.GetLabelAndControlRects(label, out labelLastDrawPosition, out bodyLastDrawPosition);

				// make room for the snapping icon
				bodyLastDrawPosition.x += DrawGUI.SingleLineHeight;
				bodyLastDrawPosition.width -= DrawGUI.SingleLineHeight;

				bodyLastDrawPosition.GetSingleRowControlRects(visibleMembers, ref memberRects);

				localDrawAreaOffset = DrawGUI.GetLocalDrawAreaOffset();
				return;
			}

			base.GetDrawPositions(position);
		}
		
		/// <inheritdoc />
		public abstract void Snap();

		/// <inheritdoc />
		public void SnapMemberValue(int memberIndex, ref float memberValue, Func<double, float> nicifyAndConvert)
		{
			double snap = GetSnapStep(memberIndex);

			#if DEV_MODE && DEBUG_SNAPPING
			var was = memberValue;
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(snap > 0d);
			#endif

			memberValue = nicifyAndConvert(Math.Round(memberValue / snap) * snap);

			#if DEV_MODE && DEBUG_SNAPPING
			if(!was.Equals(memberValue)) { Debug.Log("SnapMemberValue: "+was+" => "+memberValue+" with snap="+snap); }
			#endif
		}


		#if ENABLE_WORLD_SPACE_TOOLTIPS
		public sealed override void OnMouseoverEnter(Event inputEvent, bool isDrag)
		{
			base.OnMouseoverEnter(Event inputEvent, bool isDrag)
			UpdateTooltips();
		}
		#endif
		
		#if ENABLE_WORLD_SPACE_TOOLTIPS
		/// <inheritdoc />
		public sealed override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			base.OnMemberValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
			
			if(Mouseovered)
			{
				UpdateTooltips();
			}
		}
		#endif

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			#if DEV_MODE && DEBUG_UPDATE_CACHED_VALUES
			Debug.Log(ToString() + ".TryToManuallyUpdateCachedValueFromMember(" + memberIndex+", "+StringUtils.ToString((float)memberValue)+") with Value="+StringUtils.ToString(Value)+", GetValue(0)="+StringUtils.ToString(GetValue(0)));
			#endif

			var setValue = Value;
			try
			{
				setValue[memberIndex] = (float)memberValue;
			}
			#if DEV_MODE
			catch(IndexOutOfRangeException e)
			{
				Debug.LogError(e);
			#else
			catch(IndexOutOfRangeException)
			{
			#endif
				return false;
			}
			#if DEV_MODE
			catch(InvalidCastException e)
			{
				Debug.LogError(e);
			#else
			catch(InvalidCastException)
			{
			#endif
				return false;
			}
			DoSetValue(setValue, false, false);
			return true;
		}

		/// <inheritdoc cref="IDrawer.OnMouseover" />
		public override void OnMouseover()
		{
			base.OnMouseover();

			if(draggableMemberVisible)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!ReadOnly);
				#endif

				DrawGUI.Active.SetCursor(MouseCursor.SlideArrow);

				var color = Inspector.Preferences.theme.CanDragPrefixToAdjustValueTint;

				// highlight the control even when mouseovering the prefix
				// to make it clear than dragging will change the value of that field
				var draggingTargets = DraggingTargetsMembers;
				for(int n = draggingTargets.Length - 1; n >= 0; n--)
				{
					int index = draggingTargets[n];
					var draggableMember = (IDraggablePrefix<float>)members[index];
					if(draggableMember.ShouldShowInInspector)
					{
						DrawGUI.DrawMouseoverEffect(draggableMember.ControlPosition, color, localDrawAreaOffset);
					}
				}
			}
		}
		
		/// <summary> Updates the tooltips to display value in world space. </summary>
		protected abstract void UpdateTooltips();

		/// <inheritdoc />
		public void OnPrefixDragStart(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			Debug.Assert(draggableMemberVisible);
			#endif

			if(draggableMemberVisible)
			{
				var draggingTargets = DraggingTargetsMembers;
				for(int n = draggingTargets.Length - 1; n >= 0; n--)
				{
					int index = draggingTargets[n];
					var draggableMember = (TransformFloatDrawer)members[index];
					if(draggableMember.ShouldShowInInspector)
					{
						draggableMember.EnableOnSceneGUI();
					}
				}
			}
		}

		/// <inheritdoc />
		public void OnPrefixDragged(Event inputEvent)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			Debug.Assert(draggableMemberVisible);
			#endif

			if(!draggableMemberVisible)
			{
				return;
			}
			
			float mouseDelta = this.GetMouseDelta(inputEvent, MouseDownPosition);

			// if using high Snapping values, multiply delta value
			if(SnappingEnabled || DrawGUI.ActionKey)
			{
				if(draggableMemberVisible)
				{
					float snapAmount = GetSnapStep(DraggingTargetsMembers[0]);
					if(snapAmount > 1f)
					{
						mouseDelta *= snapAmount;
					}
				}
			}

			var values = GetValues();
			var mouseDownValues = MouseDownValues;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(values.Length == mouseDownValues.Length);
			Debug.Assert(mouseDownValues.Length > 0);
			#endif

			bool changed = false;
			for(int n = values.Length - 1; n >= 0; n--)
			{
				var valueWas = (Vector3)values[n];
				var mouseDownValue = (Vector3)MouseDownValues[n];
				var setValue = valueWas;
				OnPrefixDragged(ref setValue, mouseDownValue, mouseDelta);

				if(!setValue.Equals(valueWas))
				{
					values[n] = setValue;
					changed = true;
				}
			}

			if(changed)
			{
				SetValues(values);
			}
		}

		/// <inheritdoc/>
		public bool DraggingPrefixAffectsMember(IDrawer member)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(member != null);
			Debug.Assert(Array.IndexOf(members, member) != -1);
			Debug.Assert(memberBuildState == MemberBuildState.MembersBuilt);
			#endif

			return Array.IndexOf(DraggingTargetsMembers, Array.IndexOf(members, member)) != -1;
		}

		/// <inheritdoc/>
		public void OnPrefixDraggedInterface(ref object inputValue, object inputMouseDownValue, float mouseDelta)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			Debug.Assert(draggableMemberVisible);
			#endif

			if(!draggableMemberVisible)
			{
				return;
			}

			var valueCast = (Vector3)inputValue;
			var mouseDownValueCast = (Vector3)inputMouseDownValue;
			OnPrefixDragged(ref valueCast, mouseDownValueCast, mouseDelta);
		}

		/// <inheritdoc />
		public void OnPrefixDragged(ref Vector3 inputValue, Vector3 inputMouseDownValue, float mouseDelta)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			Debug.Assert(draggableMemberVisible);
			#endif

			if(!draggableMemberVisible)
			{
				return;
			}
			
			DoOnPrefixDragged(ref inputValue, inputMouseDownValue, mouseDelta);
		}

		/// <inheritdoc cref="OnPrefixDragged(ref Vector3,Vector3,float)" />
		protected virtual void DoOnPrefixDragged(ref Vector3 inputValue, Vector3 inputMouseDownValue, float mouseDelta)
		{
			var draggingTargets = DraggingTargetsMembers;

			var color = Inspector.Preferences.theme.CanDragPrefixToAdjustValueTint;

			for(int n = draggingTargets.Length - 1; n >= 0; n--)
			{
				int index = draggingTargets[n];
				var draggableMember = (IDraggablePrefix<float>)members[index];
				if(draggableMember.ShouldShowInInspector)
				{
					DrawGUI.DrawMouseoverEffect(draggableMember.ControlPosition, color, localDrawAreaOffset);

					float memberValue = inputValue[index];
					float mouseDownValue = inputMouseDownValue[index];
					draggableMember.OnPrefixDragged(ref memberValue, mouseDownValue, mouseDelta);
					inputValue[index] = memberValue;
				}
			}
		}

		#if UNITY_EDITOR
		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			#if DEV_MODE && DEBUG_INFINITE_AXIS
			Debug.Log(ToString()+ ".OnMouseUpAfterDownOverControl");
			#endif

			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);

			if(draggableMemberVisible)
			{
				var draggingTargets = DraggingTargetsMembers;
				for(int n = draggingTargets.Length - 1; n >= 0; n--)
				{
					int index = draggingTargets[n];
					var draggableMember = (TransformFloatDrawer)members[index];
					if(draggableMember.ShouldShowInInspector)
					{
						draggableMember.DisableOnSceneGUI();
					}
				}
			}
		}
		#endif


		/// <inheritdoc />
		protected sealed override bool GetDataIsValidUpdated()
		{
			for(int n = VisibleMembers.Length - 1; n >= 0; n--)
			{
				var memb = members[n];
				if(memb != null)
				{
					if(!memb.DataIsValid)
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <inheritdoc />
		public override void OnVisibleMembersChanged()
		{
			UpdateDraggableMembers();
			UpdateDraggableMemberVisible();

			base.OnVisibleMembersChanged();
		}

		/// <summary> Updates the draggable members. </summary>
		protected abstract void UpdateDraggableMembers();

		/// <summary> Updates value of draggableMemberVisible based on whether or not any draggable members are visible or not. </summary>
		private void UpdateDraggableMemberVisible()
		{
			if(!ReadOnly)
			{
				var draggingTargets = DraggingTargetsMembers;
				for(int n = draggingTargets.Length - 1; n >= 0; n--)
				{
					int index = draggingTargets[n];
					var draggableMember = (IDraggablePrefix<float>)members[index];
					if(draggableMember.ShouldShowInInspector)
					{
						draggableMemberVisible = true;
						return;
					}
				}
			}
			draggableMemberVisible = false;
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			if(Clipboard.CopiedType == Types.Float || Clipboard.CopiedType == Types.Int)
			{
				SetMemberValues(Clipboard.Paste<float>());
				return;
			}

			base.DoPasteFromClipboard();
		}
	}
}