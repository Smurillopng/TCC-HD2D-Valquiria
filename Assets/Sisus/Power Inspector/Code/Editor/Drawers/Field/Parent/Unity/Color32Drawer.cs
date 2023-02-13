using System;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(Color32), false, true)]
	public class Color32Drawer : ColorBaseDrawer<Color32>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static Color32Drawer Create(Color32 value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			Color32Drawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new Color32Drawer();
			}
			result.Setup(value, typeof(Color32), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}
	}
}