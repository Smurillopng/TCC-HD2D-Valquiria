//#define DEBUG_ADD_COMPONENT

using System;
using System.Collections.Generic;
using UnityEngine;
using Sisus.Attributes;
using JetBrains.Annotations;
using Object = UnityEngine.Object;

namespace Sisus
{
	[Serializable, DrawerForGameObject(true, true)] 
	public class CategorizedGameObjectDrawer : GameObjectDrawer
	{
		private static readonly Stack<List<Component[]>> componentsListPool = new Stack<List<Component[]>>();
		protected readonly Dictionary<string, List<Component[]>> categorizedBuildList = new Dictionary<string, List<Component[]>>();
		
		/// <inheritdoc/>
		public override bool MemberIsReorderable(IReorderable member)
		{
			return false;
		}

		/// <inheritdoc/>
		protected override void DoGenerateMemberBuildList()
		{
			base.DoGenerateMemberBuildList();

			ClearCategorizedBuildList();

			for(int n = 0, count = memberBuildList.Count; n < count; n++)
			{
				var memberComponents = memberBuildList[n];
				var firstComponent = memberComponents[0];
				string category;
				if(firstComponent == null)
				{
					category = "Missing Scripts";
				}
				else
				{
					category = ComponentCategories.Get(firstComponent);
				}

				if(!categorizedBuildList.TryGetValue(category, out var componentsUnderCategory))
				{
					componentsUnderCategory = GetEmptyListOfComponentArrays();
					categorizedBuildList.Add(category, componentsUnderCategory);
				}

				componentsUnderCategory.Add(memberComponents);
			}
		}
		
		/// <inheritdoc/>
		protected override void DoBuildMembers()
		{
			#if DEV_MODE
			Debug.Log(ToString()+ ".DoBuildMembers");
			#endif

			int categoryCount = categorizedBuildList.Count;
			int newMemberCount = categoryCount;
			
			bool includeAddComponentButton = ShouldIncludeAddComponentButton();
			if(includeAddComponentButton)
			{
				newMemberCount++;
			}

			if(componentsOnlyOnSomeObjectsFound)
			{
				newMemberCount++;
			}

			DrawerArrayPool.Resize(ref members, newMemberCount);

			int index = 0;
			foreach(var categoryAndComponents in categorizedBuildList)
			{
				var category = categoryAndComponents.Key;
				var categoryComponents = categoryAndComponents.Value;
				int categoryMemberCount = categoryComponents.Count;

				if(category.Length == 0)
				{
					int increaseMemberCount = categoryMemberCount - 1;
					if(increaseMemberCount > 0)
					{
						newMemberCount += increaseMemberCount;
						DrawerArrayPool.Resize(ref members, newMemberCount);
					}

					for(int n = 0; n < categoryMemberCount; n++)
					{
						var memberComponents = categoryComponents[n];
						var componentDrawer = DrawerProvider.GetForComponents(inspector, memberComponents, this);
						members[index] = componentDrawer;
						index++;
					}
				}
				else
				{
					var categoryDrawer = CategorizedComponentsDrawer.Create(this, GUIContentPool.Create(category));
					var setMembers = DrawerArrayPool.Create(categoryMemberCount);

					for(int n = 0; n < categoryMemberCount; n++)
					{
						var memberComponents = categoryComponents[n];
						var componentDrawer = DrawerProvider.GetForComponents(inspector, memberComponents, categoryDrawer);
						setMembers[n] = componentDrawer;
					}

					categoryDrawer.SetMembers(setMembers, true);
					members[index] = categoryDrawer;
					index++;
				}
			}

			if(componentsOnlyOnSomeObjectsFound)
			{
				members[index] = GameObjectBoxDrawer.Create(this, GUIContentPool.Create("Components found only on some selected objects can't be multi-edited."));
				index++;
			}

			if(includeAddComponentButton)
			{
				members[index] = AddComponentButtonDrawer.Create(this, inspector);
			}
		}

		public override void RebuildMaterialDrawers()
		{
			// todo: implement this
		}

		/// <inheritdoc/>
		public override void AddComponentMember(int memberIndex, [NotNull]IComponentDrawer componentDrawer)
		{
			var category = ComponentCategories.Get(componentDrawer.Component);

			#if DEV_MODE
			Debug.Log("AddComponentMember("+memberIndex+", "+ componentDrawer.GetType().Name+") category=\""+category+ "\", members.Length="+ members.Length);
			#endif

			var categoryDrawer = GetOrCreateCategoryDrawer(category);

			if(categoryDrawer == null)
			{
				var setMembers = members;

				if(memberIndex == -1)
				{
					memberIndex = setMembers.Length - LastCollectionMemberCountOffset + 1;
				}

				DrawerArrayPool.InsertAt(ref setMembers, memberIndex, componentDrawer, false);
				SetMembers(setMembers);
				return;
			}

			var setCategoryMembers = categoryDrawer.Members;
			DrawerArrayPool.InsertAt(ref setCategoryMembers, setCategoryMembers.Length, componentDrawer, false);
			categoryDrawer.SetMembers(setCategoryMembers);
		}

		[CanBeNull]
		protected CategorizedComponentsDrawer GetOrCreateCategoryDrawer(Component component)
		{
			var category = ComponentCategories.Get(component);
			return GetOrCreateCategoryDrawer(category);
		}

