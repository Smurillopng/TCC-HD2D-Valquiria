#define SAFE_MODE

using System;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	/// <summary>
	/// GUI drawers representing all the parameters of a method or a property indexer.
	/// </summary>
	[Serializable]
	public class ParameterDrawer : ParentFieldDrawer<object[]>
	{
		public ParameterInfo[] parameterInfos;
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
				return typeof(object[]);
			}
		}

		/// <inheritdoc />
		public override object[] Value
		{
			get
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					#if DEV_MODE
					Debug.Log("Building ParameterDrawer members because Value was called. memberBuildState="+memberBuildState+", memberBuildList.Count="+memberBuildList.Count);
					#endif
					
					BuildMembers();
				}

				int count = members.Length;
				
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(count == parameterInfos.Length, "members.Length="+members.Length+" != "+parameterInfos.Length);
				#endif

				var result = ArrayPool<object>.Create(count);
				for(int n = count - 1; n >= 0; n--)
				{
					result[n] = members[n].GetValue();
				}
				return result;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="parameterInfos"> ParameterInfos that the created drawers represents. </param>
		/// <param name="methodOrPropertyInfo"> LinkedMemberInfo that represents the property or method that the parameters belong to. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="readOnly"> True if drawer should be read only. </param>
		/// <returns> The drawer instance, ready to be used. </returns>
		public static ParameterDrawer Create(ParameterInfo[] parameterInfos, [NotNull]LinkedMemberInfo methodOrPropertyInfo, IParentDrawer parent, GUIContent label, bool readOnly)
		{
			ParameterDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ParameterDrawer();
			}
			result.Setup(parameterInfos, methodOrPropertyInfo, parent, label, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			var type = DrawerUtility.GetType(setMemberInfo, setValue);
			if(type.IsArray)
			{
				Setup(setValue as ParameterInfo[], setMemberInfo, setParent, setLabel, setReadOnly);
			}
			else
			{
				Setup(ArrayPool<ParameterInfo>.CreateWithContent(setValue as ParameterInfo), setMemberInfo, setParent, setLabel, setReadOnly);
			}
		}
		
		/// <inheritdoc/>
		protected sealed override void Setup(object[] setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			throw new NotSupportedException("Please use other Setup method.");
		}

		private void Setup(ParameterInfo[] setParameterInfos, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(setMemberInfo != null);
			#endif

			#if DEV_MODE
			if(setReadOnly)
			{
				Debug.LogWarning(StringUtils.ToColorizedString(ToString(), ".Setup - readonly=", true, ". Really don't allow editing parameter value? This is usually desired even for read-only properties."));
			}
			#endif

			parameterInfos = setParameterInfos;
			drawInSingleRow = parameterInfos.Length == 1 && DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(parameterInfos[0].ParameterType);

			if(setLabel == null)
			{
				setLabel = GUIContentPool.Create("Parameters");
			}
			
			int count = parameterInfos.Length;
			var setValue = ArrayPool<object>.Create(count);
			for(int n = count - 1; n >= 0; n--)
			{
				setValue[n] = ParameterValues.GetValue(parameterInfos[n]);
			}

			// always set readonly to false to fix issue where
			// parameters of read-only indexer Properties could not be modified
			base.Setup(setValue, typeof(object[]), setMemberInfo, setParent, setLabel, setReadOnly);
		}
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList()
		{
			int count = parameterInfos.Length;
			if(count == 0)
			{
				return;
			}

			var hierarchy = MemberHierarchy;
			if(hierarchy == null)
			{
				for(int n = 0; n < count; n++)
				{
					memberBuildList.Add(null);
				}
				return;
			}

			for(int n = 0; n < count; n++)
			{
				memberBuildList.Add(hierarchy.Get(memberInfo, parameterInfos[n]));
			}
		}
		
		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			int count = memberBuildList.Count;

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(count == parameterInfos.Length, "memberBuildList.Count="+memberBuildList.Count+" != "+parameterInfos.Length);
			#endif

			#if DEV_MODE
			Debug.Log("ParameterDrawer.DoBuildMembers called with parameterInfos.Length="+parameterInfos.Length+", memberBuildList.Count="+memberBuildList.Count+", drawInSingleRow="+drawInSingleRow);
			#endif

			DrawerArrayPool.Resize(ref members, count);
			
			// If only has one parameter can be drawn in a single row.
			if(drawInSingleRow)
			{
				#if DEV_MODE && PI_ASSERTATIONS
				Debug.Assert(memberBuildList.Count == 1);
				Debug.Assert(parameterInfos.Length == 1);
				#endif

				var methodOrPropertyInfo = memberBuildList[0];
				ParameterInfo parameterInfo;
				Type parameterType;
				object parameterValue;
				if(methodOrPropertyInfo == null)
				{
					parameterInfo = null;
					parameterType = typeof(object);
					parameterValue = null;
				}
				else
				{
					parameterInfo = methodOrPropertyInfo.ParameterInfo;
					parameterType = GetParameterTypeAndLabel(methodOrPropertyInfo, ref label);
					parameterValue = ParameterValues.GetValue(parameterInfo);
				}
				
				var member = DrawerProvider.GetForField(parameterValue, parameterType, methodOrPropertyInfo, this, GUIContent.none, ReadOnly);
				
				#if DEV_MODE || SAFE_MODE
				if(member == null)
				{
					Debug.LogError(ToString()+" Failed to create Drawer for member "+ methodOrPropertyInfo + " of type "+StringUtils.ToString(parameterType)+".\nparent="+StringUtils.ToString(parent));
					DrawerArrayPool.Resize(ref members, 0);
					return;
				}
				#endif

				//can't draw the member in a single row after all!
				if(!DrawerUtility.CanDrawInSingleRow(member))
				{
					drawInSingleRow = false;
					member.Dispose();
					DoBuildMembers();
					return;
				}

				members[0] = member;
			}
			else
			{
				for(int n = count - 1; n >= 0; n--)
				{
					var memberFieldInfo = memberBuildList[n];
					var parameterInfo = memberFieldInfo.ParameterInfo;
					var memberLabel = GUIContentPool.Empty();
					var type = GetParameterTypeAndLabel(memberFieldInfo, ref memberLabel);
					var member = DrawerProvider.GetForField(ParameterValues.GetValue(parameterInfo), type, memberFieldInfo, this, memberLabel, ReadOnly);

					#if DEV_MODE || SAFE_MODE
					if(member == null)
					{
						for(int d = count - 1; d > n; d--)
						{
							members[d].Dispose();
						}
						Debug.LogError(ToString()+" Failed to create Drawer for members["+n+"] "+ memberFieldInfo + " of type " + StringUtils.ToString(type) + ".\nparent=" + StringUtils.ToString(parent));
						DrawerArrayPool.Resize(ref members, 0);
						return;
					}
					#endif

					members[n] = member;
				}
			}
		}

		private static Type GetParameterTypeAndLabel([NotNull]LinkedMemberInfo linkedMemberInfo, ref GUIContent label)
		{
			GUIContentPool.Replace(ref label, linkedMemberInfo.DisplayName, linkedMemberInfo.Tooltip);
			if(label.tooltip.Length == 0)
			{
				label.tooltip = "Parameter";
			}

			var type = linkedMemberInfo.Type;
			if(type.IsByRef) //is ref or out
			{
				#if DEV_MODE
				Debug.Log("isByRef: "+ linkedMemberInfo + ", with IsOut="+ linkedMemberInfo.ParameterInfo.IsOut+", Type="+StringUtils.ToString(type)+", elementType="+StringUtils.ToString(type.GetElementType()));
				#endif

				type = type.GetElementType();

				if(linkedMemberInfo.ParameterInfo.IsOut)
				{
					label.text = string.Concat("out ", label);
				}
				else
				{
					label.text = string.Concat("ref ", label);
				}
			}

			return type;
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
			parametersValues[memberIndex] = memberValue;
			SetValue(parametersValues, false, false);
			return true;
		}
	}
}