using System;
using Sisus.Attributes;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable, DrawerForAttribute(typeof(PMaxAttribute), typeof(float))]
	public sealed class MaxFloatDrawer : NumericDrawer<float>, IPropertyDrawerDrawer
	{
		private float max;

		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return false;
			}
		}

		/// <inheritdoc />
		protected override float ValueDuringMixedContent
		{
			get
			{
				return 7921058762891625713f;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="max"> The maximum value for the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static MaxFloatDrawer Create(float value, float max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			MaxFloatDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MaxFloatDrawer();
			}
			result.Setup(value, max, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		private void Setup(float setValue, float setMax, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			max = setMax;
			base.Setup(setValue > max ? max : setValue, typeof(float), setMemberInfo, setParent, setLabel, setReadOnly);
			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value less than or equal to ", StringUtils.ToString(max));
			}
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		/// <inheritdoc/>
		protected sealed override void Setup(float setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		/// <inheritdoc/>
		public void SetupInterface([CanBeNull]object attribute, [CanBeNull]object setValue, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly)
		{
			PMaxAttribute maxAttribute;

			float setMax;
			if(!Compatibility.PluginAttributeConverterProvider.TryConvert(attribute, out maxAttribute))
			{
				Debug.LogError("MaxIntDrawer created via IPropertyDrawerDrawer interface but can't convert attribute "+StringUtils.TypeToString(attribute) + " to PMaxAttribute! Please implement conversion support via a PluginAttributeConverterProvider.");
				setMax = float.MaxValue;
			}
			else
			{
				setMax = maxAttribute.max;
			}
			
			Setup((float)setValue, setMax, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(float setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(setValue < max ? max : setValue, applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref float inputValue, float inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Max(inputMouseDownValue + mouseDelta * FloatDrawer.DragSensitivity, max);
		}

		/// <inheritdoc />
		public override float DrawControlVisuals(Rect position, float value)
		{
			return DrawGUI.Active.MaxFloatField(position, value, max);
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(float a, float b)
		{
			return a.Approximately(b);
		}

		/// <inheritdoc />
		protected override bool GetDataIsValidUpdated()
		{
			return !float.IsNaN(Value);
		}

		/// <inheritdoc />
		protected override float GetRandomValue()
		{
			return UnityEngine.Random.Range(float.MinValue, max);
		}
	}
}