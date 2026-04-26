using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class EnemyAIController : MonoBehaviour
{
    public event System.Action Died;

    [Header("Targeting")]
    [SerializeField] private Transform target;
    public float rotationSmoothSpeed = 8f;

    [Header("AI Realism")]
    public float trackingSmoothTime = 0.25f;
    private Vector3 perceivedTargetPos;
    private Vector3 perceptionVelocity;

    [Header("Movement Settings")]
    public float moveSpeed = 2.5f; 
    public float gravity = 9.81f;
    public float optimalDistance = 1.0f; 
    public float distanceThreshold = 0.15f; 

    [Header("Boxing Rhythm")]
    public float stepFrequency = 0.85f; 
    public float stepDuration = 0.35f;  

    [Header("Combat AI")]
    public float minAttackInterval = 2f;
    public float maxAttackInterval = 4f;
    public float blockChance = 0.25f; // Chance to block an incoming attack
    
    [Header("Combat Detection")]
    public float hitRange = 2.0f;
    public float hitAngle = 90f;
    public float hitDelay = 0.4f;

    [Header("Health & Damage")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float jabDamage = 8f;
    [SerializeField] private float hookDamage = 12f;
    [SerializeField] private float uppercutDamage = 18f;
    [SerializeField] private float crossDamage = 10f;
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
    private Vector3 verticalVelocity;
    private Vector3 moveVelocity;
    
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

    private float attackTimer = 0f;
    private float nextAttackTime;
    private float sideStepChance = 0.4f;
    private float sideStepDir = 1f;
    private bool playerWasAttacking = false;
    
    // FIX 1: Lock variable to prevent rapid-fire direction flipping
    private bool hasRolledSideStep = false; 

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
        
        SetNextAttackTime();

        if (target != null)
        {
            perceivedTargetPos = target.position;
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (target == null || !target.gameObject.scene.IsValid())
        {
            FindPlayer();
            return;
        }

        perceivedTargetPos = Vector3.SmoothDamp(perceivedTargetPos, target.position, ref perceptionVelocity, trackingSmoothTime);
        moveVelocity = Vector3.zero;

        UpdatePulse();
        AlwaysFaceTarget();
        ApplyGravity();
        HandleAIBehavior();
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

        bool needsRotate = false;
        if (target != null)
        {
            Vector3 posFlat = transform.position; posFlat.y = 0;
            Vector3 targetFlat = perceivedTargetPos; targetFlat.y = 0;
            Vector3 dir = (targetFlat - posFlat);
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                needsRotate = Quaternion.Angle(transform.rotation, targetRot) > 5f;
            }
        }

        bool needsMove = true; 

        if (needsMove || needsRotate)
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

    private void FindPlayer()
    {
        var players = Object.FindObjectsByType<RobotController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.gameObject.scene.IsValid())
            {
                target = p.transform;
                perceivedTargetPos = target.position; 
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

        Vector3 direction = (perceivedTargetPos - transform.position);
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

    private void HandleAIBehavior()
    {
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

        // --- Defensive Reaction ---
        if (target != null)
        {
            Animator targetAnimator = target.GetComponent<Animator>();
            if (targetAnimator != null)
            {
                var targetState = targetAnimator.GetCurrentAnimatorStateInfo(0);
                bool isPlayerAttacking = IsAttackState(targetState);

                if (isPlayerAttacking && !playerWasAttacking)
                {
                    float distToPlayer = Vector3.Distance(transform.position, target.position);
                    if (distToPlayer < (optimalDistance + distanceThreshold) * 1.5f)
                    {
                        if (Random.value < blockChance)
                        {
                            TriggerBlock();
                            playerWasAttacking = true; // Set to true to avoid double roll this attack
                            return;
                        }
                    }
                }
                playerWasAttacking = isPlayerAttacking;
            }
        }

        float scale = transform.localScale.y;
        float maxAttackDist = (optimalDistance + distanceThreshold) * scale;
        float minAttackDist = (optimalDistance - distanceThreshold) * scale;

        Vector3 posFlat = transform.position; posFlat.y = 0;
        Vector3 targetFlat = perceivedTargetPos; targetFlat.y = 0;
        float distance = Vector3.Distance(posFlat, targetFlat);
        
        attackTimer += Time.deltaTime;
        if (attackTimer >= nextAttackTime && distance <= maxAttackDist)
        {
            if (PerformRandomAttack())
            {
                attackTimer = 0;
                SetNextAttackTime();
                moveVelocity = Vector3.zero;
                return;
            }
        }

        Vector3 moveDir = Vector3.zero;

        // FIX 1: Lock the random roll so it only triggers ONCE at the start of a cycle
        float cycleTime = (stepTimer * stepFrequency) % 1f;
        if (cycleTime < 0.05f) 
        {
            if (!hasRolledSideStep)
            {
                if (Random.value < sideStepChance) 
                {
                    sideStepDir = (Random.value > 0.5f) ? 1f : -1f;
                }
                hasRolledSideStep = true;
            }
        }
        else
        {
            hasRolledSideStep = false; // Reset the lock once we are past the 0.05f window
        }

        if (distance > maxAttackDist)
        {
            moveDir = (targetFlat - posFlat).normalized;
        }
        else if (distance < minAttackDist)
        {
            moveDir = ((posFlat - targetFlat).normalized + (transform.right * sideStepDir * 0.35f)).normalized;
        }
        else
        {
            moveDir = transform.right * sideStepDir;
        }

        if (currentPulse > 0.01f && moveDir != Vector3.zero)
        {
            moveVelocity = moveDir.normalized * (moveSpeed * currentPulse);
        }

        // FIX 2: Smoothly Lerp animator values to prevent snappy leg glitches
        if (animator != null)
        {
            Vector3 localMove = moveDir != Vector3.zero ? transform.InverseTransformDirection(moveDir.normalized) : Vector3.zero;
            
            float targetX = 0f;
            if (Mathf.Abs(localMove.x) > 0.1f) targetX = Mathf.Sign(localMove.x); 
            else if (Mathf.Abs(pivotContribution) > 0.01f) targetX = pivotContribution * 0.5f; 
            
            float animIntensity = currentPulse; // Removed the forced 0.5f clamp so feet properly plant during rest

            float finalMoveX = targetX * animIntensity;
            float finalMoveZ = localMove.z * animIntensity;

            // Lerp the blend tree to smooth out sudden directional changes
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

    private void SetNextAttackTime() => nextAttackTime = Random.Range(minAttackInterval, maxAttackInterval);

    private bool IsAttackState(AnimatorStateInfo state)
    {
        return state.IsName("Jab") || state.IsName("Hook") || state.IsName("Uppercut") || state.IsName("Cross");
    }

    private bool PerformRandomAttack()
    {
        int rand = Random.Range(0, 5);
        switch (rand)
        {
            case 0: return TriggerJab();
            case 1: return TriggerCross();
            case 2: return TriggerHook();
            case 3: return TriggerUppercut();
            default: return TriggerBlock();
        };
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
                var player = target.GetComponent<RobotController>();
                if (player != null)
                {
                    player.TakeHit(pendingDamage);
                    Debug.Log($"<color=green>Enemy Hit SUCCESS:</color> Player at {dist:F2}m, Angle: {angle:F1}°, Damage: {pendingDamage}");
                }
            }
            else
            {
                Debug.Log($"<color=yellow>Enemy Hit MISSED (Angle):</color> Angle {angle:F1}° > {hitAngle}°");
            }
        }
        else
        {
            Debug.Log($"<color=red>Enemy Hit MISSED (Range):</color> Distance {dist:F2}m > {adjustedHitRange:F2}m");
        }
    }

    public void TakeHit(float damage)
    {
        if (isDead || animator == null) return;

        // Check if we are blocking
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Block"))
        {
            Debug.Log("Enemy blocked the hit!");
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        Debug.Log($"Enemy took {damage} damage! Remaining HP: {currentHealth}");

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
