using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LevelPoint : MonoBehaviour
{
    public static UnityAction<LevelPoint> OnLevelPointSelection;
    public string ID = "1";
    public enum LevelState
    {
        Locked, Unlocked, Passed
    }

    public LevelState CurrentState = LevelState.Locked;

    [Min(0)] public int LoadingSceneID = 0;
    [Range(0, 1)] public float SuccessRate = 0;

    [SerializeField] private bool nameIsID = true;
    [SerializeField] private bool headerDisabling = true;

    public TMP_Text levelTitle;
    [SerializeField] private Toggle[] rewardToggles;
    [SerializeField] private Button button;
    [SerializeField] private Image pointImage;
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockedPointSprite;
    [SerializeField] private Sprite unlockedPointSprite;
    [SerializeField] private Sprite passedPointSprite;
    private void Awake()
    {
        UpdateInformation();
    }

    private void Chose()
    {
        OnLevelPointSelection.Invoke(this);
    }

    public void UpdateInformation()
    {
        if (CurrentState == LevelState.Locked)
        {
            // Заблокирован
            for (int i = 0; i < rewardToggles.Length; i++)
            {
                rewardToggles[i].gameObject.SetActive(false);
            }

            if (levelTitle && headerDisabling) levelTitle.gameObject.SetActive(false);
            if (button) button.interactable = false;
            if (lockImage) lockImage.enabled = true;
            if (pointImage && lockedPointSprite)
            {
                pointImage.sprite = lockedPointSprite;
            }

        }
        else if (CurrentState == LevelState.Unlocked)
        {
            // Разблокирован
            for (int i = 0; i < rewardToggles.Length; i++)
            {
                rewardToggles[i].gameObject.SetActive(false);
            }

            if (levelTitle) levelTitle.gameObject.SetActive(true);
            if (button)
            {
                button.interactable = true;
                button.onClick.AddListener(Chose);
            }
            if (lockImage) lockImage.enabled = false;
            if (pointImage && unlockedPointSprite)
            {
                pointImage.sprite = unlockedPointSprite;
            }

        }
        else if (CurrentState == LevelState.Passed)
        {
            // Пройден
            for (int i = 0; i < rewardToggles.Length; i++)
            {
                rewardToggles[i].gameObject.SetActive(true);
                rewardToggles[i].isOn = false;
            }

            float success = SuccessRate * rewardToggles.Length;
            int result = Mathf.RoundToInt(success);
            for (int i = 0; i < result; i++)
            {
                rewardToggles[i].isOn = true;
            }
            if (levelTitle) levelTitle.gameObject.SetActive(true);
            if (button)
            {
                button.interactable = true;
                button.onClick.AddListener(Chose);
            }
            if (lockImage) lockImage.enabled = false;
            if (pointImage && passedPointSprite)
            {
                pointImage.sprite = passedPointSprite;
            }


        }

        if (nameIsID && levelTitle)
        {
            levelTitle.text = ID;
        }
    }

}
