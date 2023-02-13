using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(AnimationCurve), false, true)]
	public class AnimationCurveDrawer : PrefixControlComboDrawer<AnimationCurve>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static AnimationCurveDrawer Create(AnimationCurve value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool setReadOnly)
		{
			AnimationCurveDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new AnimationCurveDrawer();
			}
			result.Setup(value, typeof(AnimationCurve), memberInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override AnimationCurve DrawControlVisuals(Rect position, AnimationCurve value)
		{
			position.y += 1f;
			position.height = 16f;
			return DrawGUI.Active.CurveField(position, value);
		}

		/// <inheritdoc />
		public override object DefaultValue(bool preferNotNull = false)
		{
			return CanBeNull && !preferNotNull ? null : new AnimationCurve(new Keyframe[0]);
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as AnimationCurve, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override AnimationCurve GetRandomValue()
		{
			var curve = Value;
			var keys = curve.keys;
			for(int n = keys.Length - 1; n >= 0; n--)
			{
				curve.keys[n] = new Keyframe(RandomUtils.Float(0f, float.MaxValue), RandomUtils.Float(float.MinValue, float.MaxValue));
			}
			curve.keys = keys;
			return curve;
		}
	}
}