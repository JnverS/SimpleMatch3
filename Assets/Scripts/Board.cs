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

    public float swapTime = 0.3f;

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
        FillBoard();
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
    void FillBoard()
    {
        int maxIterations = 100;
        int iterations = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameItem item = FillRandomAt(i, j);
                iterations = 0;

                while (HasMatchOnFill(i, j))
                {
                    ClearItemAt(i, j);
                    item = FillRandomAt(i, j);
                    iterations++;

                    if (iterations >= maxIterations)
                    {
                        break;
                    }
                }
            }
        }
    }

    private GameItem FillRandomAt(int x, int y)
    {
        GameObject item = GetRandomGameItem();
        if (item != null)
        {
            GameObject randomItem = Instantiate(item, Vector3.zero, Quaternion.identity);
            if (randomItem != null)
            {
                randomItem.GetComponent<GameItem>().Init(this);
                PlaceGameItem(randomItem.GetComponent<GameItem>(), x, y);
                randomItem.transform.parent = transform;
                return randomItem.GetComponent<GameItem>();
            }
        }
        else
        {
            Debug.LogError("item is null! fill the array!");
        }
        return null;
    }

    bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GameItem> leftMatches = FindMatches(x, y, new Vector3(-1, 0), minLength);
        List<GameItem> downMatches = FindMatches(x, y, new Vector3(0, -1), minLength);

        if (leftMatches == null)
            leftMatches = new List<GameItem>();
        if (downMatches == null)
            downMatches = new List<GameItem>();

        return (leftMatches.Count > 0 || downMatches.Count > 0);
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
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        GameItem clicked = _allGameItems[clickedTile.xIndex, clickedTile.yIndex];
        GameItem target = _allGameItems[targetTile.xIndex, targetTile.yIndex];

        if (clicked != null && target != null)
        {
            clicked.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
            target.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);

            yield return new WaitForSeconds(swapTime);

            List<GameItem> clickedMatches = FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
            List<GameItem> targetMatches = FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

            if (targetMatches.Count == 0 && clickedMatches.Count == 0)
            {
                clicked.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                target.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
            }
            else
            {
                yield return new WaitForSeconds(swapTime);

                ClearAndRefillBoard(clickedMatches.Union(targetMatches).ToList());
            }
        }
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

            if (nextItem == null)
                break;
            else
            {
                if (nextItem.matchValue == startItem.matchValue && !matches.Contains(nextItem))
                {
                    matches.Add(nextItem);
                }
                else
                    break;
            }
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

    void HighlightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = _allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
    }

    void HighlightTileOn(int x, int y, Color color)
    {
        SpriteRenderer spriteRenderer = _allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }

    void HighlightMatches()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                HighlightMatchesAt(i, j);
            }
        }
    }

    private void HighlightMatchesAt(int x, int y)
    {
        HighlightTileOff(x, y);

        List<GameItem> combineMatches = FindMatchesAt(x, y);

        if (combineMatches.Count > 0)
        {
            foreach (GameItem item in combineMatches)
            {
                HighlightTileOn(item.xIndex, item.yIndex, item.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    void HighlightItems(List<GameItem> gameItems)
    {
        foreach (GameItem item in gameItems)
        {
            if (item != null)
            {
                HighlightTileOn(item.xIndex, item.yIndex, item.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    private List<GameItem> FindMatchesAt(int x, int y, int minLength = 3)
    {
        List<GameItem> horizMatches = FindHorizontalMatches(x, y, minLength);
        List<GameItem> verticalMatches = FindVerticalMatches(x, y, minLength);

        if (horizMatches == null)
        {
            horizMatches = new List<GameItem>();
        }
        if (verticalMatches == null)
        {
            verticalMatches = new List<GameItem>();
        }

        List<GameItem> combineMatches = horizMatches.Union(verticalMatches).ToList();
        return combineMatches;
    }
    List<GameItem> FindMatchesAt(List<GameItem> gameItems, int minLength = 3)
    {
        List<GameItem> matches = new List<GameItem>();
        foreach (GameItem item in gameItems)
        {
            matches = matches.Union(FindMatchesAt(item.xIndex, item.yIndex, minLength)).ToList();
        }
        return matches;
    }
    void ClearItemAt(int x, int y)
    {
        GameItem itemToClear = _allGameItems[x, y];
        if (itemToClear != null)
        {
            _allGameItems[x, y] = null;
            Destroy(itemToClear.gameObject);
        }
        HighlightTileOff(x, y);
    }

    void ClearBoard()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                ClearItemAt(i, j);
            }
        }
    }

    void ClearItemAt(List<GameItem> gameItems)
    {
        foreach (GameItem item in gameItems)
        {
            if (item != null)
            {
                ClearItemAt(item.xIndex, item.yIndex);
            }
        }
    }

    List<GameItem> CollapseColumn(int column, float collapseTime = .2f)
    {
        List<GameItem> movingItems = new List<GameItem>();

        for (int i = 0; i < height - 1; i++)
        {
            if (_allGameItems[column, i] == null)
            {
                for (int j = i + 1; j < height; j++)
                {
                    if (_allGameItems[column, j] != null)
                    {
                        _allGameItems[column, j].Move(column, i, collapseTime);
                        _allGameItems[column, i] = _allGameItems[column, j];
                        _allGameItems[column, i].SetCoord(column, i);

                        if (!movingItems.Contains(_allGameItems[column, i]))
                        {
                            movingItems.Add(_allGameItems[column, i]);
                        }
                        _allGameItems[column, j] = null;
                        break;
                    }
                }
            }
        }
        return movingItems;
    }

    List<GameItem> CollapseColumn(List<GameItem> gameItems)
    {
        List<GameItem> movingItems = new List<GameItem>();
        List<int> columnsToCollapse = GetColumns(gameItems);

        foreach (int column in columnsToCollapse)
        {
            movingItems = movingItems.Union(CollapseColumn(column)).ToList();
        }
        return movingItems;
    }

    List<int> GetColumns(List<GameItem> gameItems)
    {
        List<int> columns = new List<int>();

        foreach (GameItem item in gameItems)
        {
            if (!columns.Contains(item.xIndex))
            {
                columns.Add(item.xIndex);
            }
        }
        return columns;
    }

    void ClearAndRefillBoard(List<GameItem> gameItems)
    {
        StartCoroutine(ClearAndRefillBoardRoutine(gameItems));
    }

    IEnumerator ClearAndRefillBoardRoutine(List<GameItem> gameItems)
    {
        StartCoroutine(ClearAndCollapseRoutine(gameItems));
        yield return null;
    }

    IEnumerator ClearAndCollapseRoutine(List<GameItem> gameItems)
    {
        List<GameItem> movingItems = new List<GameItem>();
        List<GameItem> matches = new List<GameItem>();
        
        HighlightItems(gameItems);
        yield return new WaitForSeconds(.25f);

        bool isFinished = false;
        while (!isFinished)
        {
            ClearItemAt(gameItems);
            yield return new WaitForSeconds(.25f);
            movingItems = CollapseColumn(gameItems);

            yield return new WaitForSeconds(.25f);
            matches = FindMatchesAt(movingItems);

            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
            {
                yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            }
        }

        yield return null;
    }
}
