using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class ItemEditor : OdinMenuEditorWindow
{
    private CreateNewItemConsumable _createNewItemConsumable;
    private CreateNewItemEquipment _createNewItemEquipment;

    [MenuItem("Tools/TCC/Item Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<ItemEditor>();
        window.Show();
        window.titleContent.image = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_createNewItemConsumable != null)
            DestroyImmediate(_createNewItemConsumable.ItemData);
        if (_createNewItemEquipment != null)
            DestroyImmediate(_createNewItemEquipment.ItemData);
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        _createNewItemConsumable = new CreateNewItemConsumable();
        _createNewItemEquipment = new CreateNewItemEquipment();
        tree.Add("Create New Consumable", new CreateNewItemConsumable());
        tree.Add("Create New Equipment", new CreateNewItemEquipment());
        tree.AddAllAssetsAtPath("Consumables", "Assets/Resources/Scriptable Objects/Items", typeof(Consumable));
        tree.AddAllAssetsAtPath("Equipments", "Assets/Resources/Scriptable Objects/Items", typeof(Equipment));
        tree.MenuItems[0].AddIcon(SdfIconType.PencilSquare);
        tree.MenuItems[1].AddIcon(SdfIconType.PencilSquare);
        tree.MenuItems[2].AddIcon(SdfIconType.Archive).ChildMenuItems.AddIcons(EditorIcons.CharGraph);
        tree.MenuItems[3].AddIcon(SdfIconType.Archive).ChildMenuItems.AddIcons(EditorIcons.CharGraph);
        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        var selected = MenuTree.Selection;
        SirenixEditorGUI.BeginHorizontalToolbar();
        {
            GUILayout.FlexibleSpace();
            if (SirenixEditorGUI.ToolbarButton("Delete Current Item"))
            {
                switch (selected.SelectedValue)
                {
                    case Consumable deleteConsumable:
                        {
                            var path = AssetDatabase.GetAssetPath(deleteConsumable);
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.SaveAssets();
                            break;
                        }
                    case Equipment deleteEquipment:
                        {
                            var path = AssetDatabase.GetAssetPath(deleteEquipment);
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.SaveAssets();
                            break;
                        }
                }
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }

    public class CreateNewItemConsumable
    {
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public Consumable ItemData;

        public CreateNewItemConsumable()
        {
            ItemData = CreateInstance<Consumable>();
            ItemData.ItemName = "New Consumable";
        }

        [Button("Add New Consumable SO")]
        private void AddNewConsumable()
        {
            AssetDatabase.CreateAsset(ItemData, $"Assets/Resources/Scriptable Objects/Items/{ItemData.Filename}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ItemData = CreateInstance<Consumable>();
            ItemData.ItemName = "New Consumable";
        }
    }

    public class CreateNewItemEquipment
    {
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public Equipment ItemData;

        public CreateNewItemEquipment()
        {
            ItemData = CreateInstance<Equipment>();
            ItemData.ItemName = "New Equipment";
        }

        [Button("Add New Equipment SO")]
        private void AddNewEquipment()
        {
            AssetDatabase.CreateAsset(ItemData, $"Assets/Resources/Scriptable Objects/Items/{ItemData.Filename}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}