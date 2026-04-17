using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image Icon;
    [SerializeField] private TextMeshProUGUI TitleText;
    [SerializeField] private TextMeshProUGUI DescriptionText;
    [SerializeField] private TextMeshProUGUI ProgressText;
    [Header("Progress Elements")]
    [SerializeField] private Transform SegmentContainer;
    [SerializeField] private Color ColorActive = new Color(0.0157f, 1f, 0.6862f); // 04FFAF
    [SerializeField] private Color ColorInactive = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Reward Button")]
    [SerializeField] private Button ClaimButton;
    [SerializeField] private TextMeshProUGUI ClaimButtonText;
    [SerializeField] private Image ClaimButtonImage;
    [SerializeField] private Color ColorClaimable = new Color(0.0157f, 1f, 0.6862f); // 04FFAF
    [SerializeField] private Color ColorLocked = Color.gray;

    private MissionType _type;
    private MissionScope _scope;
    private MissionUI _parent;

    public void Setup(MissionType type, MissionScope scope, MissionConfigSO config, MissionUI parent)
    {
        _type = type;
        _scope = scope;
        _parent = parent;

        var scopeConfig = scope == MissionScope.SingleRun ? config.SingleRun : config.Total;
        if (Icon != null && scopeConfig.Icon != null) Icon.sprite = scopeConfig.Icon;
        
        ClaimButton.onClick.RemoveAllListeners();
        ClaimButton.onClick.AddListener(OnClaimClicked);

        RefreshData();
    }

    public void RefreshData()
    {
        if (MissionManager.Instance == null) return;

        var config = MissionManager.Instance.GetConfig(_type);
        if (config == null) return;

        int tier = MissionManager.Instance.GetTier(_type, _scope);
        int progress = MissionManager.Instance.GetProgress(_type, _scope);
        MissionStatus status = MissionManager.Instance.GetStatus(_type, _scope);

        int goal = config.GetGoal(_scope, tier);
        int reward = config.GetReward(goal);

        var scopeConfig = _scope == MissionScope.SingleRun ? config.SingleRun : config.Total;

        if (TitleText != null) 
        {
            string suffix = _scope == MissionScope.SingleRun ? " (1 Ván)" : " (Tổng)";
            TitleText.text = scopeConfig.Title + " Lv." + tier + suffix;
        }

        if (DescriptionText != null) 
        {
            DescriptionText.text = config.GetDescription(_scope, goal, reward);
        }

        if (ProgressText != null) 
            ProgressText.text = progress + " / " + goal;

        // Cập nhật 5 ô tiến trình (mỗi ô 20%)
        if (SegmentContainer != null)
        {
            float percent = (float)progress / goal;
            for (int i = 0; i < SegmentContainer.childCount; i++)
            {
                Image segImg = SegmentContainer.GetChild(i).GetComponent<Image>();
                if (segImg != null)
                {
                    // Ô thứ i (0-4) sẽ sáng nếu tiến độ đạt mức (i+1)*20%
                    // VD: ô 0 sáng nếu đạt 20%, ô 1 sáng nếu đạt 40%...
                    segImg.color = (percent >= (i + 1) * 0.199f) ? ColorActive : ColorInactive;
                }
            }
        }

        if (status == MissionStatus.Completed)
        {
            ClaimButton.interactable = true;
            ClaimButtonText.text = "NHẬN " + reward + " G";
            if (ClaimButtonImage != null) ClaimButtonImage.color = ColorClaimable;
        }
        else
        {
            ClaimButton.interactable = false;
            ClaimButtonText.text = reward + " G";
            if (ClaimButtonImage != null) ClaimButtonImage.color = ColorLocked;
        }
    }

    private void OnClaimClicked()
    {
        if (MissionManager.Instance != null)
        {
            if (MissionManager.Instance.ClaimReward(_type, _scope))
            {
                // Play sound
                // AudioManager.Instance.PlaySFX("Bonus");
                
                // Refresh parent to update total gold on UI
                _parent.RefreshAll();
            }
        }
    }
}
