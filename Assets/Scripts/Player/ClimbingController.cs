using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ClimbingController : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;


    [Header("Climbing Settings")]
    [Tooltip("The exact tag your vine colliders use.")]
    public string vineTag = "Vine";

    [Tooltip("Vertical speed when climbing up/down the vine.")]
    public float climbSpeed = 3f;

    [Tooltip("Parameter name for the Animator float to drive climb animation speed.")]
    public string animClimbSpeed = "ClimbSpeed";

    [Tooltip("Animator boolean parameter name for being in the climbing state.")]
    public string animIsClimbingBool = "isClimbing";

    [Tooltip("Time in seconds to smooth the climbing animation speed changes.")]
    public float animDampTime = 0.1f; 

    [Tooltip("Speed at which the player rotates around the vine.")]
    public float rotationSpeed = 90f; // Degrees per second

    [Tooltip("Distance to maintain from the vine center when climbing/orbiting.")]
    public float orbitRadius = 0.15f; // Fallback orbit distance if no CapsuleCollider found

    private bool isClimbing = false;
    private Collider currentVine;

    void Start()
    {
        if (animator == null)
            Debug.LogError("VineClimbingController: Missing Animator on Player.");
        if (playerController == null)
            Debug.LogError("VineClimbingController: Missing PlayerController on Player.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(vineTag))
        {
            currentVine = other;
            StartClimbing();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == currentVine)
            StopClimbing();
    }

    void Update()
    {
        if (!isClimbing) return;

        HandleClimbing();
    }

    private void StartClimbing()
    {
        isClimbing = true;
        playerController.enabled = false;                    // disable normal movement
        AlignWithVine();                                    // snap to vine center & orient
        
        // Offset the player by orbitRadius after aligning and orienting
        // transform.forward points towards the vine after AlignWithVine
        transform.position -= transform.forward * GetVineRadius();

        animator.SetBool(animIsClimbingBool, true);
        Debug.Log("Climbing animation started (isClimbing = true)");
        animator.SetFloat(animClimbSpeed, 0f); 
    }

    private void StopClimbing()
    {
        isClimbing = false;
        animator.SetFloat(animClimbSpeed, 0f); // Ensure animation stops
        animator.SetBool(animIsClimbingBool, false);
        Debug.Log("Climbing animation stopped (isClimbing = false)");
        playerController.enabled = true;                     // restore control
    }

    private void AlignWithVine()
    {
        // Lock player XZ to the vine's local position
        Vector3 p = transform.position;
        p.x = currentVine.transform.position.x;
        p.z = currentVine.transform.position.z;
        transform.position = p;

        // Initially orient player to face the vine
        Vector3 directionToVine = (currentVine.transform.position - transform.position).normalized;
        directionToVine.y = 0; // Keep horizontal
        if (directionToVine != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToVine);
        }
    }

    private void HandleClimbing()
    {
        float v_input = Input.GetAxis("Vertical");
        float h_input = Input.GetAxis("Horizontal");
        Vector3 vinePos = currentVine.transform.position;

        // --- Horizontal Orbiting ---
        if (Mathf.Abs(h_input) > 0.05f)
        {
            Debug.Log("h_input: " + h_input + "Rotating around vine");
            // Rotate the player object around the vine's Y-axis
            transform.RotateAround(vinePos, Vector3.up, -h_input * rotationSpeed * Time.deltaTime);
        }

        // --- Enforce Orbit Radius ---
        // Calculate target position on the orbit circle at current player height
        Vector3 dirFromVineToPlayer = transform.position - vinePos;
        dirFromVineToPlayer.y = 0; // Project to XZ plane
        
        // Ensure dirFromVineToPlayer is not zero vector before normalizing
        if (dirFromVineToPlayer.sqrMagnitude < 0.0001f)
        {
            // If player is too close to center (e.g. at start or due to an issue), pick a default direction.
            // This uses the player's forward, assuming it's somewhat aligned from AlignWithVine or previous frames.
            dirFromVineToPlayer = transform.forward; 
            dirFromVineToPlayer.y = 0;
            if (dirFromVineToPlayer.sqrMagnitude < 0.0001f) // Still zero? Use world X axis
            {
                dirFromVineToPlayer = Vector3.right;
            }
        }
        
        Vector3 targetPositionOnOrbit = vinePos + dirFromVineToPlayer.normalized * GetVineRadius();
        targetPositionOnOrbit.y = transform.position.y; // Maintain current Y

        // Calculate delta to move to this target position to correct orbit
        Vector3 correctionMovement = targetPositionOnOrbit - transform.position;

        // --- Vertical Climbing Movement ---
        Vector3 verticalMovement = Vector3.up * v_input * climbSpeed * Time.deltaTime;

        // --- Combine Movements & Apply ---
        Vector3 totalMovement = correctionMovement + verticalMovement;
        if (totalMovement.sqrMagnitude > 0.00001f) // Avoid tiny moves if already in place
        {
            characterController.Move(totalMovement);
        }
        
        // --- Orientation: Always face the vine (after all position changes) ---
        Vector3 directionToVineCenter = vinePos - transform.position;
        directionToVineCenter.y = 0;
        if (directionToVineCenter.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(directionToVineCenter.normalized);
        }

        // --- Animation ---
        animator.SetFloat(animClimbSpeed, v_input, animDampTime, Time.deltaTime);
        Debug.Log($"Climbing animation target speed: {v_input}");
        Debug.Log("Update: isClimbing = " + isClimbing); // This log was added by user

        // --- Jump ---
        if (Input.GetButtonDown("Jump"))
        {
            StopClimbing(); 

            float jumpHeight = playerController.jumpHeight;
            float gravityValue = playerController.gravity; 
            float initialUpwardVelocity = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            Vector3 jumpDirectionAwayFromVine = -transform.forward;
            playerController.InitiateExternalJump(jumpDirectionAwayFromVine, initialUpwardVelocity);
        }
    }

    // Get the dynamic orbit radius from the vine's CapsuleCollider
    private float GetVineRadius()
    {
        if (currentVine != null)
        {
            CapsuleCollider capsule = currentVine.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                // Subtract offset from capsule radius to keep player INSIDE the trigger collider
                return Mathf.Max(0.05f, capsule.radius - 0.01f);
            }
        }
        // Fallback to the inspector value
        return orbitRadius;
    }
}
