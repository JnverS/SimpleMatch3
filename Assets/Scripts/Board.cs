using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;

    public int borderSize;

    public GameObject tileNormalPrefab;
    public GameObject tileObstaclePrefab;
    public GameObject[] gameItemPrefabs;

    public GameObject RowBombPrefab;
    public GameObject ColumnBombPrefab;
    public GameObject AdjacentBombPrefab;

    GameObject _clickedTileBomb;
    GameObject _targetTileBomb;

    public float swapTime = 0.1f;

    Tile[,] _allTiles;
    GameItem[,] _allGameItems;

    Tile _clickedTile;
    Tile _targetTile;

    bool _playerInputEnabled = true;
    public StartingObject[] startingTiles;
    public StartingObject[] startingGameItems;
    private ParticleManager _particleManager;
    public int fillYOffset = 10;
    public float fillMoveTime = .5f;

    [System.Serializable]
    public class StartingObject
    {
        public GameObject prefab;
        public int x;
        public int y;
        public int z;
    }

    private void Awake()
    {
        _allTiles = new Tile[width, height];
        _allGameItems = new GameItem[width, height];
    }
    void Start()
    {
        SetupTiles();
        SetupGameItems();

        SetupCamera();
        FillBoard(fillYOffset, fillMoveTime);
        _particleManager = GameObject.FindWithTag("ParticleManager").GetComponent<ParticleManager>();
    }

    void SetupTiles()
    {
        foreach (StartingObject sTile in startingTiles)
        {
            if (sTile != null)
            {
                MakeTile(sTile.prefab, sTile.x, sTile.y, sTile.z);
            }
        }
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (_allTiles[i, j] == null)
                {
                    MakeTile(tileNormalPrefab, i, j);
                }
            }
        }
    }

    void SetupGameItems()
    {
        foreach (StartingObject sItem in startingGameItems)
        {
            if (sItem != null)
            {
                GameObject item = Instantiate(sItem.prefab, new Vector3(sItem.x, sItem.y, 0), Quaternion.identity);
                MakeGameItem(item, sItem.x, sItem.y, fillYOffset, fillMoveTime);
            }
        }
    }

    private void MakeTile(GameObject prefab, int x, int y, int z = 0)
    {
        if (prefab != null && IsWithinBounds(x, y))
        {
            GameObject tile = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity);
            tile.name = "Tile (" + x + "," + y + ")";
            _allTiles[x, y] = tile.GetComponent<Tile>();
            tile.transform.parent = transform;
            _allTiles[x, y].Init(x, y, this);
        }
    }

    void MakeGameItem(GameObject prefab, int x, int y, int falseYOffset = 0, float moveTime = 0.1f)
    {

        if (prefab != null && IsWithinBounds(x, y))
        {
            prefab.GetComponent<GameItem>().Init(this);
            PlaceGameItem(prefab.GetComponent<GameItem>(), x, y);

            if (falseYOffset != 0)
            {
                prefab.transform.position = new Vector3(x, y + falseYOffset, 0);
                prefab.GetComponent<GameItem>().Move(x, y, moveTime);
            }
            prefab.transform.parent = transform;
        }
    }

    GameObject MakeBomb(GameObject prefab, int x, int y)
    {
        if (prefab != null && IsWithinBounds(x, y))
        {
            GameObject bomb = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
            bomb.GetComponent<Bomb>().Init(this);
            bomb.GetComponent<Bomb>().SetCoord(x, y);
            bomb.transform.parent = transform;
            return bomb;
        }
        return null;
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
        if (IsWithinBounds(x, y))
            _allGameItems[x, y] = gameItem;
        gameItem.SetCoord(x, y);
    }

    bool IsWithinBounds(int x, int y) =>
       (x >= 0 && x < width && y >= 0 && y < height);
    void FillBoard(int falseYOffset = 0, float moveTime = .1f)
    {
        int maxIterations = 100;
        int iterations = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (_allGameItems[i, j] == null && _allTiles[i, j].tileType != TileType.Obstacle)
                {
                    GameItem item = FillRandomAt(i, j, falseYOffset, moveTime);
                    iterations = 0;

                    while (HasMatchOnFill(i, j))
                    {
                        ClearItemAt(i, j);
                        item = FillRandomAt(i, j, falseYOffset, moveTime);
                        iterations++;

                        if (iterations >= maxIterations)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    private GameItem FillRandomAt(int x, int y, int falseYOffset = 0, float moveTime = .1f)
    {
        if (IsWithinBounds(x, y))
        {
            GameObject item = Instantiate(GetRandomGameItem(), Vector3.zero, Quaternion.identity);
            MakeGameItem(item, x, y, falseYOffset, moveTime);
            return item.GetComponent<GameItem>();
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
        if (_playerInputEnabled)
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
                    Vector2 swipeDirection = new Vector2(targetTile.xIndex - clickedTile.xIndex, targetTile.yIndex - clickedTile.xIndex);
                    _clickedTileBomb = DropBomb(clickedTile.xIndex, clickedTile.yIndex, swipeDirection, clickedMatches);
                    _targetTileBomb = DropBomb(targetTile.xIndex, targetTile.yIndex, swipeDirection, targetMatches);
                    ClearAndRefillBoard(clickedMatches.Union(targetMatches).ToList());
                }
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

        if (IsWithinBounds(startX, startY))
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

            if (!IsWithinBounds(nextX, nextY))
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
        if (_allTiles[x, y].tileType != TileType.Breakable)
        {
            SpriteRenderer spriteRenderer = _allTiles[x, y].GetComponent<SpriteRenderer>();
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
        }
    }

    void HighlightTileOn(int x, int y, Color color)
    {
        if (_allTiles[x, y].tileType != TileType.Breakable)
        {
            SpriteRenderer spriteRenderer = _allTiles[x, y].GetComponent<SpriteRenderer>();
            spriteRenderer.color = color;
        }
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

    List<GameItem> FindAllMatches()
    {
        List<GameItem> combinedMatches = new List<GameItem>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                List<GameItem> matches = FindMatchesAt(i, j);
                combinedMatches = combinedMatches.Union(matches).ToList();
            }
        }
        return combinedMatches;
    }
    void ClearItemAt(int x, int y)
    {
        GameItem itemToClear = _allGameItems[x, y];
        if (itemToClear != null)
        {
            _allGameItems[x, y] = null;
            Destroy(itemToClear.gameObject);
        }

        //HighlightTileOff(x, y);
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
                if (_particleManager != null)
                {
                    _particleManager.ClearItemFXAt(item.xIndex, item.yIndex);
                }
            }
        }
    }

    void BreakTileAt(int x, int y)
    {
        Tile tileToBreak = _allTiles[x, y];
        if (tileToBreak != null && tileToBreak.tileType == TileType.Breakable)
        {
            if (_particleManager != null)
            {
                _particleManager.BreakTileFXAt(tileToBreak.breakableValue, x, y);
            }
            tileToBreak.BreakTile();
        }
    }

    void BreakTileAt(List<GameItem> gameItems)
    {
        foreach (GameItem item in gameItems)
        {
            if (item != null)
                BreakTileAt(item.xIndex, item.yIndex);
        }
    }

    List<GameItem> CollapseColumn(int column, float collapseTime = .1f)
    {
        List<GameItem> movingItems = new List<GameItem>();

        for (int i = 0; i < height - 1; i++)
        {
            if (_allGameItems[column, i] == null && _allTiles[column, i].tileType != TileType.Obstacle)
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
        _playerInputEnabled = false;
        List<GameItem> matches = gameItems;
        do
        {
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            yield return null;
            yield return StartCoroutine(RefillRoutine());
            matches = FindAllMatches();

            yield return new WaitForSeconds(.2f);
        }
        while (matches.Count != 0);
        _playerInputEnabled = true;
    }

    IEnumerator RefillRoutine()
    {
        FillBoard(fillYOffset, fillMoveTime);
        yield return null;
    }

    IEnumerator ClearAndCollapseRoutine(List<GameItem> gameItems)
    {
        List<GameItem> movingItems = new List<GameItem>();
        List<GameItem> matches = new List<GameItem>();

        //HighlightItems(gameItems);
        yield return new WaitForSeconds(.2f);

        bool isFinished = false;
        while (!isFinished)
        {
            List<GameItem> bombedItems = GetBombedItems(gameItems);
            gameItems = gameItems.Union(bombedItems).ToList();

            ClearItemAt(gameItems);
            BreakTileAt(gameItems);

            if (_clickedTileBomb != null)
            {
                ActivateBomb(_clickedTileBomb);
                _clickedTileBomb = null;
            }

            if (_targetTileBomb != null)
            {
                ActivateBomb(_targetTileBomb);
                _targetTileBomb = null;
            }
            yield return new WaitForSeconds(.2f);
            movingItems = CollapseColumn(gameItems);

            while (!IsCollapsed(movingItems))
            {
                yield return null;
            }
            yield return new WaitForSeconds(.2f);
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

    bool IsCollapsed(List<GameItem> gameItems)
    {
        foreach (GameItem item in gameItems)
        {
            if (item != null)
            {
                if (item.transform.position.y - (float)item.yIndex > .001f)
                {
                    return false;
                }
            }
        }
        return true;
    }

    List<GameItem> GetRowItems(int row)
    {
        List<GameItem> gameItems = new List<GameItem>();

        for (int i = 0; i < width; i++)
        {
            if (_allGameItems[i, row] != null)
            {
                gameItems.Add(_allGameItems[i, row]);
            }
        }
        return gameItems;
    }

    List<GameItem> GetColumnItems(int column)
    {
        List<GameItem> gameItems = new List<GameItem>();

        for (int i = 0; i < height; i++)
        {
            if (_allGameItems[column, i] != null)
            {
                gameItems.Add(_allGameItems[column, i]);
            }
        }
        return gameItems;
    }

    List<GameItem> GetAdjacentItems(int x, int y, int offset = 1)
    {
        List<GameItem> gameItems = new List<GameItem>();

        for (int i = x - offset; i <= x + offset; i++)
        {
            for (int j = y - offset; j <= y + offset; j++)
            {
                if (IsWithinBounds(i, j))
                {
                    gameItems.Add(_allGameItems[i, j]);
                }
            }
        }
        return gameItems;
    }

    List<GameItem> GetBombedItems(List<GameItem> gameItems)
    {
        List<GameItem> allItemsToClear = new List<GameItem>();
        foreach (GameItem item in gameItems)
        {
            if (item != null)
            {
                List<GameItem> itemsToClear = new List<GameItem>();
                Bomb bomb = item.GetComponent<Bomb>();

                if (bomb != null)
                {
                    switch (bomb.bombType)
                    {
                        case BombType.Column:
                            itemsToClear = GetColumnItems(bomb.xIndex);
                            break;
                        case BombType.Row:
                            itemsToClear = GetRowItems(bomb.yIndex);
                            break;
                        case BombType.Adjacent:
                            itemsToClear = GetAdjacentItems(bomb.xIndex, bomb.yIndex, 1);
                            break;
                        case BombType.Color:

                            break;
                    }
                    allItemsToClear = allItemsToClear.Union(itemsToClear).ToList();
                }
            }
        }
        return allItemsToClear;
    }

    bool IsCormerMatch(List<GameItem> gameItems)
    {
        bool vertical = false;
        bool horizontal = false;
        int xStart = -1;
        int yStart = -1;

        foreach (GameItem item in gameItems)
        {
            if (item != null)
            {
                if (xStart == -1 || yStart == -1)
                {
                    xStart = item.xIndex;
                    yStart = item.yIndex;
                    continue;
                }
                if (item.xIndex != xStart && item.yIndex == yStart)
                {
                    horizontal = true;
                }
                if (item.xIndex == xStart && item.yIndex != yStart)
                {
                    vertical = true;
                }
            }
        }

        return (horizontal && vertical);
    }

    GameObject DropBomb(int x, int y, Vector2 swapDirection, List<GameItem> gameItems)
    {
        GameObject bomb = null;

        if (gameItems.Count >= 4)
        {
            if (IsCormerMatch(gameItems))
            {
                if (AdjacentBombPrefab != null)
                {
                    bomb = MakeBomb(AdjacentBombPrefab, x, y);
                }
            }
            else
            {
                if (swapDirection.x != 0)
                {
                    if (RowBombPrefab != null)
                    {
                        bomb = MakeBomb(RowBombPrefab, x, y);
                    }
                }
                else
                {
                    if (ColumnBombPrefab != null)
                    {
                        bomb = MakeBomb(ColumnBombPrefab, x, y);
                    }
                }
            }
        }

        return bomb;
    }

    void ActivateBomb(GameObject bomb)
    {
        int x = (int)bomb.transform.position.x;
        int y = (int)bomb.transform.position.y;

        if (IsWithinBounds(x, y))
        {
            _allGameItems[x, y] = bomb.GetComponent<GameItem>();
        }
    }
}
