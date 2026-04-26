using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class RobotController : MonoBehaviour
{
    public event System.Action Died;

    [Header("Targeting")]
    [SerializeField] private Transform target;
    public float rotationSmoothSpeed = 10f;

    [Header("Movement Settings")]
    public float moveSpeed = 3.5f; 
    public float gravity = 9.81f;

    [Header("Boxing Rhythm")]
    [Range(0.1f, 5f)] public float stepFrequency = 1.0f; 
    [Range(0.1f, 1f)] public float stepDuration = 0.35f;  

    [Header("Combat Detection")]
    public float hitRange = 2.0f;
    public float hitAngle = 90f;
    public float hitDelay = 0.35f; // Player might be slightly faster

    [Header("Health & Damage")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float jabDamage = 10f;
    [SerializeField] private float hookDamage = 15f;
    [SerializeField] private float uppercutDamage = 20f;
    [SerializeField] private float crossDamage = 12f;
    [SerializeField] private Slider healthSlider;

    private float currentHealth;
    private float pendingDamage;
    private bool isDead = false;
    
    [Header("Arena Bounds")]
    private Vector3 arenaCenter;
    private float arenaHalfSize = 0.45f;
    private bool hasArenaBounds = false;

    private Animator animator;
    private CharacterController characterController;
    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Vector3 verticalVelocity;
    private Vector3 moveVelocity;
    private Camera mainCamera;
    
    private float stepTimer = 0f;
    private float currentPulse = 0f;
    private float pivotContribution = 0f;

    public bool IsBusy()
    {
        if (animator == null) return false;
        
        var currentState = animator.GetCurrentAnimatorStateInfo(0);
        var nextState = animator.GetNextAnimatorStateInfo(0);
        bool inTransition = animator.IsInTransition(0);

        bool isCurrentCombat = IsCombatState(currentState);
        bool isNextCombat = IsCombatState(nextState);

        if (isCurrentCombat && (currentState.normalizedTime < 1.0f || inTransition)) return true;
        if (inTransition && isNextCombat) return true;

        return false;
    }

    private bool IsCombatState(AnimatorStateInfo state)
    {
        return state.IsName("Jab") || state.IsName("Hook") || state.IsName("Uppercut") || 
               state.IsName("Cross") || state.IsName("Block") || state.IsName("Hit") || 
               state.IsName("Knockout");
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = true;
        }

        playerControls = new PlayerControls();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
        playerControls.Player.Move.performed += OnMovePerformed;
        playerControls.Player.Move.canceled += OnMoveCanceled;
    }

    private void OnDisable()
    {
        playerControls.Player.Move.performed -= OnMovePerformed;
        playerControls.Player.Move.canceled -= OnMoveCanceled;
        playerControls.Player.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context) => moveInput = context.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext context) => moveInput = Vector2.zero;

    private void Update()
    {
        if (isDead) return;

        if (target == null || !target.gameObject.scene.IsValid()) FindEnemy();
        
        moveVelocity = Vector3.zero;
        
        UpdatePulse();
        AlwaysFaceTarget();
        ApplyGravity();
        CalculateMovement();
    }

    private void OnAnimatorMove()
    {
        if (characterController == null || animator == null) return;

        Vector3 finalMove;
        
        if (IsBusy())
        {
            // Use root motion (animation physics) for combat states
            finalMove = animator.deltaPosition;
            transform.rotation *= animator.deltaRotation;
        }
        else
        {
            // During locomotion, use manual pulse-based movement
            finalMove = moveVelocity * Time.deltaTime;
        }

        // Apply vertical velocity (gravity)
        finalMove.y += verticalVelocity.y * Time.deltaTime;
        
        characterController.Move(finalMove);
        RestrictToArena();
    }

    private void UpdatePulse()
    {
        if (currentPulse > 0.001f)
        {
            stepTimer += Time.deltaTime;
            float cycleTime = (stepTimer * stepFrequency) % 1f;

            float rawPulse = (cycleTime < stepDuration) ? Mathf.Sin((cycleTime / stepDuration) * Mathf.PI) : 0f;
            currentPulse = Mathf.Pow(rawPulse, 0.7f);

            if (currentPulse <= 0.001f)
            {
                currentPulse = 0f;
                stepTimer = 0f;
            }
            return;
        }

        if (IsBusy())
        {
            currentPulse = 0f;
            stepTimer = 0f;
            return;
        }

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        bool isRotating = false;

        if (target != null)
        {
            Vector3 dir = (target.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                isRotating = Quaternion.Angle(transform.rotation, targetRot) > 5f;
            }
        }

        if (isMoving || isRotating)
        {
            stepTimer += Time.deltaTime;
            float cycleTime = (stepTimer * stepFrequency) % 1f;
            
            float rawPulse = (cycleTime < stepDuration) ? Mathf.Sin((cycleTime / stepDuration) * Mathf.PI) : 0f;
            currentPulse = Mathf.Pow(rawPulse, 0.7f);
        }
        else
        {
            stepTimer = 0f;
            currentPulse = 0f;
        }
    }

    public void SetArenaContext(Vector3 center, float size)
    {
        arenaCenter = center;
        float padding = 0.1f * transform.localScale.y;
        arenaHalfSize = (size / 2f) - padding;
        hasArenaBounds = true;
    }

    private void FindEnemy()
    {
        var enemies = Object.FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.gameObject.scene.IsValid())
            {
                target = enemy.transform;
                break;
            }
        }
    }

    private void AlwaysFaceTarget()
    {
        if (target == null || IsBusy()) 
        {
            pivotContribution = 0f;
            return;
        }

        Vector3 direction = (target.position - transform.position);
        direction.y = 0; 
        
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
            
            float angleDiff = Vector3.Angle(transform.forward, direction);

            if (angleDiff > 2f)
            {
                float signedAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
                pivotContribution = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
            }
            else
            {
                pivotContribution = 0f;
            }
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; 
        }
        else
        {
            verticalVelocity.y -= gravity * Time.deltaTime;
        }
    }

    private void CalculateMovement()
    {
        // Smoothly return to idle if an attack interrupts movement
        if (IsBusy())
        {
            moveVelocity = Vector3.zero;
            if (animator != null)
            {
                animator.SetFloat("MoveX", Mathf.Lerp(animator.GetFloat("MoveX"), 0f, Time.deltaTime * 15f));
                animator.SetFloat("MoveZ", Mathf.Lerp(animator.GetFloat("MoveZ"), 0f, Time.deltaTime * 15f));
            }
            return;
        }

        Vector3 moveDirection = Vector3.zero;
        bool isMovingIntent = moveInput.sqrMagnitude > 0.01f;

        if (isMovingIntent && mainCamera != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
            moveDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
            
            float pulseSpeed = moveSpeed * currentPulse * 1.5f; 
            moveVelocity = moveDirection * pulseSpeed;
        }

        if (animator != null)
        {
            Vector3 localMove = moveDirection != Vector3.zero ? transform.InverseTransformDirection(moveDirection) : Vector3.zero;
            
            float targetX = 0f;
            if (Mathf.Abs(localMove.x) > 0.1f) targetX = Mathf.Sign(localMove.x);
            else if (Mathf.Abs(pivotContribution) > 0.01f) targetX = pivotContribution * 0.5f;

            bool isActivelyPivoting = Mathf.Abs(pivotContribution) > 0.01f;
            float animIntensity = (isMovingIntent || isActivelyPivoting) ? Mathf.Max(currentPulse, 0.5f) : currentPulse;
            
            float finalMoveX = targetX * animIntensity;
            float finalMoveZ = localMove.z * animIntensity;

            // FIX: Smoothly Lerp the animator parameters to prevent snappy transitions when inputs change rapidly
            animator.SetFloat("MoveX", Mathf.Lerp(animator.GetFloat("MoveX"), finalMoveX, Time.deltaTime * 12f));
            animator.SetFloat("MoveZ", Mathf.Lerp(animator.GetFloat("MoveZ"), finalMoveZ, Time.deltaTime * 12f));
        }
    }

    private void RestrictToArena()
    {
        if (!hasArenaBounds) return;

        Vector3 currentPos = transform.position;
        Vector3 localPos = currentPos - arenaCenter;
        
        float clampedX = Mathf.Clamp(localPos.x, -arenaHalfSize, arenaHalfSize);
        float clampedZ = Mathf.Clamp(localPos.z, -arenaHalfSize, arenaHalfSize);

        float threshold = 0.01f * transform.localScale.y;
        if (Mathf.Abs(localPos.x - clampedX) > threshold || Mathf.Abs(localPos.z - clampedZ) > threshold)
        {
            transform.position = new Vector3(arenaCenter.x + clampedX, currentPos.y, arenaCenter.z + clampedZ);
        }
    }

    public bool TriggerJab() 
    {
        if (SafeTrigger("Jab"))
        {
            pendingDamage = jabDamage;
            return ScheduleHitCheck();
        }
        return false;
    }

    public bool TriggerHook()
    {
        if (SafeTrigger("Hook"))
        {
            pendingDamage = hookDamage;
            return ScheduleHitCheck();
        }
        return false;
    }

    public bool TriggerUppercut()
    {
        if (SafeTrigger("Uppercut"))
        {
            pendingDamage = uppercutDamage;
            return ScheduleHitCheck();
        }
        return false;
    }

    public bool TriggerCross()
    {
        if (SafeTrigger("Cross"))
        {
            pendingDamage = crossDamage;
            return ScheduleHitCheck();
        }
        return false;
    }

    public bool TriggerBlock() => SafeTrigger("Block");
    public bool TriggerHit() => SafeTrigger("Hit", true);
    public bool TriggerKnockout() => SafeTrigger("Knockout", true);

    private bool ScheduleHitCheck()
    {
        Invoke(nameof(CheckForHit), hitDelay);
        return true;
    }

    private void CheckForHit()
    {
        if (target == null) return;

        // Use 2D distance to be more robust against Y-offset in AR
        Vector3 posFlat = transform.position; posFlat.y = 0;
        Vector3 targetFlat = target.position; targetFlat.y = 0;
        
        float dist = Vector3.Distance(posFlat, targetFlat);
        float scale = transform.localScale.y;
        float adjustedHitRange = hitRange * scale;
        
        if (dist <= adjustedHitRange)
        {
            Vector3 dirToTarget = (targetFlat - posFlat).normalized;
            if (dirToTarget == Vector3.zero) dirToTarget = transform.forward;

            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (angle <= hitAngle)
            {
                var enemy = target.GetComponent<EnemyAIController>();
                if (enemy != null)
                {
                    enemy.TakeHit(pendingDamage);
                    Debug.Log($"<color=green>Player Hit SUCCESS:</color> Enemy at {dist:F2}m, Angle: {angle:F1}°, Damage: {pendingDamage}");
                }
            }
            else
            {
                Debug.Log($"<color=yellow>Player Hit MISSED (Angle):</color> Angle {angle:F1}° > {hitAngle}°");
            }
        }
        else
        {
            Debug.Log($"<color=red>Player Hit MISSED (Range):</color> Distance {dist:F2}m > {adjustedHitRange:F2}m");
        }
    }

    public void TakeHit(float damage)
    {
        if (isDead || animator == null) return;

        // Check if we are blocking
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Block"))
        {
            Debug.Log("Player blocked the hit!");
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        Debug.Log($"Player took {damage} damage! Remaining HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            isDead = true;
            TriggerKnockout();
            Died?.Invoke();
        }
        else
        {
            TriggerHit();
        }

        UpdateHealthUI();
    }

    public void SetHealthSlider(Slider slider)
    {
        healthSlider = slider;
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider == null) return;

        float safeMaxHealth = Mathf.Max(1f, maxHealth);
        healthSlider.wholeNumbers = false;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = safeMaxHealth;
        healthSlider.value = Mathf.Clamp(currentHealth, 0f, safeMaxHealth);
    }

    private bool SafeTrigger(string triggerName, bool force = false)
    {
        if (animator == null) return false;
        
        bool busy = IsBusy();
        // Relaxed: Allow attacks even if moving (currentPulse > 0)
        if (!force && busy) return false;

        // If forcing a hit, we want to interrupt current animations
        // but not if we are already in a Hit or Knockout state
        if (force)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Hit") || state.IsName("Knockout")) return false;
        }
        
        animator.ResetTrigger("Jab");
        animator.ResetTrigger("Hook");
        animator.ResetTrigger("Uppercut");
        animator.ResetTrigger("Cross");
        animator.ResetTrigger("Block");
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Knockout");
        
        animator.SetTrigger(triggerName);
        return true;
    }
}
