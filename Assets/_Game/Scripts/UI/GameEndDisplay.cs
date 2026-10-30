using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

[MovedFrom(true, null, "Dungeon.Runtime", "GameEndDisplay")]
[DisallowMultipleComponent]
public sealed class GameEndDisplay : MonoBehaviour
{
    [SerializeField] GameManager gameManager;
    [SerializeField] CanvasGroup screen;
    [SerializeField] TextMeshProUGUI message;
    [SerializeField] ScoreDisplay scoreDisplay;
    [SerializeField] RectTransform scorePanel;
    [SerializeField] TextMeshProUGUI bestScoreLabel;
    [SerializeField, Min(.1f)] float scoreCountDuration = 2f;
    [SerializeField] UnityEngine.UI.Button replayButton;
    [SerializeField] UnityEngine.UI.Button mainMenuButton;
    [SerializeField] string gameplayScene = "Assets/_Game/Scenes/Ryad/Generation.unity";
    [SerializeField] string mainMenuScene = "Assets/_Game/Scenes/MainMenu.unity";
    Vector2 hudAnchorMin, hudAnchorMax, hudPivot, hudPosition, hudSize;
    PointManager resultOwner;
    int resultVersion;
    float finalScore, previousBestScore, elapsed;
    bool layoutCached, hasResult;

    public bool IsVisible => screen != null && screen.gameObject.activeInHierarchy && screen.alpha > 0f;
    public float DisplayedScore { get; private set; }
    public float DisplayedBestScore => Mathf.Max(previousBestScore, DisplayedScore);
    public bool IsCounting { get; private set; }
    public bool IsTransitioning { get; private set; }

    void Awake() => CacheHudLayout();
    void OnEnable()
    {
        if (replayButton != null) replayButton.onClick.AddListener(Replay);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (gameManager == null) return;
        gameManager.GameEnded += Show;
        gameManager.RunStarted += Hide;
        if (gameManager.IsGameEnded) Show(); else Hide();
    }
    void OnDisable()
    {
        if (replayButton != null) replayButton.onClick.RemoveListener(Replay);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        ClearButtonSelection();
        if (gameManager != null)
        {
            gameManager.GameEnded -= Show;
            gameManager.RunStarted -= Hide;
        }
        SetVisible(false);
        RestoreHudLayout();
    }
    void Update()
    {
        if (!IsCounting || !IsVisible) return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(.1f, scoreCountDuration));
        float eased = 1f - Mathf.Pow(1f - progress, 3f);
        DisplayedScore = progress >= 1f ? finalScore
            : Mathf.Min(finalScore, Mathf.Floor((float)((double)finalScore * eased)));
        IsCounting = progress < 1f;
        RefreshScores();
    }
    public void Show()
    {
        if (message != null)
            message.text = gameManager != null && gameManager.IsDefeat
                ? "<color=#FF6B6B>Défaite !</color>\n<size=65%>Partie terminée</size>"
                : "<color=#BC69FF>Sortie atteinte !</color>\n<size=65%>Partie terminée</size>";
        PointManager points = gameManager != null ? gameManager.PointManager : null;
        if (!hasResult || resultOwner != points || (points != null && resultVersion != points.ResultVersion))
        {
            resultOwner = points;
            resultVersion = points != null ? points.ResultVersion : 0;
            finalScore = points != null ? points.Points : 0f;
            previousBestScore = points != null ? points.BestScoreBeforeResult : 0f;
            elapsed = DisplayedScore = 0f;
            IsCounting = finalScore > 0f;
            hasResult = true;
        }
        CacheHudLayout();
        if (scorePanel != null)
        {
            scorePanel.anchorMin = scorePanel.anchorMax = new Vector2(.5f, .5f);
            scorePanel.pivot = new Vector2(.5f, .5f);
            scorePanel.anchoredPosition = new Vector2(-230f, 0f);
            scorePanel.sizeDelta = new Vector2(420f, 120f);
        }
        SetVisible(true);
        RefreshScores();
        if (!IsTransitioning && replayButton != null) replayButton.Select();
    }
    public void Hide()
    {
        IsTransitioning = false;
        ClearButtonSelection();
        IsCounting = hasResult = false;
        elapsed = DisplayedScore = previousBestScore = 0f;
        resultOwner = null;
        SetVisible(false);
        RestoreHudLayout();
    }
    public void Replay()
    {
        string target = gameManager != null ? gameManager.gameObject.scene.path : gameplayScene;
        if (!Application.CanStreamedLevelBeLoaded(target)) target = gameplayScene;
        BeginSceneTransition(target);
    }
    public void ReturnToMainMenu() => BeginSceneTransition(mainMenuScene);
    void BeginSceneTransition(string target)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !IsVisible || IsTransitioning
            || gameManager == null || !gameManager.IsGameEnded) return;
        if (!Application.CanStreamedLevelBeLoaded(target))
        {
            Debug.LogError("Add the destination scene to the build scene list before leaving the result screen.", this);
            return;
        }
        IsTransitioning = true;
        if (replayButton != null) replayButton.interactable = false;
        if (mainMenuButton != null) mainMenuButton.interactable = false;
        ClearButtonSelection();
        gameManager.PrepareForSceneChange();
        SceneManager.LoadSceneAsync(target, LoadSceneMode.Single);
    }
    void ClearButtonSelection()
    {
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null) return;
        GameObject selected = events.currentSelectedGameObject;
        if ((replayButton != null && selected == replayButton.gameObject)
            || (mainMenuButton != null && selected == mainMenuButton.gameObject)) events.SetSelectedGameObject(null);
    }
    void RefreshScores()
    {
        if (scoreDisplay != null) scoreDisplay.ShowResult(DisplayedScore);
        if (bestScoreLabel != null)
            bestScoreLabel.text = "Meilleur score\n<color=#FFD66B><size=125%>"
                + ScoreDisplay.FormatPoints(DisplayedBestScore) + " pts</size></color>";
    }
    void CacheHudLayout()
    {
        if (layoutCached || scorePanel == null) return;
        hudAnchorMin = scorePanel.anchorMin; hudAnchorMax = scorePanel.anchorMax;
        hudPivot = scorePanel.pivot; hudPosition = scorePanel.anchoredPosition; hudSize = scorePanel.sizeDelta;
        layoutCached = true;
    }
    void RestoreHudLayout()
    {
        if (layoutCached && scorePanel != null)
        {
            scorePanel.anchorMin = hudAnchorMin; scorePanel.anchorMax = hudAnchorMax;
            scorePanel.pivot = hudPivot; scorePanel.anchoredPosition = hudPosition; scorePanel.sizeDelta = hudSize;
        }
        if (scoreDisplay != null) scoreDisplay.ResumeHud();
    }
    void SetVisible(bool visible)
    {
        if (screen == null) return;
        screen.alpha = visible ? 1f : 0f;
        screen.interactable = screen.blocksRaycasts = visible;
        if (replayButton != null) replayButton.interactable = visible && !IsTransitioning;
        if (mainMenuButton != null) mainMenuButton.interactable = visible && !IsTransitioning;
    }
}
