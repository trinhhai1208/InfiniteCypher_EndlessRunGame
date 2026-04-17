using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MissionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject MissionPanel;
    [SerializeField] private Transform ContentContainer;
    [SerializeField] private GameObject MissionItemPrefab;
    [SerializeField] private TextMeshProUGUI TotalGoldText;

    private List<MissionItemUI> _spawnedItems = new List<MissionItemUI>();

    private void Start()
    {
        if (MissionPanel != null) MissionPanel.SetActive(false);
        PopulateList();
    }

    public void OpenMissions()
    {
        if (MissionPanel != null) MissionPanel.SetActive(true);
        RefreshAll();
    }

    public void CloseMissions()
    {
        if (MissionPanel != null) MissionPanel.SetActive(false);
    }

    private void PopulateList()
    {
        if (MissionManager.Instance == null || ContentContainer == null || MissionItemPrefab == null) return;

        // Clear old ones
        foreach (Transform child in ContentContainer)
        {
            Destroy(child.gameObject);
        }
        _spawnedItems.Clear();

        var types = MissionManager.Instance.GetAllAvailableTypes();
        foreach (var type in types)
        {
            var config = MissionManager.Instance.GetConfig(type);
            if (config != null)
            {
                // Instantiate Single Run Mission
                GameObject goSingle = Instantiate(MissionItemPrefab, ContentContainer);
                MissionItemUI itemSingle = goSingle.GetComponent<MissionItemUI>();
                itemSingle.Setup(type, MissionScope.SingleRun, config, this);
                _spawnedItems.Add(itemSingle);

                // Instantiate Total Mission
                GameObject goTotal = Instantiate(MissionItemPrefab, ContentContainer);
                MissionItemUI itemTotal = goTotal.GetComponent<MissionItemUI>();
                itemTotal.Setup(type, MissionScope.Total, config, this);
                _spawnedItems.Add(itemTotal);
            }
        }
    }

    public void RefreshAll()
    {
        if (TotalGoldText != null)
        {
            TotalGoldText.text = PlayerPrefs.GetInt("TotalGold", 0).ToString("N0");
        }

        foreach (var item in _spawnedItems)
        {
            item.RefreshData();
        }
    }
}
