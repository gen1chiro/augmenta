using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[System.Serializable]
public class RobotPlacedEvent : UnityEvent<RobotController> {}

public class ARPlacementManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject placementIndicator;
    public GameObject arenaPrefab;
    public GameObject robotPrefab;
    public GameObject enemyPrefab;

    [Header("Events")]
    public RobotPlacedEvent OnRobotPlaced;
    
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private bool arenaPlaced = false;
    private Camera arCamera;

    void Start()
    {
        raycastManager = FindFirstObjectByType<ARRaycastManager>();
        planeManager = FindFirstObjectByType<ARPlaneManager>();
        arCamera = Camera.main;
        
        if (arCamera == null) arCamera = FindFirstObjectByType<Camera>();

        // Explicitly set plane detection to only Horizontal to ignore walls
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        }

        if (placementIndicator != null && !placementIndicator.gameObject.scene.IsValid())
        {
            placementIndicator = Instantiate(placementIndicator);
        }

        if (placementIndicator != null) placementIndicator.SetActive(false);
    }

    void Update()
    {
        if (arenaPlaced) return;

        UpdatePlacementPose();
        UpdatePlacementIndicator();

        if (placementPoseIsValid && WasTapped())
        {
            PlaceObjects();
        }
    }

    private bool WasTapped()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame) return true;
        return false;
    }

    private void UpdatePlacementPose()
    {
        if (raycastManager == null || arCamera == null)
        {
            placementPoseIsValid = false;
            return;
        }

        var screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        var hits = new List<ARRaycastHit>();
        
        // Raycast against planes (using PlaneWithinPolygon for better accuracy)
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            bool foundHorizontal = false;
            foreach (var hit in hits)
            {
                // If planeManager is available, use it to strictly verify alignment
                if (planeManager != null)
                {
                    var plane = planeManager.GetPlane(hit.trackableId);
                    if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp) continue;
                }
                else
                {
                    // Fallback: If planeManager is missing, check the hit's own trackable type 
                    // and ensure it's not a vertical/unclassified hit if possible.
                    // However, it's safer to require the planeManager for alignment verification.
                    continue; 
                }

                placementPose = hit.pose;
                
                var cameraForward = arCamera.transform.forward;
                var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                
                if (cameraBearing.sqrMagnitude > 0.01f)
                {
                    placementPose.rotation = Quaternion.LookRotation(cameraBearing, Vector3.up);
                }

                foundHorizontal = true;
                break;
            }
            placementPoseIsValid = foundHorizontal;
        }
        else
        {
            placementPoseIsValid = false;
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementIndicator == null) return;

        if (placementPoseIsValid)
        {
            if (!placementIndicator.activeSelf) placementIndicator.SetActive(true);
            // Quads are vertical by default (facing +Z). To make it lay flat on a horizontal plane, 
            // we need to rotate it 90 degrees around its X axis so its normal (+Z) points Up (+Y).
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation * Quaternion.Euler(90, 0, 0));
        }
        else
        {
            if (placementIndicator.activeSelf) placementIndicator.SetActive(false);
        }
    }

    private void PlaceObjects()
    {
        if (arenaPrefab == null || robotPrefab == null || enemyPrefab == null) return;

        Instantiate(arenaPrefab, placementPose.position, placementPose.rotation);
        
        // Arena is approx 1m x 1m. Spawn robots in opposite corners.
        float offset = 0.35f; 
        float arenaSize = 1.0f;
        
        Vector3 playerOffset = (placementPose.rotation * new Vector3(-offset, 0, -offset));
        Vector3 enemyOffset = (placementPose.rotation * new Vector3(offset, 0, offset));

        Vector3 playerSpawnPos = placementPose.position + playerOffset + Vector3.up * 0.01f;
        Vector3 enemySpawnPos = placementPose.position + enemyOffset + Vector3.up * 0.01f;

        // Spawn Player
        GameObject spawnedRobot = Instantiate(robotPrefab, playerSpawnPos, placementPose.rotation);
        RobotController playerController = spawnedRobot.GetComponent<RobotController>();
        if (playerController != null) playerController.SetArenaContext(placementPose.position, arenaSize);

        // Spawn Enemy
        GameObject spawnedEnemy = Instantiate(enemyPrefab, enemySpawnPos, placementPose.rotation * Quaternion.Euler(0, 180, 0));
        EnemyAIController enemyAI = spawnedEnemy.GetComponent<EnemyAIController>();
        if (enemyAI != null) enemyAI.SetArenaContext(placementPose.position, arenaSize);

        // -----------------------------

        if (placementIndicator != null) placementIndicator.SetActive(false);
        arenaPlaced = true;
        
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
        }

        if (playerController != null)
        {
            OnRobotPlaced?.Invoke(playerController);
        }
    }
}
