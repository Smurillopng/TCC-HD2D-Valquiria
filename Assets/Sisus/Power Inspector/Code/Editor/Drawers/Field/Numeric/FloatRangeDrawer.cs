using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing a float field with the Range attribute.
	/// </summary>
	[Serializable, DrawerForAttribute(true, typeof(RangeAttribute), typeof(float)), DrawerForAttribute(true, typeof(PRangeAttribute), typeof(float))]
	public sealed class FloatRangeDrawer : RangeDrawer<float>, IPropertyDrawerDrawer
	{
		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return true;
			}
		}

		public static FloatRangeDrawer Create(float value, RangeAttribute range, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			return Create(value, range.min, range.max, memberInfo, parent, label, setReadOnly);
		}

		public static FloatRangeDrawer Create(float value, float min, float max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			FloatRangeDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new FloatRangeDrawer();
			}
			result.Setup(value, min, max, memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public void SetupInterface(object attribute, object setValue, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var range = attribute as RangeAttribute;
			if(range != null)
			{
				Setup((float)setValue, range.min, range.max, setMemberInfo, setParent, setLabel, setReadOnly);
				return;
			}

			PRangeAttribute prange;
			if(Compatibility.PluginAttributeConverterProvider.TryConvert(attribute, out prange))
			{
				Setup((float)setValue, prange.min, prange.max, setMemberInfo, setParent, setLabel, setReadOnly);
				return;
			}

			var parameterProvider = attribute as IDrawerSetupDataProvider;
			if(parameterProvider != null)
			{
				var parameters = parameterProvider.GetSetupParameters();
				float setMin = (float)parameters[0];
				float setMax = (float)parameters[1];
				Setup((float)setValue, setMin, setMax, setMemberInfo, setParent, setLabel, setReadOnly);
				return;
			}

			Debug.LogWarning("IntRangeDrawer could not determine min or max sizes via RangeAttribute or attribute implementing IDrawerSetupDataProvider.");

			Setup((float)setValue, 0f, 1f, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override float DrawNumberFieldVisuals(Rect position, float value)
		{
			var setValue = DrawGUI.Active.FloatField(position, value);
			return setValue != value ? Clamped(setValue) : value;
		}

		/// <inheritdoc/>
		protected override float Clamped(float input)
		{
			return Mathf.Clamp(input, min, max);
		}

		/// <inheritdoc/>
		protected override bool Equals(float a, float b)
		{
			return a == b;
		}

		/// <inheritdoc cref="IDrawer.GetRandomValue" />
		protected override float GetRandomValue()
		{
			return RandomUtils.Float(min, max);
		}
	}
}