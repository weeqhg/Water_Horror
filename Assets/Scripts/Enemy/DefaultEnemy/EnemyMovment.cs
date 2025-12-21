using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class FishMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Depth Settings")]
    [SerializeField] private float minDepth = -10f;
    [SerializeField] private float maxDepth = -2f;
    [SerializeField] private float depthChangeInterval = 3f;

    [Header("AI Settings")]
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackDuration = 2f;
    [SerializeField] private float fleeDuration = 5f;
    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private float circleSpeed = 1f;
    [SerializeField] private float attackChance = 0.3f;
    [SerializeField] private float attackCheckInterval = 1f;

    [Header("Physics Settings")]
    [SerializeField] private float movementForce = 10f;
    [SerializeField] private float attackMovementForce = 11f;
    [SerializeField] private float drag = 1f;
    [SerializeField] private float maxVerticalSpeed = 3f;
    [SerializeField] private float depthResponseSpeed = 2f;
    [SerializeField] private float verticalDrag = 2f;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 5f;
    [SerializeField] private float groundAvoidanceForce = 5f;
    [SerializeField] private float groundCheckInterval = 0.1f;

    [Header("References")]
    [SerializeField] private Animator animator;

    // Components
    private AddPenalty addPenalty;
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private Transform player;

    // State Management
    private FishState currentState = FishState.Patrol;
    private FishState lastState;

    // Timers
    private float attackCheckTimer = 0f;
    private float attackStartTime = 0f;
    private float fleeStartTime = 0f;
    private float lastDepthChangeTime = 0f;
    private float lastGroundCheckTime = 0f;

    // Movement variables
    private float currentPatrolDepth;
    private float depthAcceleration;
    private float circleDirection = 1f;
    private float circleAngle;
    private float currentDistanceToPlayer;
    private Vector3 chasePos;

    // Constants
    private const float STOPPING_DISTANCE_THRESHOLD = 1f;
    private const float MIN_CIRCLE_RADIUS_MULTIPLIER = 0.5f;
    private const float MAX_CIRCLE_RADIUS_MULTIPLIER = 1f;
    private const float DIRECTION_CHANGE_CHANCE = 0.1f;
    private const float ATTACK_DEPTH_OFFSET = 0.2f;
    private const float BODY_TILT_MULTIPLIER = 10f;
    private const float MAX_BODY_TILT_ANGLE = 30f;
    private const float DEPTH_ERROR_MULTIPLIER = 5f;
    private const float SPHERE_CAST_RADIUS = 1f;

    private enum FishState
    {
        Patrol,
        Chase,
        Attack,
        Flee
    }

    #region Unity Lifecycle
    public override void OnNetworkSpawn()
    {
        InitializeComponents();
    }

    private void Update()
    {
        if (!IsServer) return;

        UpdateState();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        CheckGroundDistance();
        HandleHorizontalMovement();
        HandleVerticalMovement();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        addPenalty = GetComponent<AddPenalty>();

        if (IsServer)
        {
            ConfigurePhysics();
            ConfigureNavigation();
        }
        else
        {
            ConfigureClient();
        }
    }

    private void ConfigurePhysics()
    {
        if (rb == null) return;

        rb.useGravity = false;
        rb.drag = drag;
        rb.angularDrag = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void ConfigureNavigation()
    {
        if (navAgent == null) return;

        navAgent.updatePosition = false;
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
    }

    private void ConfigureClient()
    {
        if (navAgent != null)
            navAgent.enabled = false;

        if (rb != null)
            rb.isKinematic = true;
    }
    #endregion

    #region State Management
    private void UpdateState()
    {
        switch (currentState)
        {
            case FishState.Patrol:
                UpdatePatrolState();
                break;
            case FishState.Chase:
                UpdateChaseState();
                break;
            case FishState.Attack:
                UpdateAttackState();
                break;
            case FishState.Flee:
                UpdateFleeState();
                break;
        }
    }

    private void UpdateAnimation()
    {
        if (currentState == lastState) return;

        switch (currentState)
        {
            case FishState.Patrol:
            case FishState.Chase:
            case FishState.Flee:
                animator.SetBool("isAttack", false);
                break;
            case FishState.Attack:
                animator.SetBool("isAttack", true);
                break;
        }

        lastState = currentState;
    }
    #endregion

    #region Patrol State
    private void UpdatePatrolState()
    {
        if (!ShouldFindNewPatrolPoint()) return;

        FindRandomPatrolPoint();
    }

    private bool ShouldFindNewPatrolPoint()
    {
        return !navAgent.hasPath || navAgent.remainingDistance < STOPPING_DISTANCE_THRESHOLD;
    }

    private void FindRandomPatrolPoint()
    {
        Vector3 randomPoint = GenerateRandomPointInRadius(patrolRadius);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
        }
    }

    private Vector3 GenerateRandomPointInRadius(float radius)
    {
        Vector3 randomSphere = Random.insideUnitSphere * radius;
        return transform.position + new Vector3(randomSphere.x, 0f, randomSphere.z);
    }
    #endregion

    #region Chase State
    private void UpdateChaseState()
    {
        if (player == null)
        {
            SwitchToState(FishState.Patrol);
            return;
        }

        UpdateChaseTimers();
        CalculatePlayerDistance();
        CheckForAttackOpportunity();
        CalculateChaseTargetPosition();
    }

    private void UpdateChaseTimers()
    {
        attackCheckTimer += Time.deltaTime;
    }

    private void CalculatePlayerDistance()
    {
        currentDistanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    private void CheckForAttackOpportunity()
    {
        if (!ShouldCheckForAttack()) return;

        TryChangeDirection();
        TryStartAttack();
        ResetAttackCheckTimer();
    }

    private bool ShouldCheckForAttack()
    {
        return attackCheckTimer >= attackCheckInterval;
    }

    private void TryChangeDirection()
    {
        if (Random.value < DIRECTION_CHANGE_CHANCE)
        {
            circleDirection *= -1f;
        }
    }

    private void TryStartAttack()
    {
        if (Random.value < attackChance)
        {
            SwitchToState(FishState.Attack);
            attackStartTime = Time.time;
        }
    }

    private void ResetAttackCheckTimer()
    {
        attackCheckTimer = 0f;
    }

    private void CalculateChaseTargetPosition()
    {
        float dynamicRadius = CalculateDynamicCircleRadius();
        UpdateCircleAngle();
        chasePos = CalculateCirclePosition(circleAngle, dynamicRadius);
    }

    private float CalculateDynamicCircleRadius()
    {
        float distanceRatio = Mathf.Clamp01(currentDistanceToPlayer / chaseRange);
        return Mathf.Lerp(
            circleRadius * MIN_CIRCLE_RADIUS_MULTIPLIER,
            circleRadius * MAX_CIRCLE_RADIUS_MULTIPLIER,
            distanceRatio
        );
    }

    private void UpdateCircleAngle()
    {
        circleAngle += circleSpeed * circleDirection * Time.deltaTime;
    }

    private Vector3 CalculateCirclePosition(float angle, float radius)
    {
        Vector3 circlePos = new (
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );

        Vector3 horizontalPosition = player.position + circlePos;
        horizontalPosition.y = transform.position.y;

        return horizontalPosition;
    }
    #endregion

    #region Attack State
    private void UpdateAttackState()
    {
        if (player == null)
        {
            SwitchToState(FishState.Patrol);
            return;
        }

        float elapsedAttackTime = Time.time - attackStartTime;

        if (IsPlayerInAttackRange())
        {
            PerformAttack();
            return;
        }

        if (IsAttackDurationExceeded(elapsedAttackTime))
        {
            EndAttack();
        }
    }

    private bool IsPlayerInAttackRange()
    {
        return Vector3.Distance(transform.position, player.position) < attackRange;
    }

    private void PerformAttack()
    {
        OxygenSystem playerOxygen = player.GetComponent<OxygenSystem>();
        if (playerOxygen != null && addPenalty != null)
        {
            addPenalty.AddPenaltyPlayer(playerOxygen);
        }

        SwitchToState(FishState.Flee);
        fleeStartTime = Time.time;
    }

    private bool IsAttackDurationExceeded(float elapsedTime)
    {
        return elapsedTime >= attackDuration;
    }

    private void EndAttack()
    {
        if (IsPlayerInRange(chaseRange))
        {
            SwitchToState(FishState.Chase);
        }
        else
        {
            SwitchToState(FishState.Patrol);
        }
    }
    #endregion

    #region Flee State
    private void UpdateFleeState()
    {
        float elapsedFleeTime = Time.time - fleeStartTime;

        if (IsFleeDurationExceeded(elapsedFleeTime))
        {
            EndFlee();
        }
    }

    private bool IsFleeDurationExceeded(float elapsedTime)
    {
        return elapsedTime >= fleeDuration;
    }

    private void EndFlee()
    {
        if (IsPlayerInRange(chaseRange))
        {
            SwitchToState(FishState.Chase);
        }
        else
        {
            SwitchToState(FishState.Patrol);
        }
    }
    #endregion

    #region Movement
    private void HandleHorizontalMovement()
    {
        switch (currentState)
        {
            case FishState.Patrol:
                MoveToPatrolPoint();
                break;
            case FishState.Chase:
                MoveToChasePosition();
                break;
            case FishState.Attack:
                MoveToAttackTarget();
                break;
        }
    }

    private void MoveToPatrolPoint()
    {
        if (!navAgent.hasPath || navAgent.remainingDistance <= navAgent.stoppingDistance)
            return;

        Vector3 nextPathPoint = navAgent.steeringTarget;
        Vector3 direction = (nextPathPoint - transform.position).normalized;
        Vector3 horizontalDirection = new (direction.x, 0f, direction.z);

        MoveInDirection(horizontalDirection, movementForce);
    }

    private void MoveToChasePosition()
    {
        if (player == null || chasePos == Vector3.zero) return;

        Vector3 direction = (chasePos - transform.position).normalized;
        MoveInDirection(direction, movementForce);
    }

    private void MoveToAttackTarget()
    {
        if (player == null) return;

        Vector3 attackDirection = (player.position - transform.position).normalized;
        MoveInDirection(attackDirection, attackMovementForce);
    }

    private void MoveInDirection(Vector3 direction, float force)
    {
        if (direction == Vector3.zero) return;

        ApplyMovementForce(direction, force);
        RotateTowardsDirection(direction);
        SyncNavigationWithPhysics();
    }

    private void ApplyMovementForce(Vector3 direction, float force)
    {
        Vector3 forceVector = direction * (force * 1000) * Time.fixedDeltaTime;
        rb.AddForce(forceVector, ForceMode.Force);
    }

    private void RotateTowardsDirection(Vector3 direction)
    {
        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );
    }

    private void SyncNavigationWithPhysics()
    {
        navAgent.nextPosition = transform.position;
    }
    #endregion

    #region Vertical Movement
    private void HandleVerticalMovement()
    {
        float desiredDepth = CalculateDesiredDepth();
        ApplyVerticalForces(desiredDepth);
        UpdateBodyTilt();
    }

    private float CalculateDesiredDepth()
    {
        return currentState switch
        {
            FishState.Attack => CalculateAttackDepth(),
            FishState.Chase => CalculatePatrolDepth(),
            FishState.Patrol => CalculatePatrolDepth(),
            FishState.Flee => maxDepth,
            _ => transform.position.y,
        };
    }

    private float CalculateAttackDepth()
    {
        if (player == null) return transform.position.y;

        return Mathf.Clamp(player.position.y + ATTACK_DEPTH_OFFSET, minDepth, maxDepth);
    }

    private float CalculatePatrolDepth()
    {
        if (ShouldChangePatrolDepth())
        {
            currentPatrolDepth = Random.Range(minDepth, maxDepth);
            lastDepthChangeTime = Time.time;
        }

        return currentPatrolDepth;
    }

    private bool ShouldChangePatrolDepth()
    {
        return Time.time - lastDepthChangeTime > depthChangeInterval;
    }

    private void ApplyVerticalForces(float desiredDepth)
    {
        float predictedDepth = transform.position.y + rb.velocity.y * Time.fixedDeltaTime;
        float depthError = desiredDepth - predictedDepth;

        float targetAcceleration = depthError * depthResponseSpeed;
        depthAcceleration = Mathf.Lerp(depthAcceleration, targetAcceleration, 0.1f);

        float currentVerticalSpeed = rb.velocity.y;
        float speedError = Mathf.Clamp(desiredDepth - transform.position.y, -1f, 1f) * maxVerticalSpeed;

        rb.AddForce(Vector3.up * depthAcceleration, ForceMode.Acceleration);
        rb.AddForce(Vector3.up * (speedError - currentVerticalSpeed) * DEPTH_ERROR_MULTIPLIER, ForceMode.Force);
        rb.AddForce(-rb.velocity.y * verticalDrag * Vector3.up, ForceMode.Force);
    }

    private void UpdateBodyTilt()
    {
        float tiltAngle = -rb.velocity.y * BODY_TILT_MULTIPLIER;
        tiltAngle = Mathf.Clamp(tiltAngle, -MAX_BODY_TILT_ANGLE, MAX_BODY_TILT_ANGLE);

        Quaternion targetTilt = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetTilt, 0.1f);
    }
    #endregion

    #region Ground Detection
    private void CheckGroundDistance()
    {
        if (!ShouldCheckGround()) return;

        PerformGroundCheck();
        lastGroundCheckTime = Time.time;
    }

    private bool ShouldCheckGround()
    {
        return Time.time - lastGroundCheckTime > groundCheckInterval;
    }

    private void PerformGroundCheck()
    {
        if (!Physics.SphereCast(
            transform.position,
            SPHERE_CAST_RADIUS,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundLayer))
            return;

        Vector3 avoidanceForce = Vector3.up * groundAvoidanceForce;
        rb.AddForce(avoidanceForce, ForceMode.Impulse);
    }
    #endregion

    #region Helper Methods
    private void SwitchToState(FishState newState)
    {
        currentState = newState;
    }

    private bool IsPlayerInRange(float range)
    {
        return player != null && Vector3.Distance(transform.position, player.position) <= range;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;

        player = other.transform;
        SwitchToState(FishState.Chase);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer || !other.CompareTag("Player")) return;

        player = null;
        SwitchToState(FishState.Patrol);
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmosSelected()
    {
        DrawDetectionRanges();
        DrawDepthRange();
        DrawChaseVisualization();
        DrawVelocity();
    }

    private void DrawDetectionRanges()
    {
        Gizmos.color = Color.yellow;
        Vector3 groundPos = new (transform.position.x, 0f, transform.position.z);
        Gizmos.DrawWireSphere(groundPos, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void DrawDepthRange()
    {
        Gizmos.color = Color.cyan;
        Vector3 minDepthPos = new(transform.position.x, minDepth, transform.position.z);
        Vector3 maxDepthPos = new(transform.position.x, maxDepth, transform.position.z);
        Gizmos.DrawLine(minDepthPos, maxDepthPos);
    }

    private void DrawChaseVisualization()
    {
        if (player == null || currentState != FishState.Chase) return;

        Gizmos.color = Color.green;
        Vector3 circleCenter = new(player.position.x, transform.position.y, player.position.z);
        Gizmos.DrawWireSphere(circleCenter, circleRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(chasePos, 0.3f);
        Gizmos.DrawLine(transform.position, chasePos);
    }

    private void DrawVelocity()
    {
        if (!IsServer) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, rb.velocity.normalized * 2f);
    }
    #endregion
}