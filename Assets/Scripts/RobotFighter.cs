using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RobotFighter : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 10f;
    // The Inspector slot is gone. We handle it in code now!

    [Header("Animation Settings")]
    [Tooltip("Assign the child GameObject holding the robot's mesh here.")]
    [SerializeField] private Transform visualTransform; 

    [SerializeField] private float commandDistance = 0.12f;
    [SerializeField] private float commandLeanAngle = 20f;
    [SerializeField] private float commandSpeed = 12f;
    [SerializeField] private float commandHopHeight = 0.06f;
    [SerializeField] private float commandTwistAngle = 25f;

    private bool isReacting = false;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Vector3 originalScale;

    // --- SCRIPT-BASED INPUT ---
    private PlayerControls controls;

    private void Awake()
    {
        // Initialize the auto-generated input script
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        // Enable the "Player" action map
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    void Start()
    {
        if (visualTransform == null)
        {
            Debug.LogError("RobotFighter: Please assign a Visual Transform in the Inspector!");
            visualTransform = this.transform; 
        }

        originalLocalPosition = visualTransform.localPosition;
        originalLocalRotation = visualTransform.localRotation;
        originalScale = visualTransform.localScale;
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Read directly from the script reference
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            transform.Translate(moveDirection * (moveSpeed * Time.deltaTime), Space.World);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }

    public void PerformJab() { PerformOne(); }
    public void PerformOne() { TriggerPlaceholderReaction("one", PlaceholderAnimation.One); }
    public void PerformTwo() { TriggerPlaceholderReaction("two", PlaceholderAnimation.Two); }
    public void PerformThree() { TriggerPlaceholderReaction("three", PlaceholderAnimation.Three); }

    private enum PlaceholderAnimation { One, Two, Three }

    private void TriggerPlaceholderReaction(string commandName, PlaceholderAnimation animationType)
    {
        if (isReacting) return;
        StartCoroutine(PlaceholderReactionRoutine(animationType));
    }

    private IEnumerator PlaceholderReactionRoutine(PlaceholderAnimation animationType)
    {
        isReacting = true;

        originalLocalPosition = visualTransform.localPosition;
        originalLocalRotation = visualTransform.localRotation;
        originalScale = visualTransform.localScale;

        if (animationType == PlaceholderAnimation.One) yield return PlayOneAnimation();
        else if (animationType == PlaceholderAnimation.Two) yield return PlayTwoAnimation();
        else yield return PlayThreeAnimation();

        visualTransform.localPosition = originalLocalPosition;
        visualTransform.localRotation = originalLocalRotation;
        visualTransform.localScale = originalScale;
        isReacting = false;
    }

    private IEnumerator PlayOneAnimation()
    {
        Vector3 targetPos = originalLocalPosition + originalLocalRotation * Vector3.forward * commandDistance;
        Quaternion targetRot = originalLocalRotation * Quaternion.Euler(commandLeanAngle, 0f, 0f);
        Vector3 targetScale = originalScale * 1.05f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * commandSpeed;
            visualTransform.localPosition = Vector3.Lerp(originalLocalPosition, targetPos, t);
            visualTransform.localRotation = Quaternion.Lerp(originalLocalRotation, targetRot, t);
            visualTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * commandSpeed;
            visualTransform.localPosition = Vector3.Lerp(targetPos, originalLocalPosition, t);
            visualTransform.localRotation = Quaternion.Lerp(targetRot, originalLocalRotation, t);
            visualTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
    }

    private IEnumerator PlayTwoAnimation()
    {
        float dashSpeed = commandSpeed * 1.35f;
        float sideAmount = commandDistance * 1.25f;

        Vector3 leftPos = originalLocalPosition + originalLocalRotation * Vector3.left * sideAmount;
        Vector3 rightPos = originalLocalPosition + originalLocalRotation * Vector3.right * sideAmount;
        Vector3 crouchScale = new Vector3(originalScale.x * 1.08f, originalScale.y * 0.86f, originalScale.z * 1.08f);

        Quaternion leftTilt = originalLocalRotation * Quaternion.Euler(0f, -commandTwistAngle * 0.7f, commandLeanAngle * 1.2f);
        Quaternion rightTilt = originalLocalRotation * Quaternion.Euler(0f, commandTwistAngle * 0.7f, -commandLeanAngle * 1.2f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * dashSpeed;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.Lerp(originalLocalPosition, leftPos, eased);
            visualTransform.localRotation = Quaternion.Slerp(originalLocalRotation, leftTilt, eased);
            visualTransform.localScale = Vector3.Lerp(originalScale, crouchScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * dashSpeed * 1.1f;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.Lerp(leftPos, rightPos, eased);
            visualTransform.localRotation = Quaternion.Slerp(leftTilt, rightTilt, eased);
            visualTransform.localScale = Vector3.Lerp(crouchScale, crouchScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * dashSpeed;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.Lerp(rightPos, originalLocalPosition, eased);
            visualTransform.localRotation = Quaternion.Slerp(rightTilt, originalLocalRotation, eased);
            visualTransform.localScale = Vector3.Lerp(crouchScale, originalScale, eased);
            yield return null;
        }
    }

    private IEnumerator PlayThreeAnimation()
    {
        float burstSpeed = commandSpeed * 1.1f;

        Vector3 windupPos = originalLocalPosition + originalLocalRotation * Vector3.back * (commandDistance * 0.45f);
        Quaternion windupRot = originalLocalRotation * Quaternion.Euler(commandLeanAngle * 0.75f, -commandTwistAngle * 0.8f, 0f);
        Vector3 windupScale = new Vector3(originalScale.x * 0.9f, originalScale.y * 0.85f, originalScale.z * 1.15f);

        Vector3 launchPos = originalLocalPosition + (originalLocalRotation * Vector3.up) * (commandHopHeight * 2.8f);
        Quaternion launchRot = originalLocalRotation * Quaternion.Euler(-commandLeanAngle * 0.3f, commandTwistAngle * 6f, 0f);
        Vector3 launchScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 1.22f, originalScale.z * 0.92f);

        Vector3 impactScale = new Vector3(originalScale.x * 1.18f, originalScale.y * 0.78f, originalScale.z * 1.18f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * burstSpeed * 0.9f;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.Lerp(originalLocalPosition, windupPos, eased);
            visualTransform.localRotation = Quaternion.Slerp(originalLocalRotation, windupRot, eased);
            visualTransform.localScale = Vector3.Lerp(originalScale, windupScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * burstSpeed * 1.4f;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.Lerp(windupPos, launchPos, eased);
            visualTransform.localRotation = Quaternion.Slerp(windupRot, launchRot, eased);
            visualTransform.localScale = Vector3.Lerp(windupScale, launchScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * burstSpeed;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localPosition = Vector3.Lerp(launchPos, originalLocalPosition, eased);
            visualTransform.localRotation = Quaternion.Slerp(launchRot, originalLocalRotation, eased);
            visualTransform.localScale = Vector3.Lerp(launchScale, impactScale, eased);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * burstSpeed * 1.2f;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            visualTransform.localScale = Vector3.Lerp(impactScale, originalScale, eased);
            yield return null;
        }
    }
}