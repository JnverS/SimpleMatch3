using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class button : MonoBehaviour
{

    public ScrollRect  scrollRect; // Ваш ScrollRect
    public RectTransform content; // Контент ScrollView
    public GameObject levelButtonPrefab; // Префаб кнопки уровня
    public int totalLevels = 10; // Общее количество уровней
    public float scrollSpeed = 0.5f; // Скорость прокрутки

    private int currentLevel = 0; // Текущий выбранный уровень
    private float targetPositionX;

    void Start()
    {
        GenerateLevelButtons();
        UpdateScrollPosition();
    }

    void Update()
    {
        // Плавное перемещение ScrollView
        
    }

    void GenerateLevelButtons()
    {
        for (int i = 0; i < totalLevels; i++)
        {
            GameObject newButton = Instantiate(levelButtonPrefab, content);
            newButton.GetComponent<LevelPoint>().ID = i.ToString();
            newButton.GetComponent<LevelPoint>().levelTitle.text = i.ToString();
            
            
        }

        // Устанавливаем ширину Content в зависимости от количества уровней
        float contentWidth = totalLevels * levelButtonPrefab.GetComponent<RectTransform>().sizeDelta.x;

        content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
       // scrollRect.sizeDelta = new Vector2(contentWidth, scrollRect.sizeDelta.y);
       

    }

    void OnLevelButtonClicked(int levelIndex)
    {
        currentLevel = levelIndex;
        UpdateScrollPosition();
    }

    void UpdateScrollPosition()
    {
        // Рассчитываем новую позицию по X на основе текущего уровня
        float contentWidth = content.rect.width;
        float viewportWidth = scrollRect.viewport.rect.width;
        float levelWidth = contentWidth / totalLevels;

        targetPositionX = currentLevel * levelWidth - (viewportWidth - levelWidth) / 2;
        Debug.Log(targetPositionX);
        content.localPosition = new Vector2(-targetPositionX, content.anchoredPosition.y);
    }

    public void ScrollLeft()
    {
        if (currentLevel > 0)
        {
            currentLevel--;
            UpdateScrollPosition();
        }
        else
        {
            currentLevel = totalLevels - 1;
            UpdateScrollPosition();
        }
    }

    public void ScrollRight()
    {
        
        if (currentLevel < totalLevels - 1)
        {
            currentLevel++;
            UpdateScrollPosition();
        }
        else
        {
            currentLevel = 0;
            UpdateScrollPosition();
        }
    }
}