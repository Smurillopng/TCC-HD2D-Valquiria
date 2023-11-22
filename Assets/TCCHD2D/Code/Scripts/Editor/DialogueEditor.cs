using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class DialogueEditor : OdinMenuEditorWindow
{
    private CreateNewDialogue _createNewDialogue;

    [MenuItem("Tools/TCC/Dialogue Editor")]
    private static void OpenWindow()
    {
        var window = GetWindow<DialogueEditor>();
        window.Show();
        window.titleContent.image = EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_createNewDialogue != null)
            DestroyImmediate(_createNewDialogue.DialogueData);
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        _createNewDialogue = new CreateNewDialogue();
        tree.Add("Create New Dialogue", new CreateNewDialogue());
        tree.AddAllAssetsAtPath("Dialogues", "Assets/Resources/Scriptable Objects/Dialogue", typeof(DialogueData), true, true);
        tree.SortMenuItemsByName();
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
            if (SirenixEditorGUI.ToolbarButton("Delete Current Dialogue"))
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

    public class CreateNewDialogue
    {
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
        public DialogueData DialogueData;

        public CreateNewDialogue()
        {
            DialogueData = CreateInstance<DialogueData>();
            DialogueData.ID = "New Dialogue";
        }

        [Button("Add New Dialogue SO")]
        private void AddNewEnemy()
        {
            AssetDatabase.CreateAsset(DialogueData, $"Assets/Resources/Scriptable Objects/Dialogue/{DialogueData.ID}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DialogueData = CreateInstance<DialogueData>();
            DialogueData.ID = "New Enemy";
        }
    }
}
