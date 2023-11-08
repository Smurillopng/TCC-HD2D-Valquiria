using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class EnemyEditor : OdinMenuEditorWindow
{
    private CreateNewEnemy _createNewEnemy;

    [MenuItem("Tools/TCC/Enemy Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<EnemyEditor>();
        window.Show();
        window.titleContent.image = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_createNewEnemy != null)
            DestroyImmediate(_createNewEnemy.EnemyData);
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        _createNewEnemy = new CreateNewEnemy();
        tree.Add("Create New Enemy", new CreateNewEnemy());
        tree.AddAllAssetsAtPath("Enemies", "Assets/Resources/Scriptable Objects/Enemies", true);
        tree.MenuItems[0].AddIcon(SdfIconType.PencilSquare);
        tree.MenuItems[1].AddIcon(SdfIconType.Archive).ChildMenuItems.AddIcons(EditorIcons.CharGraph);
        return tree;
    }

    protected override void OnBeginDrawEditors()
    {
        var selected = MenuTree.Selection;
        SirenixEditorGUI.BeginHorizontalToolbar();
        {
            GUILayout.FlexibleSpace();
            if (SirenixEditorGUI.ToolbarButton("Delete Current Enemy"))
            {
                switch (selected.SelectedValue)
                {
                    case Unit deleteConsumable:
                        {
                            var path = AssetDatabase.GetAssetPath(deleteConsumable);
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.SaveAssets();
                            break;
                        }
                }
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }

    public class CreateNewEnemy
    {
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public Unit EnemyData;

        public CreateNewEnemy()
        {
            EnemyData = CreateInstance<Unit>();
            EnemyData.UnitName = "New Enemy";
        }

        [Button("Add New Enemy SO")]
        private void AddNewEnemy()
        {
            AssetDatabase.CreateAsset(EnemyData, $"Assets/Resources/Scriptable Objects/Enemies/{EnemyData.Filename}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EnemyData = CreateInstance<Unit>();
            EnemyData.UnitName = "New Enemy";
        }
    }
}
