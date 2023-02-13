#define SKIP_BUILD_MEMBERS_IF_NULL
#define DRAW_IN_SINGLE_ROW_IF_NULL

//#define DEBUG_NULL_MEMBERS
//#define DEBUG_SIZE
//#define DEBUG_RESIZE
//#define DEBUG_BUILD_DRAWERS_FOR_MEMBERS
//#define DEBUG_REORDERING
//#define DEBUG_DUPLICATE_MEMBER
//#define DEBUG_DELETE_MEMBER

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	/// <summary>
	/// ArrayDrawer and ListDrawer inherit from this
	/// </summary>
	/// <typeparam name="TValue"> Type of the collection. </typeparam>
	/// <typeparam name="TSize"> Type that can describe the size of the collection. </typeparam>
	/// <typeparam name="TIndex"> Type that can describe the index of an element in the collection. </typeparam>
	[Serializable]
	public abstract class CollectionDrawer<TValue, TSize, TIndex> : ParentFieldDrawer<TValue>, ICollectionDrawer, IReorderableParent where TValue : class, IEnumerable where TSize : struct, IEquatable<TSize> where TIndex : struct, IEquatable<TIndex>
	{
		/// <summary>
		/// If all targets have the same length, equals said length else equals InvalidSize.
		/// </summary>
		protected TSize size;

		/// <summary>
		/// Contains label describing collection element count.
		/// </summary>
		[NotNull]
		protected readonly GUIContent itemCountLabel = new GUIContent("");
		protected Rect itemCountRect = default(Rect);

		/// <summary>
		/// If all targets have the same element count, equals said count.
		/// If all targets don't have equal element count, equals -1.
		/// If any target is null, equals -1.
		/// </summary>
		protected int elementCount = -1;
		private int reorderingMemberAtIndex = -1;

		private PropertyAttributeInfo propertyAttributeInfo;

		/// <summary>
		/// True if element type of IDictionary key type is UnityEngine.Object.
		/// Knowing this is necessary to handle the fact that UnityEngine.Objects
		/// override the equality comparer.
		/// </summary>
		protected bool isUnityObjectCollection;

		private bool elementCountMouseovered;

		/// <inheritdoc cref="IParentDrawer.DrawInSingleRow" />
		public sealed override bool DrawInSingleRow
		{
			get
			{
				#if SKIP_BUILD_MEMBERS_IF_NULL
				if(CanBeNull && Value == null)
				{
					return true;
				}
				#endif

				return false;
			}
		}

		/// <inheritdoc cref="IParentDrawer.RequiresConstantRepaint" />
		public sealed override bool RequiresConstantRepaint
		{
			get
			{
				return InspectorUtility.ActiveManager.MouseDownInfo.NowReordering;
			}
		}

		/// <inheritdoc />
		public override string DocumentationPageUrl
		{
			get
			{
				return PowerInspectorDocumentation.GetDrawerInfoUrl("collection-drawer");
			}
		}

		/// <summary>
		/// How many dimensions does the collection have; the array rank.
		/// </summary>
		/// <value>
		/// The rank.
		/// </value>
		protected abstract int Rank { get; }

		/// <summary> Gets a value indicating whether the collection these drawers represent can contain multiple instances of the same element. </summary>
		/// <value> True if collection can contain duplicate elements, false if not. </value>
		protected virtual bool CanContainDuplicates
		{
			get
			{
				return true;
			}
		}

		/// <summary> Gets a value indicating whether the collection these drawers represent is of fixed size and can't be resized after created. </summary>
		/// <value> True if collection is fixed size, false if resizable. </value>
		protected abstract bool IsFixedSize
		{
			get;
		}

		/// <summary> Gets a value indicating whether the collection these drawers represent is a read only collection. </summary>
		/// <value> True if this collection is a read only collection, false if not. </value>
		protected virtual bool IsReadOnlyCollection
		{
			get
			{
				return false;
			}
		}

		/// <summary> Gets the drawers of the field for resizing the collection or adding to it / removing items from it. </summary>
		/// <value> Drawer responsible for adding to and removing from collection. </value>
		protected IDrawer Resizer
		{
			get
			{
				return members[0];
			}
		}

		/// <inheritdoc />
		public Rect FirstReorderableDropTargetRect
		{
			get
			{
				if(!Unfolded)
				{
					return Rect.zero;
				}

				Rect dropRect;

				int count = visibleMembers.Length;
				
				if(count > 0)
				{
					var firstMember = visibleMembers[0];

					//if the first member is the resize field, the right spot is underneath it
					if(firstMember == Resizer)
					{
						dropRect = firstMember.Bounds;
						dropRect.height = DrawGUI.SingleLineHeight;
						dropRect.y += firstMember.Height - DrawGUI.SingleLineHeight * 0.5f;
						return 	dropRect;
					}
				}

				//if there are no visible members (e.g. all filtered out)
				//or if the first member is not the resize field (e.g it has been filtered out)
				//then the right spot is below the header
				dropRect = lastDrawPosition;
				dropRect.height = DrawGUI.SingleLineHeight;
				dropRect.y += HeaderHeight - DrawGUI.SingleLineHeight * 0.5f;
				return dropRect;
			}
		}

		/// <inheritdoc/>
		public virtual int FirstCollectionMemberIndexOffset
		{
			get
			{
				return 1;
			}
		}

		/// <inheritdoc cref="ICollectionDrawer.LastCollectionMemberCountOffset" />
		public virtual int LastCollectionMemberCountOffset
		{
			get
			{
				return 1;
			}
		}

		/// <inheritdoc cref="ICollectionDrawer.FirstCollectionMemberIndex" />
		public int FirstCollectionMemberIndex
		{
			get
			{
				return members.Length <= FirstCollectionMemberIndexOffset ? -1 : FirstCollectionMemberIndexOffset;
			}
		}

		/// <inheritdoc cref="ICollectionDrawer.LastCollectionMemberIndex" />
		public int LastCollectionMemberIndex
		{
			get
			{
				return members.Length <= FirstCollectionMemberIndexOffset ? -1 : members.Length - LastCollectionMemberCountOffset;
			}
		}

		/// <inheritdoc cref="ICollectionDrawer.FirstVisibleCollectionMemberIndex" />
		public int FirstVisibleCollectionMemberIndex
		{
			get
			{
				int count = visibleMembers.Length;
				for(int n = 0; n < count; n++)
				{
					if(MemberRepresentsValueInCollection(visibleMembers[n]))
					{
						return n;
					}
				}
				return -1;
			}
		}

		/// <inheritdoc cref="ICollectionDrawer.LastVisibleCollectionMemberIndex" />
		public virtual int LastVisibleCollectionMemberIndex
		{
			get
			{
				for(int n = visibleMembers.Length - 1; n >= 0; n--)
				{
					if(MemberRepresentsValueInCollection(visibleMembers[n]))
					{
						return n;
					}
				}
				return -1;
			}
		}

		/// <inheritdoc cref="IDrawer.Height" />
		public override float Height
		{
			get
			{
				return ReorderableParentDrawerUtility.CalculateHeight(this);
			}
		}

		/// <summary> Gets LinkedMemberInfo for the collection resizer drawers. </summary>
		/// <value> Collection resizer LinkedMemberInfo. </value>
		protected LinkedMemberInfo ResizerMemberInfo
		{
			get
			{
				return memberBuildList[0];
			}
		}

		/// <summary>
		/// Returns the element type of this collection, i.e. the type of members of these Drawer.
		/// E.g. If this type is T[][], then returns T[], since a jagged array is an array of arrays.
		/// For T[,], T[] and List&lt;T&gt; return T.
		/// </summary>
		/// <value>
		/// Type of members representing collection's values.
		/// </value>
		protected abstract Type MemberType
		{
			get;
		}

		/// <summary> Gets a value indicating whether collection sizes on targets are not all the same. </summary>
		/// <value> True if target collections don't all have the same size, false if they all do. </value>
		protected bool MixedSize
		{
			get
			{
				return elementCount == -1;
			}
		}

		/// <inheritdoc/>
		protected sealed override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return RebuildingMembersAllowed;
			}
		}

		/// <inheritdoc />
		protected override void Setup(TValue setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE
			Debug.Assert(itemCountLabel.text.Length == 0);
			#endif

			memberInfo = setMemberInfo;

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);

			// by using Comparer defining type instead of MemberType, this same value
			// can be useful both for Dictionaries (when deciding what comparer to use)
			// and for other collections (when deciding if should use UnityEngine.Object
			// specific methods)
			isUnityObjectCollection = ElementComparerDefiningType().IsUnityObject();

			if(setMemberInfo != null)
			{
				PropertyAttribute propertyAttribute = null;
				Type drawerType;
				var type = Type;
				if(memberInfo != null ? CustomEditorUtility.TryGetPropertyDrawerType(type, memberInfo, out propertyAttribute, out drawerType) : CustomEditorUtility.TryGetPropertyDrawerType(type, out drawerType))
				{
					propertyAttributeInfo = new PropertyAttributeInfo(propertyAttribute, drawerType);
				}
			}
		}

		[CanBeNull]
		private LinkedMemberInfo CreateResizerFieldInfo()
		{
			if(memberInfo == null)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.LogWarning(ToString()+".CreateResizerFieldInfo - memberInfo was null. Can't create LinkedMemberInfo for resizer.");
				#endif

				return null;
			}

			return MemberHierarchy.GetCollectionResizer(memberInfo, typeof(TSize), GetGetCollectionSizeDelegate(), GetSetCollectionSizeDelegate()); 
		}

		/// <summary>
		/// Gets delegate which can be used to return the size of a collection.
		/// Delegate should refer to a static method NOT an instance method (for easier serialization).
		/// </summary>
		/// <returns> Delegate for getting collection size. </returns>
		[Pure]
		protected abstract GetSize GetGetCollectionSizeDelegate();

		/// <summary>
		/// Gets delegate which can be used to set the size of a collection to given value.
		/// Delegate should refer to a static method NOT an instance method (for easier serialization).
		/// </summary>
		/// <returns> Delegate for setting collection size. </returns>
		[Pure]
		protected abstract SetSize GetSetCollectionSizeDelegate();
		
		[CanBeNull]
		private LinkedMemberInfo CreateMemberFieldInfo(int flattenedIndex)
		{
			if(memberInfo == null)
			{
				return null;
			}

			return MemberHierarchy.GetCollectionMember(memberInfo, MemberType, flattenedIndex, GetGetCollectionMemberValueDelegate(), GetSetCollectionMemberValueDelegate());
		}

		/// <summary>
		/// Gets delegate which can be used to return value of a collection element at given index.
		/// Delegate should refer to a static method NOT an instance method (for easier serialization).
		/// </summary>
		/// <returns> Delegate for getting collection element value. </returns>
		protected abstract GetCollectionMember GetGetCollectionMemberValueDelegate();

		/// <summary>
		/// Gets delegate which can be used to set value of a collection element at given index.
		/// Delegate should refer to a static method NOT an instance method (for easier serialization).
		/// </summary>
		/// <returns> Delegate for setting collection element value. </returns>
		protected abstract SetCollectionMember GetSetCollectionMemberValueDelegate();

		/// <inheritdoc/>
		protected sealed override void DoGenerateMemberBuildList()
		{
			UpdateSize();

			#if SKIP_BUILD_MEMBERS_IF_NULL
			if(CanBeNull && Value == null)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString() + ".DoGenerateMemberBuildList called with Value=null and CanBeNull=true. Will leave memberBuildList empty.");
				#endif
				return;
			}
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			if(memberInfo == null)
			{
				Debug.LogWarning(ToString()+".DoGenerateMemberBuildList - memberInfo was null. Collection resizer will be readonly!");
			}
			#endif

			memberBuildList.Add(CreateResizerFieldInfo());
			for(int flattenedIndex = 0; flattenedIndex < elementCount; flattenedIndex++)
			{
				memberBuildList.Add(CreateMemberFieldInfo(flattenedIndex));
			}
		}

		/// <inheritdoc/>
		protected sealed override void DoBuildMembers()
		{
			UpdateSize();

			#if SKIP_BUILD_MEMBERS_IF_NULL
			if(CanBeNull && Value == null)
			{
				if(members.Length > 0)
				{
					DrawerArrayPool.Dispose(ref members, true);
					members = ArrayPool<IDrawer>.ZeroSizeArray;

					if(visibleMembers.Length > 0)
					{
						DrawerArrayPool.Dispose(ref visibleMembers, false);
						visibleMembers = ArrayPool<IDrawer>.ZeroSizeArray;
					}
				}

				#if DEV_MODE
				Debug.LogWarning(ToString() + ".DoBuildMembers called with Value=null and CanBeNull=true. Will leave memberBuildList empty.");
				#endif

				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(memberBuildList.Count == 0, memberBuildList.Count);
				#endif

				return;
			}
			#endif
			
			int memberCount = MixedSize ? FirstCollectionMemberIndexOffset : elementCount + FirstCollectionMemberIndexOffset;
			
			DrawerArrayPool.Resize(ref members, memberCount);

			members[0] = BuildResizeField();

			if(elementCount > 0)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(!size.Equals(default(TSize)));
				#endif

				//build collection members besides the resizer field
				BuildDrawersForMembers(default(TSize), size);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			ValidateMembers();
			#endif
		}

		/// <summary> Builds the drawers for the control used for resizing the collection. </summary>
		/// <returns> The resizer IDrawer. </returns>
		protected abstract IDrawer BuildResizeField();
		
		/// <summary>
		/// Given the size of an array, returns it's total element count.
		/// </summary>
		/// <returns> The element count.  </returns>
		protected abstract int GetElementCount(TSize collectionIndex);

		/// <summary> Gets the collection index given the zero-based index of an instruction in the members array. </summary>
		/// <param name="memberIndex"> Zero-based index of the member in the members array. </param>
		/// <returns> The collection index. </returns>
		protected abstract TIndex GetCollectionIndex(int memberIndex);

		/// <summary>
		/// If all targets have the same size, sets size to that collectively shared size, else sets it to -1.
		/// </summary>
		private void UpdateSize()
		{
			if(!MixedContent)
			{
				var collection = Value;
				if(collection == null)
				{
					SetCachedSize(default(TSize), false, true);
					return;
				}

				SetCachedSize(GetCollectionSize(collection), false, false);
				return;
			}

			// NOTE: even if MixedContent is true, length itself could still be uniform across all collections.

			var firstCollection = GetValue(0) as TValue;
			if(firstCollection == null)
			{
				SetCachedSize(default(TSize), true, false);
				return;
			}

			var firstSize = GetCollectionSize(firstCollection);
			for(int n = memberInfo.TargetCount - 1; n >= 1; n--)
			{
				var otherCollection = GetValue(n) as TValue;
				if(otherCollection == null)
				{
					SetCachedSize(default(TSize), true, false);
					return;
				}
				
				var otherSize = GetCollectionSize(otherCollection);
				if(!firstSize.Equals(otherSize))
				{
					SetCachedSize(firstSize, true, false);
					return;
				}
			}

			if(!firstSize.Equals(size) || elementCount < 0)
			{
				SetCachedSize(firstSize, false, false);
			}
		}

		protected void UpdateItemCountRect()
		{
			itemCountRect = labelLastDrawPosition;
			itemCountRect.width = lastDrawPosition.width;
			var labelSize = InspectorPreferences.Styles.SecondaryInfo.CalcSize(itemCountLabel);
			
			float removeHeight = labelLastDrawPosition.height - labelSize.y;
			float padding = removeHeight * 0.5f;
			itemCountRect.y += padding;
			itemCountRect.height -= removeHeight;
			itemCountRect.width = labelSize.x;

			if(!DrawInSingleRow)
			{
				itemCountRect.x = lastDrawPosition.xMax - labelSize.x - DrawGUI.RightPadding;
			}
			else
			{
				itemCountRect.x = bodyLastDrawPosition.x - 20f;
			}
		}

		/// <summary>
		/// Sets value of the cached size variable and updates element count, item count label and item count rect.
		/// </summary>
		/// <param name="setSize"> New cached size value. </param>
		/// <param name="mixedContent"> True if has mixed content. </param>
		/// <param name="isNull"> True if value of every target collection is null. </param>
		protected void SetCachedSize(TSize setSize, bool mixedContent, bool isNull)
		{
			#if DEV_MODE && DEBUG_SIZE
			Debug.Log("SetCachedSize("+setSize+", mixedContent="+mixedContent+", isNull="+isNull+") with sizeWas="+size+", elementCountWas="+elementCount);
			#endif

			if(mixedContent)
			{
				size = default(TSize);
				elementCount = -1;
				itemCountLabel.text = "Size: —";
				UpdateItemCountRect();
			}
			else if(isNull)
			{
				if(itemCountLabel.text.Length != 4)
				{
					itemCountLabel.text = "null";
					UpdateItemCountRect();
				}
				elementCount = -1;
				size = default(TSize);
			}
			else
			{
				if(!size.Equals(setSize) || itemCountLabel.text.Length == 0)
				{
					size = setSize;
					itemCountLabel.text = "Size: " + StringUtils.ToString(size);
					UpdateItemCountRect();
				}
				
				elementCount = GetElementCount(size);
			}
		}


		/// <inheritdoc cref="IDrawer.UpdateCachedValuesFromFieldsRecursively" />
		public sealed override void UpdateCachedValuesFromFieldsRecursively()
		{
			if(inactive)
			{
				return;
			}

			var sizeWas = size;
			if(!MixedContent)
			{
				var firstFieldValue = GetValue(0) as TValue;
				if(firstFieldValue == null)
				{
					if(Value != null && !ReadOnly)
					{
						DoSetValue(firstFieldValue, false, true);
						return;
					}
					else
					{
						SetCachedSize(default(TSize), false, true);
					}
				}
				else
				{
					var sizeFromField = GetCollectionSize(firstFieldValue);
					if(!sizeFromField.Equals(size))
					{
						SetCachedSize(sizeFromField, false, false);
					}
				}
			}
			else
			{
				UpdateSize();
			}
			
			if(!size.Equals(sizeWas))
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(memberInfo == null || memberInfo.CanWrite, StringUtils.ToColorizedString(ToString(), ".UpdateCachedValuesFromFieldsRecursively - size (", size, ") != sizeWas (", sizeWas, ") but CanWrite was ", false));
				#endif

				var setSize = size;
				size = sizeWas;
				Resize(setSize);
				return;
			}

			base.UpdateCachedValuesFromFieldsRecursively();
		}

		protected virtual object MemberDefaultValue()
		{
			return MemberType.DefaultValue();
		}

		private void Resize(object newSize)
		{
			Resize((TSize)newSize);
		}

		/// <summary>
		/// Resizes all target arrays and cached value to given size and rebuilds the
		/// memberBuildList and member Drawer to match the new collection values.
		/// </summary>
		/// <param name="newSize"> new size for all target collections. </param>
		private void Resize(TSize newSize)
		{
			#if DEV_MODE && DEBUG_RESIZE
			Debug.Log(GetType().Name+".Resize("+newSize+") called! (with size="+size+", GetCollectionSize(Value)="+GetCollectionSize(Value)+")------------------------------");
			#endif

			var collections = GetCopyOfValuesInternal();
			int targetCount = collections.Length;
			
			//resize collection for each target
			for(int t = targetCount - 1; t >= 0; t--)
			{
				var collection = collections[t];
				var oldSize = GetCollectionSize(collection);
				if(!oldSize.Equals(newSize))
				{
					ResizeCollection(ref collection, newSize);

					#if DEV_MODE && PI_ASSERTATIONS
					Debug.Assert(collection.GetType() == Type, ToString()+".ResizeCollection resulting collection type was "+StringUtils.TypeToString(collection)+" when it should be "+StringUtils.ToString(Type));
					#endif
					
					collections[t] = collection;
				}
			}
			SetValues(collections, false, true);

			SetCachedSize(newSize, false, false);
			
			int memberCount = memberBuildList.Count;
			int oldSharedElementCount = memberCount - 1;
			int newSharedElementCount = GetElementCount(newSize);
			if(newSharedElementCount == -1)
			{
				newSharedElementCount = 0;
			}

			//handle building new or disposing old member Drawer
			if(oldSharedElementCount != newSharedElementCount)
			{
				for(int flattenedIndex = oldSharedElementCount; flattenedIndex > newSharedElementCount; flattenedIndex--)
				{
					#if DEV_MODE
					Debug.Log(ToString() + ".Resize - memberBuildList.RemoveAt(" + flattenedIndex + "): " + memberBuildList[flattenedIndex]);
					#endif
					memberBuildList.RemoveAt(flattenedIndex);

					if(members[flattenedIndex] != null)
					{
						members[flattenedIndex].Dispose();
						members[flattenedIndex] = null;
					}
				}

				int newMemberCount = newSharedElementCount + FirstCollectionMemberIndexOffset;
				DrawerArrayPool.Resize(ref members, newMemberCount);

				for(int flattenedIndex = oldSharedElementCount; flattenedIndex < newSharedElementCount; flattenedIndex++)
				{
					memberBuildList.Add(CreateMemberFieldInfo(flattenedIndex));

					var member = CreateMember(GetCollectionIndex(flattenedIndex), flattenedIndex);
					members[flattenedIndex + FirstCollectionMemberIndexOffset] = member;
					member.OnParentAssigned(this);
				}
				UpdateVisibleMembers();
			}

			OnCachedValueChanged(false, false);
		}
		
		/// <summary>
		/// Resizes collection to given length. If length is increased, sets
		/// values of each new member to MemberDefaultValue
		/// </summary>
		/// <param name="collection">collection to resize</param>
		/// <param name="length"> new length for collection. </param>
		protected abstract void ResizeCollection([NotNull]ref TValue collection, TSize length);

		/// <summary>
		/// Gets collection member value at given collectionIndex.
		/// </summary>
		/// <param name="collection"> [in,out] The collection whose member value we are getting. </param>
		/// <param name="collectionIndex"> Zero-based member collectionIndex in the collection. </param>
		[Pure]
		protected abstract object GetCollectionValue([NotNull]TValue collection, TIndex collectionIndex);

		/// <summary>
		/// Sets collection member value at given collectionIndex.
		/// </summary>
		/// <param name="collection"> [in,out] The collection that is modified. </param>
		/// <param name="collectionIndex"> Zero-based member collectionIndex in the collection. </param>
		/// <param name="memberValue"> The value to set. </param>
		protected abstract void SetCollectionValue([NotNull]ref TValue collection, TIndex collectionIndex, object memberValue);

		private bool TryInsertAt(ref TValue collection, TIndex toIndex, object memberValue)
		{
			if(ReadOnly)
			{
				return false;
			}

			try
			{
				InsertAt(ref collection, toIndex, memberValue);
				return true;
			}
			catch(InvalidCastException)
			{
				if(!Converter.TryChangeType(ref memberValue, MemberType))
				{
					return false;
				}

				try
				{
					InsertAt(ref collection, toIndex, memberValue);
					return true;
				}
				catch(InvalidCastException)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Inserts new member in collection at given collectionIndex, and increasing size of collection by one.
		/// NOTE: In some instances increasing the size of the collection by one might not be possible
		/// (read-only collection, 2D array), and in those instances inserted value should instead
		/// replace the existing value at the collectionIndex (so working identically to SetCollectionValue).
		/// </summary>
		/// <param name="collection"> [in,out] The collection that is modified. </param>
		/// <param name="toIndex"> Zero-based member collectionIndex in the collection. </param>
		/// <param name="memberValue"> The value to set. </param>
		protected abstract void InsertAt(ref TValue collection, TIndex toIndex, object memberValue);

		/// <inheritdoc />
		public int GetMemberIndexInCollection(IDrawer member)
		{
			int memberIndex = Array.IndexOf(members, member);
			return memberIndex - FirstCollectionMemberIndexOffset;
		}

		/// <summary> Gets dimensions of given collection. </summary>
		/// <param name="collection"> The collection whose dimensions to return. </param>
		/// <returns> The collection dimensions. </returns>
		[Pure]
		protected abstract TSize GetCollectionSize([CanBeNull]TValue collection);

		/// <summary>
		/// Gets element count of given collection.
		/// For one-dimensional collections this is identical to GetCollectionSize.
		/// </summary>
		/// <param name="collection"> The collection whose element count to get. </param>
		/// <returns> The collection element count. </returns>
		private int GetCollectionElementCount(TValue collection)
		{
			return GetElementCount(GetCollectionSize(collection));
		}

		private void BuildDrawersForMembers(TSize oldSize, TSize newSize)
		{
			if(!oldSize.Equals(newSize))
			{
				int oldElementCount = GetElementCount(oldSize);
				int newElementCount = GetElementCount(newSize);

				int newMemberCount = newElementCount + FirstCollectionMemberIndexOffset;
				DrawerArrayPool.Resize(ref members, newMemberCount);

				#if DEV_MODE && DEBUG_BUILD_DRAWERS_FOR_MEMBERS
				Debug.Log(ToString()+".BuildDrawersForMembers size from "+oldSize+" to "+newSize+": element count from "+oldElementCount+" to "+newElementCount+".");
				#endif

				for(int flattenedIndex = oldElementCount; flattenedIndex < newElementCount; flattenedIndex++)
				{
					var collectionIndex = GetCollectionIndex(flattenedIndex);

					#if DEV_MODE && DEBUG_BUILD_DRAWERS_FOR_MEMBERS
					Debug.Log(ToString()+".BuildDrawersForMembers GetCollectionIndex("+flattenedIndex+"): "+collectionIndex);
					#endif

					var member = CreateMember(collectionIndex, flattenedIndex);
					members[flattenedIndex + FirstCollectionMemberIndexOffset] = member;
					
					member.OnParentAssigned(this);
				}

				UpdateVisibleMembers();
			}
		}
		
		[NotNull]
		private IDrawer CreateMember(TIndex collectionIndex, int flattenedIndex)
		{
			var collections = Values;
			int collectionCount = collections.Length;
			
			// build array for element values from all target collections
			var elementValues = ArrayPool<object>.Create(collectionCount);
			for(int t = 0; t < collectionCount; t++)
			{
				var collection = collections[t];
				int collectionElementCount = GetCollectionElementCount(collection);
				if(collectionElementCount > flattenedIndex)
				{
					elementValues[t] = GetCollectionValue(collection, collectionIndex);
				}
				else
				{
					elementValues[t] = MemberDefaultValue(); // possible bug: if some targets have MemberDefaultValue as actual value, Mixed Values detection could give incorrect results
				}
			}

			return CreateMember(elementValues, collectionIndex, flattenedIndex);
		}
		
		[NotNull]
		private IDrawer CreateMember(object[] values, TIndex collectionIndex, int flattenedIndex)
		{
			var membLabel = GetIndexBasedLabelForMember(collectionIndex);
			var memb = CreateMemberDrawer(values, flattenedIndex, membLabel);
			SetupNewlyCreatedMember(memb);
			return memb;
		}

		/// <summary>
		/// Gets label for element at given zero-based index in collection. The label should generally be based
		/// on the collection index.
		/// 
		/// NOTE: AppendFirstFieldValueToMemberNameIfString can also modify the member name after this method.
		/// 
		/// </summary>
		/// <param name="collectionIndex"> Zero-based index of the collection. </param>
		/// <returns> The base label for member. </returns>
		protected abstract GUIContent GetIndexBasedLabelForMember(TIndex collectionIndex);

		/// <summary> Creates drawer for collection member at index. </summary>
		/// <param name="values"> The values for the member. </param>
		/// <param name="flattenedIndex"> Zero-based flat index of the element in the collection. </param>
		/// <param name="memberLabel"> Prefix label for the drawer. </param>
		/// <returns> The newly-created member drawers. This will never be null. </returns>
		[NotNull]
		private IDrawer CreateMemberDrawer(object[] values, int flattenedIndex, GUIContent memberLabel)
		{
			var memberType = MemberType;
			var memberFieldInfo = CreateMemberFieldInfo(flattenedIndex);
			return CreateMemberDrawer(values[0], memberType, memberFieldInfo, memberLabel);
		}

		/// <summary> Creates GUI drawers for collection member. </summary>
		/// <param name="value"> The initial cached value for the member. </param>
		/// <param name="memberType"> Type of the member. </param>
		///  <param name="memberFieldInfo"> LinkedMemberInfo for the member. </param>
		/// <param name="memberLabel"> Prefix label for the drawers. </param>
		/// <returns> The newly-created member drawers. This will never be null. </returns>
		[NotNull]
		protected virtual IDrawer CreateMemberDrawer(object value, Type memberType, LinkedMemberInfo memberFieldInfo, GUIContent memberLabel)
		{
			if(propertyAttributeInfo != null && !memberType.IsCollection())
			{
				return DrawerProvider.GetForPropertyDrawer(propertyAttributeInfo.attribute, value, memberType, memberFieldInfo, this, memberLabel, ReadOnly);
			}
			return DrawerProvider.GetForField(value, memberType, memberFieldInfo, this, memberLabel, ReadOnly);
		}

		private void SetupNewlyCreatedMember(IDrawer memb)
		{
			AppendFirstFieldValueToMemberNameIfString(memb);
			memb.OnKeyboardInputBeingGiven += OnMemberKeyboardInputBeingGiven;
		}

		private void UpdateMemberLabelForIndex(IDrawer member, TIndex collectionIndex)
		{
			var labelWas = member.Label;
			var setLabel = GetIndexBasedLabelForMember(collectionIndex);
			setLabel.tooltip = labelWas.tooltip;
			member.Label = setLabel;
			AppendFirstFieldValueToMemberNameIfString(member);
		}

		private void AppendFirstFieldValueToMemberNameIfString(IDrawer memb)
		{
			var membAsParent = memb as IParentDrawer;
			if(membAsParent != null)
			{
				var memberType = membAsParent.GetMemberType(0);
				if(memberType == Types.String || memberType == Types.Enum)
				{
					var membLabel = GUIContentPool.Create(memb.Label);
					var append = membAsParent.GetMemberValue(0).ToString();

					// if value has line breaks in it, abort
					if(append.IndexOf('\r') == -1 && append.IndexOf('\n') == -1)
					{
						membLabel.text = string.Concat(membLabel.text, " ", append);
						memb.Label = membLabel;
					}
				}
			}
		}

		/// <summary> Removes collection element at index. </summary>
		/// <param name="collection"> [in,out] The collection. </param>
		/// <param name="collectionIndex"> Zero-based index of element in the collection. </param>
		protected abstract void RemoveAt(ref TValue collection, TIndex collectionIndex);

		/// <inheritdoc/>
		protected override void DoReset()
		{
			Resizer.UpdateCachedValuesFromFieldsRecursively(); //if resize field is currently selected, discard inputted value
			DrawGUI.EditingTextField = false; //if resize field is currently selected, deselect it

			Resize(default(TSize));
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}

			bool canWrite = !ReadOnly;
			
			if(canWrite)
			{
				if(Value != null)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Sort", Sort);
					if(CanContainDuplicates)
					{
						menu.Add("Remove Duplicates", RemoveDuplicates);
					}
				}
			
				bool membersExpandable = false;
				if(memberBuildState == MemberBuildState.MembersBuilt)
				{
					if(members.Length > 0)
					{
						var memberAsParent = members[0] as IParentDrawer;
						if(memberAsParent != null)
						{
							if(memberAsParent.Foldable)
							{
								membersExpandable = true;
							}
						}
					}
				}
				else if(memberBuildState == MemberBuildState.BuildListGenerated)
				{
					if(memberBuildList.Count > 0)
					{
						if(!DrawerUtility.CanDrawInSingleRow(memberBuildList[0].Type, DebugMode))
						{
							membersExpandable = true;
						}
					}
				}

				if(membersExpandable)
				{
					menu.AddSeparatorIfNotRedundant();
					menu.Add("Unfold All Members", () => SetUnfolded(true, true));
					menu.Add("Fold All Members", () => SetUnfolded(false, true));
				}
			}
			
			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			if(canWrite)
			{
				int firstValueMemberIndex = FirstCollectionMemberIndex;
				if(firstValueMemberIndex >= 0 && members.Length > firstValueMemberIndex)
				{
					int insertAt = menu.IndexOf("Reset");
					var menuItem = Menu.Item("Reset Member Values", ResetMembers);
					if(insertAt != -1)
					{
						menu.Insert(insertAt + 1, menuItem);
					}
					else
					{
						menu.AddSeparatorIfNotRedundant();
						menu.Add(menuItem);
					}

					var firstValueMember = members[firstValueMemberIndex];
					if(Clipboard.CanPasteAs(firstValueMember.Type))
					{
						insertAt = menu.IndexOf("Copy");
						menuItem = Menu.Item("Paste On Members", PasteOnMembers);
						if(insertAt != -1)
						{
							menu.Insert(insertAt + 1, menuItem);
						}
						else
						{
							menu.AddSeparatorIfNotRedundant();
							menu.Add(menuItem);
						}
					}
				}
			}
		}
		
		private void ResetMembers()
		{
			for(int n = FirstCollectionMemberIndexOffset, last = LastCollectionMemberIndex; n <= last; n++)
			{
				members[n].Reset(false);
			}
		}

		private void PasteOnMembers()
		{
			for(int n = FirstCollectionMemberIndexOffset, last = LastCollectionMemberIndex; n <= last; n++)
			{
				var member = members[n];
				member.SetValue(Clipboard.Paste(member.Type));
			}
		}

		public void OnMemberReorderingStarted(IReorderable reordering)
		{
			#if DEV_MODE && DEBUG_REORDERING
			Debug.Log("OnMemberReorderingStarted("+reordering+")");
			#endif
			reorderingMemberAtIndex = Array.IndexOf(members, reordering);
		}

		/// <inheritdoc/>
		public void OnMemberDrag(MouseDownInfo mouseDownInfo, Object[] draggedObjects) { }

		/// <inheritdoc/>
		public void OnSubjectOverDropTarget(MouseDownInfo mouseDownInfo, Object[] draggedObjects) { }

		/// <inheritdoc/>
		public void OnMemberDragNDrop(MouseDownInfo mouseDownInfo, Object[] draggedObjects)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(Inspector.Manager.MouseDownInfo.CursorMovedAfterMouseDown);
			Debug.Assert(!Inspector.Manager.MouseDownInfo.IsClick);
			#endif

			var reordering = mouseDownInfo.Reordering;
			var dropTarget = reordering.MouseoveredDropTarget;

			int dropTargetVisibleMemberIndex = dropTarget.MemberIndex;
			if(dropTargetVisibleMemberIndex == -1)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(false, "OnMemberDragNDrop was called with dropTarget.MemberIndex == -1");
				#endif

				//invalid drop target, don't reorder anything
				//I don't think that this should even be possible...
				return;
			}
		
			//get index of drop target in members
			int toCollectionFlatIndex = GetReorderingDropTargetIndexInCollection(dropTargetVisibleMemberIndex);
			
			var droppedDrawer = reordering.Drawer;

			bool draggedOnlyObjectReferences = droppedDrawer == null || reordering.IsUnityObjectHeaderDrag || (droppedDrawer is IParentDrawer && (droppedDrawer as IParentDrawer).IsParentOrGrandParentOf(this));

			// Only allow dragging Object references over the header of an Object collection.
			// If an Object reference was just dragged over an Object reference field inside the collection then abort here.
			if(draggedOnlyObjectReferences && Inspector.Manager.MouseoveredSelectable != this)
			{
				#if DEV_MODE
				Debug.LogWarning("Ignoring OnMemberDragNDrop because draggedOnlyObjectReferences && Inspector.Manager.MouseoveredSelectable != this");
				#endif
				return;
			}

			var draggedValues = draggedOnlyObjectReferences ? draggedObjects : droppedDrawer.GetValues();

			// create a copy of current values, so that OnValueChanged events and Undo get called properly if the value gets changed
			var collections = GetCopyOfValuesInternal();
			int collectionCount = collections.Length;

			#if DEV_MODE && DEBUG_REORDERING
			Debug.Log("OnMemberDragNDrop from "+ (reordering.Parent != this ? -1 : reordering.MemberIndex - FirstCollectionMemberIndexOffset) + " to "+ toCollectionFlatIndex + " with draggedOnlyObjectReferences="+ draggedOnlyObjectReferences);
			#endif

			// Handle removing dragged value from source parent drawer (if any)
			if(reordering.Parent == this)
			{
				int fromCollectionFlatIndex = reordering.MemberIndex - FirstCollectionMemberIndexOffset;
				if(fromCollectionFlatIndex == toCollectionFlatIndex)
				{
					//target was returned to its original position, no reordering necessary
					return;
				}
				
				for(int n = collectionCount - 1; n >= 0; n--)
				{
					var collection = collections[n];
					if(GetCollectionElementCount(collection) > fromCollectionFlatIndex)
					{
						var fromIndex = GetCollectionIndex(fromCollectionFlatIndex);
						RemoveAt(ref collection, fromIndex);
						collections[n] = collection;
					}
				}
			}
			else if(!draggedOnlyObjectReferences)
			{
				reordering.Parent.DeleteMember(droppedDrawer);
			}
			
			var toIndex = GetCollectionIndex(toCollectionFlatIndex);

			// insert dragged value to target parent
			for(int n = collectionCount - 1; n >= 0; n--)
			{
				var collection = collections[n];
				if(GetCollectionElementCount(collection) >= toCollectionFlatIndex)
				{
					// Values intentionally inserted in reverse order. Since they are
					// always added in the same index, the resulting order is correct.
					for(int v = draggedValues.Length - 1; v >= 0; v--)
					{
						TryInsertAt(ref collection, toIndex, draggedValues[v]);
					}
					collections[n] = collection;
				}
			}

			SetValues(collections, false, true);
			
			RebuildMemberBuildListAndMembers();
			OnCachedValueChanged(false, false);

			var selectMember = members[toCollectionFlatIndex + FirstCollectionMemberIndexOffset];
			if(selectMember.ShouldShowInInspector && selectMember.Selectable)
			{
				members[toCollectionFlatIndex + FirstCollectionMemberIndexOffset].Select(ReasonSelectionChanged.Initialization);
			}
			else
			{
				Select(ReasonSelectionChanged.Initialization);
			}
		}

		/// <inheritdoc />
		public void OnMemberReorderingEnded(IReorderable reordering)
		{
			reorderingMemberAtIndex = -1;
		}
		
		/// <inheritdoc />
		public int GetDropTargetIndexAtPoint(Vector2 point)
		{
			// new test: allow dragging on the header to add as last member
			if(labelLastDrawPosition.Contains(point))
			{
				return LastVisibleCollectionMemberIndex + 1;
			}

			return ReorderableParentDrawerUtility.GetDropTargetIndexAtPoint(this, point, false);
		}

		/// <inheritdoc />
		public virtual bool MemberIsReorderable(IReorderable member)
		{
			if(ReadOnly)
			{
				return false;
			}

			// Don't allow reodering if has mixed content?
			if(MixedContent)
			{
				return false;
			}

			var memberParent = member.Parent;
			if(memberParent == this)
			{
				// Don't allow reordering the resize field.
				return member != Resizer;
			}

			// Allow cross-collection drag-n-drop as long as member types match
			if(!MemberType.IsAssignableFrom(member.Type))
			{
				return false;
			}

			// If dragged value is already contained in a collection that can't contain duplicates, then return false.
			// NOTE: Currently also makes it impossible to reorder values of a collection that can't contain duplicates.
			if(!CanContainDuplicates && Contains(member.GetValue()))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Determines whether or not it is possible to add the given value to all target collections.
		/// </summary>
		/// <param name="value"> Value to test. </param>
		/// <returns> True if value can be added to all target collections, otherwise false. </returns>
		protected virtual bool CanAddValue(object value)
		{
			for(int t = MixedContent ? MemberInfo.TargetCount : 1 - 1; t >= 0; t--)
			{
				if(!CanAddValueToCollection((TValue)GetValue(t), value))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Determines whether or not it is possible to add the given value to the collection.
		/// </summary>
		/// <param name="collection"> Collection to test. Can be null. </param>
		/// <param name="value"> Value to test. </param>
		/// <returns> True if value can be added to all target collections, otherwise false. </returns>
		protected virtual bool CanAddValueToCollection([CanBeNull]TValue collection, object value)
		{
			// if collection is null, we can just create a new instance automatically
			return true;
		}

		/// <summary>
		/// Determines whether or not any target collection contains the given value.
		/// </summary>
		/// <param name="value"> Value for which to check. </param>
		/// <returns> True if any target collection contains value. </returns>
		protected bool Contains(object value)
		{
			for(int t = MixedContent ? MemberInfo.TargetCount : 1 - 1; t >= 0; t--)
			{
				if(CollectionContains((TValue)GetValue(t), value))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determines whether or not the collection contains the value.
		/// </summary>
		/// <param name="collection"> Collection to test. Can be null. </param>
		/// <param name="value"> Value for which to check. </param>
		/// <returns> True if collection contains value, otherwise false. </returns>
		protected abstract bool CollectionContains([CanBeNull]TValue collection, object value);

		/// <inheritdoc />
		public bool SubjectIsReorderable(Object subject)
		{
			if(ReadOnly)
			{
				return false;
			}

			if(subject == null)
			{
				return false;
			}

			// Only allow dragging Object references into collection
			// if can be added to collection
			if(!MemberType.IsAssignableFrom(subject.GetType()))
			{
				return false;
			}

			// if dragged value is already contained in a collection that can't contain duplicates, then return false
			if(!CanContainDuplicates && Contains(subject))
			{
				return false;
			}

			/*
			// Only allow dragging Object references on top of the 
			// prefix label to add them to the collection. If an
			// Object reference field inside the collection is mouseovered
			// for exampler, then decline the drag n drop.
			if(Inspector.Manager.MouseoveredSelectable != this)
			{
				return false;
			}
			*/

			return true;
		}

		/// <inheritdoc />
		public override bool DrawPrefix(Rect position)
		{
			bool dirty = base.DrawPrefix(position);

			if(Event.current.type == EventType.Repaint)
			{
				GUI.Label(itemCountRect, itemCountLabel, InspectorPreferences.Styles.SecondaryInfo);
			}

			return dirty;
		}

		/// <summary>
		/// Draws the body on multiple rows. E.g. arrays.
		/// </summary>
		public override bool DrawBodyMultiRow(Rect position)
		{
			bool dirty = false;

			#if GROUP_MEMBERS_INSIDE_BOX
			var boxRect = position;
			boxRect.height = Height - HeaderHeight;
			DrawGUI.AddMarginsAndIndentation(ref boxRect);
			//GUI.Box(boxRect, GUIContent.none, UnityEditor.EditorStyles.helpBox);
			#endif

			#if DEV_MODE && PI_ASSERTATIONS
			int indentLevelWas = DrawGUI.IndentLevel;
			#endif

			if(Unfoldedness >= 1f)
			{
				DrawFoldableContent(position);
			}
			else
			{
				using(new MemberScaler(position.min, Unfoldedness))
				{
					DrawFoldableContent(position);
				}
			}

			#if DEV_MODE && PI_ASSERTATIONS
			if(indentLevelWas != DrawGUI.IndentLevel) { Debug.LogWarning("indentLevelWas ("+ indentLevelWas+") != DrawGUI.IndentLevel ("+ DrawGUI.IndentLevel +")"); }
			#endif

			return dirty;
		}

		protected virtual bool DrawFoldableContent(Rect position)
		{
			bool dirty = false;

			int appendIndentLevel = AppendIndentLevel;

			DrawGUI.IndentLevel += appendIndentLevel;
			{
				int count = visibleMembers.Length;

				var inspector = InspectorUtility.ActiveInspector;

				int reorderDropTargetIndex;
				int reorderSourceIndex;
				bool nowReordering;
				ReorderInfo reordering;

				var inspectorDrawer = inspector.InspectorDrawer;
				var mouseDownInfo = inspectorDrawer.Manager.MouseDownInfo;
				if(!mouseDownInfo.NowReordering)
				{
					nowReordering = false;
					reordering = null;
					reorderSourceIndex = -1;
					reorderDropTargetIndex = -1;
				}
				else
				{
					reordering = mouseDownInfo.Reordering;
					if(reordering.IsUnityObjectHeaderDrag || reordering.Drawer == null || (reordering.Drawer is IParentDrawer && (reordering.Drawer as IParentDrawer).IsParentOrGrandParentOf(this)))
					{
						nowReordering = false;
						reordering = null;
						reorderSourceIndex = -1;
						reorderDropTargetIndex = -1;
					}
					else
					{
						reorderSourceIndex = reorderingMemberAtIndex;
						if(reordering.MouseoveredDropTarget.Parent == this)
						{
							reorderDropTargetIndex = reordering.MouseoveredDropTarget.MemberIndex;
						}
						else
						{
							reorderDropTargetIndex = -1;
						}
						nowReordering = reorderSourceIndex != -1 || reorderDropTargetIndex != -1;
					}
				}
				
				int visibleMemberIndex = 0;
				if(!nowReordering)
				{
					while(visibleMemberIndex < count)
					{
						float height = visibleMembers[visibleMemberIndex].Height;

						position.height = height;

						if(!inspector.IsAboveViewport(position.yMax))
						{
							break;
						}

						position.y += height;
						visibleMemberIndex++;
					}
				}

				var drawGapPosition = Rect.zero;
				var drawReorderedRect = Rect.zero;
				while(visibleMemberIndex < count)
				{
					var member = visibleMembers[visibleMemberIndex];
					
					// If dragged element is currently positioned at this index draw the element here
					// (unless it's 
					if(reorderDropTargetIndex == visibleMemberIndex)
					{
						if(!reordering.IsUnityObjectHeaderDrag && (!(reordering.Drawer is IParentDrawer) || !(reordering.Drawer as IParentDrawer).IsParentOrGrandParentOf(this)))
						{
							position.height = reordering.Drawer.Height;
							drawReorderedRect = position;
							position.y += position.height;

							//move on to draw the member at the current index...
						}
					}

					//if member at this index is currently being dragged elsewhere
					if(reorderSourceIndex == visibleMemberIndex)
					{
						//draw a hole here to indicate the position where the item left
						//TO DO: Leave a small gap here even?
						//also maybe draw this as the last thing, so can make a 2px hole that overlaps two members?
						if(reorderDropTargetIndex != reorderSourceIndex)
						{
							drawGapPosition = position;
							drawGapPosition.height = ReorderableParentDrawerUtility.DraggedObjectGapHeight;
							DrawGUI.AddMarginsAndIndentation(ref drawGapPosition);

							position.y += ReorderableParentDrawerUtility.DraggedObjectGapHeight;
						}

						//skip drawing this member here as it's being dragged somewhere else
						visibleMemberIndex++;
						continue;
					}
					
					position.height = member.Height;

					//don't draw controls that are off-screen for performance reasons
					if(inspector.IsBelowViewport(position.y) && !nowReordering)
					{
						break;
					}
					
					GUI.changed = false;

					GUIContent labelWas = null;
					if(nowReordering)
					{
						int modIndex = 0;
						if(reorderSourceIndex != -1 && reorderSourceIndex <= visibleMemberIndex)
						{
							modIndex--;
						}
						if(reorderDropTargetIndex != -1 && reorderDropTargetIndex <= visibleMemberIndex)
						{
							modIndex++;
						}

						if(modIndex != 0)
						{
							int reorderedIndexInCollection = GetMemberIndexInCollection(member) + modIndex;
							labelWas = GUIContentPool.Create(member.Label);
							UpdateMemberLabelForIndex(member, GetCollectionIndex(reorderedIndexInCollection));
						}
					}

					if((nowReordering ? DrawMemberDuringReordering(member, position) : member.Draw(position)) || GUI.changed)
					{
						dirty = true;
						GUI.changed = true;
					}

					if(labelWas != null)
					{
						member.Label = labelWas;
					}

					position.y += position.height;
					visibleMemberIndex++;
				}

				if(nowReordering)
				{
					var reorderingDrawer = reordering.Drawer;
					var isUnityObjectHeaderDrag = reordering.IsUnityObjectHeaderDrag;

					//there's also a valid reorder drop rect below the last member
					if(reorderDropTargetIndex == visibleMemberIndex && !isUnityObjectHeaderDrag && (reordering.Drawer as IParentDrawer == null || !(reordering.Drawer as IParentDrawer).IsParentOrGrandParentOf(this)))
					{
						drawReorderedRect = position;
						drawReorderedRect.height = reordering.Drawer.Height;
					}
					else if(reorderSourceIndex + FirstCollectionMemberIndexOffset == visibleMemberIndex)
					{
						drawGapPosition = position;
						drawGapPosition.y -= ReorderableParentDrawerUtility.DraggedObjectGapHeight; //test.. hmm...
						drawGapPosition.height = ReorderableParentDrawerUtility.DraggedObjectGapHeight;
						DrawGUI.AddMarginsAndIndentation(ref drawGapPosition);
					}

					if(drawGapPosition.width > 0f)
					{
						var col = InspectorUtility.Preferences.theme.Background;
						col.r -= 0.1f;
						col.g -= 0.1f;
						col.b -= 0.1f;
						DrawGUI.Active.Label(drawGapPosition, GUIContent.none, "ReorderDropTargetBackground");

						if(reorderDropTargetIndex == -1)
						{
							var arrowPos = drawGapPosition;
							arrowPos.x -= 16f;
							arrowPos.y -= 4f;
							GUI.Label(arrowPos, GUIContent.none, "Icon.ExtrapolationContinue");
						}
					}

					if(drawReorderedRect.width > 0)
					{
						var bgRect = drawReorderedRect;
						bgRect.y = drawReorderedRect.y;
						DrawGUI.AddMarginsAndIndentation(ref bgRect);
						DrawGUI.Active.Label(bgRect, GUIContent.none, "ReorderDropTargetBackground");

						var arrowPos = drawReorderedRect;
						arrowPos.x += DrawGUI.LeftPadding;
						arrowPos.width = 18f;
						arrowPos.y += 3f;
						GUI.Label(arrowPos, GUIContent.none, "Icon.ExtrapolationContinue");

						drawReorderedRect.y = Cursor.LocalPosition.y + reorderingDrawer.MouseDownCursorTopLeftCornerOffset.y; //add some clamping?
						bgRect.y = drawReorderedRect.y;
						DrawGUI.Active.ColorRect(bgRect, DrawGUI.TintedBackgroundColor);

						var dropShadowRect = drawReorderedRect;
						dropShadowRect.y -= 2f;
						dropShadowRect.height += 4f;
						DrawGUI.AddMarginsAndIndentation(ref dropShadowRect);
						dropShadowRect.width += 2f;

						GUIContent labelWas;
						if(reorderDropTargetIndex == reorderSourceIndex)
						{
							labelWas = null;
						}
						else
						{
							int dropTargetCollectionIndex = GetReorderingDropTargetIndexInCollection(reorderDropTargetIndex);
							labelWas = GUIContentPool.Create(reorderingDrawer.Label);
							UpdateMemberLabelForIndex(reorderingDrawer, GetCollectionIndex(dropTargetCollectionIndex));
						}

						if(!isUnityObjectHeaderDrag && reorderingDrawer.Draw(drawReorderedRect))
						{
							dirty = true;
							GUI.changed = true;
						}
					
						DrawGUI.DrawLeftClickAreaMouseoverEffect(bgRect);
						
						if(labelWas != null)
						{
							reordering.Drawer.Label = labelWas;
						}
					}
				}

				#if GROUP_MEMBERS_INSIDE_BOX
				DrawGUI.DrawRect(boxRect, new Color(1f, 1f, 1f, 0.1f));
				boxRect.x += 1f;
				boxRect.y += 1f;
				boxRect.width -= 2f;
				boxRect.height -= 2f;
				DrawGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.1f));
				#endif
			}
			DrawGUI.IndentLevel -= appendIndentLevel;

			return dirty;
		}

		private bool DrawMemberDuringReordering(IDrawer member, Rect position)
		{
			var currentPos = member.Bounds;
			
			var manager = InspectorUtility.ActiveManager;

			if(manager.IgnoreAllMouseInputs)
			{
				return false;
			}

			float currentYPos = currentPos.y;
			float targetYPos = position.y;
			float diff = targetYPos - currentYPos;
			float abs = Mathf.Abs(diff);

			var inspector = manager.ActiveInspector;
			var inspectorDrawer = inspector.InspectorDrawer;

			if(abs < 0.05f || abs > 40f || !inspectorDrawer.UpdateAnimationsNow)
			{
				return member.Draw(position);
			}
			
			#if DEV_MODE
			float tweenSpeed = inspector.Preferences.reorderingAnimationSpeed;
			float distanceMultiplier;
			switch(inspector.Preferences.reorderingEaseType)
			{
				case 1:
					distanceMultiplier = Mathf.Sqrt(abs);
					break;
				case 2:
					distanceMultiplier = abs * abs;
					break;
				default:
					distanceMultiplier = abs;
					break;
			}
			#else
			float tweenSpeed = 14;
			float distanceMultiplier = abs;
			#endif

			float stepDistance = tweenSpeed * inspectorDrawer.AnimationDeltaTime * distanceMultiplier;

			if(diff > 0f)
			{
				currentYPos += stepDistance;
				if(currentPos.y > targetYPos)
				{
					currentYPos = targetYPos;
				}
			}
			else
			{
				currentYPos -= stepDistance;
				if(currentPos.y < targetYPos)
				{
					currentYPos = targetYPos;
				}
			}
			currentPos.y = currentYPos;
			return member.Draw(currentPos);
		}
		
		/// <inheritdoc/>
		public void DuplicateMember([NotNull]IFieldDrawer member)
		{
			if(!CanContainDuplicates)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".DuplicateMember("+member+ ") called but CanContainDuplicates was false");
				#endif
				return;
			}


			var flattenedIndex = GetMemberIndexInCollection(member);
			if(flattenedIndex == -1)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+".DuplicateMember("+member+") called but member does not represent an element in the collection");
				#endif
				return;
			}

			var collectionIndex = GetCollectionIndex(flattenedIndex);
			var collections = GetCopyOfValuesInternal();

			#if DEV_MODE && DEBUG_DUPLICATE_MEMBER
			Debug.Log(ToString()+".DuplicateMember("+member+") with collectionIndex"+collectionIndex);
			#endif

			var memberValuesDuplicated = member.GetCopyOfValues();
			// NOTE: Currently duplicate does not support duplicating multiple values.
			// Instead value of first target will be given for all targets.
			var duplicatedValue = memberValuesDuplicated[0];

			for(int n = collections.Length - 1; n >= 0; n--)
			{
				var collection = collections[n];
				if(GetElementCount(GetCollectionSize(collection)) > flattenedIndex)
				{
					#if DEV_MODE && DEBUG_DUPLICATE_MEMBER
					Debug.Log(ToString()+".DuplicateMember("+member+") Creating copy of collection value at index "+collectionIndex);
					#endif

					#if DEV_MODE && DEBUG_DUPLICATE_MEMBER
					Debug.Log(ToString()+".DuplicateMember("+member+") inserting copy at index "+collectionIndex+"\noriginal: "+StringUtils.ToString(GetCollectionValue(collection, collectionIndex))+"\ncopy:"+StringUtils.ToString(duplicatedValue));
					#endif

					TryInsertAt(ref collection, collectionIndex, duplicatedValue);
					collections[n] = collection;
				}
			}
			
			#if DEV_MODE && DEBUG_DUPLICATE_MEMBER
			Debug.Log(ToString()+".DuplicateMember("+member+") calling SetValues with collections "+StringUtils.ToString(collections));
			#endif

			SetValues(collections, false, true);
			RebuildMemberBuildListAndMembers();
			OnCachedValueChanged(false, false);
		}

		public override void OnMouseover()
		{
			elementCountMouseovered = itemCountRect.MouseIsOver();

			base.OnMouseover();
		}

		/// <inheritdoc/>
		public override void OnMouseoverDuringDrag(MouseDownInfo mouseDownInfo, Object[] dragAndDropObjectReferences)
		{
			// support dragging content on top of the header to add as last children
			if(mouseDownInfo.Reordering.MouseoveredDropTarget.Parent == this)
			{
				DrawGUI.Active.DragAndDropVisualMode = DragAndDropVisualMode.Copy;
				return;
			}

			base.OnMouseoverDuringDrag(mouseDownInfo, dragAndDropObjectReferences);
		}

		public override void OnMouseUpAfterDownOverControl(Event inputEvent, bool isClick)
		{
			if(isClick && elementCountMouseovered)
			{
				var menu = Menu.Create();
				menu.Add("Create Instance", ()=>SetValue(DefaultValue(true)));
				menu.OpenAt(itemCountRect);
				return;
			}

			base.OnMouseUpAfterDownOverControl(inputEvent, isClick);
		}

		/// <summary>
		/// Called before keyboard input is sent to a selected member of the collection.
		///  </summary>
		/// <param name="member"> The member that is selected. </param>
		/// <param name="inputEvent"> Iput data. </param>
		/// <param name="keys"> Key configuration data. </param>
		/// <returns> True if input event should be consumed, i.e. not sent to the selected member, otherwise false. </returns>
		private bool OnMemberKeyboardInputBeingGiven(IDrawer member, Event inputEvent, KeyConfigs keys)
		{
			if(DrawGUI.EditingTextField)
			{
				return false;
			}

			if(keys.duplicate.DetectAndUseInput(inputEvent))
			{
				if(!CanContainDuplicates)
				{
					if(member != Resizer)
					{
						Inspector.Message(StringUtils.ToStringSansNamespace(Type) + " Cannot Contain Duplicates");
					}
					return true;
				}

				DuplicateMember(member as IFieldDrawer);
				return true;
			}

			switch(inputEvent.keyCode)
			{
				case KeyCode.Home:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						var firstVisibleMember = visibleMembers[0];
						if(member != firstVisibleMember)
						{
							firstVisibleMember.Select(ReasonSelectionChanged.KeyPressShortcut);
							return true;
						}
						
						//select collection itself if first member already selected?
						Select(ReasonSelectionChanged.KeyPressShortcut);
						return true;
					}
					return false;
				case KeyCode.Delete:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);

						if(ReadOnly || IsReadOnlyCollection)
						{
							InspectorUtility.ActiveInspector.Message("Can't delete member of a read-only collection");
							return false;
						}
						var manager = InspectorUtility.ActiveManager;
						if(manager.HasMultiSelectedControls)
						{
							DeleteMembers(manager.MultiSelectedControls);
						}
						else
						{
							DeleteMember(member);
						}
						return true;
					}
					return false;
				case KeyCode.End:
					if(inputEvent.modifiers == EventModifiers.FunctionKey)
					{
						GUI.changed = true;
						DrawGUI.Use(inputEvent);
						var lastVisibleMember = visibleMembers[visibleMembers.Length - 1];
						if(member != lastVisibleMember)
						{
							lastVisibleMember.Select(ReasonSelectionChanged.KeyPressShortcut);
							return true;
						}

						//select next control if last member already selected?
						var next = GetNextSelectableDrawerDown(GetSelectedRowIndex(), this);
						if(next != null)
						{
							next.Select(ReasonSelectionChanged.KeyPressShortcut);
							return true;
						}
					}
					return false;

			}
			return false;
		}

		/// <inheritdoc cref="IParentDrawer.OnMemberValueChanged" />
		public override void OnMemberValueChanged(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			if(inactive)
			{
				return;
			}
			
			if(IsReadOnlyCollection)
			{
				#if DEV_MODE
				Debug.LogWarning(ToString()+ " - Value of a child in a collection but IsReadOnlyCollection was true...Safe to ignore? Rebuild CollectionDrawer from scratch?");
				#endif
				return;
			}

			if(memberIndex >= FirstCollectionMemberIndexOffset)
			{
				int collectionIndex = memberIndex - FirstCollectionMemberIndexOffset;

				var cachedValueElementCount = GetCollectionElementCount(Value);
				if(cachedValueElementCount <= collectionIndex)
				{
					#if DEV_MODE
					Debug.LogWarning(ToString()+ ".OnMemberValueChanged("+memberIndex+") with collectionIndex "+collectionIndex+" but Value element count was only "+ cachedValueElementCount);
					#endif
					RebuildMemberBuildListAndMembers();
					parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), Value, MemberInfo);
					return;
				}

				//update cached array value
				if(!IsFixedSize) //LinkedMemberInfos should handle updating this whole value if IsFixedSize is true...
				{
					TryToManuallyUpdateCachedValueFromMember(memberIndex, memberValue, memberLinkedMemberInfo);
				}
				
				//update member label in case member's first string field's contents are shown in the label
				UpdateMemberLabelForIndex(members[memberIndex], GetCollectionIndex(collectionIndex));

				UpdateDataValidity(true);
			}
			// if changed member was the resizer, then update size and rebuild members
			else
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(members[memberIndex] == Resizer);
				#endif

				// set cached value silently?
				
				UpdateCachedValueFromField(true);
				UpdateSize();
				RebuildMemberBuildListAndMembers();
			}

			for(int n = members.Length - 1; n >= 0; n--)
			{
				if(n != memberIndex)
				{
					members[n].OnSiblingValueChanged(memberIndex, memberValue, memberLinkedMemberInfo);
				}
			}

			if(parent != null)
			{
				parent.OnMemberValueChanged(Array.IndexOf(parent.Members, this), Value, MemberInfo);
			}

			#if DEV_MODE && PI_ASSERTATIONS
			ValidateMembers();
			#endif
		}
		#if DEV_MODE && PI_ASSERTATIONS
		protected void ValidateMembers()
		{
			int count = members.Length;
			for(int a = 1; a < count; a++)
			{
				var linkedInfoA = members[a].MemberInfo;
				
				if(linkedInfoA == null)
				{
					Debug.LogError("members["+a+"] had no LinkedMemberInfo");
					continue;
				}

				if(!linkedInfoA.CanRead)
				{
					Debug.LogWarning("members["+a+"] LinkedMemberInfo CanRead was false");
					continue;
				}

				Debug.Assert(linkedInfoA.CollectionIndex == a - 1, ToString()+" members["+a+"] CollectionIndex was "+linkedInfoA.CollectionIndex+" and not expected value "+(a-1)+".");

				var valueA = linkedInfoA.GetValue(0);

				for(int b = 0; b < count; b++)
				{
					if(a == b)
					{
						continue;
					}

					var linkedInfoB = members[b].MemberInfo;
					if(linkedInfoB == null || !linkedInfoB.CanRead)
					{
						continue;
					}
					
					var valueB = linkedInfoB.GetValue(0);
					
					if(valueA != null && valueB != null && !valueA.GetType().IsValueType && !valueA.GetType().IsUnityObject())
					{
						Debug.Assert(!ReferenceEquals(valueA, valueB), ToString()+" Members ["+a+"] and ["+b+"] refer to same instance: "+StringUtils.ToString(valueA));
					}
				}
			}
		}
		#endif

		/// <summary>
		/// Given index of drop target in VisibleMembers, returns the index of the element the drawer represents in the collection.
		/// </summary>
		/// <param name="dropTargetVisibleMemberIndex"> Zero-based index of the drop target in VisibleMembers. </param>
		/// <returns> The reordering drop target index in collection. </returns>
		private int GetReorderingDropTargetIndexInCollection(int dropTargetVisibleMemberIndex)
		{
			// If there is a visible member above which the target was dropped then the index in collection equals the index of said member.
			// Except if drop source is same collection, but index below drop target, then we reduce the index by one.
			if(dropTargetVisibleMemberIndex < visibleMembers.Length)
			{
				var member = visibleMembers[dropTargetVisibleMemberIndex];
				int collectionIndex = GetMemberIndexInCollection(member);

				//if the member in question didn't represent a value in the collection then set index to 0
				if(collectionIndex <= 0)
				{
					return 0;
				}

				if(reorderingMemberAtIndex != -1)
				{
					int memberIndex = Array.IndexOf(members, member);
					if(memberIndex > reorderingMemberAtIndex)
					{
						collectionIndex--;
					}
				}

				
				return collectionIndex;
			}

			var elementCount = GetElementCount(GetCollectionSize(Value));
			if(reorderingMemberAtIndex != -1)
			{
				return elementCount - 1;
			}

			//else set index equal to the size of the collection (so it'd be added to the end)
			return elementCount;
		}

		private void DeleteMembers(List<IDrawer> delete)
		{
			int count = delete.Count;
			if(count == 0)
			{
				return;
			}

			var focusedControl = InspectorUtility.ActiveManager.FocusedDrawer;
			var selectedIndexPath = focusedControl == null ? null : focusedControl.GenerateMemberIndexPath(this);
			int indexPathLastElement = selectedIndexPath == null ? -1 : selectedIndexPath.Length - 1;
			int selectMemberAtIndex = indexPathLastElement == - 1 ? - 1 : selectedIndexPath[indexPathLastElement];
			
			var collections = GetCopyOfValuesInternal();
			int targetCount = collections.Length;

			for(int n = members.Length - 1; n >= 1; n--)
			{
				var member = members[n];
				if(!delete.Contains(member))
				{
					continue;
				}

				int flattenedIndex = GetMemberIndexInCollection(member);
				
				for(int t = targetCount - 1; t >= 0; t--)
				{
					var collection = collections[t];
					
					if(flattenedIndex >= 0)
					{
						if(GetElementCount(GetCollectionSize(collection)) > flattenedIndex)
						{
							var collectionIndex = GetCollectionIndex(flattenedIndex);
							RemoveAt(ref collection, collectionIndex);
							collections[t] = collection;
						}
					}
				}
				
				if(flattenedIndex <= selectMemberAtIndex)
				{
					selectMemberAtIndex--;
				}
			}

			SetValues(collections, false, true);
			
			RebuildMemberBuildListAndMembers();
			
			if(selectedIndexPath != null)
			{
				if(indexPathLastElement != -1)
				{
					selectedIndexPath[indexPathLastElement] = selectMemberAtIndex;
				}

				SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.Dispose);
			}

			OnCachedValueChanged(false, false);
		}

		/// <inheritdoc cref="ICollectionDrawer.DeleteMember" />
		public void DeleteMember(IDrawer delete)
		{
			int flattenedIndex = GetMemberIndexInCollection(delete);

			#if DEV_MODE && DEBUG_DELETE_MEMBER
			Debug.Log("DeleteMember(" + delete + ") with indexInCollection=" + flattenedIndex);
			#endif

			if(flattenedIndex >= 0)
			{
				var focusedControl = InspectorUtility.ActiveInspector.FocusedDrawer;
				var selectedIndexPath = focusedControl == null ? null : focusedControl.GenerateMemberIndexPath(this);
				int indexPathLastElement = selectedIndexPath == null ? -1 : selectedIndexPath.Length - 1;
				int selectMemberAtIndex = indexPathLastElement == -1 ? -1 : selectedIndexPath[indexPathLastElement];

				var collectionIndex = GetCollectionIndex(flattenedIndex);
				var setValues = GetCopyOfValuesInternal();
				int targetCount = setValues.Length;


				//remove value at index in all target collections
				for(int t = targetCount - 1; t >= 0; t--)
				{
					var collection = setValues[t];
					if(GetElementCount(GetCollectionSize(collection)) > flattenedIndex)
					{
						#if DEV_MODE && DEBUG_DELETE_MEMBER
						Debug.Log("RemoveAt(" + collectionIndex + ")");
						#endif

						RemoveAt(ref collection, collectionIndex);
						setValues[t] = collection;
					}
				}

				SetValues(setValues, false, true);
				
				RebuildMemberBuildListAndMembers();
				
				if(selectedIndexPath != null)
				{
					if(indexPathLastElement != -1)
					{
						selectedIndexPath[indexPathLastElement] = selectMemberAtIndex;
					}

					SelectMemberAtIndexPath(selectedIndexPath, ReasonSelectionChanged.Dispose);
				}
				
				OnCachedValueChanged(false, false);
			}
		}
		
		/// <inheritdoc/>
		public override void Dispose()
		{
			elementCountMouseovered = false;
			propertyAttributeInfo = null;
			isUnityObjectCollection = false;
			size = default(TSize);
			elementCount = -1;
			reorderingMemberAtIndex = -1;
			itemCountLabel.text = "";

			base.Dispose();
		}

		/// <inheritdoc />
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			if(memberIndex < FirstCollectionMemberIndexOffset)
			{
				Resize((int)memberValue);
				return true;
			}
			var setValue = Value;
			int flattenedIndex = memberIndex - FirstCollectionMemberIndexOffset;
			
			var cachedValueElementCount = GetCollectionElementCount(setValue);
			if(cachedValueElementCount <= flattenedIndex)
			{
				#if DEV_MODE
				Debug.LogError("TryToManuallyUpdateCachedValueFromMember called with elementCount "+ cachedValueElementCount+" <= index "+flattenedIndex);
				#endif
				RebuildMemberBuildListAndMembers();
				return true;
			}

			var collectionIndex = GetCollectionIndex(flattenedIndex);

			SetCollectionValue(ref setValue, collectionIndex, memberValue);
			
			SetValue(setValue, false, false);
			return true;
		}
		
		/// <summary> Sort elements of collections, using the IComparable implementation of each element of the collection. </summary>
		private void Sort()
		{
			bool changed = false;
			var comparer = GetElementComparer();

			var setValues = GetCopyOfValuesInternal();

			#if DEV_MODE && DEBUG_SORT
			Debug.Log("Sorting using comparer "+comparer.GetType().Name+" with definingType="+ElementComparerDefiningType().Name);
			#endif

			for(int n = setValues.Length - 1; n >= 0; n--)
			{
				var setValue = setValues[n];

				#if DEV_MODE && DEBUG_SORT
				Debug.Log("setValues["+n+"].Length="+ GetCollectionElementCount(setValue));
				#endif

				if(GetCollectionElementCount(setValue) == 0)
				{
					continue;
				}

				if(Sort(ref setValue, comparer))
				{
					changed = true;
					setValues[n] = setValue;
				}
			}
			
			if(changed)
			{
				changed = SetValues(setValues, false, true);

				#if DEV_MODE && PI_ASSERTATIONS
				Assert(changed);
				#endif
				
				RebuildMembers();
				
				OnCachedValueChanged(false, false);

				#if DEV_MODE && DEBUG_SORT
				Debug.Log(ToString()+" is now sorted.");
				#endif
			}
			#if DEV_MODE && DEBUG_SORT
			else { Debug.Log(ToString()+" was already sorted."); }
			#endif
		}
		
		/// <summary> Sort elements of the collection using the IComparable. </summary>
		protected abstract bool Sort(ref TValue collection, IComparer comparer);

		/// <inheritdoc />
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			base.OnCachedValueChanged(applyToField, updateMembers);
			UpdateSize();
		}

		private void RemoveDuplicates()
		{
			var memberType = MemberType;
			if(isUnityObjectCollection || memberType.IsValueType)
			{
				RemoveUnityObjectOrValueTypeDuplicates();
			}
			else
			{
				RemoveReferenceTypeDuplicates();
			}
		}
		
		private void RemoveUnityObjectOrValueTypeDuplicates()
		{
			var collections = Values;
			int collectionCount = collections.Length;
			bool isFixedSize = IsFixedSize;
			TValue[] setValues = null;

			for(int n = collectionCount - 1; n >= 0; n--)
			{
				var collection = collections[n];
				TValue setValue = null;
				int countWas = GetCollectionElementCount(collection);
				int countIs = countWas;
			
				for(int e = countIs - 1; e >= 1; e--)
				{
					var index = GetCollectionIndex(e);
					var element = GetCollectionValue(collection, index);

					for(int e2 = e - 1; e2 >= 0; e2--)
					{
						var index2 = GetCollectionIndex(e2);
						var element2 = GetCollectionValue(collection, index2);

						if(element == element2)
						{
							#if DEV_MODE
							Debug.Log("DUPLICATE FOUND: #" + e + " and #" + e2 + ":\n" + StringUtils.ToStringCompact(element));
							#endif

							if(setValue == null)
							{
								setValue = isFixedSize ? collection : PrettySerializer.Copy(collection) as TValue;
							}

							RemoveAt(ref setValue, index);
							countIs--;

							break;
						}
					}
				}

				if(countWas != countIs)
				{
					if(setValues == null)
					{
						setValues = PrettySerializer.Copy(collections) as TValue[];
					}

					setValues[n] = setValue;
					
					#if DEV_MODE
					Debug.Log("Remove Duplicates: removed "+ (countWas - countIs) +" items");
					#endif
				}
			}

			if(setValues != null)
			{
				SetValues(setValues, false, true);
				RebuildMemberBuildListAndMembers();
				OnCachedValueChanged(false, false);
			}
			#if DEV_MODE
			else { Debug.Log("RemoveReferenceTypeDuplicates finished without any changes"); }
			#endif
		}

		private void RemoveReferenceTypeDuplicates()
		{
			var collections = Values;
			int collectionCount = collections.Length;
			bool isFixedSize = IsFixedSize;
			TValue[] setValues = null;

			for(int n = collectionCount - 1; n >= 0; n--)
			{
				var collection = collections[n];
				TValue setValue = null;
				int countWas = GetCollectionElementCount(collection);
				int countIs = countWas;
			
				for(int e = countIs - 1; e >= 1; e--)
				{
					var index = GetCollectionIndex(e);
					var element = GetCollectionValue(collection, index);

					List<Object> objectReferences;
					var byteData = PrettySerializer.ToBytes(element, out objectReferences);
					for(int e2 = e - 1; e2 >= 0; e2--)
					{
						var index2 = GetCollectionIndex(e2);
						var element2 = GetCollectionValue(collection, index2);
			
						List<Object> objectReferences2;
						var byteData2 = PrettySerializer.ToBytes(element2, out objectReferences2);

						if(byteData.ContentsMatch(byteData2) && objectReferences.ContentsMatch(objectReferences2))
						{
							#if DEV_MODE
							Debug.Log("DUPLICATE FOUND: #" + e + " and #" + e2 + ":\n" + StringUtils.ToStringCompact(element));
							#endif

							if(setValue == null)
							{
								setValue = isFixedSize ? collection : PrettySerializer.Copy(collection) as TValue;
							}

							RemoveAt(ref setValue, index);
							countIs--;

							break;
						}
					}
				}

				if(countWas != countIs)
				{
					if(setValues == null)
					{
						setValues = PrettySerializer.Copy(collections) as TValue[];
					}

					setValues[n] = setValue;
					
					#if DEV_MODE
					Debug.Log("Remove Duplicates: removed "+ (countWas - countIs) +" items");
					#endif
				}
			}

			if(setValues != null)
			{
				//set collection drawers inactive to suppress various callbacks
				SetValues(setValues, false, true);
				RebuildMemberBuildListAndMembers();
				OnCachedValueChanged(false, false);
			}
			#if DEV_MODE
			else { Debug.Log("RemoveReferenceTypeDuplicates finished without any changes"); }
			#endif
		}

		private bool MemberRepresentsValueInCollection(IDrawer member)
		{
			return member != Resizer;
		}

		private IComparer GetElementComparer()
		{
			if(isUnityObjectCollection)
			{
				return new UnityObjectElementComparer();
			}

			var type = ElementComparerDefiningType();
			if(typeof(IComparable).IsAssignableFrom(type))
			{
				return new ComparableElementComparer();
			}
			
			return new FallbackElementComparer();
		}

		/// <summary>
		/// When building IComparer for sorting the collection, what is the type of the key
		/// that should be used when sorting? </summary>
		/// <returns> The defining key type for sorting. </returns>
		protected virtual Type ElementComparerDefiningType()
		{
			return MemberType;
		}

		private class ComparableElementComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return (x as IComparable).CompareTo(y as IComparable);
			}
		}

		private class UnityObjectElementComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				var xo = x as Object;
				var yo = y as Object;
				if(ReferenceEquals(xo, yo))
				{
					return 0;
				}
				if(xo == null)
				{
					return -1;
				}
				if(yo == null)
				{
					return 1;
				}
				return string.CompareOrdinal(xo.name, yo.name);
			}
		}

		/// <inheritdoc />
		protected override void GetDrawPositions(Rect position)
		{
			base.GetDrawPositions(position);
			UpdateItemCountRect();
		}

		private class FallbackElementComparer : IComparer
		{
			public int Compare(object x, object y)
			{
				return string.CompareOrdinal(PrettySerializer.Serialize(x), PrettySerializer.Serialize(y));
			}
		}
	}
}