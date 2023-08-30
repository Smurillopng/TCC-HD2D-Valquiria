using CI.QuickSave;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SavePoint : MonoBehaviour
{
    [SerializeField]
    private Unit playerUnit; // The Unit component of the player
    private PlayerMovement _playerMovement; // The PlayerMovement component of the player

    public void Awake()
    {
        if (_playerMovement == null) _playerMovement = FindObjectOfType<PlayerMovement>();
    }

    public void SaveGame()
    {
        var save = QuickSaveWriter.Create("GameSave");
        
        var inventoryInfoReader = QuickSaveReader.Create("InventoryInfo");
        var inventoryInfoKeys = inventoryInfoReader.GetAllKeys();
        foreach (var key in inventoryInfoKeys)
            save.Write(key, inventoryInfoReader.Read<string>(key));
        var itemInfoReader = QuickSaveReader.Create("ItemInfo");
        var itemInfoKeys = itemInfoReader.GetAllKeys();
        foreach (var key in itemInfoKeys)
            save.Write(key, itemInfoReader.Read<bool>(key));
        
        save.Write("Level", playerUnit.Level);
        save.Write("Experience", playerUnit.Experience);
        save.Write("PlayerAttack", playerUnit.Attack);
        save.Write("PlayerDefence", playerUnit.Defence);
        save.Write("PlayerSpeed", playerUnit.Speed);
        save.Write("PlayerLuck", playerUnit.Luck);
        save.Write("PlayerDexterity", playerUnit.Dexterity);
        save.Write("AttributesPoints", playerUnit.AttributesPoints);
        save.Write("PlayerPosition", _playerMovement.transform.position);
        save.Write("CurrentScene", SceneManager.GetActiveScene().name);
        
        save.Commit();
    }
}