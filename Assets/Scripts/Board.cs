using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoardDeadlock))]
[RequireComponent(typeof(BoardSuffler))]
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
    public GameObject ColorBombPrefab;

    public int maxCollectibles = 3;
    public int CollectibleCount = 0;

    [Range(0, 1)]
    public float chanceForCollectible = .1f;
    public GameObject[] collectiblePrefabs;

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

    int _scoreMultiplier = 0;

    public bool isRefilling = false;

    BoardDeadlock _boardDeadlock;
    BoardSuffler _boardSuffler;

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
        _particleManager = GameObject.FindWithTag("ParticleManager").GetComponent<ParticleManager>();
        _boardDeadlock = GetComponent<BoardDeadlock>();
        _boardSuffler = GetComponent<BoardSuffler>();
    }

    public void SetupBoard()
    {
        SetupTiles();
        SetupGameItems();

        List<GameItem> startingCollectibles = FindAllCollectibles();
        CollectibleCount = startingCollectibles.Count;

        SetupCamera();
        FillBoard(fillYOffset, fillMoveTime);
    }

    void SetupTiles()
    {
        if (startingTiles.Length > 0)
        {
            foreach (StartingObject sTile in startingTiles)
            {
                if (sTile != null)
                {
                    MakeTile(sTile.prefab, sTile.x, sTile.y, sTile.z);
                }
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
    GameObject GetRandomObject(GameObject[] objectArray)
    {
        int randomIndex = Random.Range(0, objectArray.Length);
        if (objectArray[randomIndex] == null)
        {
            Debug.LogWarning("BOARD.GetRandomObject at index " + randomIndex + " does not contain a valid GameObject!");
        }
        return objectArray[randomIndex];
    }
    GameObject GetRandomGameItem()
    {
        return GetRandomObject(gameItemPrefabs);
    }
    GameObject GetRandomCollectible()
    {
        return GetRandomObject(collectiblePrefabs);
    }
    public void PlaceGameItem(GameItem gameItem, int x, int y)
    {
        if (gameItem == null)
        {
            Debug.LogWarning("BOARD.PlaceGameItem");
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

    void FillBoardFromList(List<GameItem> gameItems)
    {
        Queue<GameItem> unusedItems = new Queue<GameItem>(gameItems);
        int maxIterations = 100;
        int iterations = 0;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (_allGameItems[i,j] == null && _allTiles[i,j].tileType != TileType.Obstacle)
                {
                    _allGameItems[i, j] = unusedItems.Dequeue();
                    iterations = 0;
                    while (HasMatchOnFill(i, j))
                    {
                        unusedItems.Enqueue(_allGameItems[i, j]);
                        _allGameItems[i, j] = unusedItems.Dequeue();
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

                    if (j == height - 1 && CanAddCollectible())
                    {
                        FillRandomCollectibleAt(i, j, falseYOffset, moveTime);
                        CollectibleCount++;
                    }
                    else
                    {

                        FillRandomGameItemAt(i, j, falseYOffset, moveTime);
                        iterations = 0;

                        while (HasMatchOnFill(i, j))
                        {
                            ClearItemAt(i, j);
                            FillRandomGameItemAt(i, j, falseYOffset, moveTime);
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
    }

    private GameItem FillRandomGameItemAt(int x, int y, int falseYOffset = 0, float moveTime = .1f)
    {
        if (IsWithinBounds(x, y))
        {
            GameObject item = Instantiate(GetRandomGameItem(), Vector3.zero, Quaternion.identity);
            MakeGameItem(item, x, y, falseYOffset, moveTime);
            return item.GetComponent<GameItem>();
        }
        return null;
    }
    private GameItem FillRandomCollectibleAt(int x, int y, int falseYOffset = 0, float moveTime = .1f)
    {
        if (IsWithinBounds(x, y))
        {
            GameObject item = Instantiate(GetRandomCollectible(), Vector3.zero, Quaternion.identity);
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
        if (_playerInputEnabled && !GameManager.Instance.IsGameOver)
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
                List<GameItem> colorMatches = new List<GameItem>();
                if (IsColorBomb(clicked) && !IsColorBomb(target))
                {
                    clicked.matchValue = target.matchValue;
                    colorMatches = FindAllMatchValue(clicked.matchValue);
                }
                else if (!IsColorBomb(clicked) && IsColorBomb(target))
                {
                    target.matchValue = clicked.matchValue;
                    colorMatches = FindAllMatchValue(target.matchValue);
                }
                else if (IsColorBomb(clicked) && IsColorBomb(target))
                {
                    foreach (GameItem item in _allGameItems)
                    {
                        if (!colorMatches.Contains(item))
                        {
                            colorMatches.Add(item);
                        }
                    }
                }

                if (targetMatches.Count == 0 && clickedMatches.Count == 0 && colorMatches.Count == 0)
                {
                    clicked.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime);
                    target.Move(targetTile.xIndex, targetTile.yIndex, swapTime);
                }
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.movesLeft--;
                        GameManager.Instance.UpdateMoves();
                    }

                    yield return new WaitForSeconds(swapTime);
                    Vector2 swipeDirection = new Vector2(targetTile.xIndex - clickedTile.xIndex, targetTile.yIndex - clickedTile.xIndex);
                    _clickedTileBomb = DropBomb(clickedTile.xIndex, clickedTile.yIndex, swipeDirection, clickedMatches);
                    _targetTileBomb = DropBomb(targetTile.xIndex, targetTile.yIndex, swipeDirection, targetMatches);

                    if (_clickedTileBomb != null && target != null)
                    {
                        GameItem clickedBombItem = _clickedTileBomb.GetComponent<GameItem>();
                        if (!IsColorBomb(clickedBombItem))
                            clickedBombItem.ChangeColor(target);
                    }
                    if (_targetTileBomb != null && clicked != null)
                    {
                        GameItem targetBombItem = _targetTileBomb.GetComponent<GameItem>();
                        if (!IsColorBomb(targetBombItem))
                            targetBombItem.ChangeColor(clicked);
                    }
                    ClearAndRefillBoard(clickedMatches.Union(targetMatches).ToList().Union(colorMatches).ToList());
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
                if (nextItem.matchValue == startItem.matchValue && !matches.Contains(nextItem) && nextItem.matchValue != MatchValue.None)
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

                if (_particleManager != null)
                {
                    _particleManager.ClearItemFXAt(i, j);
                }
            }
        }
    }

    void ClearItemAt(List<GameItem> gameItems, List<GameItem> bombedItems)
    {
        foreach (GameItem item in gameItems)
        {
            if (item != null)
            {
                ClearItemAt(item.xIndex, item.yIndex);
                int bonus = 0;
                if (gameItems.Count >= 4)
                {
                    bonus = 20;
                }
                item.ScorePoints(_scoreMultiplier, bonus);

                if (_particleManager != null)
                {
                    if (bombedItems.Contains(item))
                        _particleManager.BombFXAt(item.xIndex, item.yIndex);

                    else
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
    List<GameItem> CollapseColumn(List<int> columnsToCollapse)
    {
        List<GameItem> movingItems = new List<GameItem>();

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
            if (item != null)
            {
                if (!columns.Contains(item.xIndex))
                {
                    columns.Add(item.xIndex);
                }
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
        isRefilling = true;
        List<GameItem> matches = gameItems;

        _scoreMultiplier = 0;
        do
        {
            _scoreMultiplier++;
            yield return StartCoroutine(ClearAndCollapseRoutine(matches));
            yield return null;
            yield return StartCoroutine(RefillRoutine());
            matches = FindAllMatches();

            yield return new WaitForSeconds(.2f);
        }
        while (matches.Count != 0);

        if (_boardDeadlock.IsDeadlocked(_allGameItems, 3))
        {
            yield return new WaitForSeconds(4f);
           // ClearBoard();
            yield return StartCoroutine(ShuffleBoardRoutine());

            yield return new WaitForSeconds(1f);

            yield return StartCoroutine(RefillRoutine());
        }

        _playerInputEnabled = true;
        isRefilling = false;
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

            bombedItems = GetBombedItems(gameItems);
            gameItems = gameItems.Union(bombedItems).ToList();

            List<GameItem> collectedItems = FindCollectiblesAt(0, true);

            List<GameItem> allCollectibles = FindAllCollectibles();
            List<GameItem> blockers = gameItems.Intersect(allCollectibles).ToList();
            collectedItems = collectedItems.Union(blockers).ToList();

            CollectibleCount -= collectedItems.Count;

            gameItems = gameItems.Union(collectedItems).ToList();

            List<int> columnsToCollapse = GetColumns(gameItems);

            ClearItemAt(gameItems, bombedItems);
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
            yield return new WaitForSeconds(.25f);
            movingItems = CollapseColumn(columnsToCollapse);

            while (!IsCollapsed(movingItems))
            {
                yield return null;
            }
            yield return new WaitForSeconds(.2f);
            matches = FindMatchesAt(movingItems);
            collectedItems = FindCollectiblesAt(0, true);
            matches = matches.Union(collectedItems).ToList();

            if (matches.Count == 0)
            {
                isFinished = true;
                break;
            }
            else
            {
                _scoreMultiplier++;
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBonusSound();
                }
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
                if (item.transform.position.x - (float)item.xIndex > .001f)
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
                    allItemsToClear = RemoveCollectibles(allItemsToClear);
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
            if (gameItems.Count >= 5 && !IsCormerMatch(gameItems))
            {
                if (ColorBombPrefab != null)
                {
                    bomb = MakeBomb(ColorBombPrefab, x, y);
                }
            }
            else if (IsCormerMatch(gameItems))
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

    List<GameItem> FindAllMatchValue(MatchValue value)
    {
        List<GameItem> foundItems = new List<GameItem>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (_allGameItems[i, j] != null)
                {
                    if (_allGameItems[i, j].matchValue == value)
                    {
                        foundItems.Add(_allGameItems[i, j]);
                    }
                }
            }
        }
        return foundItems;
    }

    bool IsColorBomb(GameItem gameItem)
    {
        Bomb bomb = gameItem.GetComponent<Bomb>();

        if (bomb != null)
        {
            return (bomb.bombType == BombType.Color);
        }
        return false;
    }

    List<GameItem> FindCollectiblesAt(int row, bool clearedAtBottomOnly = false)
    {
        List<GameItem> foundCollectibles = new List<GameItem>();

        for (int i = 0; i < width; i++)
        {
            if (_allGameItems[i, row] != null)
            {
                Collectible collectibleComponent = _allGameItems[i, row].GetComponent<Collectible>();
                if (collectibleComponent != null)
                {
                    if (!clearedAtBottomOnly || (clearedAtBottomOnly && collectibleComponent.clearedByBottom))
                        foundCollectibles.Add(_allGameItems[i, row]);
                }
            }
        }
        return foundCollectibles;
    }

    List<GameItem> FindAllCollectibles()
    {
        List<GameItem> foundCollectibles = new List<GameItem>();

        for (int i = 0; i < height; i++)
        {
            List<GameItem> collectibleRow = FindCollectiblesAt(i);
            foundCollectibles = foundCollectibles.Union(collectibleRow).ToList();
        }
        return foundCollectibles;
    }
    bool CanAddCollectible()
    {
        return (Random.Range(0f, 1f) <= chanceForCollectible && collectiblePrefabs.Length > 0
            && CollectibleCount < maxCollectibles);
    }
    List<GameItem> RemoveCollectibles(List<GameItem> bombedItems)
    {
        List<GameItem> itemsToRemove = new List<GameItem>();
        List<GameItem> collectibleItems = FindAllCollectibles();

        foreach (GameItem item in collectibleItems)
        {
            Collectible collectibleComponent = item.GetComponent<Collectible>();

            if (collectibleComponent != null)
            {
                if (!collectibleComponent.clearedByBomb)
                {
                    itemsToRemove.Add(item);
                }
            }
        }

        return bombedItems.Except(itemsToRemove).ToList();
    }

    public void TestDeadlock()
    {
        _boardDeadlock.IsDeadlocked(_allGameItems, 3);
    }

    public void SuffleBoard()
    {
        if (_playerInputEnabled)
        {
            StartCoroutine(ShuffleBoardRoutine());
        }
    }

    IEnumerator ShuffleBoardRoutine()
    {
        List<GameItem> allItems = new List<GameItem>();
        foreach (GameItem item in _allGameItems)
        {
            allItems.Add(item);
        }

        while (!IsCollapsed(allItems))
        {
            yield return null;
        }

        List<GameItem> normalItems = _boardSuffler.RemoveNormalItems(_allGameItems);
        _boardSuffler.SuffleList(normalItems);
        FillBoardFromList(normalItems);
        _boardSuffler.MoveItems(_allGameItems, swapTime);
        List<GameItem> matches = FindAllMatches();
        StartCoroutine(ClearAndRefillBoardRoutine(matches));
    }  
}
