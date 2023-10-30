using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FastTravelUI : MonoBehaviour
{
    [SerializeField] private GameObject fastTravelPanel;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonParent;
    private FastTravelManager _fastTravelManager;
    private bool _isPanelActive, _buttonsSet;

    private void Awake()
    {
    }

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
                buttonButton.onClick.AddListener(() =>
                {
                    FastTravelManager.Instance.TravelTo(location);
                    TogglePanel(false);
                });
            else
                buttonButton.interactable = false;
        }
    }

    private void RemoveButtons()
    {
        foreach (Transform child in buttonParent.transform) Destroy(child.gameObject);
    }
}