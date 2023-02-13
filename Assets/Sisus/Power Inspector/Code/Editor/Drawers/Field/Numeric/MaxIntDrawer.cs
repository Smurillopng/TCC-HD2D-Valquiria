using System;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable, DrawerForAttribute(typeof(PMaxAttribute), typeof(int))]
	public sealed class MaxIntDrawer : NumericDrawer<int>, IPropertyDrawerDrawer
	{
		private int max;

		public bool RequiresPropertyDrawerType
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		/// <inheritdoc />
		protected override int ValueDuringMixedContent
		{
			get
			{
				return 1961271802;
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
		public static MaxIntDrawer Create(int value, int max, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			MaxIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MaxIntDrawer();
			}
			result.Setup(value, max, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public sealed override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		/// <inheritdoc/>
		protected sealed override void Setup(int setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method");
		}

		/// <inheritdoc/>
		public void SetupInterface([CanBeNull] object attribute, [CanBeNull] object setValue, [CanBeNull] LinkedMemberInfo setMemberInfo, [CanBeNull] IParentDrawer setParent, [CanBeNull] GUIContent setLabel, bool setReadOnly)
		{
			PMaxAttribute maxAttribute;
			int setMax;
			if(!Compatibility.PluginAttributeConverterProvider.TryConvert(attribute, out maxAttribute))
			{
				Debug.LogError("MaxInDrawer created via IPropertyDrawerDrawer interface but can't convert attribute "+StringUtils.TypeToString(attribute) + " to PMaxAttribute! Please implement conversion support via a PluginAttributeConverterProvider.");
				setMax = int.MaxValue;
			}
			else
			{
				setMax = Mathf.RoundToInt(maxAttribute.max);
			}
			
			Setup((int)setValue, setMax, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		private void Setup(int value, int setMax, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			max = setMax;
			base.Setup(value > max ? max : value, typeof(int), setMemberInfo, setParent, setLabel, setReadOnly);

			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value smaller than or equal to ", StringUtils.ToString(max));
			}
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(int setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(setValue > max ? max : setValue, applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Min(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), max);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			return DrawGUI.Active.MaxIntField(controlLastDrawPosition, value, max);
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range(int.MinValue, max);
		}
	}
}