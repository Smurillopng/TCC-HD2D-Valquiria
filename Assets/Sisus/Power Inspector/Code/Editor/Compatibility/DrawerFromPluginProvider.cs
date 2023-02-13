#define SAFE_MODE

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Sisus.Compatibility
{
	/// <summary>
	/// A class that can be extended to provide drawer providing logic related to third party plugins.
	/// 
	/// They should provide the ability to detect whether or not the plugin in question is currently "active", meaning the drawers related to it should currently be used.
	/// </summary>
	public abstract class DrawerFromPluginProvider
	{
		private static DrawerFromPluginProvider[] all;

		/// <summary> Gets instances of all DrawerFromPluginProvider types that exist in the project . </summary>
		/// <value> all. </value>
		public static DrawerFromPluginProvider[] All
		{
			get
			{
				if(all == null)
				{
					var allTypes = TypeExtensions.GetExtendingTypes<DrawerFromPluginProvider>(true).ToList();
					int count = allTypes.Count;
					all = new DrawerFromPluginProvider[count];
					for(int n = count - 1; n >= 0; n--)
					{
						#if SAFE_MODE
						try
						{
							all[n] = (DrawerFromPluginProvider)allTypes[n].CreateInstance();
						}
						catch(Exception e)
						{
							UnityEngine.Debug.LogError(e);
							all = all.RemoveAt(n);
						}
						#else
						all[n] = (DrawerFromPluginProvider)allTypes[n].CreateInstance();
						#endif
					}
					Array.Sort(all, new DrawerFromPluginProviderComparer());
				}
				return all;
			}
		}

		/// <summary> Priority of this provider compared against other providers. Providers with higher numbers are prioritized before ones with lower numbers. </summary>
		/// <value> The priority number of this provider. </value>
		protected virtual int Priority
		{
			get
			{
				return 0;
			}
		}

		/// <summary> Is the plugin currently installed and active? </summary>
		/// <value> True if plugin is installed and active, false if not. </value>
		public abstract bool IsActive { get; }

		/// <summary> Adds IFieldGUIInstruction types related to the plugin to the Dictionary. </summary>
		/// <param name="fieldDrawer"> Dictionary of field drawers where key is exact field type and value is Drawer type. </param>
		public virtual void AddFieldDrawer([NotNull]Dictionary<Type, Type> fieldDrawer){ }

		/// <summary> Adds IDecoratorDrawerGUIInstruction types related to the plugin to the Dictionary. </summary>
		/// <param name="decoratorDrawerDrawer"> Dictionary of DecoratorDrawer drawers where key is exact field type and value is Drawer type. </param>
		public virtual void AddDecoratorDrawerDrawer([NotNull]Dictionary<Type, Type> decoratorDrawerDrawer){ }

		/// <summary> Adds IPropertyDrawerGUIInstruction types related to the plugin to the Dictionary. </summary>
		/// <param name="propertyDrawerDrawerByAttributeType"> Dictionary of PropertyDrawer drawers where key is exact attribute type and value is Drawer type. </param>
		///  <param name="propertyDrawerDrawerByFieldType"> Dictionary of PropertyDrawer drawers where key is exact field type and value is Drawer type. </param>
		public virtual void AddPropertyDrawerDrawer([NotNull]Dictionary<Type, Dictionary<Type, Type>> propertyDrawerDrawerByAttributeType, [NotNull]Dictionary<Type, Type> propertyDrawerDrawerByFieldType){ }

		/// <summary> Adds IComponentGUIInstruction types related to the plugin to the Dictionary. </summary>
		/// <param name="componentDrawer"> [in,out] Dictionary of component drawers where key is exact component type and value is Drawer type. </param>
		public virtual void AddComponentDrawer([NotNull]Dictionary<Type, Type> componentDrawer){ }
		
		/// <summary> Adds IAssetGUIInstruction types related to the plugin to the Dictionary. </summary>
		/// <param name="assetDrawerByType"> Dictionary of asset drawers where key is exact asset type and value is Drawer type. </param>
		///  <param name="assetDrawerByExtension"> Dictionary of asset drawers where key is file extension (including leading dot) and value is Drawer type. </param>
		public virtual void AddAssetDrawer([NotNull]Dictionary<Type, Type> assetDrawerByType, [NotNull]Dictionary<string, Type> assetDrawerByExtension){ }

		public class DrawerFromPluginProviderComparer : IComparer<DrawerFromPluginProvider>
		{
			public int Compare(DrawerFromPluginProvider x, DrawerFromPluginProvider y)
			{
				return x.Priority.CompareTo(y.Priority);
			}
		}
	}
}