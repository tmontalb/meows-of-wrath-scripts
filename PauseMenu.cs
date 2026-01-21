using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text[] options;            // 0=Resume, 1=Restart, 2=Quit
    [SerializeField] private RectTransform selector;        // SelectorArrow RectTransform
    [SerializeField] private Toggle easyModeToggle;

    [Header("Gameplay refs")]
    [SerializeField] private Player player;                 // must have ApplyDifficulty(bool)

    [Header("Input")]
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;
    public KeyCode selectKey = KeyCode.Return;
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode easyKey = KeyCode.E;

    [Header("Selector Offset")]
    public Vector2 selectorOffset = new Vector2(-40f, 0f);

    private int selectedIndex = 0;
    private bool isPaused = false;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Cache player once
        if (player == null)
            player = FindObjectOfType<Player>();

        // One-time reset while testing (REMOVE before shipping)
        // #if UNITY_EDITOR
        // PlayerPrefs.DeleteKey("EasyMode");
        // #endif
    }

    private void Start()
    {
        SyncEasyModeUI();
        UpdateSelectorPosition();
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
            SetPaused(!isPaused);

        if (!isPaused) return;

        if (options != null && options.Length > 0)
        {
            if (Input.GetKeyDown(upKey))
            {
                selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
                UpdateSelectorPosition();
            }
            else if (Input.GetKeyDown(downKey))
            {
                selectedIndex = (selectedIndex + 1) % options.Length;
                UpdateSelectorPosition();
            }
            else if (Input.GetKeyDown(selectKey))
            {
                ActivateSelection();
            }
        }

        // Easy mode hotkey while paused
        if (Input.GetKeyDown(easyKey))
        {
            bool newValue = !DifficultySettings.EasyMode;
            ApplyEasyMode(newValue);
            SyncEasyModeUI(); // <- guarantees the visible toggle reflects the new value
        }
    }

    // Hook this in Toggle -> OnValueChanged(bool)
    public void OnEasyModeToggleChanged(bool enabled)
    {
        ApplyEasyMode(enabled);
        SyncEasyModeUI(); // <- keeps UI consistent even if other scripts touched it
    }

    private void ApplyEasyMode(bool enabled)
    {
        DifficultySettings.EasyMode = enabled;

        if (player != null)
            player.ApplyDifficulty(enabled);
    }

    private void SyncEasyModeUI()
    {
        if (easyModeToggle != null)
            easyModeToggle.SetIsOnWithoutNotify(DifficultySettings.EasyMode);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        if (pausePanel != null)
            pausePanel.SetActive(paused);

        bool dialogOpen = (player != null && player.dialog);

        if (paused)
        {
            if (!dialogOpen)
            {
                Time.timeScale = 0f;
                if (MusicManager.I != null)
                    MusicManager.I.PauseMusic();
            }

            // Optional: always start on Resume
            // selectedIndex = 0;

            if (options != null && options.Length > 0)
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Length - 1);
                UpdateSelectorPosition();
            }

            SyncEasyModeUI();
        }
        else
        {
            if (!dialogOpen)
            {
                Time.timeScale = 1f;
                if (MusicManager.I != null)
                    MusicManager.I.ResumeMusic();
            }
        }
    }

    private void UpdateSelectorPosition()
    {
        if (selector == null || options == null || options.Length == 0) return;

        RectTransform target = options[selectedIndex].rectTransform;
        selector.position = target.position + (Vector3)selectorOffset;
    }

    private void ActivateSelection()
    {
        if (options == null || options.Length == 0) return;

        switch (selectedIndex)
        {
            case 0:
                SetPaused(false);
                break;

            case 1:
                Time.timeScale = 1f;
                GameState.I.doubleJump = false;
                GameState.I.respawnAtLastDoor = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;

            case 2:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}
