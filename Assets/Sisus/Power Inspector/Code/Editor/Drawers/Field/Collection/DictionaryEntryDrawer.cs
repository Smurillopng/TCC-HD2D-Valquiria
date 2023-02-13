//#define DEBUG_SETUP

using System;
using System.Collections;
using System.Reflection;
using JetBrains.Annotations;
using Sisus.Attributes;
using UnityEngine;

namespace Sisus
{
	[Serializable, DrawerForField(typeof(DictionaryEntry), false, true)]
	public class DictionaryEntryDrawer : ParentFieldDrawer<DictionaryEntry>
	{
		private static bool propertyInfosGenerated;
		private static PropertyInfo propertyKey;
		private static PropertyInfo propertyValue;

		private Func<int, object[],bool> validateKey;
		private bool drawInSingleRow;
		private Type keyType;
		private Type valueType;

		/// <inheritdoc />
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="value"> The starting cached value of the drawer. </param>
		/// <param name="keyType"> The type of the keys in the dictionary. </param>
		/// <param name="valueType"> The type of the values in the dictionary. </param>
		/// <param name="memberInfo"> LinkedMemberInfo for the field, property or parameter that the drawer represents. Can be null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static DictionaryEntryDrawer Create(DictionaryEntry value, Type keyType, Type valueType, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly, Func<int, object[],bool> validateKey)
		{
			DictionaryEntryDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new DictionaryEntryDrawer();
			}
			result.Setup(value, DrawerUtility.GetType(memberInfo, value), keyType, valueType, memberInfo, parent, label, readOnly, validateKey);
			result.LateSetup();
			return result;
		}
		
		private static bool ValidateNotNull(int elementIndex, object[] values)
		{
			for(int n = values.Length - 1; n >= 0; n--)
			{
				if(values[n] == null)
				{
					return false;
				}
			}
			return true;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var dictionaryEntry = (DictionaryEntry)setValue;
			Type entryKeyType;
			Type entryValueType;

			if(setValueType == null)
			{
				if(setParent != null)
				{
					setValueType = setParent.Type;
				}
			}

			if(setValueType != null)
			{
				if(!TryGetMemberTypes(setValueType, out entryKeyType, out entryValueType))
				{
					#if DEV_MODE
					Debug.LogWarning(ToString(setLabel, setMemberInfo)+" - Setting memberTypes to System.Object because TryGetMemberTypes failed for type "+StringUtils.ToStringSansNamespace(setValueType) +".");
					#endif
					entryKeyType = Types.SystemObject;
					entryValueType = Types.SystemObject;
				}
			}
			else
			{
				#if DEV_MODE
				Debug.LogWarning("DictionaryEntry.Setup called with setValueType null. Determining key and value types might not be possible...");
				#endif

				var key = dictionaryEntry.Key;
				if(key != null)
				{
					entryKeyType = key.GetType();
				}
				else
				{
					Debug.LogWarning("DictionaryEntry.Setup called with no parent and null Key: can't determine key type");
					entryKeyType = Types.SystemObject;
				}

				var value = dictionaryEntry.Value;
				if(value != null)
				{
					entryValueType = value.GetType();
				}
				else
				{
					Debug.LogWarning("DictionaryEntry.Setup called with no parent and null Value: can't determine value type");
					entryValueType = Types.SystemObject;
				}
			}
			
			Setup(dictionaryEntry, setValueType, entryKeyType, entryValueType, setMemberInfo, setParent, setLabel, setReadOnly, null);
		}

