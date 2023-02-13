using System;
using System.Reflection;
using System.Collections.Generic;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(MethodInfo), true, true)] //use for extending classes must be true, so that MethodInfoDrawer is used for RuntimeType correctly
	public class MethodInfoDrawer : MemberInfoBaseDrawer<MethodInfo>
	{
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMethodInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static MethodInfoDrawer Create(MethodInfo value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			MethodInfoDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new MethodInfoDrawer();
			}
			result.Setup(value, typeof(MethodInfo), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(MemberInfoDrawerUtility.IsReady);
			#endif

			rootItems = MemberInfoDrawerUtility.methodRootItems;
			groupsByLabel = MemberInfoDrawerUtility.methodGroupsByLabel;
			itemsByLabel = MemberInfoDrawerUtility.methodItemsByLabel;
		}

		/// <inheritdoc />
		protected override GUIContent MenuLabel()
		{
			return GUIContentPool.Create("Method");
		}

		/// <inheritdoc />
		protected override string GetLabelText(MethodInfo value)
		{
			var sb = StringBuilderPool.Create();

			var type = value.ReflectedType;
			if(type != null)
			{
				sb.Append(TypeExtensions.GetShortName(value.ReflectedType));
				sb.Append('.');
			}
			else
			{
				type = value.DeclaringType;
				if(type != null)
				{
					sb.Append(TypeExtensions.GetShortName(value.ReflectedType));
					sb.Append('.');
				}
			}
			
			StringUtils.ToString(value, sb);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		/// <inheritdoc />
		protected override MethodInfo GetRandomValue()
		{
			int count = MemberInfoDrawerUtility.methodItemsByLabel.Count;
			int random = Random.Range(0, count);
			var ienumerator = MemberInfoDrawerUtility.methodItemsByLabel.Values.GetEnumerator();
			for(int n = random - 1; n >= 0; n--)
			{
				ienumerator.MoveNext();
			}
			return (MethodInfo)ienumerator.Current.IdentifyingObject;
		}
	}
}