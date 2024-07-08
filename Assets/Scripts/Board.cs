using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;

    public int borderSize;

    public GameObject tilePrefab;
    public GameObject[] gameItemPrefabs;

    public float swapTime = 0.5f;

    Tile[,] _allTiles;
    GameItem[,] _allGameItems;

    Tile _clickedTile;
    Tile _targetTile;

    void Start()
    {
        _allTiles = new Tile[width, height];
        _allGameItems = new GameItem[width, height];

        SetupTiles();
        SetupCamera();
        FillRandom();
        HighlightMatches();
    }

    void SetupTiles()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0), Quaternion.identity);
                tile.name = "Tile (" + i + "," + j + ")";
                _allTiles[i, j] = tile.GetComponent<Tile>();
                tile.transform.parent = transform;
                _allTiles[i, j].Init(i, j, this);
            }
        }
    }

    void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10f);

        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float verticalSize = (float)height / 2 + (float)borderSize;

        float horizontalSize = ((float)width / 2 + (float)borderSize) / aspectRatio;

        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    GameObject GetRandomGameItem()
    {
        int randomIndex = Random.Range(0, gameItemPrefabs.Length);

        return gameItemPrefabs[randomIndex];
    }

    public void PlaceGameItem(GameItem gameItem, int x, int y)
    {
        if (gameItem == null)
        {
            return;
        }

        gameItem.transform.position = new Vector3(x, y, 0);
        gameItem.transform.rotation = Quaternion.identity;
        if (IsWithBounds(x, y))
            _allGameItems[x, y] = gameItem;
        gameItem.SetCoord(x, y);
    }

    bool IsWithBounds(int x, int y) =>
       (x >= 0 && x < width && y >= 0 && y < height);
    void FillRandom()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameObject item = GetRandomGameItem();
                if (item != null)
                {
                    GameObject randomItem = Instantiate(item, Vector3.zero, Quaternion.identity);
                    if (randomItem != null)
                    {
                        randomItem.GetComponent<GameItem>().Init(this);
                        PlaceGameItem(randomItem.GetComponent<GameItem>(), i, j);
                        randomItem.transform.parent = transform;
                    }
                }
                else
                {
                    Debug.LogError("item is null! fill the array!");
                }
            }
        }
    }

    public void ClickTile(Tile tile)
    {
        if (_clickedTile == null)
        {
            _clickedTile = tile;
        }
    }

    public void DradToTile(Tile tile)
    {
        if (_clickedTile != null && IsNextTo(tile, _clickedTile))
        {
            _targetTile = tile;
        }
    }

    public void ReleaseTile()
    {
        if (_clickedTile != null && _targetTile != null)
        {
            SwitchTiles(_clickedTile, _targetTile);
        }

        _clickedTile = null;
        _targetTile = null;
    }

    private void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        GameItem clicked = _allGameItems[clickedTile.xIndex, clickedTile.yIndex];
        GameItem target = _allGameItems[targetTile.xIndex, targetTile.yIndex];

        clicked.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
        target.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
    }

    bool IsNextTo(Tile start, Tile end)
    {
        if (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
        {
            return true;
        }
        if (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex)
        {
            return true;
        }
        return false;
    }

    List<GameItem> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength = 3)
    {
        List<GameItem> matches = new List<GameItem>();
        GameItem startItem = null;

        if (IsWithBounds(startX, startY))
        {
            startItem = _allGameItems[startX, startY];
        }
        if (startItem != null)
        {
            matches.Add(startItem);
        }
        else
            return null;

        int nextX;
        int nextY;

        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + (int)Mathf.Clamp(searchDirection.x, -1, 1) * i;
            nextY = startY + (int)Mathf.Clamp(searchDirection.y, -1, 1) * i;

            if (!IsWithBounds(nextX, nextY))
            {
                break;
            }

            GameItem nextItem = _allGameItems[nextX, nextY];
            if (nextItem.matchValue == startItem.matchValue && !matches.Contains(nextItem))
            {
                matches.Add(nextItem);
            }
            else
                break;
        }

        if (matches.Count >= minLength)
            return matches;
        return null;
    }

    List<GameItem> FindVerticalMatches(int startX, int startY, int minLength = 3)
    {
        List<GameItem> upwardMatches = FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GameItem> downwardMatches = FindMatches(startX, startY, new Vector2(0, -1), 2);

        if (upwardMatches == null)
        {
            upwardMatches = new List<GameItem>();
        }
        if (downwardMatches == null)
        {
            downwardMatches = new List<GameItem>();
        }

        List<GameItem> combineMatches = upwardMatches.Union(downwardMatches).ToList();

        return (combineMatches.Count >= minLength) ? combineMatches : null;
    }

    List<GameItem> FindHorizontalMatches(int startX, int startY, int minLength = 3)
    {
        List<GameItem> rightMatches = FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GameItem> leftdMatches = FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightMatches == null)
        {
            rightMatches = new List<GameItem>();
        }
        if (leftdMatches == null)
        {
            leftdMatches = new List<GameItem>();
        }

        List<GameItem> combineMatches = rightMatches.Union(leftdMatches).ToList();

        return (combineMatches.Count >= minLength) ? combineMatches : null;
    }

    void HighlightMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                SpriteRenderer spriteRenderer = _allTiles[i, j].GetComponent<SpriteRenderer>();
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);

                List<GameItem> horizMatches = FindHorizontalMatches(i, j, 3);
                List<GameItem> verticalMatches = FindVerticalMatches(i, j, 3);

                if (horizMatches == null)
                {
                    horizMatches = new List<GameItem>();
                }
                if (verticalMatches == null)
                {
                    verticalMatches = new List<GameItem>();
                }

                List<GameItem> combineMatches = horizMatches.Union(verticalMatches).ToList();

                if (combineMatches.Count > 0)
                {
                    foreach (GameItem item in combineMatches)
                    {
                        spriteRenderer = _allTiles[item.xIndex, item.yIndex].GetComponent<SpriteRenderer>();
                        spriteRenderer.color = item.GetComponent<SpriteRenderer>().color;
                    }
                }
            }
        }
    }
}
