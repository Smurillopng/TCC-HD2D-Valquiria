using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// Drawer representing an int field with the Range attribute.
	/// </summary>
	[Serializable, DrawerForAttribute(true, typeof(RangeAttribute), typeof(int)), DrawerForAttribute(true, typeof(PRangeAttribute), typeof(int))]
	public sealed class IntRangeDrawer : RangeDrawer<int>, IPropertyDrawerDrawer
	{
		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return false;
			}
		}

		public static IntRangeDrawer Create(int value, RangeAttribute range, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			return Create(value, Mathf.Ceil(range.min), Mathf.Floor(range.max), memberInfo, parent, label, setReadOnly);
		}

		public static IntRangeDrawer Create(int value, float min, float max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			IntRangeDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new IntRangeDrawer();
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
				Setup((int)setValue, range.min, range.max, setMemberInfo, setParent, setLabel, setReadOnly);
				return;
			}

			PRangeAttribute prange;
			if(Compatibility.PluginAttributeConverterProvider.TryConvert(attribute, out prange))
			{
				Setup((int)setValue, prange.min, prange.max, setMemberInfo, setParent, setLabel, setReadOnly);
				return;
			}

			var parameterProvider = attribute as IDrawerSetupDataProvider;
			if(parameterProvider != null)
			{
				var parameters = parameterProvider.GetSetupParameters();
				float setMin = (float)parameters[0];
				float setMax = (float)parameters[1];
				Setup((int)setValue, setMin, setMax, setMemberInfo, setParent, setLabel, setReadOnly);
				return;
			}

			Debug.LogWarning("IntRangeDrawer could not determine min or max sizes via RangeAttribute or attribute implementing IDrawerSetupDataProvider.");

			Setup((int)setValue, 0f, 1f, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override int DrawNumberFieldVisuals(Rect position, int value)
		{
			var setValue = DrawGUI.Active.IntField(position, value);
			return setValue != value ? Clamped(setValue) : value;
		}

		/// <inheritdoc />
		protected override bool Equals(int a, float b)
		{
			return a.Equals((int)b);
		}

		/// <inheritdoc />
		protected override int Clamped(int input)
		{
			return MathUtils.Clamp(input, min, max);
		}

		/// <inheritdoc cref="IDrawer.GetRandomValue" />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range((int)min, (int)max);
		}
	}
}