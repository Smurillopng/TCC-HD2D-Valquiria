using JetBrains.Annotations;
using System;

namespace Sisus
{
	[Serializable]
	public class ComponentCategory
	{
		public string name;

		[NotNull]
		public ComponentType[] components;

		public ComponentCategory()
		{
			name = "";

			components = new ComponentType[0];
		}

		public ComponentCategory(string categoryName, [NotNull]params Type[] componentTypes)
		{
			name = categoryName;

			int count = componentTypes.Length;
			components = new ComponentType[count];

			for(int n = 0; n < count; n++)
			{
				components[n] = new ComponentType(componentTypes[n]);
			}
		}
	}
}
