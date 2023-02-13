using System;
using Sisus.Attributes;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus
{
	
	[Serializable, DrawerForAttribute(typeof(PMinAttribute), typeof(float))]
	#if UNITY_2018_3_OR_NEWER
	[DrawerForAttribute(typeof(MinAttribute), typeof(float))]
	#endif
	public sealed class MinFloatDrawer : NumericDrawer<float>, IPropertyDrawerDrawer
	{
		private float min;

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
		/// <param name="min"> The minimum value for the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static MinFloatDrawer Create(float value, float min, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			MinFloatDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MinFloatDrawer();
			}
			result.Setup(value, min, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		private void Setup(float setValue, float setMin, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			min = setMin;
			base.Setup(setValue < min ? min : setValue, typeof(float), setMemberInfo, setParent, setLabel, setReadOnly);
			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value greater than or equal to ", StringUtils.ToString(min));
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
			float setMin;
			#if UNITY_2018_3_OR_NEWER
			var unityMinAttribute = attribute as MinAttribute;
			if(unityMinAttribute != null)
			{
				setMin = unityMinAttribute.min;
			}
			else
			#endif
			{
				PMinAttribute minAttribute;
				if(!Compatibility.PluginAttributeConverterProvider.TryConvert(attribute, out minAttribute))
				{
					Debug.LogError("MinFloatDrawer created via IPropertyDrawerDrawer interface but can't convert attribute "+StringUtils.TypeToString(attribute) + " to PMinAttribute! Please implement conversion support via a PluginAttributeConverterProvider.");
					setMin = float.MinValue;
				}
				else
				{
					setMin = minAttribute.min;
				}
			}
			
			Setup((float)setValue, setMin, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(float setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(setValue < min ? min : setValue, applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref float inputValue, float inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Max(inputMouseDownValue + mouseDelta * FloatDrawer.DragSensitivity, min);
		}

		/// <inheritdoc />
		public override float DrawControlVisuals(Rect position, float value)
		{
			return DrawGUI.Active.MinFloatField(position, value, min);
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
			return UnityEngine.Random.Range(min, float.MaxValue);
		}
	}
}