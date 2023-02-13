#define SAFE_MODE

using System;
using System.Reflection;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// GUI drawers representing all the generic type arguments of a generic type definition.
	/// </summary>
	[Serializable]
	public class GenericTypeArgumentDrawer : ParentFieldDrawer<Type[]>
	{
		public Type genericTypeDefinition;
		private bool drawInSingleRow;

		/// <inheritdoc />
		public override bool DrawInSingleRow
		{
			get
			{
				return drawInSingleRow;
			}
		}

		/// <inheritdoc />
		public override Type Type
		{
			get
			{
				return typeof(Type[]);
			}
		}

		/// <inheritdoc />
		public override Type[] Value
		{
			get
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					#if DEV_MODE
					Debug.Log("Building GenericTypeArgumentDrawer members because Value was called. memberBuildState="+memberBuildState+", memberBuildList.Count="+memberBuildList.Count);
					#endif
					
					BuildMembers();
				}

				int count = members.Length;
				
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(count == genericTypeDefinition.GetGenericArguments().Length, "members.Length="+ count + " != "+ genericTypeDefinition.GetGenericArguments().Length);
				#endif

				var result = ArrayPool<Type>.Create(count);
				for(int n = count - 1; n >= 0; n--)
				{
					result[n] = (Type)members[n].GetValue();
				}
				return result;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="type"> Generic type definition whose type arguments the created drawer will represent. </param>
		/// <param name="memberInfo"> LinkedMemberInfo that represents the class member that the generic arguments belong to. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static GenericTypeArgumentDrawer Create(Type genericTypeArgument, LinkedMemberInfo memberInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			GenericTypeArgumentDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new GenericTypeArgumentDrawer();
			}
			result.Setup(genericTypeArgument, memberInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Create method.");
		}
		
		/// <inheritdoc/>
		protected sealed override void Setup(Type[] setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method.");
		}

		private void Setup(Type setGenericTypeDefinition, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE
			if(setReadOnly)
			{
				Debug.LogWarning(StringUtils.ToColorizedString(ToString(), ".Setup - readonly=", true, ". Really don't allow editing generic type argument? This is usually desired even for read-only properties."));
			}
			#endif

			genericTypeDefinition = setGenericTypeDefinition;

			var genericTypeArguments = genericTypeDefinition.GetGenericArguments();
			drawInSingleRow = genericTypeArguments.Length == 1;

			if(setLabel == null)
			{
				setLabel = GUIContentPool.Create("Arguments", "Type arguments for the generic type definition.");
			}
			
			int count = genericTypeArguments.Length;
			var setValue = ArrayPool<Type>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				setValue[n] = GenericArgumentValues.GetValue(setGenericTypeDefinition, n);
			}

			// always set readonly to false to fix issue where
			// parameters of read-only indexer Properties could not be modified
			base.Setup(setValue, typeof(Type[]), setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			var genericTypeArguments = Value;
			int count = genericTypeArguments.Length;
			if(count > 0)
			{
				var hierarchy = MemberHierarchy;
				for(int n = 0; n < count; n++)
				{
					memberBuildList.Add(hierarchy.Get(memberInfo, genericTypeArguments[n], n));
				}
			}
		}
		
		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			int count = memberBuildList.Count;

			var genericTypeArguments = Value;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(count == genericTypeArguments.Length, "memberBuildList.Count="+memberBuildList.Count+" != "+genericTypeArguments.Length);
			#endif

			#if DEV_MODE
			Debug.Log("GenericTypeArgumentDrawer.DoBuildMembers called with types.Length="+genericTypeArguments.Length+", memberBuildList.Count="+memberBuildList.Count+", drawInSingleRow="+drawInSingleRow);
			#endif

			DrawerArrayPool.Resize(ref members, count);
			for(int n = count - 1; n >= 0; n--)
			{
				var argumentMemberInfo = memberBuildList[0];
				var type = argumentMemberInfo.Type;
				var baseTypeConstraints = type.GetGenericParameterConstraints();
				TypeConstraint typeCategoryConstraint;
				var specialConstraintMask = type.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
				if((specialConstraintMask & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
				{
					typeCategoryConstraint = TypeConstraint.Class;
				}
				else if((specialConstraintMask & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
				{
					typeCategoryConstraint = TypeConstraint.Struct;
				}
				else
				{
					typeCategoryConstraint = TypeConstraint.None;
				}

				if(baseTypeConstraints.Length > 0)
				{
					members[n] = ConstraintedTypeDrawer.Create(type, baseTypeConstraints, typeCategoryConstraint, argumentMemberInfo, this, GUIContent.none, ReadOnly);
				}
				else
				{
					members[n] = TypeDrawer.Create(type, argumentMemberInfo, this, GUIContent.none, ReadOnly);
				}
			}
		}

		/// <inheritdoc/>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override bool TryToManuallyUpdateCachedValueFromMember(int memberIndex, object memberValue, LinkedMemberInfo memberLinkedMemberInfo)
		{
			var parametersValues = Value;
			parametersValues[memberIndex] = (Type)memberValue;
			SetValue(parametersValues, false, false);
			return true;
		}
	}
}