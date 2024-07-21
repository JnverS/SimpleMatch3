using System;
using System.Collections;
using UnityEngine;


public enum TileType
{
    Normal,
    Obstacle,
    Breakable
}
[RequireComponent(typeof(SpriteRenderer))]
public class Tile : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board _board;

    public TileType tileType = TileType.Normal;
    SpriteRenderer _spriteRenderer;
    public int breakableValue = 0;
    public Sprite[] breakableSprites;
    public Color normalColor;
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        
    }

    
    public void Init(int x, int y, Board board)
    {
        xIndex = x;
        yIndex = y;
        _board = board;
        if (tileType == TileType.Breakable)
        {
            if (breakableSprites[breakableValue] != null)
            {
                _spriteRenderer.sprite = breakableSprites[breakableValue];
            }
        }
    }

    private void OnMouseDown()
    {
        if (_board != null)
        {
            _board.ClickTile(this);
        }
    }
    private void OnMouseEnter()
    {
        if (_board != null)
        {
            _board.DradToTile(this);
        }
    }
    private void OnMouseUp()
    {
        if (_board != null)
        {
            _board.ReleaseTile();
        }
    }

    public void BreakTile()
    {
        if (tileType != TileType.Breakable)
            return;
        StartCoroutine(BreakTileRoutine());
    }

    IEnumerator BreakTileRoutine()
    {
        breakableValue = Mathf.Clamp(breakableValue--, 0, breakableValue);
        yield return new WaitForSeconds(.1f);

        if (breakableSprites[breakableValue] != null)
        {
            _spriteRenderer.sprite = breakableSprites[breakableValue];
        }

        if (breakableValue == 0)
        {
            tileType = TileType.Normal;
            _spriteRenderer.color = normalColor;
        }
    }
}
