using System;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable, DrawerForAttribute(typeof(PMinAttribute), typeof(int))]
	#if UNITY_2018_3_OR_NEWER
	[DrawerForAttribute(typeof(MinAttribute), typeof(int))]
	#endif
	public sealed class MinIntDrawer : NumericDrawer<int>, IPropertyDrawerDrawer
	{
		private int min;

		/// <inheritdoc/>
		public bool RequiresPropertyDrawerType
		{
			get
			{
				return false;
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
		/// <param name="min"> The minimum value for the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static MinIntDrawer Create(int value, int min, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			MinIntDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MinIntDrawer();
			}
			result.Setup(value, min, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc/>
		public void SetupInterface([CanBeNull] object attribute, [CanBeNull] object setValue, [CanBeNull] LinkedMemberInfo setMemberInfo, [CanBeNull] IParentDrawer setParent, [CanBeNull] GUIContent setLabel, bool setReadOnly)
		{
			int setMin;
			#if UNITY_2018_3_OR_NEWER
			var unityMinAttribute = attribute as MinAttribute;
			if(unityMinAttribute != null)
			{
				setMin = Mathf.RoundToInt(unityMinAttribute.min);
			}
			else
			#endif
			{
				PMinAttribute minAttribute;
				if(!Compatibility.PluginAttributeConverterProvider.TryConvert(attribute, out minAttribute))
				{
					Debug.LogError("MinIntDrawer created via IPropertyDrawerDrawer interface but can't convert attribute "+StringUtils.TypeToString(attribute) + " to PMinAttribute! Please implement conversion support via a PluginAttributeConverterProvider.");
					setMin = int.MinValue;
				}
				else
				{
					setMin = Mathf.RoundToInt(minAttribute.min);
				}
			}
			
			Setup((int)setValue, setMin, setMemberInfo, setParent, setLabel, setReadOnly);
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

		private void Setup(int setValue, int setMin, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			min = setMin;
			base.Setup(setValue < min ? min : setValue, typeof(int), setMemberInfo, setParent, setLabel, setReadOnly);

			if(string.IsNullOrEmpty(label.tooltip))
			{
				label.tooltip = string.Concat("Value greater than or equal to ", StringUtils.ToString(min));
			}
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(int setValue, bool applyToField, bool updateMembers)
		{
			return base.DoSetValue(setValue < min ? min : setValue, applyToField, updateMembers);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref int inputValue, int inputMouseDownValue, float mouseDelta)
		{
			inputValue = Mathf.Max(inputMouseDownValue + Mathf.RoundToInt(mouseDelta * IntDrawer.DragSensitivity), min);
		}

		/// <inheritdoc />
		public override int DrawControlVisuals(Rect position, int value)
		{
			return DrawGUI.Active.MinIntField(controlLastDrawPosition, value, min);
		}

		/// <inheritdoc />
		protected override int GetRandomValue()
		{
			return UnityEngine.Random.Range(min, int.MaxValue);
		}
	}
}