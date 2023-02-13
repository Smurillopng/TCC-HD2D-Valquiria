using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(Gradient), false, true)]
	public class GradientDrawer : PrefixControlComboDrawer<Gradient>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static GradientDrawer Create(Gradient value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			GradientDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GradientDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as Gradient, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override Gradient DrawControlVisuals(Rect position, Gradient value)
		{
			position.y += 1f;
			position.height = 16f;
			return DrawGUI.Active.GradientField(position, value);
		}

		/// <inheritdoc />
		public override object DefaultValue(bool preferNotNull = false)
		{
			return CanBeNull && !preferNotNull ? null : new Gradient();
		}

		/// <inheritdoc />
		protected override Gradient GetRandomValue()
		{
			var gradient = Value;
			var colorKeys = gradient.colorKeys;
			for(int n = colorKeys.Length - 1; n >= 0; n--)
			{
				var color = new Color(RandomUtils.Float(0f, 1f), RandomUtils.Float(0f, 1f), RandomUtils.Float(0f, 1f), RandomUtils.Float(0f, 1f));
				gradient.colorKeys[n] = new GradientColorKey(color, RandomUtils.Float(0f, 1f));
			}
			gradient.colorKeys = colorKeys;
			return gradient;
		}
	}
}