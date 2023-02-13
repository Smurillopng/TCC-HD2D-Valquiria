using System;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(LayerMask), false, true)]
	public class LayerMaskDrawer : PrefixControlComboDrawer<LayerMask>, IMaskDrawer
	{
		private Rect buttonRect;
		private bool mouseIsOverButton;
	
		public int MaskValue
		{
			get
			{
				return Value;
			}

			set
			{
				Value = value;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static IDrawer Create(LayerMask value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			LayerMaskDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new LayerMaskDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((LayerMask)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <summary>
		/// Just Draw the control with current value and return changes made to the value via the control,
		/// without fancy features like data validation color coding
		/// </summary>
		public override LayerMask DrawControlVisuals(Rect position, LayerMask inputValue)
		{
			return DrawGUI.Active.MaskPopup(position, MaskValue);
		}

		/// <inheritdoc />
		public override void OnMouseover()
		{
			if(mouseIsOverButton)
			{
				// From version 2019.3 onwards Unity has built-in mouseover effects for enum fields
				#if !UNITY_2019_3_OR_NEWER
				DrawGUI.DrawMouseoverEffect(buttonRect, localDrawAreaOffset);
				#endif
				return;
			}
			
		
			base.OnMouseover();
		}
		
		/// <inheritdoc/>
		protected override void OnLayoutEvent(Rect position)
		{
			base.OnLayoutEvent(position);
			
			mouseIsOverButton = buttonRect.MouseIsOver();
		}

		/// <inheritdoc/>
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);

			buttonRect = ControlPosition;
			buttonRect.height -= 2f;
		}

		/// <inheritdoc />
		protected override LayerMask GetRandomValue()
		{
			//get all 31 layers
			var layers = new System.Collections.Generic.List<string>(32);
			for(int i = 0; i <= 31; i++)
			{
				var layerName = LayerMask.LayerToName(i);
				layers.Add(layerName);
			}
			var pickedLayers = new System.Collections.Generic.List<string>(16);

			for(int i = Random.Range(0, 32); i >= 0; i--)
			{
				int index = Random.Range(0, layers.Count);
				pickedLayers.Add(layers[index]);
				layers.RemoveAt(index);
			}
			var layerMask = LayerMask.GetMask(pickedLayers.ToArray());
			return layerMask;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			mouseIsOverButton = false;
			base.Dispose();
		}
	}
}