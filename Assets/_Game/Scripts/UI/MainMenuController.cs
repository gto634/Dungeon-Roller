using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] Camera viewCamera;
    [SerializeField] Transform windshield;
    [SerializeField] Transform menuRoot;
    [SerializeField] Vector3 menuLocalPosition;
    [SerializeField] Vector3 menuLocalEuler;
    [SerializeField, Min(.01f)] float menuScale = 1f;
    [SerializeField, Range(10f, 100f)] float referenceFieldOfView = 48f;
    [SerializeField] UnityEngine.UI.Button playButton;
    [SerializeField] UnityEngine.UI.Button exitButton;
    [SerializeField] EventSystem eventSystem;
    [SerializeField] string gameplayScene = "Assets/_Game/Scenes/Base.unity";
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;

    public Camera ViewCamera => viewCamera;
    public Transform Windshield => windshield;
    public Transform MenuRoot => menuRoot;
    public string GameplayScene => gameplayScene;
    public bool IsTransitioning { get; private set; }

    void OnEnable()
    {
        RefreshView();
        if (!Application.isPlaying) return;
        IsTransitioning = false;
        SetButtonsInteractable(true);
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDisable()
    {
        if (!Application.isPlaying) return;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    void Start()
    {
        if (Application.isPlaying && eventSystem != null && playButton != null)
            eventSystem.SetSelectedGameObject(playButton.gameObject);
    }

    void LateUpdate() => RefreshView();

    public void RefreshView()
    {
        if (windshield != null && menuRoot != null)
        {
            menuRoot.SetPositionAndRotation(windshield.TransformPoint(menuLocalPosition),
                windshield.rotation * Quaternion.Euler(menuLocalEuler));
            menuRoot.localScale = windshield.lossyScale * menuScale;
        }
        if (viewCamera == null) return;
        float tangent = Mathf.Tan(referenceFieldOfView * .5f * Mathf.Deg2Rad);
        float vertical = 2f * Mathf.Atan(tangent * (16f / 9f) / Mathf.Max(.1f, viewCamera.aspect)) * Mathf.Rad2Deg;
        viewCamera.fieldOfView = Mathf.Clamp(vertical, 10f, 110f);
    }

    void SetButtonsInteractable(bool value)
    {
        if (playButton != null) playButton.interactable = value;
        if (exitButton != null) exitButton.interactable = value;
    }

    public void Play()
    {
        if (!Application.isPlaying || IsTransitioning) return;
        if (!Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            Debug.LogError("Add the gameplay scene to the build scene list before starting the game.", this);
            return;
        }
        IsTransitioning = true;
        SetButtonsInteractable(false);
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Single);
    }

    public void Exit()
    {
        if (!Application.isPlaying || IsTransitioning) return;
        IsTransitioning = true;
        SetButtonsInteractable(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
