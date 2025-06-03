using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player object the enemy will chase.")]
    public Transform playerTransform;
    [Tooltip("The child GameObject containing the enemy's visual model to be bobbed up and down.")]
    public Transform modelToBob; // Transform of the model to apply bobbing to
    private NavMeshAgent agent;

    [Header("Movement Settings")]
    [Tooltip("Speed of the enemy when roaming.")]
    public float roamSpeed = 3.5f;
    [Tooltip("Speed of the enemy when chasing the player.")]
    public float chaseSpeed = 5f;
    [Tooltip("Acceleration of the NavMeshAgent.")]
    public float acceleration = 8f;
    [Tooltip("Angular speed (turning speed) of the NavMeshAgent.")]
    public float angularSpeed = 120f;

    [Header("Detection & Roaming")]
    [Tooltip("Radius within which the enemy detects and starts chasing the player.")]
    public float detectionRadius = 10f;
    [Tooltip("Radius around its starting point where the enemy will roam.")]
    public float roamRadius = 20f;
    [Tooltip("Minimum time to wait before picking a new roam destination.")]
    public float minRoamWaitTime = 2f;
    [Tooltip("Maximum time to wait before picking a new roam destination.")]
    public float maxRoamWaitTime = 5f;

    private Vector3 roamOrigin;
    private Vector3 currentRoamTarget;
    private float roamWaitTimer;
    private bool isChasing = false;
    private bool isRoamingPointSet = false;

    [Header("Procedural Animation")] 
    [Tooltip("How fast the model bobs up and down.")]
    public float bobbingSpeed = 10f;
    [Tooltip("How high the model bobs.")]
    public float bobbingAmount = 0.05f;

    private Vector3 initialModelLocalPosition; // To store the model's starting local position

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (modelToBob == null)
        {
            Debug.LogWarning("[EnemyAI] Model To Bob is not assigned. Procedural bobbing will not work. Please assign a child Transform that represents the enemy's visual model.", this);
        }
        else
        {
            initialModelLocalPosition = modelToBob.localPosition;
        }

        if (playerTransform == null)
        {
            // Try to find the player by tag if not assigned
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("[EnemyAI] PlayerTransform not set and couldn't find GameObject with tag 'Player'. Enemy will not chase.", this);
            }
        }

        roamOrigin = transform.position;
        agent.speed = roamSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;

        SetNewRoamDestination();
    }

    void Update()
    {
        if (playerTransform == null) return; // Cannot operate without a player target

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (!isChasing)
            {
                Debug.Log("[EnemyAI] Player detected. Starting chase.", this);
            }
            isChasing = true;
        }
        else
        {
            if (isChasing)
            {
                Debug.Log("[EnemyAI] Player lost or out of range. Resuming roam.", this);
                SetNewRoamDestination(); // Pick a new roam point once player is lost
            }
            isChasing = false;
        }

        if (isChasing)
        {
            HandleChase();
        }
        else
        {
            HandleRoam();
        }

        UpdateProceduralBobbing();
    }

    void HandleChase()
    {
        agent.speed = chaseSpeed;
        if (agent.isOnNavMesh) agent.SetDestination(playerTransform.position);
    }

    void HandleRoam()
    {
        agent.speed = roamSpeed;

        if (agent.isOnNavMesh && !agent.pathPending)
        {
            // Check if we've reached the destination or are very close
            if (agent.remainingDistance <= agent.stoppingDistance + 0.1f) // Added a small buffer
            {
                if (isRoamingPointSet) // Ensure we don't repeatedly set new points if stuck
                {
                    // Reached destination, wait then pick a new one
                    roamWaitTimer -= Time.deltaTime;
                    if (roamWaitTimer <= 0)
                    {
                        SetNewRoamDestination();
                    }
                }
            }
            else if (!isRoamingPointSet) // If we haven't set a point (e.g. after chase ends)
            {
                 SetNewRoamDestination();
            }
        }
    }

    void SetNewRoamDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += roamOrigin;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, NavMesh.AllAreas))
        {
            currentRoamTarget = hit.position;
            if(agent.isOnNavMesh) agent.SetDestination(currentRoamTarget);
            roamWaitTimer = Random.Range(minRoamWaitTime, maxRoamWaitTime);
            isRoamingPointSet = true;
            Debug.Log($"[EnemyAI] New roam target set to: {currentRoamTarget}", this);
        }
        else
        {
            Debug.LogWarning("[EnemyAI] Could not find a valid NavMesh point for roaming. Trying again next cycle.", this);
            isRoamingPointSet = false; // Allow trying again soon
            roamWaitTimer = 0.5f; // Short wait before retry
        }
    }

    void UpdateProceduralBobbing() 
    {
        if (modelToBob == null) return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.01f; // A small threshold to detect movement

        if (isMoving)
        {
            // Calculate bobbing offset using a sine wave
            float bobOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmount;
            modelToBob.localPosition = new Vector3(initialModelLocalPosition.x, initialModelLocalPosition.y + bobOffset, initialModelLocalPosition.z);
        }
        else
        {
            // Reset to initial local position when not moving, smoothly or directly
            // For simplicity, direct reset. For smoothness, you could Lerp.
            modelToBob.localPosition = initialModelLocalPosition;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw roam radius from origin
        Gizmos.color = Color.green;
        if(Application.isPlaying) Gizmos.DrawWireSphere(roamOrigin, roamRadius);
        else Gizmos.DrawWireSphere(transform.position, roamRadius); // Show based on current position if not playing

        // Draw current roam target
        if (Application.isPlaying && isRoamingPointSet && !isChasing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, currentRoamTarget);
            Gizmos.DrawSphere(currentRoamTarget, 0.5f);
        }
    }
} 