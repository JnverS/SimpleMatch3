using System.Collections;
using System.Collections.Generic;
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
                    j--;
                    continue;
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
}
