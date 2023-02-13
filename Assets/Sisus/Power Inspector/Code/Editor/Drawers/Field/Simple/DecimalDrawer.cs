using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(decimal), false, true)]
	public sealed class DecimalDrawer : NumericDrawer<decimal>
	{
		private const float DragSensitivity = 0.04f;

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DecimalDrawer Create(decimal value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			DecimalDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DecimalDrawer();
			}
			result.Setup(value, typeof(decimal), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((decimal)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		public override void OnPrefixDragged(ref decimal inputValue, decimal inputMouseDownValue, float mouseDelta)
		{
			try
			{
				inputValue = inputMouseDownValue + Convert.ToDecimal(mouseDelta * DragSensitivity);
			}
			catch(Exception e)
			{
				Debug.LogWarning(e + " with inputValue="+ inputValue+ ", inputMouseDownValue="+ inputMouseDownValue+ ", mouseDelta="+ mouseDelta);
			}
		}
		
		/// <inheritdoc/>
		public override decimal DrawControlVisuals(Rect position, decimal value)
		{
			return DrawGUI.Active.DecimalField(position, value);
		}

		/// <inheritdoc/>
		protected override bool ValuesAreEqual(decimal a, decimal b)
		{
			return a != b;
		}

		/// <inheritdoc/>
		protected override bool GetDataIsValidUpdated()
		{
			return true;
		}

		/// <inheritdoc/>
		protected override decimal GetRandomValue()
		{
			return RandomUtils.Decimal();
		}
	}
}