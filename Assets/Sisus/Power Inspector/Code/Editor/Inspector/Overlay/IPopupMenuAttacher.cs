namespace Sisus
{
	public interface IPopupMenuAttacher
	{
		PopupMenuManager.OpenRequest OnRequestingOpen
		{
			get;
		}

		PopupMenuManager.SelectItemRequest OnRequestingSelectItem
		{
			get;
		}
	}
}