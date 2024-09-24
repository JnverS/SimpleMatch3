using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(LevelGoal))]
public class GameManager : Singleton<GameManager>
{
    // public int movesLeft = 30;
    // public int scoreGoal = 10000;
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
    LevelGoal _levelGoal;
    public ScoreMeter scoreMeter;
    public override void Awake()
    {
        base.Awake();
        _levelGoal = GetComponent<LevelGoal>();

        _board = GameObject.FindObjectOfType<Board>().GetComponent<Board>();
    }
    public bool IsGameOver { get => _isGameOver; set => _isGameOver = value; }

    void Start()
    {
        if (scoreMeter!= null)
        {
            scoreMeter.SetupStars(_levelGoal);
        }
        Scene scene = SceneManager.GetActiveScene();

        if (levelNameText != null)
        {
            levelNameText.text = scene.name;
        }
        _levelGoal.movesLeft++;
        UpdateMoves();
        StartCoroutine("ExecuteGameLoop");
    }
    public void UpdateMoves()
    {
        _levelGoal.movesLeft--;
        if (movesLeftText != null)
            movesLeftText.text = _levelGoal.movesLeft.ToString();
    }
    IEnumerator ExecuteGameLoop()
    {
        yield return StartCoroutine("StartGameRoutine");
        yield return StartCoroutine("PlayGameRoutine");
        yield return StartCoroutine("WaitForBoardRoutine", 0.5f);
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
            messageWindow.ShowMessage(goalIcon, "score goal\n" + _levelGoal.scoreGoals[0].ToString(), "start");
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
        while (!IsGameOver)
        {

            _isGameOver = _levelGoal.IsGameOver();
            _isWinner = _levelGoal.IsWinner();
            yield return null;
        }
    }
    IEnumerator WaitForBoardRoutine(float delay = 0f)
    {
        if (_board != null)
        {
            yield return new WaitForSeconds(_board.swapTime);
            while (_board.isRefilling)
            {
                yield return null;
            }
        }
        yield return new WaitForSeconds(delay);
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
    public void ScorePoints(GameItem item, int multiplier = 1, int bonus = 0)
    {
        if (item != null)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(item.scoreValue * multiplier + bonus);
                _levelGoal.UpdateScoreStars(ScoreManager.Instance.CurrentScore);
                if (scoreMeter != null)
                {
                    scoreMeter.UpdateScoreMeter(ScoreManager.Instance.CurrentScore, _levelGoal.scoreStars);
                }
            }
            if (SoundManager.Instance != null && item.clearSound!= null)
            {
                SoundManager.Instance.PlayClipAtPoint(item.clearSound, Vector3.zero, SoundManager.Instance.fxVolume);
            }
        }
    }
}

