using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sisus
{
	[Serializable]
	public class AddComponentMenuConfig
	{
		[SerializeField, HideInInspector]
		public AddComponentMenuGroupConfig[] groups = new AddComponentMenuGroupConfig[0];
		public AddComponentMenuItemConfig[] items = new AddComponentMenuItemConfig[0];
		
		public void SetAllValues(AddComponentMenuItem[] set)
		{
			int count = set.Length;
			
			var groupsDictionary = new Dictionary<string, Texture>(24);
			var itemsList = new List<AddComponentMenuItemConfig>(200);

			for(int n = 0; n < count; n++)
			{
				var item = set[n];
				SetValues(item, ref groupsDictionary, ref itemsList);
			}

			count = groupsDictionary.Count;
			var groupsList = new List<AddComponentMenuGroupConfig>(count);
			foreach(var group in groupsDictionary)
			{
				groupsList.Add(new AddComponentMenuGroupConfig(group.Key, group.Value));
			}

			groups = groupsList.ToArray();
			items = itemsList.ToArray();
		}

		private void SetValues(AddComponentMenuItem set, ref Dictionary<string, Texture> groupsDictionary, ref List<AddComponentMenuItemConfig> itemsList)
		{
			if(set.IsGroup)
			{
				groupsDictionary[set.FullLabel()] = set.Preview;
				for(int n = 0, count = set.children.Length; n < count; n++)
				{
					SetValues(set.children[n], ref groupsDictionary, ref itemsList);
				}
			}
			else
			{
				itemsList.Add(new AddComponentMenuItemConfig(set.FullLabel(), set.type));
			}
		}
	}
}