		private static bool TryGetMemberTypes([NotNull]Type type, [NotNull]out Type keyType, [NotNull]out Type valueType)
		{
			Type[] genericArguments;
			if(type.TryGetGenericArgumentsFromBaseClass(Types.Dictionary, out genericArguments))
			{
				if(genericArguments.Length == 2)
				{
					keyType = genericArguments[0];
					valueType = genericArguments[1];
					return true;
				}
				#if DEV_MODE
				Debug.LogWarning("Dictionary.TryGetGenericArgumentsFromBaseClass(Dictionary<,>) returned "+genericArguments.Length+" generic arguments for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments));
				#endif
			}
			#if DEV_MODE
			else { Debug.LogWarning("Dictionary.TryGetGenericArgumentsFromBaseClass(Dictionary<,>) failed for type "+StringUtils.ToStringSansNamespace(type)+": "+StringUtils.ToString(genericArguments)); }
			#endif

			keyType = Types.SystemObject;
			valueType = Types.SystemObject;
			return false;
		}

		/// <inheritdoc/>
		protected sealed override void Setup(DictionaryEntry setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use the other Setup method");
		}
		
		private void Setup(DictionaryEntry setValue, Type setValueType, Type entryKeyType, Type entryValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly, [CanBeNull]Func<int, object[],bool> setValidateKey)
		{
			keyType = entryKeyType;
			valueType = entryValueType;
			validateKey = setValidateKey == null ? ValidateNotNull : setValidateKey;
			
			drawInSingleRow = DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(entryKeyType) && DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(entryValueType);
		

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			#if DEV_MODE && DEBUG_SETUP
			Debug.Log("DictionaryEntryDrawer<"+ StringUtils.ToString(keyType) +","+ StringUtils.ToString(valueType) + "> created with drawInSingleRow="+ StringUtils.ToColorizedString(drawInSingleRow)+", value="+StringUtils.ToString(Value));
			#endif
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			var type = Type;
			if(!propertyInfosGenerated)
			{
				propertyInfosGenerated = true;
				propertyKey = type.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
				propertyValue = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
			}

			memberBuildList.Add(MemberHierarchy.Get(memberInfo, propertyKey));
			memberBuildList.Add(MemberHierarchy.Get(memberInfo, propertyValue));
		}

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			DrawerArrayPool.Resize(ref members, 2);

			// Making key be read-only for now until code has been added to handle on-the-fly changing of keys gracefully.
			// E.g. all keys should use delayed text fields and be marked with NotNull (e.g. using a custom DrawerProvider).
			var keyInfo = memberBuildList[0];

			IDrawer keyMember;
			var keyValue = keyInfo.GetValue(0);
			var keyLabel = GUIContentPool.Create("K");
			//var keyIsReadOnly = ReadOnly;
			const bool keyIsReadOnly = true;
			if(keyType == Types.Int)
			{
				keyMember = DelayedIntDrawer.Create((int)keyValue, keyInfo, this, keyLabel, keyIsReadOnly);
			}
			else if(keyType == Types.String)
			{
				keyMember = TextDrawer.Create((string)keyValue, keyInfo, this, keyLabel, keyIsReadOnly, false, true);
			}
			else if(keyType == Types.Float)
			{
				keyMember = DelayedFloatDrawer.Create((float)keyValue, keyInfo, this, keyLabel, keyIsReadOnly);
			}
			else
			{
				keyMember = DrawerProvider.GetForField(keyValue, keyType, keyInfo, this, keyLabel, keyIsReadOnly);
			}

			keyMember.OverrideValidateValue = ValidateKey;
			
			members[0] = keyMember;
			var valueInfo = memberBuildList[1];
			var valueMember = DrawerProvider.GetForField(valueInfo.GetValue(0), valueType, valueInfo, this, GUIContentPool.Create("V"), ReadOnly);
			members[1] = valueMember;
		}

		/// <inheritdoc/>
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			// If value is not valid, don't accept it.
			if(memberIndex == 0 && !ValidateKey(ArrayExtensions.TempObjectArray(memberValue)))
			{
				var valueWas = Value.Key;
				if(valueWas != memberValue)
				{
					memberValue = Value.Key;
					return; //abort immediately
				}
			}
			base.OnMemberValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
		}

		private bool ValidateKey(object[] keyValues)
		{
			var collectionParent = parent as ICollectionDrawer;
			int elementIndex = collectionParent == null ? -1 : collectionParent.GetMemberIndexInCollection(this);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(validateKey != null, "validateKey was null!");
			#endif

			return validateKey(elementIndex, keyValues);
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);
			menu.Add("Debugging/Toggle DrawInSingleRow", ()=>{drawInSingleRow = !drawInSingleRow; UpdatePrefixDrawer();});
		}
		#endif
	}
}