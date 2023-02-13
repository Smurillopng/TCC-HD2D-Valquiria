#define DEBUG_APPLY_VALUE

using System;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForAttribute(true, typeof(DelayedAttribute), typeof(int))]
	public sealed class DelayedIntDrawer : NumericDrawer<int>, IPropertyDrawerDrawer
	{
		private int valueUnapplied;

		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc/>
		protected override bool IsDelayedField
		{
			get
			{
				return true;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		[NotNull]
		public static DelayedIntDrawer Create(int value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DelayedIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DelayedIntDrawer();
			}
			result.Setup(value, typeof(int), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc cref="IFieldDrawer.SetupInterface" />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((int)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public void SetupInterface(object attribute, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((int)setValue, typeof(int), setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(int setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			valueUnapplied = setValue;
			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(int setValue, bool applyToField, bool updateMembers)
		{
			valueUnapplied = setValue;
			if(!SelectedAndInspectorHasFocus || !DrawGUI.EditingTextField)
			{
				#if DEV_MODE && UNITY_EDITOR
				Debug.Assert(!SelectedAndInspectorHasFocus || !UnityEditor.EditorGUIUtility.editingTextField);
				#endif

				return base.DoSetValue(setValue, applyToField, updateMembers);
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(DrawGUI.EditingTextField && SelectedAndInspectorHasFocus)
			{
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();

			#if DEV_MODE && DEBUG_APPLY_VALUE
			if(valueUnapplied != Value)
			{
				Debug.Log(GetType().Name+" - Discarding unapplied value "+valueUnapplied+" because UpdateCachedValuesFromFieldsRecursively was called with EditingTextField false");
			}
			#endif

			valueUnapplied = Value;
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			valueUnapplied = DrawGUI.Active.IntField(position, valueUnapplied);
			return value;
		}

		/// <inheritdoc />
		protected override void OnDeselectedInternal(ReasonSelectionChanged reason, IDrawer losingFocusTo)
		{
			#if DEV_MODE && DEBUG_APPLY_VALUE
			Debug.Log(GetType().Name+ " - Applying valueUnapplied " + valueUnapplied+" over "+Value+" because selection changed");
			#endif

			base.DoSetValue(valueUnapplied, true, true);
			
			base.OnDeselectedInternal(reason, losingFocusTo);
		}
		
		/// <inheritdoc cref="IDrawer.OnKeyboardInputGiven" />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			//the latter part is needed to fix an issue where editingTextField
			//gets changed by Unity internally before things get this far
			if(DrawGUI.EditingTextField || Value != valueUnapplied)
			{
				switch(inputEvent.keyCode)
				{
					case KeyCode.Escape:
					{
						#if DEV_MODE && DEBUG_APPLY_VALUE
						Debug.Log(GetType().Name+" - Discarding unapplied value "+valueUnapplied+" because esc was pressed");
						#endif

						DrawGUI.Use(inputEvent);
						valueUnapplied = Value;
						StopEditingField();
						GUI.changed = true;
						return true;
					}
					case KeyCode.Return:
					case KeyCode.KeypadEnter:
					{
						#if DEV_MODE && DEBUG_APPLY_VALUE
						Debug.Log(GetType().Name+" - Applying value "+valueUnapplied+" because return or enter was pressed");
						#endif

						DrawGUI.Use(inputEvent);
						base.DoSetValue(valueUnapplied, true, true);
						StopEditingField();
						GUI.changed = true;
						return true;
					}
				}
				return false;
			}

			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc cref="IDrawer.GetRandomValue" />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		}
	}
}