using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject opponentSelectPanel;
    [SerializeField] private GameObject inGamePanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject joystickPanel;

    [Header("Opponent Select Buttons")]
    [SerializeField] private Button midasButton;
    [SerializeField] private Button metroButton;
    [SerializeField] private Button zeusButton;
    [SerializeField] private Button twinButton;

    [Header("Opponent Select Images")]
    [SerializeField] private Image midasImage;
    [SerializeField] private Image metroImage;
    [SerializeField] private Image zeusImage;
    [SerializeField] private Image twinImage;

    [Header("Opponent Sprites")]
    [SerializeField] private Sprite midasOpenSprite;
    [SerializeField] private Sprite midasDefeatedSprite;
    [SerializeField] private Sprite metroLockedSprite;
    [SerializeField] private Sprite metroOpenSprite;
    [SerializeField] private Sprite metroDefeatedSprite;
    [SerializeField] private Sprite twinLockedSprite;
    [SerializeField] private Sprite twinOpenSprite;
    [SerializeField] private Sprite twinDefeatedSprite;
    [SerializeField] private Sprite zeusLockedSprite;
    [SerializeField] private Sprite zeusOpenSprite;
    [SerializeField] private Sprite zeusDefeatedSprite;

    [Header("SFX")]
    [SerializeField] private AudioClip bgm;
    [SerializeField] private AudioClip gameStartSfx; // Bell Ring
    [SerializeField] private AudioClip gameBgm; // Crowd Cheer

    private bool matchEnded = false;

    public AudioManager GetAudioManager() => AudioManager.GetInstance();
    public AudioClip GetGameStartSfx() => gameStartSfx;
    public AudioClip GetGameBgm() => gameBgm;

    private void PlayButtonSfx()
    {
        GetAudioManager().PlayBtnSfx();
    }

    void Start()
    {
        Time.timeScale = 1f;

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (opponentSelectPanel != null)
            opponentSelectPanel.SetActive(false);

        if (inGamePanel != null)
            inGamePanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        GetAudioManager().PlayAudio(bgm, true);

        RefreshOpponentSelectState();
        UpdateJoystickVisibility();
    }

    void Update()
    {
        
    }

    public void StartGame(GameObject panelToClose, GameObject panelToOpen)
    {
        PlayButtonSfx();

        if (panelToClose != null)
            panelToClose.SetActive(false);

        if (panelToOpen != null)
            panelToOpen.SetActive(true);
    }

    public void StartGame()
    {
        StartGame(mainMenuPanel, gamePanel);
    }

    public void OpenOpponentSelect()
    {
        PlayButtonSfx();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (opponentSelectPanel != null)
            opponentSelectPanel.SetActive(true);

        RefreshOpponentSelectState();
    }

    public void ExitGame()
    {
        PlayButtonSfx();

        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    //NOT FINAL. IN GAME SHOULD BE IN ANOTHER SCENE. FOR DEMO PURPOSES
    public void OpenInGamePanel()
    {
        PlayButtonSfx();

        SceneManager.LoadScene("vsMidas");

        UpdateJoystickVisibility();
    }

    public void ShowPausePanel()
    {
        if (matchEnded) return;

        PlayButtonSfx();

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Time.timeScale = 0f;
        UpdateJoystickVisibility();
    }

    public void HidePausePanel()
    {
        if (matchEnded) return;

        PlayButtonSfx();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        UpdateJoystickVisibility();
    }

    public void ShowWinPanel()
    {
        if (matchEnded) return;

        CareerProgression.MarkWinForScene(SceneManager.GetActiveScene().name);
        RefreshOpponentSelectState();

        matchEnded = true;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(true);

        if (losePanel != null)
            losePanel.SetActive(false);

        UpdateJoystickVisibility();
    }

    public void ShowLosePanel()
    {
        if (matchEnded) return;

        matchEnded = true;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(true);

        if (winPanel != null)
            winPanel.SetActive(false);

        UpdateJoystickVisibility();
    }

    public void ReturnToMainMenu()
    {
        PlayButtonSfx();

        matchEnded = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (winPanel != null)
            winPanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);

        SceneManager.LoadScene("MainMenu");

        if (inGamePanel != null)
            inGamePanel.SetActive(false);

        if (opponentSelectPanel != null)
            opponentSelectPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        UpdateJoystickVisibility();
    }

    public void ReloadScene()
    {
        PlayButtonSfx();

        matchEnded = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //SceneManager.LoadScene("vsMetro");
    }

    public void NextScene(string sceneName)
    {
        PlayButtonSfx();

        matchEnded = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void RefreshOpponentSelectState()
    {
        int unlockedIndex = CareerProgression.GetUnlockedIndex();

        ApplyOpponentVisuals(
            0,
            midasButton,
            midasImage,
            null,
            midasOpenSprite,
            midasDefeatedSprite,
            unlockedIndex);

        ApplyOpponentVisuals(
            1,
            metroButton,
            metroImage,
            metroLockedSprite,
            metroOpenSprite,
            metroDefeatedSprite,
            unlockedIndex);

        ApplyOpponentVisuals(
            2,
            twinButton,
            twinImage,
            twinLockedSprite,
            twinOpenSprite,
            twinDefeatedSprite,
            unlockedIndex);

        ApplyOpponentVisuals(
            3,
            zeusButton,
            zeusImage,
            zeusLockedSprite,
            zeusOpenSprite,
            zeusDefeatedSprite,
            unlockedIndex);
    }

    private static void ApplyOpponentVisuals(
        int index,
        Button button,
        Image image,
        Sprite lockedSprite,
        Sprite openSprite,
        Sprite defeatedSprite,
        int unlockedIndex)
    {
        bool isUnlocked = index <= unlockedIndex;
        bool isDefeated = CareerProgression.IsOpponentDefeated(index);

        if (button != null)
        {
            button.interactable = isUnlocked;

            ColorBlock colors = button.colors;
            Color disabled = colors.disabledColor;
            disabled.a = 1f;
            colors.disabledColor = disabled;
            button.colors = colors;
        }

        if (image == null) return;

        if (isDefeated && defeatedSprite != null)
        {
            image.sprite = defeatedSprite;
            return;
        }

        if (isUnlocked && openSprite != null)
        {
            image.sprite = openSprite;
            return;
        }

        if (!isUnlocked && lockedSprite != null)
        {
            image.sprite = lockedSprite;
            return;
        }

        if (openSprite != null)
        {
            image.sprite = openSprite;
        }
    }

    private void UpdateJoystickVisibility()
    {
        if (joystickPanel == null) return;

        bool isOverlayVisible =
            (pausePanel != null && pausePanel.activeInHierarchy) ||
            (winPanel != null && winPanel.activeInHierarchy) ||
            (losePanel != null && losePanel.activeInHierarchy);

        joystickPanel.SetActive(!isOverlayVisible);
    }
}
