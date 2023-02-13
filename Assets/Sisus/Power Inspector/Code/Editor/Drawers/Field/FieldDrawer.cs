#define SAFE_MODE
//#define DISABLE_RANDOMIZE_CONTEXT_MENU_ITEM

//#define DEBUG_APPLY_VALUE
//#define DEBUG_CANT_WRITE_TO_FIELD
//#define DEBUG_GET_FIELD_GUI_DRAWER
#define DEBUG_SET_VALUE
//#define DEBUG_NULL_FIELD_INFO
//#define DEBUG_PASSES_SEARCH_FILTER
//#define DEBUG_ON_CACHED_VALUE_CHANGED
//#define DEBUG_NULL_VALUE
//#define DEBUG_VALUE_SET_TO_NULL
//#define DEBUG_MOUSE_EXIT
//#define DEBUG_READ_ONLY
//#define DEBUG_WRITE_ONLY
//#define DEBUG_SHOW_IN_INSPECTOR_IF
//#define DEBUG_UPDATE_CACHED_VALUE_FROM_FIELD

//#define DEBUG_VISUALIZE_BOUNDS

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;
using Sisus.Newtonsoft.Json;
using Sisus.Attributes;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sisus
{
	/// <summary>
	/// Drawer for a UnityEngine.Object member that has a value (field, property...)
	/// </summary>
	/// <typeparam name="TValue"> Type of the value. </typeparam>
	[Serializable]
	public abstract class FieldDrawer<TValue> : BaseDrawer, IFieldDrawer
	{
		private static readonly HashSet<object> ReusableObjectHashSet = new HashSet<object>();

		private static Texture ErrorIcon;
		private static Texture WarningIcon;

		/// <summary>
		/// Class responsible for drawing the prefix label.
		/// </summary>
		protected PrefixDrawer prefixLabelDrawer;

		/// <summary>
		/// LinkedMemberInfo of the drawer when the drawer represents a class member.
		/// </summary>
		[CanBeNull]
		protected LinkedMemberInfo memberInfo;

		/// <summary>
		/// The override has unapplied changes.
		/// </summary>
		protected Func<bool> overrideHasUnappliedChanges;

		/// <summary>
		/// If true control value portion on this and all its members will be greyed out
		/// and the value can be viewed and copied, but not altered in any way via the inspector.
		/// </summary>
		private bool readOnly = true;

		/// <summary>
		/// True if is backed by a field and can write to said field.
		/// </summary>
		private bool canWriteToField;

		/// <summary>
		/// E.g. transform.position.x. Can be used in filtering of fields by a search string.
		/// All character are always lower case.
		/// This value should be null until the moment that it's first needed, and then
		/// BuildFullClassName should be used to generate it.
		/// </summary>
		private string fullClassName;

		/// <summary>
		/// Member cached value.
		/// When representing members of multiple UnityEngine.Object targets, this contains
		/// the value of the first field.
		/// </summary>
		private TValue value;

		/// <summary>
		/// True if member is currently reorderable under current part, false if not.
		/// </summary>
		private bool isReorderable;

		/// <summary>
		/// True if prefab instance has unapplied changes, and field prefix should be drawn with bold styling, false if not.
		/// </summary>
		private bool hasUnappliedChanges;

		/// <summary>
		/// If true then field value can remain as null. If false, null value will be replaced with a non-null value during Setup phase (when possible).
		/// </summary>
		private bool canBeNull;

		protected IShowInInspectorIf showInInspectorIf;
		protected bool passedLastShowInInspectorIfTest = true;

		protected DisableIfAttribute disableIf;
		protected bool attributeWantsToDrawDisabled = true;

		private Type type;

		protected bool getValueCausedException;
		protected bool setValueCausedException;
		protected GUIContent getOrSetValueExceptionLabel;
		protected LogType getOrSetValueErrorOrWarningType = LogType.Exception;

		/// <summary>
		/// If true then field value can remain as null. If false, null value will be replaced with a non-null value during Setup phase (when possible).
		/// </summary>
		protected virtual bool CanBeNull
		{
			get
			{
				return canBeNull;
			}
		}

		/// <inheritdoc cref="IDrawer.FullClassName" />
		public override string FullClassName
		{
			get
			{
				if(fullClassName == null)
				{
					BuildFullClassName();
				}
				return fullClassName;
			}
		}
		
		/// <inheritdoc cref="IDrawer.IsReorderable" />
		public sealed override bool IsReorderable
		{
			get
			{
				return isReorderable;
			}
		}

		/// <summary>
		/// Gets the mouse down cursor top left corner offset.
		/// </summary>
		/// <value>
		/// The mouse down cursor top left corner offset.
		/// </value>
		public virtual Vector2 MouseDownCursorTopLeftCornerOffset
		{
			get
			{
				return Vector2.zero;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public virtual bool MouseDownOverReorderArea
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc cref="IDrawer.IsAnimated" />
		[JsonIgnore]
		public override bool IsAnimated
		{
			get
			{
				#if UNITY_EDITOR
				return memberInfo != null && memberInfo.IsAnimated;
				#else
				return false;
				#endif
			}
		}

		/// <inheritdoc cref="IDrawer.ReadOnly" />
		public override bool ReadOnly
		{
			get
			{
				return attributeWantsToDrawDisabled || readOnly || base.ReadOnly;
			}

			set
			{
				#if DEBUG_READ_ONLY
				Debug.Log("readonly = "+value+" (was: "+readOnly+")");
				#endif

				readOnly = value;
				if(readOnly)
				{
					canWriteToField = false;
				}
			}
		}

		/// <summary>
		/// Gets value indicating if this drawer represents a property whose value can not be read, only set.
		/// <para>
		/// <value>
		/// True if member is a write-only property, false if not.
		/// </value>
		protected bool WriteOnly
		{
			get
			{
				return memberInfo != null && !memberInfo.CanRead;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this represents multiple members on multiple UnityEngine.Objects with not all of them having the same value.
		/// </summary>
		/// <value>
		/// True if mixed content, false if not.
		/// </value>
		[JsonIgnore]
		protected virtual bool MixedContent
		{
			get
			{
				return memberInfo != null && memberInfo.MixedContent;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public override bool ShouldShowInInspector
		{
			get
			{
				return base.ShouldShowInInspector && passedLastShowInInspectorIfTest;
			}
		}

		/// <summary>
		/// Gets the value of the drawer over which the mouse button was last pressed down at the moment when the mouse button was pressed down.
		/// NOTE: This should only be called when the mouse was last pressed down over this particular drawer, and calling this can otherwise throw an invalid cast exception!
		/// </summary>
		/// <value>
		/// The values cached during mouse down.
		/// </value>
		[JsonIgnore]
		protected TValue MouseDownValue
		{
			get
			{
				return (TValue)InspectorUtility.ActiveManager.MouseDownInfo.MouseDownOverDrawerValue;
			}
		}

		/// <summary>
		/// Gets the values of the drawer over which the mouse button was last pressed down at the moment when the mouse button was pressed down.
		/// <value>
		/// The values cached during mouse down.
		/// </value>
		[JsonIgnore, NotNull]
		protected object[] MouseDownValues
		{
			get
			{
				return InspectorUtility.ActiveManager.MouseDownInfo.MouseDownOverDrawerValues;
			}
		}

		/// <inheritdoc cref="IFieldDrawer.CanReadFromFieldWithoutSideEffects" />
		public override bool CanReadFromFieldWithoutSideEffects
		{
			get
			{
				// NOTE: by default assuming that can read without side-effects if shown using FieldDrawer even if memberInfo.CanReadWithoutSideEffects is false.
				// This is so that Properties shown in the inspector as if they were normal fields (e.g. because they have been marked with ShowInInspector) are considered
				// safe to read, even if they are not auto-properties. PropertyDrawer overrides this to return false.
				return memberInfo != null && memberInfo.CanRead && (!getValueCausedException || memberInfo.PropertyInfo == null);
			}
		}
		
		/// <summary>
		/// Is it safe to read the value of this field without the risk of there being undesired side
		/// effects? Returns true for all fields, false for properties and methods that aren't considered
		/// safe based on their attributes and current display preferences.
		/// </summary>
		/// <value>
		/// True if we can read from field without risk of undesired side effects, false if not.
		/// </value>
		protected virtual bool CanWriteToFieldWithoutSideEffects
		{
			get
			{
				return memberInfo != null && memberInfo.CanWrite;
			}
		}

		/// <summary>
		/// Gets or sets Function that is evaluated instead of the normal method
		/// when determining whether the field has unapplied changes and its
		/// label should appear bolded.
		/// </summary>
		/// <value> The Func that determines if field has unapplied changes. </value>
		[CanBeNull]
		public Func<bool> OverrideHasUnappliedChanges
		{
			get
			{
				return overrideHasUnappliedChanges;
			}

			set
			{
				overrideHasUnappliedChanges = value;
				OnValidate();
			}
		}

		/// <inheritdoc cref="IDrawer.Selectable" />
		public override bool Selectable
		{
			get
			{
				return passedLastFilterCheck && ShownInInspector;
			}
		}

		/// <inheritdoc/>
		[JsonIgnore]
		public sealed override LinkedMemberHierarchy MemberHierarchy
		{
			get
			{
				if(memberInfo != null)
				{
					return memberInfo.Hierarchy;
				}
				return base.MemberHierarchy;
			}
		}

		/// <summary>
		/// Gets the cached value. or sets the cached value and value all target members.
		/// Calling get is the same as using the non-generic GetValue() in IDrawer.
		/// </summary>
		/// <value> The cached value. </value>
		[JsonIgnore]
		public virtual TValue Value
		{
			get
			{
				return value;
			}

			set
			{
				DoSetValue(value, true, true);
			}
		}

		/// <inheritdoc cref="IDrawer.Type" />
		[JsonIgnore]
		public override Type Type
		{
			get
			{
				return type;
			}
		}

		/// <inheritdoc cref="IDrawer.Tooltip" />
		public sealed override string Tooltip
		{
			set
			{
				if(label == null)
				{
					#if DEV_MODE
					Debug.Log("Tooltip = "+value+" called with label still null.");
					#endif
					return;
				}

				label.tooltip = value;

				if(prefixLabelDrawer != null)
				{
					prefixLabelDrawer.label.tooltip = value;
				}
			}
		}

		/// <inheritdoc cref="IDrawer.HasUnappliedChanges" />
		public sealed override bool HasUnappliedChanges
		{
			get
			{
				return hasUnappliedChanges;
			}

			protected set
			{
				if(hasUnappliedChanges != value)
				{
					hasUnappliedChanges = value;
					OnValidate();
				}
			}
		}

		/// <inheritdoc cref="IDrawer.UnityObject" />
		[JsonIgnore]
		public sealed override Object UnityObject
		{
			get
			{
				if(parent != null)
				{
					return parent.UnityObject;
				}
				if(memberInfo != null)
				{
					return memberInfo.UnityObject;
				}
				return null;
			}
		}

		/// <inheritdoc cref="IDrawer.UnityObjects" />
		[JsonIgnore]
		public sealed override Object[] UnityObjects
		{
			get
			{
				if(parent != null)
				{
					return parent.UnityObjects;
				}
				if(memberInfo != null)
				{
					return memberInfo.UnityObjects;
				}
				return ArrayPool<Object>.ZeroSizeArray;
			}
		}
		
		/// <summary>
		/// Gets the field values from all target UnityObjects.
		/// </summary>
		/// <value>
		/// The values of all targets.
		/// </value>
		[JsonIgnore]
		protected TValue[] Values
		{
			get
			{
				if(memberInfo != null && memberInfo.CanRead)
				{
					return memberInfo.GetValues<TValue>();
				}
				return ArrayPool<TValue>.CreateWithContent(Value);
			}
		}
		
		/// <summary>
		/// If field belongs under a Component, gets the Transform of the GameObject
		/// that holds the Component. When multiple targets are inspected, returns
		/// Transform of the first target.
		/// </summary>
		/// <value>
		/// Target GameObject's Transform or null if target is not a Component.
		/// </value>
		[CanBeNull, JsonIgnore]
		public override Transform Transform
		{
			get
			{
				var obj = UnityObject;
				return obj != null ? obj.Transform() : null;
			}
		}
		
		#if UNITY_EDITOR
		/// <summary>
		/// Gets the property modifications.
		/// </summary>
		/// <value>
		/// The property modifications.
		/// </value>
		protected PropertyModification[] PropertyModifications
		{
			get
			{
				return PrefabUtility.GetPropertyModifications(UnityObject);
			}
		}
		#endif
		
		/// <summary>
		/// Gets a value indicating whether we can write to field.
		/// </summary>
		/// <value>
		/// True if we can write to field, false if not.
		/// </value>
		private bool CanWriteToField
		{
			get
			{
				return canWriteToField && !ReadOnly && !setValueCausedException;
			}
		}
		
		/// <inheritdoc cref="IFieldDrawer.SetupInterface"/>
		public virtual void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup((TValue)setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected sealed override void Setup(IParentDrawer setParent, GUIContent setLabel)
		{
			throw new NotSupportedException("Please use the other Setup method of FieldDrawer");
		}

		/// <summary>
		/// Sets up the drawer so that it is ready to be used.
		/// LateSetup should be called right after this.
		/// </summary>
		/// <param name="setValue"> The initial cached value of the drawers. </param>
		/// <param name="setMemberInfo"> LinkedMemberInfo for the field or property that these drawers represent. </param>
		/// <param name="setParent"> Drawer whose member these Drawer are. Can be null. </param>
		/// <param name="setLabel"> The label (name) of the field. Can be null. </param>
		/// <param name="setReadOnly"> True if Drawer should be read only. </param>
		protected virtual void Setup([CanBeNull]TValue setValue, [NotNull]Type setValueType, [CanBeNull]LinkedMemberInfo setMemberInfo, [CanBeNull]IParentDrawer setParent, [CanBeNull]GUIContent setLabel, bool setReadOnly)
		{
			if(setValueType == null)
			{
				#if DEV_MODE
				Debug.LogWarning(GetType().Name+".Setup called with setValueType null");
				#endif
				setValueType = DrawerUtility.GetType(setMemberInfo, setValue);
			}

			type = setValueType;

			if(setValueType.IsValueType)
			{
				canBeNull = false;
			}
			else if(setMemberInfo != null && setMemberInfo.CanBeNull)
			{
				canBeNull = true;
			}
			else
			{
				canBeNull = false;

				#if DEV_MODE
				if(setMemberInfo != null && setMemberInfo.Parent != null && setMemberInfo.Type == setMemberInfo.Parent.Type)
				{
					Debug.LogWarning(ToString(setLabel, setMemberInfo) + " - Possible infinite loop detected! Setting canBeNull to true. setValue="+StringUtils.ToString(setValue));
				}
				#endif
			}

			memberInfo = setMemberInfo;

			if(setMemberInfo != null && setParent != null)
			{
				showInInspectorIf = setMemberInfo.GetAttribute<IShowInInspectorIf>();
				passedLastShowInInspectorIfTest = showInInspectorIf == null || showInInspectorIf.ShowInInspector(setParent.Type, setParent.GetValue(), setMemberInfo.MemberInfo);

				disableIf = setMemberInfo.GetAttribute<DisableIfAttribute>();
				attributeWantsToDrawDisabled = disableIf != null && disableIf.ShouldDrawDisabled(setParent.Type, setParent.GetValue(), setMemberInfo.MemberInfo);
			}
			else
			{
				showInInspectorIf = null;
				passedLastShowInInspectorIfTest = true;
				disableIf = null;
				attributeWantsToDrawDisabled = false;
			}

			#if DEV_MODE
			Debug.Assert(setMemberInfo == null || setMemberInfo.Type.IsAssignableFrom(setMemberInfo.Type), ToString(setLabel, setMemberInfo) + ".Setup - memberInfo " + setMemberInfo + " type " + StringUtils.ToStringSansNamespace(setMemberInfo == null ? null : setMemberInfo.Type)+" not assignable from TValue "+StringUtils.ToStringSansNamespace(typeof(TValue)) + "!");
			#endif
			
			#if DEV_MODE
			if(setParent != null && setParent.ReadOnly && !setReadOnly && GetType() != typeof(ParameterDrawer))
			{
				Debug.LogWarning(StringUtils.ToColorizedString(ToString(setLabel, setMemberInfo), ".Setup - readonly=", false, " but parent.ReadOnly=", true, ". Intentional?"));
			}
			#endif

			//setting readOnly should happen before UpdatePrefixDrawer, so it won't try and get SerializedProperty
			if(setReadOnly)
			{
				readOnly = true;
				canWriteToField = false;
			}
			// Set fields with readonly keyword and properties with no setter as readOnly.
			else if(setMemberInfo != null)
			{
				// NOTE: using MemberData.CanWrite instead of LinkedMemberInfo.CanWrite directly,
				// because the prior only considers things like readonly keyword and property setter,
				// while the latter also considers things like whether or not the parent chain is broken.
				// We don't want to necessarily grey out fields just because their parent chain is broken,
				// because being able to change the values might still be desirable, with effects applied
				// through means such as OnMemberValueChanged or the onValueChanged delegate.
				var memberData = setMemberInfo.Data;
				if(memberData.CanWrite)
				{
					readOnly = false;
					canWriteToField = true;
				}
				// New test: allow readonly controls to be edited in the inspector if their parent is a struct which is not readonly.
				// E.g. KeyValuePair members are readonly properties, but still intuitively feel like they should be editable,
				// with the changes being applied on the parent level.
				else if(setParent != null && setParent.MemberInfo != null && setParent.MemberInfo.CanWrite && setParent.Type.IsValueType)
				{
					readOnly = false;
					canWriteToField = false;
				}
				else
				{
					// If MemberInfo does not represent a FieldInfo or a PropertyInfo don't automatically make it readonly.
					readOnly = setMemberInfo.MemberType == MemberTypes.Field || setMemberInfo.MemberType == MemberTypes.Property;
					canWriteToField = false;
				}
			}
			else
			{
				readOnly = false;
				canWriteToField = false;
			}

			#if DEV_MODE && (DEBUG_READ_ONLY || DEBUG_WRITE_ONLY)
			if(readOnly || !canWriteToField)
			{
				if(setMemberInfo != null) { Debug.Log(StringUtils.ToColorizedString(ToString(setLabel, setMemberInfo), " readOnly=", readOnly, ", setReadOnly=", setReadOnly, ", canWriteToField=", canWriteToField, ", memberInfo=", setMemberInfo, ", memberInfo.CanWrite=", setMemberInfo.CanWrite, ", Data.CanWrite=", setMemberInfo.Data.CanWrite, ", parent.MemberInfo=", setParent == null ? null : setParent.MemberInfo)); }
				else { Debug.Log(StringUtils.ToColorizedString(ToString(setLabel, setMemberInfo), " readOnly=", readOnly, ", setReadOnly=", setReadOnly, ", canWriteToField=", canWriteToField, ", setMemberInfo=", null, ", setParent.MemberInfo=", setParent == null ? null : setParent.MemberInfo)); }
			}
			#endif
			
			if(setValue == null && !CanBeNull)
			{
				setValue = GetFirstFieldOrDefaultValue();

				#if DEV_MODE && PI_ASSERTATIONS
				if(setValue == null)
				{
					Debug.LogError(ToString()+ ".Setup called with null value, CanBeNull=false and GetFirstFieldOrDefaultValue() returned null.");
				}
				#endif

				#if DEV_MODE && DEBUG_NULL_VALUE
				Debug.Log(Msg(ToString(), ".Setup - setValue from ", null, " to  ", setValue, " with setMemberInfo=", setMemberInfo, ", CanWriteToFieldWithoutSideEffects =", CanWriteToFieldWithoutSideEffects, ", IsStatic=", (setMemberInfo == null ? false : setMemberInfo.IsStatic), ", IsGenericType=", (setMemberInfo == null ? false : setMemberInfo.Type.IsGenericType)));
				#endif

				// NOTE: CanReadWithoutSideEffects was added so that Property setters aren't called arbitrarily,
				// since they could have unpredictable side-effects
				if(setValue != null && setMemberInfo != null && CanWriteToFieldWithoutSideEffects && !setMemberInfo.IsStatic)
				{
					setMemberInfo.SetValue(setValue);
				}
			}

			if(setLabel == null)
			{
				setLabel = setMemberInfo != null ? setMemberInfo.GetLabel() : GUIContentPool.Empty();
			}

			value = setValue;
			InspectorValues.Register(this, value);

			GenerateValidationOverrideFromAttributes();
			
			base.Setup(setParent, setLabel);

			// Apply possible per-Drawer override to CanReadWithoutSideEffects on the LinkedMemberInfo level.
			// E.g. while it is not usually safe to call getters of properties that are not auto-properties,
			// in some instances we know that it is safe to call the getter on some specific properties
			// (transform member, when they have ShowInInspector attribute etc.).
			if(setMemberInfo != null)
			{
				setMemberInfo.CanReadWithoutSideEffects = CanReadFromFieldWithoutSideEffects;
			}

			HasUnappliedChanges = GetHasUnappliedChangesUpdated();

			#if DEV_MODE
			if(showInInspectorIf != null) { Debug.Log(ToString()+".passedLastShowInInspectorIfTest = "+ passedLastShowInInspectorIfTest); }
			#endif

			#if DEV_MODE
			//if(!Type.IsPrimitive && Type != typeof(string)) { Debug.Log(ToString() + ".Setup with CanBeNull="+CanBeNull+", Type="+StringUtils.ToStringSansNamespace(Type)+", Value="+StringUtils.ToString(Value)+ ", FullClassName=" + FullClassName+", Parent="+ (parent != null ? parent.ToString() : "null")); }
			#endif
		}

		/// <summary>
		/// Gets the first field or default value.
		/// </summary>
		/// <returns>
		/// The first field or default value.
		/// </returns>
		protected TValue GetFirstFieldOrDefaultValue()
		{
			#if DEV_MODE
			if(CanBeNull) { Debug.LogWarning("GetFirstFieldOrDefaultValue called for field with CanBeNull true. Should we return null here? Should this method call be removed?"); }
			#endif

			var result = default(TValue);
			if(CanReadFromFieldWithoutSideEffects)
			{
				result = memberInfo.GetValue<TValue>(0);
			}

			if(result == null)
			{
				try
				{
					result = (TValue)DefaultValue();
				}
				catch(InvalidCastException e)
				{
					var defaultValue = DefaultValue();
					Debug.LogError(ToString()+ ".GetFirstOrDefaultValue - Could not cast DefaultValue " + StringUtils.ToString(defaultValue)+" of type "+StringUtils.TypeToString(defaultValue)+" to type "+typeof(TValue).Name+"\n"+e+"\nType="+Type+", HasLinkedMemberInfo="+(memberInfo != null ? "True" : "False"));
				}
			}
			return result;
		}

		/// <inheritdoc cref="IDrawer.OnParentAssigned" />
		public override void OnParentAssigned(IParentDrawer newParent)
		{
			isReorderable = GetIsReorderable();
		}

		/// <inheritdoc cref="IDrawer.LateSetup" />
		public override void LateSetup()
		{
			UpdatePrefixDrawer();
			base.LateSetup();

			#if DEV_MODE && DEBUG_CONTROL_ID
			if(label.text.Length > 0) { OnNextLayout(()=>label.text = label.text + " ("+controlId+")"); }
			#endif
		}

		/// <summary>
		/// Generates full class name of field and stores it in the variable fullClassName.
		/// All characters will be in lower case for easier filtering.
		/// </summary>
		private void BuildFullClassName()
		{
			var sb = StringBuilderPool.Create();
			sb.Append(Name);
			for(var p = parent; p != null; p = p.Parent)
			{
				var parentType = p.GetType();
				
				if(parentType == typeof(GameObjectDrawer))
				{
					break;
				}

				// add Type instead of Name for Components and Assets and stop after that
				if(typeof(IUnityObjectDrawer).IsAssignableFrom(parentType))
				{
					sb.Insert(0, '.');
					sb.Insert(0, StringUtils.ToStringSansNamespace(p.Type));
					break;
				}

				// stop if parent implements IRootDrawer (and wasn't GameObjectDrawer or IUnityObjectDrawer)
				if(typeof(IRootDrawer).IsAssignableFrom(parentType))
				{
					break;
				}

				sb.Insert(0, '.');
				sb.Insert(0, StringUtils.RemoveSpaces(p.Name));
			}
			fullClassName = StringBuilderPool.ToStringAndDispose(ref sb).ToLower();
		}

		/// <summary>
		/// Sets cached value silent.
		/// </summary>
		/// <param name="setValue">
		/// The set value. </param>
		protected void SetCachedValueSilent(TValue setValue)
		{
			value = setValue;
		}

		/// <inheritdoc cref="IDrawer.CopyToClipboard" />
		public override void CopyToClipboard()
		{
			if(MixedContent)
			{
				Clipboard.TryCopy(Values);
				SendCopyToClipboardMessage();
				return;
			}
			var valueToCopy = Value;
			Clipboard.TryCopy(valueToCopy, valueToCopy == null ? Type : valueToCopy.GetType());
			SendCopyToClipboardMessage();
		}

		/// <inheritdoc/>
		protected override void DoPasteFromClipboard()
		{
			var assignableType = GetAssignableType();

			if(MixedContent)
			{
				object multipleValues = null;
				var arrayType = assignableType.MakeArrayType();
				if(Clipboard.TryPaste(arrayType, ref multipleValues))
				{
					var array = multipleValues as object[];
					if(array != null)
					{
						SetValues(array);
						return;
					}
				}
			}

			if(assignableType.IsAbstract || assignableType == Types.SystemObject)
			{
				SetValue(Clipboard.Paste(Clipboard.CopiedType));
			}
			else
			{
				SetValue(Clipboard.Paste(assignableType));
			}
		}

		/// <summary>
		/// Returns interface, value type or base type of classes which can be assigned to the field.
		/// 
		/// Usually GetAssignableType() is equal to Type, but sometimes Type can be a more specific derived type.
		/// E.g. in PolymorphicDrawer Type can return the current working type selected by the user.
		/// GetAssignableType on the other hand always returns the simplest base type that can be assigned as a value to the drawer.
		/// As such GetAssignableType() should be used instead of Type when determing e.g. whether or not a copied value can be pasted
		/// on the drawer.
		/// </summary>
		/// <returns> Interface, class or value type. </returns>
		protected virtual Type GetAssignableType()
		{
			// new test
			if(memberInfo != null && !memberInfo.Type.IsAssignableFrom(Type))
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ ".GetAssignableType returning memberInfo.Type "+ memberInfo.Type.Name+ " instead of Type "+Type.Name+" because memberInfo.Type was not assignable from Type");
				#endif
				return memberInfo.Type;
			}
			return Type;
		}

		/// <inheritdoc/>
		protected override string GetCopyToClipboardMessage()
		{
			return "Copied{0} value";
		}

		/// <inheritdoc/>
		protected override string GetPasteFromClipboardMessage()
		{
			return memberInfo != null && memberInfo.MixedContent ? "Pasted values{0}." : "Pasted value{0}.";
		}

		/// <inheritdoc/>
		public override bool CanPasteFromClipboard()
		{
			return !ReadOnly && Clipboard.CanPasteAs(GetAssignableType());
		}

		/// <inheritdoc cref="IDrawer.SetValue(object)" />
		public override bool SetValue(object setValue)
		{
			try
			{
				return DoSetValue((TValue)setValue, true, true);
			}
			catch(InvalidCastException e)
			{
				Debug.LogError(ToString()+".SetValue - Could not cast value " + StringUtils.ToStringCompact(setValue) + " of type "+StringUtils.TypeToString(setValue) +" to type "+typeof(TValue).Name+"\n"+e+"\nType="+ StringUtils.ToString(Type) + ", HasLinkedMemberInfo="+(StringUtils.ToColorizedString(memberInfo != null)));
				return DoSetValue((TValue)DefaultValue(), true, true);
			}
		}
		
		/// <summary>
		/// Sets values on all LinkedMemberInfo targets (if any) as well as the cached value.
		/// If there's no LinkedMemberInfo, then sets cached value only using first value in array.
		/// Will also update all member drawers, unless inactive is true.
		/// </summary>
		/// <param name="values"> The values for all targets. </param>
		/// <returns> True if cached value changed, false if nothing happened. </returns>
		protected bool SetValues(object[] values)
		{
			return SetValues(values, !inactive, inactive);
		}

		/// <summary>
		/// Sets values on all LinkedMemberInfo targets (if any) as well as the cached value.
		/// If there's no LinkedMemberInfo, then sets cached value only using first value in array.
		/// </summary>
		/// <param name="values"> The values for all targets. </param>
		/// <param name="updateMembers">
		/// Should member drawers be rebuilt? Set this to false if only if you intent to
		/// manually update the members to match the new values.
		/// </param>
		/// <param name="setCachedValueSilently">
		/// Should cached value be set silently without broadcasting the OnCachedValueChanged event?
		/// You should generally only set this to false if you call OnCachedValueChanged manually
		/// later on.
		/// </param>
		/// <returns> True if cached value changed, false if nothing happened. </returns>
		protected bool SetValues(object[] values, bool updateMembers, bool setCachedValueSilently)
		{
			bool mixedContentWas = MixedContent;
			bool cachedValueChanged;

			// handle setting cached value
			if(setCachedValueSilently)
			{
				var setValue = (TValue)values[0];
				if(!ValueEquals(setValue))
				{
					SetCachedValueSilent(setValue);
					cachedValueChanged = true;
				}
				else
				{
					cachedValueChanged = false;
				}
			}
			else
			{
				// Handles calling OnCachedValueChanged if it changed cached value was not already was equal to values[0].
				// If however cached value already was equal to values[0] then it is not called here,
				// even if MixedValue state changes before the end of the method.
				cachedValueChanged = SetValue(values[0], false, updateMembers);
			}

			if(CanWriteToField)
			{
				ApplyValuesToFields(values);
			}
			#if DEV_MODE
			else if(values.Length > 1) { Debug.LogWarning(Msg(ToString(), ".SetValues was called with " + values.Length+" values but CanWriteToField was false, so all values except first value will be ignored. ")); }
			#endif
			
			// If cached value did not change, but MixedContent changed, we still want to call OnCachedValueChanged
			if(!cachedValueChanged && MixedContent != mixedContentWas)
			{
				OnCachedValueChanged(false, updateMembers);
				return true;
			}

			return cachedValueChanged;
		}

		/// <summary>
		/// Apply values to drawer targets if possible.
		/// This is usually done using the LinkedMemberInfo of the drawer.
		/// If CanWriteToField is false, this usually does nothing.
		/// </summary>
		/// <param name="values"> Values to write. </param>
		protected virtual void ApplyValuesToFields(object[] values)
		{
			if(!CanWriteToField)
			{
				#if DEV_MODE
				if(values.Length > 1) { Debug.LogWarning(Msg(ToString(), ".HandleWriteToField was called with "+ values.Length+" values but CanWriteToField was false, so all values except first value will be ignored. ")); }
				#endif
				return;
			}

			#if DEV_MODE
			Debug.Assert(!ReadOnly);
			Debug.Assert(memberInfo != null);
			Debug.Assert(CanWriteToField, ToString()+ ".WriteToField was called but CanWriteToField was false!");
			#endif
				
			if(memberInfo.SetValues(values))
			{
				OnFieldBackedValueChanged();
			}
		}

		/// <inheritdoc cref="IDrawer.SetValue(object,bool,bool)" />
		public sealed override bool SetValue(object setValue, bool applyToField, bool updateMembers)
		{
			return DoSetValue((TValue)setValue, applyToField, updateMembers);
		}

		/// <summary>
		/// Sets cached value and has paramaters for controlling whether or not
		/// value should also get applied to all target fields and whether or
		/// not cached values of member drawers should be updated.
		/// </summary>
		/// <param name="setValue"> The value to set the cached value. </param>
		/// <param name="applyToField"> True to apply value to target fields. </param>
		/// <param name="updateMembers"> True to update cached values of member drawers. </param>
		/// <returns> True if cached value changed, false if nothing happened. </returns>
		protected virtual bool DoSetValue(TValue setValue, bool applyToField, bool updateMembers)
		{
			#if SAFE_MODE || DEV_MODE
			if(ReadOnly)
			{
				#if DEV_MODE
				Debug.LogWarning(Msg(ToString(), " - Value.set was called for readOnly field - ignoring."));
				//UPDATE: This broke MethodDrawer that were using readonly fields for out values!
				//the problem is I'm using readonly for two things: to indicate missing fieldInfos, and to
				//indicate fields whose values should be locked
				#endif
				return false;
			}
			#endif

			#if DEV_MODE && DEBUG_SET_VALUE
			if(MemberInfo == null || MemberInfo.CanRead) { Debug.Log(Msg(ToString(), ".Value = ", setValue, " (was: ", value, ") with applyToField=", applyToField, ", updateMembers=", updateMembers, ", ValueEquals(setValue)=", MixedContent ? StringUtils.Green("(WasMixed)") : StringUtils.ToColorizedString(ValueEquals(setValue)))); }
			else { Debug.Log(Msg(ToString(), ".Value = ", setValue, " (was: ", value, ") with applyToField=", applyToField, ", updateMembers=", updateMembers)); }
			#endif

			bool callOnValueChanged;
			if(memberInfo == null)
            {
				callOnValueChanged = !ValueEquals(setValue);
			}
			else if(!memberInfo.CanRead)
			{
				callOnValueChanged = true;
			}
			else if(memberInfo.MixedContent)
            {
				callOnValueChanged = applyToField;
			}
			else
            {
				callOnValueChanged = !ValueEquals(setValue);
			}

			if(callOnValueChanged)
			{
				value = setValue;
				OnCachedValueChanged(applyToField && CanWriteToField, updateMembers);

				#if DEV_MODE && DEBUG_VALUE_SET_TO_NULL
				Debug.Assert(value != null || Type.IsUnityObject() || GetType() == typeof(NullableDrawer) || GetType() == typeof(ObjectReferenceDrawer) ||
					GetType() == typeof(AbstractDrawer) || GetType() == typeof(ObjectDrawer),
						ToString()+ " value was null after Value.set was called (this is not always necessarily a bug)");
				#endif
				return true;
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public override void UpdateCachedValuesFromFieldsRecursively()
		{
			UpdateCachedValueFromField(true);
		}

		/// <summary>
		/// Updates the cached value from current value of the class.
		/// </summary>
		/// <returns> True if successfully updated value or there is no need to update the value, false if was unable to update value. </returns>
		protected virtual bool UpdateCachedValueFromField(bool updateMembers)
		{
			if(!CanReadFromFieldWithoutSideEffects)
			{
				#if DEV_MODE && DEBUG_UPDATE_CACHED_VALUE_FROM_FIELD
				Debug.Log(ToString()+" Aborting UpdateCachedValueFromField because CanReadFromFieldWithoutSideEffects was false.");
				#endif
				return false;
			}

			// Don't update cached value when has mixed content as it serves no function. The cached value is not displayed anywhere.
			if(MixedContent)
			{
				#if DEV_MODE && DEBUG_UPDATE_CACHED_VALUE_FROM_FIELD
				Debug.Log(ToString()+" UpdateCachedValueFromField returning "+StringUtils.True+" because MixedContent was true.");
				#endif

				// Return true because we did not FAIL to update the value, there's simply no need to update it.
				return true;
			}

			TValue setValue;
			try
			{
				setValue = (TValue)memberInfo.GetValue(0);
			}
			catch(Exception e)
			{
				if(ExitGUIUtility.ShouldRethrowException(e))
				{
					throw;
				}

				OnExceptionWhenCallingGetter(e);

				#if DEV_MODE
				if(e is InvalidCastException)
				{
					var getValue = memberInfo.GetValue(0);

					string getValueString;
					try
					{
						getValueString = StringUtils.ToString(getValue);
					}
					catch(Exception toStringException)
					{
						Debug.LogWarning(toStringException);
						getValueString = "{ERROR}";
					}

					Debug.LogError(ToString() + ".UpdateCachedValueFromField Could not cast GetValue(0) " + getValueString + " of type " + StringUtils.TypeToString(getValue) + " to type " + typeof(TValue).Name + "\n" + e + "\nType=" + Type + ", HasLinkedMemberInfo=" + (memberInfo != null ? "True" : "False"));
				}
				else
				{
					Debug.LogError(ToString() + ".UpdateCachedValueFromField GetValue(0) "+ e + "\nType=" + Type + ", HasLinkedMemberInfo=" + (memberInfo != null ? "True" : "False"));
				}
				#endif
				
				return false;
			}

			if(!ValueEquals(setValue))
			{
				value = setValue;
				OnCachedValueChanged(false, true);
			}

			return true;
		}

		protected virtual void OnErrorOrWarningWhenCallingGetter(string message, LogType logType)
		{
			getValueCausedException = true;
			getOrSetValueErrorOrWarningType = logType;

			Texture icon;
			if(logType == LogType.Warning)
			{
				if(WarningIcon == null)
				{
					WarningIcon = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture;
				}
				icon = WarningIcon;
				message = message.Replace(Application.dataPath, "Assets");
			}
			else
			{
				if(ErrorIcon == null)
				{
					ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture;
				}
				icon = ErrorIcon;
				message = logType == LogType.Exception ? message.Replace(Application.dataPath.Replace('/', '\\'), "Assets") : message.Replace(Application.dataPath, "Assets");
			}
			getOrSetValueExceptionLabel = new GUIContent("", icon, message);

			// Push error to label tooltip so that users can more easily see where the field from which it came from.
			// Don't do this with PropertyDrawers though, because they have separate handling for the error, and can
			// display both the tooltip and the error simultaneously.
			if(!(this is PropertyDrawer) && label != null)
			{
				if(prefixLabelDrawer == null)
				{
					label.tooltip = getOrSetValueExceptionLabel.tooltip;
				}
				else
				{
					Tooltip = getOrSetValueExceptionLabel.tooltip;
				}
			}
		}

		protected void OnExceptionWhenCallingGetter(Exception exception)
		{
			OnErrorOrWarningWhenCallingGetter(exception.ToString(), LogType.Exception);
		}

		protected virtual void OnExceptionWhenCallingSetter(Exception exception)
		{
			setValueCausedException = true;

			if(ErrorIcon == null)
			{
				ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture;
			}

			getOrSetValueExceptionLabel = new GUIContent("", ErrorIcon, exception.ToString().Replace(Application.dataPath.Replace('/', '\\'), "Assets"));

			if(!(this is PropertyDrawer))
			{
				readOnly = true; // new test: make the field readonly to make it greyed out in order to make it clearer that auto-updating has been disabled
				GUI.changed = true;
			}

			if(prefixLabelDrawer == null)
			{
				label.tooltip = getOrSetValueExceptionLabel.tooltip;
			}
			else
			{
				Tooltip = getOrSetValueExceptionLabel.tooltip;
			}
		}

		/// <inheritdoc cref="IDrawer.GetValue()" />
		public sealed override object GetValue()
		{
			return Value;
		}

		/// <inheritdoc cref="IDrawer.GetValue(int)" />
		public override object GetValue(int index)
		{
			if(memberInfo != null && memberInfo.CanRead)
			{
				return memberInfo.GetValue(index);
			}
			return Value;
		}

		/// <inheritdoc cref="IDrawer.GetValues" />
		public override object[] GetValues()
		{
			if(memberInfo != null && memberInfo.CanRead)
			{
				return memberInfo.GetValues();
			}
			return ArrayPool<object>.CreateWithContent(Value);
		}

		/// <inheritdoc/>
		public object[] GetCopyOfValues()
		{
			return ArrayPool<TValue>.Cast<object>(GetCopyOfValuesInternal());
		}

		/// <inheritdoc/>
		public override void OnSiblingValueChanged(int memberIndex, object memberValue, [CanBeNull] LinkedMemberInfo memberLinkedMemberInfo)
		{
			base.OnSiblingValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);

			if(showInInspectorIf != null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(parent != null);
				Debug.Assert(memberInfo != null);
				#endif

				bool set = showInInspectorIf.ShowInInspector(parent.Type, parent.GetValue(), memberInfo.MemberInfo);
				if(set != passedLastShowInInspectorIfTest)
				{
					passedLastShowInInspectorIfTest = set;
					OnNextLayout(parent.UpdateVisibleMembers);
				}

				#if DEV_MODE && DEBUG_SHOW_IN_INSPECTOR_IF
				Debug.Log(ToString()+".passedLastShowInInspectorIfTest = "+ passedLastShowInInspectorIfTest);
				#endif
			}

			if(disableIf != null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(parent != null);
				Debug.Assert(memberInfo != null);
				#endif

				bool set = disableIf.ShouldDrawDisabled(parent.Type, parent.GetValue(), memberInfo.MemberInfo);
				if(set != attributeWantsToDrawDisabled)
				{
					attributeWantsToDrawDisabled = set;
					Inspector.InspectorDrawer.Repaint();
				}

				#if DEV_MODE && DEBUG_SHOW_IN_INSPECTOR_IF
				Debug.Log(ToString()+ ".shouldDrawDisabledWasTrue = " + attributeWantsToDrawDisabled);
				#endif
			}
		}

		/// <summary> Generates and returns a deep copy of values of all targets. </summary>
		/// <returns> An array of target values. </returns>
		protected TValue[] GetCopyOfValuesInternal()
		{
			if(memberInfo != null && memberInfo.CanRead)
			{
				//this is guaranteed to be a newly-created array, so no need to clone at this level
				var values = memberInfo.GetValues<TValue>();
				for(int n = values.Length - 1; n >= 0; n--)
				{
					values[n] = GetCopyOfValue(values[n]);
				}
				return values;
			}
			return ArrayPool<TValue>.CreateWithContent(GetCopyOfValue(Value));
		}

		/// <summary> Generates and returns a deep copy of given value. </summary>
		/// <param name="source"> Source to copy. </param>
		/// <returns> A deep copy of value. For null returns null. For UnityEngine.Objects, returns source as is. </returns>
		[CanBeNull]
		protected virtual TValue GetCopyOfValue([CanBeNull]TValue source)
		{
			try
			{
				return (TValue)PrettySerializer.Copy(source);
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError("Failed to copy object of type "+source.GetType().Name+". Returning it as is. "+e);
			#else
			catch(Exception)
			{
			#endif
				return source;
			}
		}

		/// <summary>
		/// Applies the value to the drawer target field or property using LinkedMemberInfo, if possible.
		/// If CanWriteToField is false, this does nothing.
		/// </summary>
		protected virtual void ApplyValueToField()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!inactive, "ApplyValueToField was called for inactive Drawer: "+ToString());
			Debug.Assert(CanWriteToField, ToString()+".ApplyValueToField was called with !CanWriteToField");
			Debug.Assert(memberInfo != null, "ApplyValueToField  called with memberInfo null");
			if(memberInfo != null) { Debug.Assert(!memberInfo.ParentChainIsBroken); }
			Debug.Assert(!ReadOnly);
			#endif

			#if DEV_MODE && DEBUG_APPLY_VALUE
			Debug.Log(Msg(GetType().Name,  ".ApplyValueToField(", value, ") with CanWriteToField=", CanWriteToField, ", memberInfo.CanWrite=", (memberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberInfo.CanWrite)), ", ReadOnly=", ReadOnly, ", DraggingPrefix=", (this as IDraggablePrefix) != null && (this as IDraggablePrefix).DraggingPrefix, ", MixedContent=", MixedContent));
			#endif
		
			if(CanWriteToField)
			{
				try
				{
					if(memberInfo.SetValue(value))
					{
						OnFieldBackedValueChanged();
					}
				}
				catch(Exception e)
				{
					OnExceptionWhenCallingSetter(e);
				}
			}
			#if DEV_MODE && DEBUG_CANT_WRITE_TO_FIELD
			else { Debug.LogWarning(ToString()+" - CanWriteToField was false with inactive=" + inactive, UnityObject); }
			#endif
		}
		
		/// <summary>
		/// Called when value of field or property has changed (via LinkedMemberInfo)
		/// </summary>
		protected virtual void OnFieldBackedValueChanged()
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(CanReadFromFieldWithoutSideEffects && !ValuesAreEqual(memberInfo.GetValue<TValue>(0), Value))
			{
				Debug.LogError(Msg(ToString(), ".OnFieldBackedValueChanged cached value did not equal memberInfo.GetValue(0).\nValue=", Value, ", GetValue(0)=", GetValue(0)));
			}
			#endif

			#if DEV_MODE && DEBUG_APPLY_VALUE
			Debug.Log(GetType().Name+ ".OnFieldBackedValueChanged(" + StringUtils.ToString(value)+ ") with CanWriteToField=" + StringUtils.ToColorizedString(CanWriteToField) + ", memberInfo.CanWrite=" + (memberInfo == null ? StringUtils.Null : StringUtils.ToColorizedString(memberInfo.CanWrite))+ ", ReadOnly="+ StringUtils.ToColorizedString(ReadOnly));
			#endif

			// UPDATE: Make sure that cached value is up-to-date, in case OnValidate, IComponentModifiedCallbackReceiver etc. has modified it.
			// Otherwise things like OnValueChanged and OnMemberValueChanged could get called with values that are no longer up-to-date.
			if(CanReadFromFieldWithoutSideEffects && !MixedContent)
			{
				value = (TValue)memberInfo.GetValue(0);
			}
		}

		/// <summary>
		/// Called when the cached field value is changed.
		/// </summary>
		/// <param name="applyToField"> True to apply value to fields of all targets (using LinkedMemberInfo). </param>
		/// <param name="updateMembers"> True if should also update values of any member drawers, or rebuild them if needed. </param>
		protected virtual void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			GUI.changed = true;
			Inspector.InspectorDrawer.Repaint();

			#if DEV_MODE && DEBUG_ON_CACHED_VALUE_CHANGED
			Debug.Log(StringUtils.ToColorizedString(ToString(), ".OnCachedValueChanged: ", StringUtils.ToString(value), " with applyToField=", applyToField, ", updateMembers = ", updateMembers, ", CanWriteToField=", CanWriteToField, ", OnValueChanged=", StringUtils.ToString(OnValueChanged), ", inactive=", inactive, ", overrideHasUnappliedChanges=", StringUtils.ToString(overrideHasUnappliedChanges)));
			#endif

			if(applyToField && CanWriteToField)
			{
				ApplyValueToField();
			}

			HasUnappliedChanges = GetHasUnappliedChangesUpdated();

			OnValidate();

			InspectorValues.Register(this, value);

			if(OnValueChanged != null)
			{
				OnValueChanged(this, value);
			}
		}

		/// <summary>
		/// Gets has unapplied changes updated.
		/// </summary>
		/// <returns>
		/// True if it succeeds, false if it fails.
		/// </returns>
		protected virtual bool GetHasUnappliedChangesUpdated()
		{
			if(overrideHasUnappliedChanges != null)
			{
				return overrideHasUnappliedChanges();
			}

			#if UNITY_EDITOR
			return canWriteToField && memberInfo.HasUnappliedChanges;
			#else
			return false;
			#endif
		}

		/// <inheritdoc cref="IDrawer.OnMouseoverEnter" />
		public override void OnMouseoverEnter(Event inputEvent, bool isDrag)
		{
			base.OnMouseoverEnter(inputEvent, isDrag);
			if(!isDrag)
			{
				UpdatePrefixDrawer();
			}
		}

		/// <inheritdoc cref="IDrawer.OnMouseoverExit" />
		public override void OnMouseoverExit(Event inputEvent)
		{
			#if DEV_MODE && DEBUG_MOUSE_EXIT
			Debug.Log(ToString()+".OnMouseoverExit("+StringUtils.ToString(inputEvent)+")");
			#endif

			base.OnMouseoverExit(inputEvent);
			UpdatePrefixDrawer();
		}

		/// <summary>
		/// Recreates the prefix drawer.
		/// </summary>
		protected virtual void UpdatePrefixDrawer()
		{
			if(prefixLabelDrawer != null)
			{
				prefixLabelDrawer.Dispose();
			}

			if(Preferences.enableTooltipIcons)
			{
				prefixLabelDrawer = PrefixDrawer.CreateLabel(label.text, Selected, Mouseovered, HasUnappliedChanges);
			}
			else
            {
				prefixLabelDrawer = PrefixDrawer.CreateLabel(label, Selected, Mouseovered, HasUnappliedChanges);
			}
		}

		/// <inheritdoc/>
		protected override void OnValidate()
		{
			if(inactive)
			{
				return;
			}

			UpdatePrefixDrawer();
			base.OnValidate();
		}

		/// <inheritdoc cref="IDrawer.MemberInfo" />
		public override LinkedMemberInfo MemberInfo
		{
			get
			{
				return memberInfo;
			}
		}

		/// <inheritdoc cref="IDrawer.GetOptimalPrefixLabelWidth" />
		public override float GetOptimalPrefixLabelWidth(int indentLevel)
		{
			float result;
			try
			{
				if(label != null)
				{
					result = DrawerUtility.GetOptimalPrefixLabelWidth(indentLevel, label, HasUnappliedChanges);
				}
				else
				{
					result = DrawGUI.DefaultPrefixLabelWidth;
					#if DEV_MODE
					Debug.LogWarning(GetType().Name+" \""+Name+"\" GetOptimalPrefixLabelWidth was called with label being null (inactive="+inactive+")");
					#endif
				}
			}
			#if DEV_MODE
			catch(Exception e)
			{
				Debug.LogError(GetType().Name + ".GetOptimalPrefixLabelWidth " + e);
			#else
			catch
			{
			#endif
				result = DrawGUI.DefaultPrefixLabelWidth;
			}

			return result;
		}

		/// <summary>
		/// Tests cached value equality against given value.
		/// 
		/// This not be called if MixedContent is true. If this happens, it will always returns false.
		/// You can use ValuesAreEqual to compare Value to another value, if you need to compare against the cached value.
		/// </summary>
		/// <param name="other">
		/// value to test against cached value. </param>
		/// <returns>
		/// True if cached value equals other value.
		/// </returns>
		[Pure]
		protected bool ValueEquals(TValue other)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			if(MemberInfo != null && !MemberInfo.CanRead){ Debug.LogError(ToString()+".ValueEquals called with MemberInfo.CanRead="+StringUtils.False); }
			if(!CanReadFromFieldWithoutSideEffects){ Debug.LogWarning(ToString()+".ValueEquals called with CanReadFromFieldWithoutSideEffects="+StringUtils.False); }
			if(MixedContent){ Debug.LogWarning(ToString()+".ValueEquals was asked against value "+StringUtils.ToString(other)+" but MixedContent was true. Intentional?");}
			#endif
			
			return !MixedContent && ValuesAreEqual(value, other);
		}

		/// <summary>
		/// Tests whether or not values are equal to one another.
		/// </summary>
		/// <param name="a"> First value. </param>
		/// <param name="b"> Second value. </param>
		/// <returns>
		/// True if values are equal to each other.
		/// </returns>
		[Pure]
		protected virtual bool ValuesAreEqual(TValue a, TValue b)
		{
			if(a == null)
			{
				return b == null;
			}
			return a.Equals(b);
		}

		/// <inheritdoc/>
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			bool canWrite = !ReadOnly;

			if(canWrite)
			{
				var parentCollection = parent as ICollectionDrawer;
				if(parentCollection != null)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Delete", DeleteInCollection);
					menu.Add("Duplicate", Duplicate);
				}

				bool addSeparator = true;
				if(CanReset)
				{
					menu.AddSeparatorIfNotRedundant();
					addSeparator = false;

					if(WriteOnly || !ValueEquals((TValue)DefaultValue()))
					{
						menu.Add("Reset", Reset);
					}
					else
					{
						menu.AddDisabled("Reset", "Target already has default value.");
					}
				}

				if(CanBeNull)
				{
					if(WriteOnly || Value != null)
					{
						if(addSeparator)
						{
							menu.AddSeparatorIfNotRedundant();
						}
						menu.Add("Set To Null", () => SetValue(null));
					}
					else if(!Type.IsAbstract && !Type.IsUnityObject() && Type != Types.SystemObject && Type != Types.Enum && Type != Types.Type && !Type.IsGenericTypeDefinition)
					{
						object defaultValue;
						try
						{
							defaultValue = DefaultValue(true);
						}
						catch
						{
							defaultValue = null;
						}
						if(!ReferenceEquals(defaultValue, null))
						{
							if(addSeparator)
							{
								menu.AddSeparatorIfNotRedundant();
							}
							menu.Add("Create Instance", () => SetValue(defaultValue));
						}
					}
				}
			}

			AddCopyMenuItems(ref menu, true);

			if(canWrite)
			{
				if(CanPasteFromClipboard())
				{
					AddPasteMenuItems(ref menu);
				}
				
				#if !DISABLE_RANDOMIZE_CONTEXT_MENU_ITEM
				if(DebugMode || extendedMenu)
				{
					menu.Add("Randomize", Randomize);
				}
				#endif

				if(CanWriteToField && MixedContent)
				{
					menu.AddSeparatorIfNotRedundant();

					var values = GetValues();
					var count = values.Length;
					for(int n = 0; n < count; n++)
					{
						int index = n;
						menu.Add(StringUtils.Concat("Unify Values/From Target ", n, ": ", values[index]), ()=>UnifyValuesFromTarget(index));
					}
				}
			}

			if((getValueCausedException || setValueCausedException) && memberInfo != null)
			{
				var propertyInfo = memberInfo.PropertyInfo;
				if(propertyInfo != null)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Blacklist Property", ()=>
					{
						#if UNITY_EDITOR
						Undo.RecordObject(Inspector.Preferences, "Blacklist Property");
						#endif

					
						Inspector.Preferences.propertyBlacklist = Inspector.Preferences.propertyBlacklist.Add(new PropertyReference { ownerTypeName = propertyInfo.DeclaringType.FullName, propertyName = propertyInfo.Name });
						PropertyBlacklist.Add(propertyInfo);
						Parent.RebuildMemberBuildListAndMembers();
					});
				}
			}

			AddMenuItemsFromAttributes(ref menu);

			if(extendedMenu && memberInfo != null && CanReadFromFieldWithoutSideEffects)
			{
				InvokeMethodUtility.AddExecuteMethodMenuItems(ref menu, UnityObjects, GetValues(), Type, DebugMode, DebugMode, true, "Invoke/");
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		private void AddCopyMenuItems(ref Menu menu, bool addSeparator)
		{
			if(memberInfo == null || !memberInfo.MixedContent)
			{
				if(addSeparator)
                {
					menu.AddSeparatorIfNotRedundant();
                }
				menu.Add("Copy", CopyToClipboard);
				return;
			}

			AddCopyToClipboardMenuItemsForMixedContent(ref menu, addSeparator);
		}

		private void AddPasteMenuItems(ref Menu menu)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!ReadOnly);
			Debug.Assert(CanPasteFromClipboard());
			#endif

			menu.Add("Paste", PasteFromClipboard);

			var parentCollection = parent as ICollectionDrawer;
			if(parentCollection != null)
			{
				menu.Add("Paste Above", PasteAbove);
				menu.Add("Paste Below", PasteBelow);
			}
		}

		#if DEV_MODE
		/// <inheritdoc/>
		protected override void AddDevModeDebuggingEntriesToRightClickMenu(ref Menu menu)
		{
			base.AddDevModeDebuggingEntriesToRightClickMenu(ref menu);

			if(!ReadOnly &&!CanPasteFromClipboard())
			{
				menu.Add("Debugging/Paste Unsafe (CanPasteFromClipboard was false)", PasteFromClipboard);
			}

			#if UNITY_EDITOR
			if(memberInfo != null && memberInfo.SerializedProperty != null)
			{
				memberInfo.SerializedProperty.serializedObject.Update();
				menu.Add("Debugging/Rebuild SerializedProperty", memberInfo.RebuildSerializedProperty);
			}
			#endif

			menu.Add("Debugging/Refresh", UpdateCachedValuesFromFieldsRecursively);
			#if PI_ASSERTATIONS
			menu.Add("Debugging/Validate Data", ()=>AssertDataIsValid());
			#endif

			#if UNITY_EDITOR
			if(memberInfo != null && memberInfo.SerializedProperty != null)
			{
				menu.Add("Debugging/Print SerializedProperty Path", ()=>Debug.Log(memberInfo.SerializedProperty.propertyPath));
			}
			#endif
		}

		/// <inheritdoc/>
		protected override object[] GetDevInfo()
		{
			if(MixedContent)
			{
				return base.GetDevInfo().Add(", Values=", Values, ", value(cached)=", value, ", memberInfo=", memberInfo);
			}

			return base.GetDevInfo().Add(", Value=", Value, ", memberInfo=", memberInfo);
		}
		#endif

		/// <inheritdoc/>
		protected override void DoRandomize()
		{
			bool changed = false;
			var setValues = GetValues();
			for(int n = setValues.Length - 1; n >= 0; n--)
			{
				var randomValue = GetRandomValue();
				if(!ValuesAreEqual((TValue)setValues[n], randomValue))
				{
					setValues[n] = randomValue;
					changed = true;
				}
			}
			
			if(changed)
			{
				SetValues(setValues);
			}
		}

		/// <summary>
		/// Gets a randomly picked value that can be assigned as the value of the drawer.
		/// </summary>
		/// <returns> Random value </returns>
		protected abstract TValue GetRandomValue();

		/// <inheritdoc cref="IDrawer.Duplicate" />
		public override void Duplicate()
		{
			var parentCollection = parent as ICollectionDrawer;

			if(parentCollection == null)
			{
				throw new NotSupportedException(ToString()+" - Duplicate was called but parent ("+StringUtils.ToString(parent)+") did not implement ICollectionDrawer!");
			}

			parentCollection.DuplicateMember(this);
		}

		/// <summary>
		/// Adds new element above this collection member and with its state pasted from the clipboard.
		/// </summary>
		private void PasteAbove()
		{
			#if DEV_MODE
			Debug.Assert(parent.Unfolded, ToString()+ " - PasteAbove was called but parent was not unfolded, which means its members might not be built!");
			Debug.Assert(!parent.DrawInSingleRow, ToString()+ " - PasteAbove was called but DrawInSingleRow was true for parent " + StringUtils.ToString(parent));
			#endif

			int index = Array.IndexOf(parent.Members, this);
			Duplicate();
			parent.Members[index].PasteFromClipboard();
		}

		/// <summary>
		/// Adds new element below this collection member and with its state pasted from the clipboard.
		/// </summary>
		private void PasteBelow()
		{
			#if DEV_MODE
			Debug.Assert(parent.Unfolded, ToString()+ " - PasteAbove was called but parent was not unfolded, which means its members might not be built!");
			Debug.Assert(!parent.DrawInSingleRow, ToString()+ " - PasteAbove was called but DrawInSingleRow was true for parent " + StringUtils.ToString(parent));
			#endif

			int index = Array.IndexOf(parent.Members, this);
			Duplicate();
			var created = parent.Members[index + 1];
			created.PasteFromClipboard();
		}

		/// <summary>
		/// Unify values from target.
		/// </summary>
		/// <param name="targetIndex">
		/// Zero-based index of the target. </param>
		private void UnifyValuesFromTarget(int targetIndex)
		{
			SetValue(GetValue(targetIndex));
		}

		/// <summary>
		/// Deletes the collection element this drawer represents in the collection that its parent represents.
		/// </summary>
		private void DeleteInCollection()
		{
			var parentCollection = parent as ICollectionDrawer;
			if(parentCollection != null)
			{
				parentCollection.DeleteMember(this);
			}
			else
			{
				Debug.LogError(ToString()+".DeleteInCollection - can't delete because had no ICollectionDrawer parent");
			}
		}

		/// <summary>
		/// Generates menu items for copying field values to clipboard when there are differing values in merged multi-editing mode.
		/// </summary>
		/// <param name="menu"> Menu into which to add items</param>
		private void AddCopyToClipboardMenuItemsForMixedContent(ref Menu menu, bool addSeparator)
		{
			var values = GetValues();
			int count = values.Length;
			var uniqueValues = ReusableObjectHashSet;
			
			for(int n = 0; n < count; n++)
			{
				var targetValue = values[n];
				if(uniqueValues.Add(targetValue))
				{
					if(addSeparator)
					{
						addSeparator = false;
						menu.AddSeparatorIfNotRedundant();
					}

					int index = n;
					menu.Add(string.Concat("Copy/", StringUtils.ToString(targetValue).Replace("/", "")), CopyTargetToClipboardFromContextMenu, index);
				}
			}

			ReusableObjectHashSet.Clear();
		}

		private void CopyTargetToClipboardFromContextMenu(object index)
		{
			CopyToClipboard((int)index);
		}

		/// <summary>
		/// Adds menu items to opening context menu defined by attributes on target,
		/// such as the ContextMenuItemAttribute.
		/// </summary>
		/// <param name="menu">
		/// [in,out] The menu. </param>
		protected virtual void AddMenuItemsFromAttributes(ref Menu menu)
		{
			if(memberInfo == null)
			{
				return;
			}
			var items = memberInfo.GetAttributes<ContextMenuItemAttribute>(true);
			int count = items.Length;

			if(count > 0)
			{
				menu.AddSeparatorIfNotRedundant();

				for(int n = 0; n < count; n++)
				{
					var item = items[n];
					menu.Add(item.name, ()=>
					{
						try
						{
							memberInfo.InvokeInOwners(item.function);
						}
						catch
						{
							try
							{
								memberInfo.InvokeStaticInOwners(item.function);
							}
							catch
							{
								try
								{
									memberInfo.InvokeInUnityObjects(item.function);
								}
								catch
								{
									Debug.LogError("ContextMenuItemAttribute NullReferenceException: could not find method by name "+item.function);
								}
							}
						}
					});
				}
			}
		}

		/// <summary>
		/// Generates a validation override from applicable field attributes, if any - like e.g.
		/// NotNullAttribute.
		/// </summary>
		private void GenerateValidationOverrideFromAttributes()
		{
			if(memberInfo == null)
			{
				return;
			}

			var validator = memberInfo.GetAttribute<IValueValidator>();
			if(validator != null)
			{
				#if DEV_MODE
				Debug.Log(ToString()+" validator override: "+validator.GetType().Name);
				#endif

				overrideValidateValue = validator.Validate;
				return;
			}

			if(memberInfo.HasAttribute<NotNullAttribute>())
			{
				overrideValidateValue = ValidateNotNull;
			}
		}

		protected bool ValidateNotNull([NotNull]object[] values)
		{
			if(values == null)
			{
				return false;
			}

			for(int n = values.Length - 1; n >= 0; n--)
			{
				if(typeof(TValue).IsUnityObject())
				{
					var test = (Object)values[n];
					if(test == null)
					{
						return false;
					}
				}
				else
				{
					var test = (TValue)values[n];
					if(test == null || test.Equals(null))
					{
						return false;
					}
				}
			}
			return GetDataIsValidUpdated();
		}

		/// <inheritdoc cref="IDrawer.Draw" />
		public override bool Draw(Rect position)
		{
			if(Event.current.type == EventType.Layout)
			{
				OnLayoutEvent(position);
			}
			else if(lastDrawPosition.height <= 0f)
			{
				GetDrawPositions(position);
			}
			
			bool dirty = DrawPrefix(PrefixLabelPosition);

			if(DrawBody(ControlPosition))
			{
				dirty = true;
			}

			#if DEV_MODE && DEBUG_VISUALIZE_BOUNDS
			if(Event.current.control && Event.current.type == EventType.Repaint)
			{
				var color = Color.cyan;
				color.a = 0.25f;
				position.x += 1f;
				position.y += 1f;
				position.width -= 2f;
				position.height -= 2f;
				DrawGUI.Active.ColorRect(position, color);

				//var color = Color.red;
				//color.a = 0.5f;
				//DrawGUI.Active.ColorRect(PrefixLabelPosition, color);

				//color = Color.green;
				//color.a = 0.5f;
				//DrawGUI.Active.ColorRect(ControlPosition, color);
			}
			#endif

			return dirty;
		}
		
		/// <inheritdoc/>
		public override bool DrawPrefix(Rect position)
		{
			#if SAFE_MODE
			if(prefixLabelDrawer == null)
			{
				#if DEV_MODE
				Debug.LogWarning(GetType().Name + ".DrawPrefix - prefixLabelDrawer of "+this+" under parent "+(parent == null ? "null" : parent.ToString())+" was null!");
				#endif

				return false;
			}
			#endif
			
			if(lastPassedFilterTestType != FilterTestType.None)
			{
				prefixLabelDrawer.Draw(position, Inspector.State.filter, fullClassName, lastPassedFilterTestType);
			}
			else
			{
				prefixLabelDrawer.Draw(position);
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.DrawSelectionRect" />
		public override void DrawSelectionRect()
		{
			//Use Unity internal selection highlighting if not full width field
			//UPDATE: Or should we always use internal highlighting by default?
			if(IsFullInspectorWidth)
			{
				if(InspectorUtility.ActiveManager.IsFocusedButNotSelected(this))
				{
					DrawGUI.DrawNonSelectedFocusedControlRect(SelectionRect);
				}
				else
				{
					DrawGUI.DrawSelectionRect(SelectionRect, localDrawAreaOffset);
				}
			}
		}

		/// <inheritdoc />
		public override void OnInspectorGainedFocusWhileSelected()
		{
			UpdatePrefixDrawer();	
		}

		/// <inheritdoc />
		public override void OnInspectorLostFocusWhileSelected()
		{
			UpdatePrefixDrawer();
		}

		/// <inheritdoc/>
		protected override bool ShouldConstantlyUpdateCachedValues()
		{
			return memberInfo != null && memberInfo.CanRead && !memberInfo.ParentChainIsBroken;
		}


		/// <inheritdoc cref="IDrawer.Dispose" />
		public override void Dispose()
		{
			base.Dispose();

			fullClassName = null;
			readOnly = true;
			canWriteToField = false;
			memberInfo = null;
			isReorderable = false;
			prefixLabelDrawer = null;
			overrideHasUnappliedChanges = null;
			hasUnappliedChanges = false;
			getValueCausedException = false;
			setValueCausedException = false;
			getOrSetValueExceptionLabel = null;

			InspectorValues.Deregister(this);
		}

		/// <inheritdoc/>
		public override bool PassesSearchFilter(SearchFilter filter)
		{
			return filter.PassesFilter(this, out lastPassedFilterTestType);
		}
		
		/// <inheritdoc />
		public void OnBeingReordered(float yOffset) { }
		
		/// <inheritdoc cref="IDrawer.DefaultValue" />
		public override object DefaultValue(bool preferNotNull = false)
		{
			if(CanBeNull && !preferNotNull)
			{
				return null;
			}

			if(memberInfo != null)
			{
				return memberInfo.DefaultValue();
			}
			return Type.DefaultValue();
		}

		/// <summary>
		/// Can the drawers be reordered inside the current parent using drag n drop?
		/// </summary>
		/// <returns>
		/// True if member order can be altered, false if not.
		/// </returns>
		private bool GetIsReorderable()
		{
			var reorderableParent = parent as IReorderableParent;
			if(reorderableParent != null)
			{
				return reorderableParent.MemberIsReorderable(this);
			}
			return false;
		}

		/// <inheritdoc cref="IDrawer.OnSelfOrParentBecameVisible" />
		public override void OnSelfOrParentBecameVisible()
		{
			UpdateCachedValueFromField(true);
		}

		/// <inheritdoc />
		protected override bool TryGetSingleValueVisualizedInInspector(out object visualizedValue)
		{
			if(MixedContent)
			{
				visualizedValue = null;
				return false;
			}

			visualizedValue = GetValue();
			return true;
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Calls Update on the SerializedObject of the target(s), if possible.
		/// </summary>
		protected void UpdateSerializedObject()
		{
			if(memberInfo != null)
			{
				var hierarchy = memberInfo.Hierarchy;
				if(hierarchy != null)
				{
					var serializedObject = hierarchy.SerializedObject;
					if(serializedObject != null)
					{
						#if DEV_MODE
						Debug.Log(ToString()+".UpdateSerializedObject - Calling serializedObject.Update for target "+serializedObject.targetObject.GetType().Name+" with memberInfo.GetValue(0):"+StringUtils.ToString(memberInfo.GetValue(0)));
						#endif

						serializedObject.Update();

						#if DEV_MODE
						Debug.Log(ToString()+".UpdateSerializedObject - done");
						#endif
					}
				}
			}
		}
		#endif

		/// <inheritdoc/>
		protected override void OnLabelChanged()
		{
			// Make sure fullClassName is rebuilt for the new name
			fullClassName = null;

			UpdatePrefixDrawer();

			base.OnLabelChanged();
		}
	}
}