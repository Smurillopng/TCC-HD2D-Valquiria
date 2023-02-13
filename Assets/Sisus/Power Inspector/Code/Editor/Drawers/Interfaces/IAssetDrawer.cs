namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawer representing UnityEngine.Objects other than Components.
	/// Examples include ScriptableObjects, GameObjects (because they can be prefabs) and all assets.
	/// 
	/// NOTE: Not implemented by ObjectReferenceDrawer, which only represents a *reference* to an UnityEngine.Object.
	/// </summary>
	public interface IAssetDrawer : IRootDrawer
	{

	}
}