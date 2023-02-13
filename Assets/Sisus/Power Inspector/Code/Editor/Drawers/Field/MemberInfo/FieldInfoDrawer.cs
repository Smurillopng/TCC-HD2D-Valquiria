using System;
using System.Reflection;
using System.Collections.Generic;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(FieldInfo), true, true)] //use for extending classes must be true, so that FieldInfoDrawer is used for RuntimeType correctly
	public class FieldInfoDrawer : MemberInfoBaseDrawer<FieldInfo>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedFieldInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static FieldInfoDrawer Create(FieldInfo value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			FieldInfoDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new FieldInfoDrawer();
			}
			result.Setup(value, typeof(FieldInfo), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}


		/// <inheritdoc />
		protected override void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(MemberInfoDrawerUtility.IsReady);
			#endif

			rootItems = MemberInfoDrawerUtility.fieldRootItems;
			groupsByLabel = MemberInfoDrawerUtility.fieldGroupsByLabel;
			itemsByLabel = MemberInfoDrawerUtility.fieldItemsByLabel;
		}

		/// <inheritdoc />
		protected override GUIContent MenuLabel()
		{
			return GUIContentPool.Create("Field");
		}

		/// <inheritdoc />
		protected override string GetLabelText(FieldInfo value)
		{
			return TypeExtensions.GetShortName(value.ReflectedType) + "." + value.Name;
		}

		/// <inheritdoc />
		protected override FieldInfo GetRandomValue()
		{
			if(!GenerateMenuItemsIfNotGenerated())
			{
				Debug.LogWarning("Can generate random value yet. Setup still in progress...");
				return Value;
			}

			int count = MemberInfoDrawerUtility.fieldItemsByLabel.Count;
			int random = Random.Range(0, count);
			var ienumerator = MemberInfoDrawerUtility.fieldItemsByLabel.Values.GetEnumerator();
			for(int n = random - 1; n >= 0; n--)
			{
				ienumerator.MoveNext();
			}
			return (FieldInfo)ienumerator.Current.IdentifyingObject;
		}
	}
}