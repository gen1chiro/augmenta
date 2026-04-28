using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class PanelRegistrar : MonoBehaviour
{
    [Header("Scene Panel References")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject joystickPanel;
    
    // Register all panels for each scene
    void Start()
    {
        UIManager globalManager = UIManager.Instance;

        if (globalManager == null)
        {
            Debug.LogError("[UIRegistrar] No UIManager found in the scene to register to!");
            return;
        }

        globalManager.RegisterGamePanels(winPanel, losePanel, pausePanel, joystickPanel);

        BindButton(pausePanel, "Buttons/Resume", globalManager.HidePausePanel);
        BindButton(pausePanel, "Buttons/Quit", globalManager.ReturnToMainMenu);
        
        BindButton(winPanel, "Buttons/Continue", () => globalManager.NextScene("vsMetro"));
        BindButton(winPanel, "Buttons/Quit", globalManager.ReturnToMainMenu);

        BindButton(losePanel, "Buttons/Restart", globalManager.ReloadScene);
        BindButton(losePanel, "Buttons/Quit", globalManager.ReturnToMainMenu);

        GameObject hud = GameObject.Find("HUD");
        if (hud == null)
        {
            Debug.LogWarning("HUD GameObject not found.");
            return;
        }

        BindButton(hud, "Pause/Button", globalManager.ShowPausePanel);
    }

    // Mount Buttons in Pause, Win, and Lose panels
    private void BindButton(GameObject panel, string hierarchyPath, UnityAction action)
    {
        if (panel == null) return;

        Button btn = panel.transform.Find(hierarchyPath)?.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning($"[PanelRegistrar] Could not find button at {panel.name}/{hierarchyPath}");
            return;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }
}
