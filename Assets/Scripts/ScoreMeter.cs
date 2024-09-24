using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Slider))]
public class ScoreMeter : MonoBehaviour
{
    public Slider slider;
    public ScoreStar[] scoreStars = new ScoreStar[3];
    LevelGoal _levelGoal;
    int _maxScore;
    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void SetupStars(LevelGoal levelGoal)
    {
        if (levelGoal == null)
        {
            Debug.LogWarning("SCOREMETER Invalid level goal!");
            return;
        }
        _levelGoal = levelGoal;
        _maxScore = _levelGoal.scoreGoals[_levelGoal.scoreGoals.Length-1];

        float sliderWidth = slider.GetComponent<RectTransform>().rect.width;
        if (_maxScore > 0)
        {
            for (int i = 0; i < _levelGoal.scoreGoals.Length; i++)
            {
                if (scoreStars[i] != null)
                {
                    float newX = (sliderWidth * levelGoal.scoreGoals[i] / _maxScore) - (sliderWidth * .5f);
                    RectTransform starRectXform = scoreStars[i].GetComponent<RectTransform>();
                    if (starRectXform != null)
                    {
                        starRectXform.anchoredPosition = new Vector3(newX, starRectXform.anchoredPosition.y); 
                    }
                }
            }
        }
    }
    public void UpdateScoreMeter(int score, int starCount) 
    {
        if (_levelGoal != null)
        {
            slider.value = (float)score / (float)_maxScore;
        }
        for (int i = 0; i < starCount; i++)
        {
            if (scoreStars[i] != null)
            {
                scoreStars[i].Activate();
            }
        }
    }
}
