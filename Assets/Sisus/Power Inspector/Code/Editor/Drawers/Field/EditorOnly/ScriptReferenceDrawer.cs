#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using JetBrains.Annotations;

namespace Sisus
{
	[Serializable]
	public class ScriptReferenceDrawer : ParentFieldDrawer<MonoScript>
	{
		/// <inheritdoc/>
		public override bool DrawInSingleRow
		{
			get
			{
				return true;
			}
		}

		private IDrawer ObjectFieldDrawer
		{
			get
			{
				return members[0];
			}
		}

		/// <inheritdoc/>
		public override Type Type
		{
			get
			{
				return typeof(MonoScript);
			}
		}

		/// <inheritdoc/>
		public override OnValueChanged OnValueChanged
		{
			get
			{
				var objectField = ObjectFieldDrawer;
				return objectField == null ? null : objectField.OnValueChanged;
			}

			set
			{
				var objectField = ObjectFieldDrawer;
				if(objectField != null)
				{
					objectField.OnValueChanged = value;
				}
				else
				{
					if(value != null)
					{
						throw new NullReferenceException();
					}
				}
			}
		}

		/// <inheritdoc/>
		public override bool ReadOnly
		{
			get
			{
				return Value != null || base.ReadOnly;
			}
		}


		/// <inheritdoc/>
		protected override bool RebuildDrawersIfValueChanged
		{
			get
			{
				return false;
			}
		}

		/// <summary> Creates a new instance of the drawer or returns a reusable instance from the pool. </summary>
		/// <param name="monoScript"> The MonoScript which the drawer points to. </param>
		/// <param name="parent"> The parent drawer of the created drawer. Can be null. </param>
		/// <param name="readOnly"> True if control should be read only. </param>
		/// <returns> The instance, ready to be used. </returns>
		public static ScriptReferenceDrawer Create([CanBeNull]MonoScript monoScript, [CanBeNull]IParentDrawer parent, bool readOnly)
		{
			ScriptReferenceDrawer result;
			if(!DrawerPool.TryGet(out result))
			{
				result = new ScriptReferenceDrawer();
			}
			result.Setup(monoScript, typeof(MonoScript), null, parent, null, readOnly);
			result.LateSetup();
			return result;
		}

		/// <inheritdoc />
		public override void SetupInterface(object setValue, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			Setup(setValue as MonoScript, setValueType, setMemberInfo, setParent, setLabel, setReadOnly);
		}

		/// <inheritdoc/>
		protected override void Setup(MonoScript monoScript, Type setValueType, LinkedMemberInfo setMemberInfo, IParentDrawer setParent, GUIContent setLabel, bool setReadOnly)
		{
			if(monoScript != null)
			{
				setReadOnly = true;
			}
			base.Setup(monoScript, setValueType, setMemberInfo, setParent, setLabel == null ? GUIContentPool.Create("Script") : setLabel, setReadOnly);
		}

		private ScriptReferenceDrawer() {}
		
		/// <inheritdoc />
		protected override void DoGenerateMemberBuildList() { }

		/// <inheritdoc />
		protected override void DoBuildMembers()
		{
			DrawerArrayPool.Resize(ref members, 1);
			members[0] = ObjectReferenceDrawer.Create(Value, Types.MonoScript, this, GUIContent.none, false, false, ReadOnly);
		}

		/// <inheritdoc />
		public override bool OnKeyboardInputGiven(Event inputEvent, KeyConfigs keys)
		{
			if(keys.activate.DetectAndUseInput(inputEvent))
			{
				EditorGUIUtility.PingObject(Value);
				return true;
			}
			return base.OnKeyboardInputGiven(inputEvent, keys);
		}

		/// <inheritdoc />
		protected override void BuildContextMenu(ref Menu menu, bool extendedMenu)
		{
			if(BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
			
			if(Value != null)
			{
				menu.AddSeparatorIfNotRedundant();

				menu.Add("Edit Script", ()=>AssetDatabase.OpenAsset(Value));
				menu.Add("Show In Assets", ()=>Inspector.Select(Value));
			}

			if(!BuildContextMenuItemsStartingFromBaseClass)
			{
				base.BuildContextMenu(ref menu, extendedMenu);
			}
		}

		/// <inheritdoc/>
		public override void UpdateCachedValuesFromFieldsRecursively() { }

		/// <inheritdoc/>
		protected override void OnCachedValueChanged(bool applyToField, bool updateMembers)
		{
			if(updateMembers && members.Length > 0 && members[0] != null)
			{
				members[0].SetValue(Value);
			}
			base.OnCachedValueChanged(applyToField, updateMembers);
		}

		/// <inheritdoc/>
		protected override bool GetHasUnappliedChangesUpdated()
		{
			return false;
		}
	}
}
#endif