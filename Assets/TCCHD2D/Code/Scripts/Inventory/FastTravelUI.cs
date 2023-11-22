// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[HideMonoScript]
public class FastTravelUI : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Fast Travel UI")]
    [BoxGroup("Fast Travel UI/Settings")]
    [SerializeField] private GameObject fastTravelPanel;

    [BoxGroup("Fast Travel UI/Settings")]
    [SerializeField] private GameObject buttonPrefab;

    [BoxGroup("Fast Travel UI/Settings")]
    [SerializeField] private Transform buttonParent;

    private FastTravelManager _fastTravelManager;
    private bool _isPanelActive, _buttonsSet;
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void TogglePanel(bool state)
    {
        fastTravelPanel.SetActive(state);
        if (state && !_buttonsSet) SetButtons();
        if (!state) _buttonsSet = false;
    }

    private void SetButtons()
    {
        _buttonsSet = true;
        RemoveButtons();
        foreach (var location in FastTravelManager.Instance.FastTravelLocations)
        {
            var buttonObject = Instantiate(buttonPrefab, buttonParent.transform);
            var buttonButton = buttonObject.GetComponent<Button>();
            var buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
            buttonText.text = location.fastTravelName;
            buttonButton.onClick.RemoveAllListeners();
            if (location.discovered && location.sceneName != SceneManager.GetActiveScene().name)
            {
                buttonButton.gameObject.SetActive(true);
                buttonButton.onClick.AddListener(() =>
                {
                    FastTravelManager.Instance.TravelTo(location);
                    TogglePanel(false);
                });
            }
            else
                buttonButton.gameObject.SetActive(false);
        }
    }

    private void RemoveButtons()
    {
        foreach (Transform child in buttonParent.transform) Destroy(child.gameObject);
    }
    #endregion ==========================================================================
}