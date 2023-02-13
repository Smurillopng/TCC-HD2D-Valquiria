using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(double), false, true)]
	public sealed class DoubleDrawer : NumericDrawer<double>
	{
		/// <inheritdoc />
		protected override double ValueDuringMixedContent
		{
			get
			{
				return 62398592365817936598231245708234d;
			}
		}

		public static DoubleDrawer Create(double value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DoubleDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DoubleDrawer();
			}
			result.Setup(value, typeof(double), memberInfo, parent, label, readOnly);
			result.LateSetup();

			return result;
		}
		
		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((double)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		public override void OnPrefixDragged(ref double inputValue, double inputMouseDownValue, float mouseDelta)
		{
			try
			{
				inputValue = inputMouseDownValue + mouseDelta * IntDrawer.DragSensitivity;
			}
			catch(Exception e)
			{
				Debug.LogWarning(e + " with inputValue="+ inputValue+ ", inputMouseDownValue="+ inputMouseDownValue+ ", mouseDelta="+ mouseDelta);
			}
		}

		/// <inheritdoc />
		public override double DrawControlVisuals(Rect position, double value)
		{
			return DrawGUI.Active.DoubleField(position, value);
		}

		/// <inheritdoc />
		protected override bool ValuesAreEqual(double a, double b)
		{
			// From .NET Documentation:
			// Because Epsilon defines the minimum expression of a positive value whose range is near zero,
			// the margin of difference between two similar values must be greater than Epsilon. Typically,
			// it is many times greater than Epsilon. Because of this, we recommend that you do not use
			// Epsilon when comparing Double values for equality.
			return a.Equals(b);
		}

		/// <inheritdoc />
		protected override bool GetDataIsValidUpdated()
		{
			var value = Value;
			return !double.IsNaN(value);
		}

		/// <inheritdoc />
		protected override double GetRandomValue()
		{
			return RandomUtils.Double(double.MinValue, double.MaxValue);
		}
	}
}