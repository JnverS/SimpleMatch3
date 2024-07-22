using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum MatchValue
{
    Yellow,
    Blue,
    Cyan,
    Magenta,
    Green,
    Indigo,
    Red,
    Teal,
    Wild, 
    None
}

public class GameItem : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board _board;

    bool _isMoving = false;
    public MatchValue matchValue;

    public void Init(Board board)
    {
        _board = board;
    }

    public void SetCoord(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void Move(int destX, int destY, float timeToMove)
    {
        if (!_isMoving)
            StartCoroutine(MoveRoutine(new Vector3(destX, destY, 0), timeToMove));
    }

    private IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        Vector3 startPosition = transform.position;

        bool reachedDestination = false;

        float elapsedTime = 0f;

        _isMoving = true;
        while (!reachedDestination)
        {
            if (Vector3.Distance(transform.position, destination) < 0.01f)
            {
                reachedDestination = true;
                if (_board != null)
                {
                    _board.PlaceGameItem(this, (int) destination.x, (int) destination.y);
                }
                break;
            }

            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp(elapsedTime / timeToMove, 0, 1);

            t = Mathf.Sin(t * Mathf.PI * .5f);

            transform.position = Vector3.Lerp(startPosition, destination, t);

            yield return null;
        }
        _isMoving = false;
    }
    public void ChangeColor(GameItem itemToMatch)
    {
        SpriteRenderer rendererToChange = GetComponent<SpriteRenderer>();
        Color colorToMath = Color.clear;
        if (itemToMatch != null)
        {
            SpriteRenderer rendererToMatch = itemToMatch.GetComponent<SpriteRenderer>();
            
            if (rendererToMatch != null && rendererToChange != null)
            {
                rendererToChange.color = rendererToMatch.color;
            }

            matchValue = itemToMatch.matchValue;
        }
    }
}
