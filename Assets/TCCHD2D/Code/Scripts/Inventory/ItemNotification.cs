// Created by Sérgio Murillo da Costa Faria

using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[HideMonoScript]
public class ItemNotification : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Item Notification")]
    [BoxGroup("Item Notification/Settings")]
    public GameObject panelPrefab;
    
    [BoxGroup("Item Notification/Settings")]
    public float displayTime = 2f;
    
    [BoxGroup("Item Notification/Settings")]
    public float fadeTime = 1f;

    public Queue<IItem> ItemQueue { get; } = new();
    public bool IsDisplaying { get; private set; }

    #endregion ==========================================================================
    
    #region === Unity Methods ===========================================================
    private void Update()
    {
        if (ItemQueue.Count > 0 && !IsDisplaying)
            StartCoroutine(nameof(DisplayItem));
    }
    #endregion ==========================================================================
    
    #region === Methods =================================================================
    public void AddConsumableWithNotification(Consumable item)
    {
        InventoryManager.Instance.AddConsumableItem(item);
        ItemQueue.Enqueue(item);
    }
    
    public void AddEquipmentWithNotification(Equipment item)
    {
        InventoryManager.Instance.AddEquipmentItem(item);
        ItemQueue.Enqueue(item);
    }
    
    public void AddItemWithNotification(IItem item)
    {
        InventoryManager.Instance.AddItem(item);
        ItemQueue.Enqueue(item);
    }
    
    public void EquipNotification(Equipment item)
    {
        InventoryManager.Instance.Equip(item);
    }

    private void DisplayItem()
    {
        IsDisplaying = true;
        var item = ItemQueue.Dequeue();
        var panel = Instantiate(panelPrefab, transform);
        var panelIcon = panel.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>();
        panelIcon.sprite = item.ItemIcon;
        panelIcon.preserveAspect = true;
        panel.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = item.ItemName;

        var animator = panel.GetComponent<Animator>();
        animator.Play("SlideIn");

        StartCoroutine(FadeOut(panel));
    }

    private IEnumerator FadeOut(GameObject panel)
    {
        yield return new WaitForSeconds(displayTime);

        var animator = panel.GetComponent<Animator>();
        animator.Play("FadeOut");
        
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.1);
        
        IsDisplaying = false;
        Destroy(panel, fadeTime);
    }
    #endregion ==========================================================================
}
