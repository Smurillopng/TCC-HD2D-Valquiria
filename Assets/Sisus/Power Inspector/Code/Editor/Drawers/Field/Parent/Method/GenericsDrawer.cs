using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class GenericsDrawer : ParentFieldDrawer<Type[]>
	{
		private bool drawInSingleRow;

		private Type[] genericArguments;

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
					Debug.LogWarning(ToString()+".Value called. Building members.");
					#endif
					BuildMembers();
				}
				
				int count = members.Length;
				var value = Value;
				ArrayPool<Type>.Resize(ref value, count);
				for(int n = count - 1; n >= 0; n--)
				{
					value[n] = visibleMembers[n].GetValue() as Type;
				}

				#if DEV_MODE
				Debug.Log(GetType().Name+" {"+StringUtils.ToString((object)genericArguments)+"} Value.get() result: "+StringUtils.ToString(value));
				#endif

				return value;
			}
		}
		
		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="methodInfo"> LinkedMemberInfo of the method whose generic parameters the drawer represent. Not null. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="label"> The prefix label. </param>
		/// <param name="setReadOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static GenericsDrawer Create([NotNull]LinkedMemberInfo methodInfo, [NotNull]MethodDrawer parent, GUIContent label, bool setReadOnly)
		{
			GenericsDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result =  new GenericsDrawer();
			}
			result.Setup(ArrayPool<Type>.ZeroSizeArray, typeof(Type[]), methodInfo, parent, label, setReadOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(ArrayPool<Type>.ZeroSizeArray, typeof(Type[]), setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc />
		protected override void Setup(Type[] setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			memberInfo = setMemberInfo;
			genericArguments = setMemberInfo.MethodInfo.GetGenericArguments();
			
			drawInSingleRow = genericArguments.Length == 1 && DrawerUtility.CanDrawMultipleControlsOfTypeInSingleRow(genericArguments[0]);

			if(setLabel == null)
			{
				setLabel = GUIContentPool.Create("Generics");
			}

			base.Setup(setValue, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override bool DoSetValue(Type[] setValue, bool applyToField, bool updateMembers)
		{
			if(Value.ContentsMatch(setValue))
			{
				return false;
			}

			SetCachedValueSilent(setValue);

			if(updateMembers)
			{
				if(memberBuildState != MemberBuildState.MembersBuilt)
				{
					BuildMembers();
				}
			
				int count = Mathf.Min(members.Length, setValue.Length);
				for(int n = count - 1; n >= 0; n--)
				{
					visibleMembers[n].SetValue(setValue[n]);
				}
			}

			return true;
		}

		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			int count = genericArguments.Length;
			drawInSingleRow = count == 1;
			
			DrawerArrayPool.Resize(ref members, count);
			
			if(count == 1)
			{
				var genericArgument = genericArguments[0];
				label.text = GetLabel(genericArgument);
				label.tooltip = "Generic Argument";
				members[0] = TypeDrawer.Create(GetDefaultValue(genericArgument), null, this, GUIContentPool.Empty(), false);
			}
			else
			{
				for(int n = count - 1; n >= 0; n--)
				{
					var genericArgument = genericArguments[n];
					string labelText = GetLabel(genericArgument);
					string tooltip = string.Concat("Generic Argument ", StringUtils.ToString(n+1), "/", StringUtils.ToString(count));
					members[n] = TypeDrawer.Create(GetDefaultValue(genericArgument), null, this, GUIContentPool.Create(labelText, tooltip), false);
				}
			}

			#if DEV_MODE
			try
			{
				Debug.Log(ToString() + ".RebuildIntructionsInChildren() - now has "+members.Length + " members:\n" + StringUtils.ToString(members));
			}
			catch(Exception e) //had a bug where even trying to access "this" resulted in a null reference exception
			{
				Debug.LogError("MethodDrawer.RebuildIntructionsInChildren() "+e);
				return;
			}

			for(int n = 0; n < members.Length; n++)
			{
				Debug.Log(GetType().Name+" Created #"+n+" "+members[n].GetType().Name);
			}
			#endif
		}

		private static string GetLabel(Type genericArgument)
		{
			string label = genericArgument.Name;
			int length = label.Length;
			switch(length)
			{
				case 0:
					return "Type";
				case 1:
					return string.Concat("Type ", label); //T => "Type T", K => "Type K" etc.
				case 2:
					if(label[0] == 'T' && char.ToUpper(label[1]) == label[1])
					{
						return string.Concat("Type", label[1]); //T1 => "Type 1", T2 => "Type 2" etc.
					}
					return string.Concat(StringUtils.SplitPascalCaseToWords(label), " Type"); //Mk => "Mk Type", FX => "FX Type"
				default:
					if(label[0] == 'T')
					{
						if(label.Equals("type", StringComparison.InvariantCultureIgnoreCase))
						{
							return "Type"; //Type => "Type"
						}

						if(char.ToUpper(label[1]) == label[1] && char.ToUpper(label[2]) != label[2]) //<TPS>, <TP1> <TYPO>
						{
							return string.Concat(StringUtils.SplitPascalCaseToWords(label.Substring(1)), " Type"); //TValue => "Value Type", TEventArgs => "Event Args Type" etc.
						}
					}
					return string.Concat(StringUtils.SplitPascalCaseToWords(label), " Type"); //Tool => "Tool Type", TYPO => "TYPO Type",
			}
		}

		private static Type GetDefaultValue(Type genericArgument)
		{
			var constraints = genericArgument.GetGenericParameterConstraints();
			if(constraints.Length > 0)
			{
				Debug.Log("GenericsDrawer.GetDefaultValue("+genericArgument.Name+"): "+constraints[0]);
				return constraints[0];
			}
			return typeof(void);
		}

		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}
	}
}