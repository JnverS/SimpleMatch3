using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardDeadlock : MonoBehaviour
{
    List<GameItem> GetRowOrColumnList(GameItem[,] allItems, int x, int y, int listLength = 3, bool checkRow = true)
    {
        int width = allItems.GetLength(0);
        int height = allItems.GetLength(1);

        List<GameItem> itemList = new List<GameItem>();
        for (int i = 0; i < listLength; i++)
        {
            if (checkRow)
            {
                if (x + i < width && y < height && allItems[x + i, y] != null)
                {
                    itemList.Add(allItems[x + i, y]);
                }
            }
            else
            {
                if (x < width && y + i < height && allItems[x, y + i] != null)
                {
                    itemList.Add(allItems[x, y + i]);
                }
            }
            
        }
        return itemList;
    }

    List<GameItem> GetMinimumMatches(List<GameItem> gameItems, int minForMatch = 2)
    {
        List<GameItem> matches = new List<GameItem>();
        var groups = gameItems.GroupBy(n => n.matchValue);

        foreach (IGrouping<MatchValue, GameItem> group in groups)
        {
            if (group.Count() >= minForMatch && group.Key != MatchValue.None)
            {
                matches = group.ToList();
            }
        }

        return matches;
    }

    List<GameItem> GetNeighbors(GameItem[,] allItems, int x, int y)
    {
        int width = allItems.GetLength(0);
        int height = allItems.GetLength(1);

        List<GameItem> neighbors = new List<GameItem>();

        Vector2[] searchDirections = new Vector2[4]
        {
            new Vector2(-1, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(0, -1)
        };

        foreach (Vector2 dir in searchDirections)
        {
            if (x + (int)dir.x >= 0 && x + (int)dir.x < width && y + (int)dir.y >= 0 && y + (int)dir.y < height)
            {
                if (allItems[x + (int)dir.x, y + (int)dir.y] != null)
                {
                    if (!neighbors.Contains(allItems[x + (int)dir.x, y + (int)dir.y]))
                    {
                        neighbors.Add(allItems[x + (int)dir.x, y + (int)dir.y]);
                    }
                }
            }
        }

        return neighbors;
    }

    bool HasMoveAt(GameItem[,] allItems, int x, int y, int listLength = 3, bool checkRow = true)
    {
        List<GameItem> items = GetRowOrColumnList(allItems, x, y, listLength, checkRow);

        List<GameItem> matches = GetMinimumMatches(items, listLength - 1);

        GameItem unmatchesItem = null;

        if (items != null && matches != null)
        {
            if (items.Count == listLength && matches.Count == listLength - 1)
            {
                unmatchesItem = items.Except(matches).FirstOrDefault();
            }
            if (unmatchesItem != null)
            {
                List<GameItem> neighbors = GetNeighbors(allItems, unmatchesItem.xIndex, unmatchesItem.yIndex);
                neighbors = neighbors.Except(matches).ToList();
                neighbors = neighbors.FindAll(n => n.matchValue == matches[0].matchValue);
                matches = matches.Union(neighbors).ToList();
            }
            if (matches.Count >= listLength)
            {
                string rowColStr = (checkRow) ? " row " : " column ";
                Debug.Log("===== Available move =====");
                Debug.Log("Move " + matches[0].matchValue + "item to " + unmatchesItem.xIndex 
                    + "," + unmatchesItem.yIndex + " to form matching " + rowColStr);
                return true;
            }
        }
        return false;
    }

    public bool IsDeadlocked(GameItem[,] allItems, int listLength = 3)
    {
        int width = allItems.GetLength(0);
        int height = allItems.GetLength(1);
        bool isDeadlocked = true;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (HasMoveAt(allItems, i, j, listLength, true) || HasMoveAt(allItems, i, j, listLength, false))
                {
                    isDeadlocked = false;
                }
            }
        }
        if (isDeadlocked)
            Debug.Log("======Board deadlocked =========");
        return isDeadlocked;
    }
}
