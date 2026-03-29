using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject opponentSelectPanel;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void StartGame(GameObject panelToClose, GameObject panelToOpen)
    {
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
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (gamePanel != null)
            gamePanel.SetActive(false);

        if (opponentSelectPanel != null)
            opponentSelectPanel.SetActive(true);
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
