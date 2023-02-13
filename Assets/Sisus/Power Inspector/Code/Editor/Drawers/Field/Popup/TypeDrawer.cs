using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(Type), true, true)] //use for extending classes must be true, so that TypeDrawer is used for RuntimeType correctly
	public class TypeDrawer : PopupMenuSelectableDrawer<Type>
	{
		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				return Types.Type;
			}
		}

		/// <inheritdoc />
		protected override bool CanBeNull
		{
			get
			{
				return true;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The initial cached value of the drawer. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the class member that the created drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static TypeDrawer Create(Type value, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			TypeDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new TypeDrawer();
			}
			result.Setup(value, typeof(Type), memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}
	
		/// <inheritdoc />
		public override object DefaultValue(bool preferNotNull = false)
		{
			if(preferNotNull)
			{
				return typeof(void);
			}
			return null;
		}

		/// <inheritdoc cref="IDrawer.OnMiddleClick" />
		public override void OnMiddleClick(Event inputEvent)
		{
			base.OnMiddleClick(inputEvent);

			var typeValue = Value;
			if(typeValue != null && inputEvent.type != EventType.Used)
			{
				var monoScript = FileUtility.FindScriptFile(typeValue);
				if(monoScript != null)
				{
					#if DEV_MODE && UNITY_EDITOR
					Debug.Log("Pinging script asset "+UnityEditor.AssetDatabase.GetAssetPath(monoScript)+"...", monoScript);
					#endif

					DrawGUI.Active.PingObject(monoScript);
					DrawGUI.Use(inputEvent);
					return;
				}

				#if DEV_MODE
				Debug.Log("script by type "+typeValue.FullName+" not found...");
				#endif
			}
			#if DEV_MODE
			else Debug.Log("typeValue="+StringUtils.ToString(typeValue)+", Event.type="+inputEvent.type);
			#endif
		}

		/// <inheritdoc />
		protected override Type GetTypeContext()
		{
			return PopupMenuUtility.GetTypeContext(memberInfo, parent);
		}

		/// <inheritdoc />
		protected override void GenerateMenuItems(ref List<PopupMenuItem> rootItems, ref Dictionary<string, PopupMenuItem> groupsByLabel, ref Dictionary<string, PopupMenuItem> itemsByLabel)
		{
			PopupMenuUtility.BuildTypePopupMenuItemsForContext(ref rootItems, ref groupsByLabel, ref itemsByLabel, GetTypeContext(), true);
		}

		/// <inheritdoc />
		protected override GUIContent MenuLabel()
		{
			return GUIContentPool.Create("Type");
		}

		/// <inheritdoc />
		protected override string GetPopupItemLabel(Type value)
		{
			return TypeExtensions.GetPopupMenuLabel(value);
		}

		/// <inheritdoc/>
		protected override string GetLabelText([NotNull]Type value)
		{
			return StringUtils.ToStringSansNamespace(value);
		}

		/// <inheritdoc/>
		protected override string GetTooltip([NotNull] Type value)
		{
			return StringUtils.ToString(value);
		}

		/// <inheritdoc/>
		public override bool CanPasteFromClipboard()
		{
			if(Clipboard.CopiedType == Types.String)
			{
				var type = TypeExtensions.GetType(Clipboard.Content);
				if(type != null)
				{
					return true;
				}
				return false;
			}
			return base.CanPasteFromClipboard();
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			if(Clipboard.CopiedType == Types.String)
			{
				var setValue = TypeExtensions.GetType(Clipboard.Content);
				SetValue(setValue);
				return;
			}

			base.DoPasteFromClipboard();
		}

		/// <inheritdoc />
		protected override Type GetRandomValue()
		{
			var allTypes = TypeExtensions.AllVisibleTypes as Type[];
			if(allTypes == null)
            {
				return allTypes[Random.Range(0, TypeExtensions.AllVisibleTypes.Count())];
			}
			return allTypes[Random.Range(0, allTypes.Length)];
		}
	}
}