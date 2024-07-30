using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{
    int _currentScore = 0;
    public int CurrentScore
    {
        get
        {
            return _currentScore;
        }
    }
    int _counterValue = 0;
    int _increment = 5;

    public TMP_Text scoreText;
   
    void Start()
    {
        UpdateScoreText(_currentScore);
    }

    public void UpdateScoreText(int scoreValue)
    {
        if (scoreText != null)
        {
            scoreText.text = scoreValue.ToString();
        }
    }

    public void AddScore(int value)
    {
        _currentScore += value;
        StartCoroutine(CountScoreRoutine());
    }

    IEnumerator CountScoreRoutine()
    {
        int iterations = 0;

        while(_counterValue <_currentScore && iterations < 100000)
        {
            _counterValue += _increment;
            UpdateScoreText(_counterValue);
            iterations++;
            yield return null;
        }
        _counterValue = _currentScore;
        UpdateScoreText(_currentScore);
    }
}
