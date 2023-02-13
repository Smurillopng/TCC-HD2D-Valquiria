using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public sealed class ClampedIntDrawer : NumericDrawer<int>
	{
		private int min;
		private int max;
		
		public static ClampedIntDrawer Create(int value, int min, int max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			ClampedIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ClampedIntDrawer();
			}
			result.Setup(value, min, max, memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		/// <inheritdoc/>
		protected override void Setup(int setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		private void Setup(int value, int min, int max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			this.min = Mathf.Min(min, max);
			this.max = Mathf.Max(min, max);
			
			base.Setup(Mathf.Clamp(value, this.min, this.max), typeof(int), memberInfo, parent, label, setReadOnly);
			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value between ", StringUtils.ToString(min), " and ", StringUtils.ToString(max));
			}
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(int setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(Mathf.Clamp(setValue, min, max), applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Clamp(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), min, max);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			return DrawGUI.Active.ClampedIntField(controlLastDrawPosition, value, min, max);
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range(min, max);
		}
	}
}