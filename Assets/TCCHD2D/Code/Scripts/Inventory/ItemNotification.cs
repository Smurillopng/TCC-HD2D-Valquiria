// Created by Sérgio Murillo da Costa Faria
// Date: 11/04/2023

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class ItemNotification : MonoBehaviour
{
    public GameObject panelPrefab;
    public float displayTime = 2f;
    public float fadeTime = 1f;

    private readonly Queue<IItem> itemQueue = new();
    private bool isDisplaying;
    
    public Queue<IItem> ItemQueue => itemQueue;
    public bool IsDisplaying => isDisplaying;

    private void Update()
    {
        if (itemQueue.Count > 0 && !isDisplaying)
            StartCoroutine(nameof(DisplayItem));
    }

    public void AddConsumableWithNotification(Consumable item)
    {
        InventoryManager.Instance.AddConsumableItem(item);
        itemQueue.Enqueue(item);
    }
    
    public void AddEquipmentWithNotification(Equipment item)
    {
        InventoryManager.Instance.AddEquipmentItem(item);
        itemQueue.Enqueue(item);
    }
    
    public void AddItemWithNotification(IItem item)
    {
        InventoryManager.Instance.AddItem(item);
        itemQueue.Enqueue(item);
    }

    private void DisplayItem()
    {
        isDisplaying = true;
        var item = itemQueue.Dequeue();
        var panel = Instantiate(panelPrefab, transform);
        panel.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite = item.ItemIcon;
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
        
        isDisplaying = false;
        Destroy(panel, fadeTime);
    }
}
