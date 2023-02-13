namespace Sisus
{
	/// <summary>
	/// Interface implemented by drawers representing UnityEngine.Objects other than GameObjects that
	/// (can) utilize an Editor for drawing their member controls, instead of using Drawer.
	/// Can represent Components, ScriptableObjects and assets.
	/// </summary>
	public interface ICustomEditorDrawer : IUnityObjectDrawer
	{
		void SetIsFirstInspectedEditor(bool value);
	}
}