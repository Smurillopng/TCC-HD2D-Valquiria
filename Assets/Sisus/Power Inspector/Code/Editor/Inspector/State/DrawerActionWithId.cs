using System;

namespace Sisus
{
	public class DrawerActionWithId
	{
		public int id;
		public Action<IDrawer> action;

		public DrawerActionWithId(int index, Action<IDrawer> action)
		{
			this.id = index;
			this.action = action;
		}
	}
}