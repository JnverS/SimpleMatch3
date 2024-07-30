using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RectXformMover : MonoBehaviour
{
    public Vector3 startPosition;
    public Vector3 onscreenPosition;
    public Vector3 endPosition;

    public float timeToMove = 1f;
    RectTransform _rectXform;
    bool _isMoving;

    void Awake()
    {
        _rectXform = GetComponent<RectTransform>();
    }

    void Move(Vector3 startPos, Vector3 endPos, float timeToMove)
    {
        if (!_isMoving)
        {
            StartCoroutine(MoveRoutine(startPos, endPos, timeToMove));
        }
    }

    private IEnumerator MoveRoutine(Vector3 startPos, Vector3 endPos, float timeToMove)
    {
        if (_rectXform!= null)
        {
            _rectXform.anchoredPosition = startPos;
        }

        bool reachedDestination = false;
        float elapsedTime = 0f;
        _isMoving = true;

        while (!reachedDestination)
        {
            if (Vector3.Distance(_rectXform.anchoredPosition, endPos) < 0.01f)
            {
                reachedDestination = true;
                break;
            }
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);
            t = t * t * t*(t * (t * 6 - 15) + 10);
            if (_rectXform != null)
            {
                _rectXform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
            }
            yield return null;
        }
        _isMoving = false;
    }

    public void MoveOn()
    {
        Move(startPosition, onscreenPosition, timeToMove);
    }
    public void MoveOff()
    {
        Move(onscreenPosition, endPosition, timeToMove);
    }
}

