using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public int movesLeft = 30;
    public int scoreGoal = 10000;
    public ScreenFader screenFader;
    public TMP_Text levelNameText;
    public TMP_Text movesLeftText;

    Board _board;
    bool _isReadyToBegin;
    bool _isGameOver;
    bool _isWinner;
    bool _isReadyToReload;

    public MessageWindow messageWindow;
    public Sprite loseIcon;
    public Sprite winIcon;
    public Sprite goalIcon;

    void Start()
    {
        _board = GameObject.FindObjectOfType<Board>().GetComponent<Board>();
        Scene scene = SceneManager.GetActiveScene();

        if (levelNameText != null)
        {
            levelNameText.text = scene.name;
        }
        UpdateMoves();
        StartCoroutine("ExecuteGameLoop");
    }
    public void UpdateMoves()
    {
        if (movesLeftText != null)
            movesLeftText.text = movesLeft.ToString();
    }
    IEnumerator ExecuteGameLoop()
    {
        yield return StartCoroutine("StartGameRoutine");
        yield return StartCoroutine("PlayGameRoutine");
        yield return StartCoroutine("EndGameRoutine");
    }
    public void BeginGame()
    {
        _isReadyToBegin = true;
    }    
    public void ReloadGame()
    {
        _isReadyToReload = true;
    }
    IEnumerator StartGameRoutine()
    {
        if (messageWindow != null)
        {
            messageWindow.GetComponent<RectXformMover>().MoveOn();
            messageWindow.ShowMessage(goalIcon, "score goal\n" + scoreGoal.ToString(), "start");
        }
        while (!_isReadyToBegin)
        {
            yield return null;
        }
        if (screenFader != null)
            screenFader.FadeOff();
        yield return new WaitForSeconds(.5f);
        if (_board != null)
            _board.SetupBoard();
    }
    IEnumerator PlayGameRoutine()
    {
        while (!_isGameOver)
        { 
            if(ScoreManager.Instance != null)
            {
                if (ScoreManager.Instance.CurrentScore >= scoreGoal)
                {
                    _isGameOver = true;
                    _isWinner = true;
                }
            }
            if (movesLeft == 0)
            {
                _isGameOver = true;
                _isWinner = false;
            }
            yield return null; 
        }
    }
    IEnumerator EndGameRoutine()
    {
        _isReadyToReload = false;

        if (_isWinner)
        {
            if (messageWindow != null)
            {
                messageWindow.GetComponent<RectXformMover>().MoveOn();
                messageWindow.ShowMessage(winIcon, "YOU WIN", "OK");
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayWinSound();
                }
            }
        }
        else
        {
            if (messageWindow != null)
            {
                messageWindow.GetComponent<RectXformMover>().MoveOn();
                messageWindow.ShowMessage(loseIcon, "YOU LOSE", "OK");
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayLoseSound();
                }
            }
        }
        yield return new WaitForSeconds(1f);
        if (screenFader != null)
            screenFader.FadeOn();

        while (!_isReadyToReload)
            yield return null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

