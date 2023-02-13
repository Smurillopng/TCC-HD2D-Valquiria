using System;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public sealed class ClampedFloatDrawer : NumericDrawer<float>
	{
		private float min;
		private float max;

		/// <summary> Creates a new instance of ClampedFloatDrawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="min">smallest possible value for drawer</param>
		/// <param name="max">largest possible value for drawer</param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> Ready-to-use instance of RectDrawer. </returns>
		public static ClampedFloatDrawer Create(float value, float min, float max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ClampedFloatDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ClampedFloatDrawer();
			}
			result.Setup(value, min, max, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <summary>
		/// Prevents a default instance of the drawer from being created.
		/// The Create method should be used instead of this constructor.
		/// </summary>
		private ClampedFloatDrawer() { }

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method of ClampedFloatDrawer.");
		}

		/// <inheritdoc/>
		protected sealed override void Setup(float setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method of ClampedFloatDrawer.");
		}

		private void Setup(float setValue, float setMin, float setMax, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			min = Mathf.Min(setMin, setMax);
			max = Mathf.Max(min, setMax);
			
			base.Setup(Mathf.Clamp(setValue, min, max), typeof(float), setMemberInfo, setParent, setLabel, setReadOnly);

			if(string.IsNullOrEmpty(setLabel.tooltip))
			{
				setLabel.tooltip = string.Concat("Value between ", StringUtils.ToString(min), " and ", StringUtils.ToString(max));
			}
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(float setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(Mathf.Clamp(setValue, min, max), applyToField, updateMembers);
		}

		/// <inheritdoc/>
		public override void OnPrefixDragged(ref float inputValue, float inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Clamp(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * FloatDrawer.DragSensitivity), min, max);
		}

		/// <inheritdoc/>
		public override float DrawControlVisuals(Rect position, float value)
		{
			return DrawGUI.Active.ClampedFloatField(controlLastDrawPosition, value, min, max);
		}

		/// <inheritdoc/>
		protected override float GetRandomValue()
		{
			return UnityEngine.Random.Range(min, max);
		}
	}
}