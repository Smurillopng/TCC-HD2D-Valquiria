namespace Sisus
{
	public static class AddressablesUtility
	{
		public static readonly bool IsInstalled;
	
		static AddressablesUtility()
		{
			IsInstalled = TypeExtensions.GetType("UnityEngine.AddressableAssets.Addressables") != null;
		}
	}
}