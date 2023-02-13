using System;
using System.Reflection;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public abstract class MemberInfoBaseDrawer<TMemberInfo> : PopupMenuSelectableDrawer<TMemberInfo> where TMemberInfo : MemberInfo
	{
		public override Type Type
		{
			get
			{
				return typeof(TMemberInfo);
			}
		}

		/// <inheritdoc />
		protected override bool IsReadyToGenerateMenuItems()
		{
			return MemberInfoDrawerUtility.IsReady;
		}

		/// <inheritdoc />
		protected override string GetLabelText(TMemberInfo value)
		{
			var sb = StringBuilderPool.Create();
			sb.Append(TypeExtensions.GetShortName(value.ReflectedType));
			sb.Append('.');
			StringUtils.ToString(value, sb);
			return StringBuilderPool.ToStringAndDispose(ref sb);
		}

		protected override string GetTooltip(TMemberInfo value)
		{
			return StringUtils.ToString(value.ReflectedType.Namespace == null ? "" : value.ReflectedType.Namespace);
		}

		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
			#if DEV_MODE
			Debug.Log("TypePopupDrawer.DefaultValue returning typeof(void)");
			#endif
			return null;
		}
	}
}