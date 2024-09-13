using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSuffler : MonoBehaviour
{
    public List<GameItem> RemoveNormalItems(GameItem[,] allItems)
    {
        List<GameItem> normalItems = new List<GameItem>();

        int width = allItems.GetLength(0);
        int height = allItems.GetLength(1);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allItems[i, j] != null)
                {
                    Bomb bomb = allItems[i, j].GetComponent<Bomb>();
                    Collectible collectible = allItems[i, j].GetComponent<Collectible>();

                    if (bomb == null && collectible == null)
                    {
                        normalItems.Add(allItems[i, j]);
                        allItems[i, j] = null;
                    }
                }
            }
        }
        return normalItems;
    }

    public void SuffleList(List<GameItem> itemsToShaffle)
    {
        int maxCount = itemsToShaffle.Count;
        for (int i = 0; i < maxCount - 1; i++)
        {
            int randomIndex = Random.Range(i, maxCount);
            
            if (randomIndex == i)
            {
                continue;
            }

            GameItem tmp = itemsToShaffle[randomIndex];
            itemsToShaffle[randomIndex] = itemsToShaffle[i];
            itemsToShaffle[i] = tmp;
            
        }
    }

    public void MoveItems(GameItem[,] allItems, float swapTime = .5f)
    {
        int width = allItems.GetLength(0);
        int height = allItems.GetLength(1);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (allItems[i, j] != null)
                {
                    allItems[i, j].Move(i, j, swapTime);
                }
            }
        }
    }
}
