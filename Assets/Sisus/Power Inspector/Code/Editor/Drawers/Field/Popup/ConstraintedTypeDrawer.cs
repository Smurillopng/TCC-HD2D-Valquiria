using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Sisus
{
	[Serializable]
	public class ConstraintedTypeDrawer : PopupMenuSelectableDrawer<Type>
	{
		private Type[] baseTypeConstraints;
		private TypeConstraint typeCategoryConstraint;

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
		public static ConstraintedTypeDrawer Create([CanBeNull]Type value, [NotNull]Type[] baseTypeConstraints, TypeConstraint typeCategoryConstraint, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ConstraintedTypeDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ConstraintedTypeDrawer();
			}
			result.Setup(value, baseTypeConstraints, typeCategoryConstraint, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		protected override void Setup(Type setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method.");
		}

		private void Setup([CanBeNull]Type setValue, [NotNull]Type[] setBaseTypeConstraints, TypeConstraint setTypeCategoryConstraint, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			baseTypeConstraints = setBaseTypeConstraints;
			typeCategoryConstraint = setTypeCategoryConstraint;
			base.Setup(setValue, typeof(Type), setMemberInfo, setParent, setLabel, setReadOnly);
		}
	
		/// <inheritdoc />
		public override object DefaultValue(bool _)
		{
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
			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.BeginSample("ConstraintedTypeDrawer.GenerateMenuItems");
			#endif

			var types = TypeExtensions.GetAllTypesThreadSafe(true, false, false);

			if(typeCategoryConstraint.HasFlag(TypeConstraint.Struct))
			{
				types = types.Where((t) => t.IsValueType && t != typeof(Nullable<>));
			}
			else
			{
				if(typeCategoryConstraint.HasFlag(TypeConstraint.Class))
				{
					types = types.Where((t) => !t.IsValueType);
				}

				if(typeCategoryConstraint.HasFlag(TypeConstraint.New))
				{
					types = types.Where((t)=>t.GetConstructor(Type.EmptyTypes) != null);
				}
			}

			switch(baseTypeConstraints.Length)
			{
				case 0:
					foreach(var type in types)
					{
						PopupMenuUtility.BuildPopupMenuItemForTypeWithLabel(ref rootItems, ref groupsByLabel, ref itemsByLabel, type, TypeExtensions.GetPopupMenuLabel(type));
					}
					break;
				case 1:
					foreach(var type in types)
					{
						if(baseTypeConstraints[0].IsAssignableFrom(type))
						{
							PopupMenuUtility.BuildPopupMenuItemForTypeWithLabel(ref rootItems, ref groupsByLabel, ref itemsByLabel, type, TypeExtensions.GetPopupMenuLabel(type));
						}
						break;
					}
					break;
				default:
					foreach(var type in types)
					{
						bool assignable = true;
						for(int n = baseTypeConstraints.Length - 1; n >= 0; n--)
						{
							if(!baseTypeConstraints[n].IsAssignableFrom(type))
							{
								assignable = false;
								break;
							}
						}
						if(assignable)
						{
							PopupMenuUtility.BuildPopupMenuItemForTypeWithLabel(ref rootItems, ref groupsByLabel, ref itemsByLabel, type, TypeExtensions.GetPopupMenuLabel(type));
						}
					}
					break;
			}

			rootItems.Sort();
			for(int n = rootItems.Count - 1; n >= 0; n--)
			{
				rootItems[n].Sort();
			}
			
			var nullItem = PopupMenuItem.Item(null as Type, "None", "A null reference; one that does not refer to any object.", null);
			nullItem.Preview = null;
			rootItems.Insert(0, nullItem);
			itemsByLabel.Add(nullItem.label, nullItem);

			#if DEV_MODE || PROFILE_POWER_INSPECTOR
			Profiler.EndSample();
			#endif
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
			GenerateMenuItemsIfNotGenerated();

			int count = generatedItemsByLabel.Count;
			if(count == 0)
			{
				return null;
			}

			int nth = Random.Range(0, count);
			var ienumerator = generatedItemsByLabel.Values.GetEnumerator();
			for(int n = nth; n >= 1; n--)
			{
				ienumerator.MoveNext();
			}
			return ienumerator.Current.type;
		}

		private static bool HasClassConstraint(Type type)
		{
			var attributes = type.GenericParameterAttributes;
			var constraints = attributes & GenericParameterAttributes.SpecialConstraintMask;
			return (constraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
		}

		private static bool HasStructConstraint(Type type)
		{
			var attributes = type.GenericParameterAttributes;
			var constraints = attributes & GenericParameterAttributes.SpecialConstraintMask;
			return (constraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
		}

		private static bool HasNewConstraint(Type type)
		{
			var attributes = type.GenericParameterAttributes;
			var constraints = attributes & GenericParameterAttributes.SpecialConstraintMask;
			return (constraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0;
		}
	}
}