		[CanBeNull]
		protected CategorizedComponentsDrawer GetOrCreateCategoryDrawer(string category)
		{
			if(category.Length == 0)
			{
				#if DEV_MODE && DEBUG_ADD_COMPONENT
				Debug.Log(ToString()+ ".GetOrCreateCategoryDrawer(" + StringUtils.ToString(category)+"): returning null");
				#endif
				return null;
			}

			// try to find existing CategorizedComponentsDrawer
			for(int n = members.Length - LastCollectionMemberCountOffset; n >= 0; n--)
			{
				var member = members[n];
				if(string.Equals(member.Name, category, StringComparison.OrdinalIgnoreCase))
				{
					var existingCategoryDrawer = member as CategorizedComponentsDrawer;
					if(existingCategoryDrawer != null)
					{
						#if DEV_MODE && DEBUG_ADD_COMPONENT
						Debug.Log(ToString()+ ".GetOrCreateCategoryDrawer(" + StringUtils.ToString(category)+"): existing found @ members["+n+"]");
						#endif
						return existingCategoryDrawer;
					}
				}
			}

			// create new CategorizedComponentsDrawer
			
			#if DEV_MODE && PI_ASSERTATIONS
			int assertCount = members.Length + 1;
			#endif

			var newCategoryDrawer = CategorizedComponentsDrawer.Create(this, GUIContentPool.Create(category));

			var setMembers = members;

			// insert new category drawer at the end, but before the add component button
			int insertAt = setMembers.Length - LastCollectionMemberCountOffset + 1;
			
			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(insertAt <= setMembers.Length);
			Debug.Assert(insertAt >= 0);
			#endif

			DrawerArrayPool.InsertAt(ref setMembers, insertAt, newCategoryDrawer, false);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(!setMembers.ContainsNullMembers());
			Debug.Assert(!members.ContainsNullMembers());
			Debug.Assert(!visibleMembers.ContainsNullMembers());
			Debug.Assert(Array.IndexOf(setMembers, newCategoryDrawer) == insertAt);
			#endif

			SetMembers(setMembers);

			#if DEV_MODE && PI_ASSERTATIONS
			Debug.Assert(members.Length == assertCount);
			Debug.Assert(setMembers.Length == assertCount);
			Debug.Assert(Array.IndexOf(members, newCategoryDrawer) == insertAt);
			#endif
			
			#if DEV_MODE && DEBUG_ADD_COMPONENT
			Debug.Log(ToString()+ ".GetOrCreateCategoryDrawer(" + StringUtils.ToString(category)+"): created new and added @ members["+ insertAt + "]. members now:\n"+StringUtils.TypesToString(members, "\n"));
			#endif

			return newCategoryDrawer;
		}

		/// <inheritdoc/>
		public override IEnumerator<IComponentDrawer> ForEachComponent()
		{
			int lastIndex = members.Length - LastCollectionMemberCountOffset;
			for(int n = 0; n <= lastIndex; n++)
			{
				var member = members[n];

				var categoryDrawer = member as CategorizedComponentsDrawer;
				if(categoryDrawer != null)
				{
					var categoryMembers = categoryDrawer.Members;
					int categoryMembersLastIndex = categoryMembers.Length - 1;
					for(int c = 0; c <= categoryMembersLastIndex; c++)
					{
						var categoryMemberComponent = categoryMembers[c] as IComponentDrawer;
						if(categoryMemberComponent != null)
						{
							yield return categoryMemberComponent;
						}
						#if DEV_MODE
						else { Debug.LogError(categoryDrawer.ToString()+ " members["+n+ "] was not IComponentDrawer: "+ categoryMembers[n] == null ? "null" : categoryMembers[n].ToString()); }
						#endif
					}
				}
				else
				{
					var componentDrawer = member as IComponentDrawer;
					if(componentDrawer != null)
					{
						yield return componentDrawer;
					}
					#if DEV_MODE
					else { Debug.LogError(ToString()+ ".ForEachComponent members["+n+ "] was not CategorizedComponentsDrawer nor IComponentDrawer: "+ members[n] == null ? "null" : members[n].ToString()); }
					#endif
				}
			}
		}

		/// <inheritdoc/>
		protected override IComponentDrawer CreateDrawerForComponents(Component[] components)
		{
			#if DEV_MODE && DEBUG_ADD_COMPONENT
			Debug.Log(ToString()+ ".CreateDrawerForComponents(" + StringUtils.TypesToString(components)+")");
			#endif

			var categoryDrawer = GetOrCreateCategoryDrawer(components[0]);
			var setParent = categoryDrawer != null ? categoryDrawer as IParentDrawer : this;
			return DrawerProvider.GetForComponents(inspector, components, setParent);
		}

		#if DEV_MODE
		/// <inheritdoc/>
		public override void ValidateMembers()
		{
			int count = members.Length;
			for(int a = 0; a < count; a++)
			{
				var testA = members[a];

				Debug.Assert(!testA.Inactive, ToString() + " Member #" + a + " was inactive: " + members[a]);
			}
		}
		#endif

		/// <inheritdoc/>
		public override bool SubjectIsReorderable(Object member)
		{
			// don't allow drag-n-drop reordering of components when using categorized components mode
			// do still allow drag n dropping script assets

			#if  UNITY_EDITOR
			// MonoScripts are editor-only.
			var type = member.GetType();

			//dragging MonoScripts on GameObject inspector can be used for adding components
			if(type == Types.MonoScript)
			{
				return true;
			}
			#endif
			
			return false;
		}

		/// <inheritdoc/>
		protected override void RebuildMemberBuildList()
		{
			ClearCategorizedBuildList();
			base.RebuildMemberBuildList();
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			ClearCategorizedBuildList();
			base.Dispose();
		}

		private List<Component[]> GetEmptyListOfComponentArrays()
		{
			return componentsListPool.Count > 0 ? componentsListPool.Pop() : new List<Component[]>();
		}

		protected void ClearCategorizedBuildList()
		{
			foreach(var componentsList in categorizedBuildList.Values)
			{
				componentsList.Clear();
				componentsListPool.Push(componentsList);
			}

			categorizedBuildList.Clear();
		}
	}	
}