using UnityEngine;

public class Tile : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board _board;
    void Start()
    {
        
    }

    
    public void Init(int x, int y, Board board)
    {
        xIndex = x;
        yIndex = y;
        _board = board;
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
}
