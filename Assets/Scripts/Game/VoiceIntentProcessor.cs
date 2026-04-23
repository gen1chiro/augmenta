using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Json;
using System;

public class VoiceIntentProcessor : MonoBehaviour
{
    [Header("Voice Settings")]
    [SerializeField] private VoiceService voiceService;
    
    private RobotController robot;
    private bool isActivated = false;

    private void OnEnable()
    {
        if (voiceService == null) voiceService = FindFirstObjectByType<VoiceService>();
        
        if (voiceService != null)
        {
            voiceService.VoiceEvents.OnResponse.AddListener(OnVoiceResponse);
            voiceService.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
            voiceService.VoiceEvents.OnError.AddListener(OnVoiceError);
        }
    }

    private void OnDisable()
    {
        if (voiceService != null)
        {
            voiceService.VoiceEvents.OnResponse.RemoveListener(OnVoiceResponse);
            voiceService.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            voiceService.VoiceEvents.OnError.RemoveListener(OnVoiceError);
        }
    }

    public void ActivateVoiceControl(RobotController robotController)
    {
        robot = robotController;
        isActivated = true;
        Debug.Log($"<color=green>Voice Control Activated</color> for robot: {robot.name}");
        StartListening();
    }

    private void StartListening()
    {
        if (!isActivated) return;
        
        if (voiceService != null && !voiceService.IsRequestActive)
        {
            Debug.Log("Voice Service: Starting to listen...");
            voiceService.Activate();
        }
    }

    private void OnStoppedListening()
    {
        // Continuous listening logic: Restart if we are still activated
        if (isActivated)
        {
            Debug.Log("Voice Service: Stopped listening. Restarting...");
            Invoke(nameof(StartListening), 0.5f); // Slight delay for stability
        }
    }

    private void OnVoiceError(string error, string message)
    {
        Debug.LogError($"Voice Error: {error} - {message}");
    }

    private void OnVoiceResponse(WitResponseNode response)
    {
        if (response == null) return;

        // Log the full response for debugging
        Debug.Log($"Voice Response received: {response.ToString()}");

        if (robot == null)
        {
            Debug.LogWarning("Voice Response received but no robot controller is assigned!");
            return;
        }

        // Extract intent
        string intent = response.GetIntentName();
        string text = response["text"]?.Value?.ToLower();

        // --- FALLBACK LOGIC ---
        // If Wit.ai returns the text but failed to map it to an intent, we map it manually.
        if (string.IsNullOrEmpty(intent) && !string.IsNullOrEmpty(text))
        {
            if (text.Contains("one")) intent = "Jab";
            else if (text.Contains("two")) intent = "Uppercut";
            else if (text.Contains("three")) intent = "Hook";
            else if (text.Contains("four")) intent = "Uppercut";
            
            if (!string.IsNullOrEmpty(intent))
            {
                Debug.Log($"<color=orange>Manual Text Fallback Match:</color> '{text}' -> {intent}");
            }
        }
        // ----------------------

        if (string.IsNullOrEmpty(intent))
        {
            Debug.LogWarning($"Voice Response received but no intent detected for text: '{text}'");
            return;
        }

        Debug.Log($"<color=cyan>Final Intent to Process:</color> {intent}");

        // Map intents to robot actions (Case-insensitive comparison)
        switch (intent.ToLower())
        {
            case "jab":
                Debug.Log("Triggering Jab");
                robot.TriggerJab();
                break;
            case "hook":
                Debug.Log("Triggering Hook");
                robot.TriggerHook();
                break;
            case "uppercut":
                Debug.Log("Triggering Uppercut");
                robot.TriggerUppercut();
                break;
            case "cross":
                Debug.Log("Triggering Cross");
                robot.TriggerCross();
                break;
            case "block":
                Debug.Log("Triggering Block");
                robot.TriggerBlock();
                break;
            default:
                Debug.LogWarning($"Intent '{intent}' not mapped to any robot action.");
                break;
        }
    }
}
