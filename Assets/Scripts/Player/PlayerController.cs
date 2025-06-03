using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speed")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float rotationSpeed = 10f;
    public float mouseSensitivity = 1f;


    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashTime = 0.2f;
    public float dashCooldown = 2f;
    public Slider dashCooldownSlider;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    // Internals
    private CharacterController characterController;
    private Vector3 velocity;
    private float currentSpeed;
    private bool isGrounded;
    private Vector3 externalVelocity = Vector3.zero;

    private bool isDashing = false;
    private float lastDashTime = -Mathf.Infinity;

    // Properties
    public bool isMoving { get; private set; }
    public Vector2 CurrentInput { get; private set; }
    public bool IsGrounded { get; private set; }
    private bool isInvulnerable = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize dash UI                                  
        if (dashCooldownSlider != null)
        {
            dashCooldownSlider.maxValue = dashCooldown;
            dashCooldownSlider.value = dashCooldown;
        }
    }

    void Update()
    {
        // Logs removed as requested
        HandleDashInput();
        UpdateCooldownSlider();

        if (!isDashing)
        {
            HandleMovement();
        }
        UpdateAnimator();
    }

    void HandleMovement()
    {
        bool previouslyGrounded = isGrounded; // Store previous state
        isGrounded = characterController.isGrounded;

        // If we just landed, reset y velocity and clear jump animation
        if (!previouslyGrounded && isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Apply a small downward force to stick to ground
            animator?.SetBool("isJumping", false);
        }

        // If we are grounded and not trying to jump up (i.e., y velocity is not positive)
        // Also, ensure external velocity isn't pushing us up significantly
        if (isGrounded && velocity.y < 0.1f && externalVelocity.y <= 0.1f) 
        {
            velocity.y = -2f; // Keep applying small downward force
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = inputDirection.magnitude > 0.1f;

        Vector3 worldMoveDirection = Vector3.zero; // This will be the direction player faces and moves

        if (isMoving)
        {
            // Determine world space movement direction based on camera's orientation
            worldMoveDirection = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * inputDirection;
            
            // Rotate player to face the movement direction
            if (worldMoveDirection.sqrMagnitude > 0.01f) // Ensure there's a valid direction
            {
                Quaternion targetRotation = Quaternion.LookRotation(worldMoveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Handle jumping
        // Ensure characterController.isGrounded is true for the current frame before allowing jump
        if (Input.GetButtonDown("Jump") && characterController.isGrounded) 
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator?.SetBool("isJumping", true);
            isGrounded = false; // Immediately set isGrounded to false after initiating a jump
            externalVelocity.y = 0f; // Nullify any external downward force when initiating a jump from ground
        }

        // Apply gravity
        if (!isGrounded) // Only apply gravity if not grounded (or if we just jumped)
        {
           velocity.y += gravity * Time.deltaTime;
        }

        // Determine current speed for input-based movement
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Calculate final velocity for CharacterController.Move
        Vector3 finalVelocity = worldMoveDirection * (isMoving ? currentSpeed : 0f); // Apply speed only if there's input
        finalVelocity += externalVelocity; // Add external forces like moving platforms
        finalVelocity.y = velocity.y; // Apply gravity and jump velocity

        characterController.Move(finalVelocity * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        float speedPercent = isMoving ? (currentSpeed == sprintSpeed ? 1f : 0.5f) : 0f;
        animator?.SetFloat("Speed", speedPercent, 0.1f, Time.unscaledDeltaTime);
        animator?.SetBool("isGrounded", isGrounded);
        animator?.SetFloat("VerticalSpeed", velocity.y);
        animator?.SetBool("isDashing", isDashing);
    }

    public void SetExternalVelocity(Vector3 platformVelocity)
    {
        externalVelocity = platformVelocity;
    }

    public void SetInvulnerable(bool state) { isInvulnerable = state; }

    public void InitiateExternalJump(Vector3 horizontalDirection, float verticalVelocity)
    {
        animator?.SetBool("isJumping", true);
        isGrounded = false; // Player is airborne after this jump

        // Determine horizontal speed for jumping off (e.g., half of sprintSpeed)
        float horizontalJumpFactor = sprintSpeed * 0.75f;
        velocity = horizontalDirection.normalized * horizontalJumpFactor;
        velocity.y = verticalVelocity;

        externalVelocity = Vector3.zero; // Reset any velocity from moving platforms
    }

    // —— DASH LOGIC START —— //
    void HandleDashInput()
    {
        // Right-click to dash if cooldown has passed
        if (Input.GetKeyDown(KeyCode.C) &&
            !isDashing &&
            Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        isDashing = true;
        UpdateAnimator();
        lastDashTime = Time.time;

        float dashEnd = Time.time + dashTime;
        Vector3 dashDir = transform.forward;

        while (Time.time < dashEnd)
        {
            characterController.Move(dashDir * dashSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        isDashing = false;
    }

    void UpdateCooldownSlider()
    {
        if (dashCooldownSlider == null) return;

        float elapsed = Time.time - lastDashTime;
        dashCooldownSlider.value = Mathf.Clamp(elapsed, 0f, dashCooldown);
    }
    // —— DASH LOGIC END —— //

    // Called by ClimbingController to trigger jump animation
    public void OnExternalJump()
    {
        animator?.SetBool("isJumping", true);
        // We might want to set isGrounded to false here too, 
        // or let the regular isGrounded check handle it.
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        // BoxCollider
        var box = GetComponent<BoxCollider>();
        if (box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        // CharacterController
        var cc = GetComponent<CharacterController>();
        if (cc)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            float radius = cc.radius * 2f; // diameter for drawing spheres
            Vector3 center = cc.center;
            float halfHeight = Mathf.Max(0, cc.height / 2f - cc.radius);
            // bottom sphere
            Gizmos.DrawWireSphere(center - Vector3.up * halfHeight, cc.radius);
            // top sphere
            Gizmos.DrawWireSphere(center + Vector3.up * halfHeight, cc.radius);
            // capsule sides (approximate by cuboid)
            Gizmos.DrawWireCube(center, new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f));
        }
    }

    public void ResetAnimatorToIdle()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", 0f);
        animator.SetBool("isGrounded", true); // Assuming idle state is grounded
        animator.SetFloat("VerticalSpeed", 0f);
        animator.SetBool("isDashing", false);
        animator.SetBool("isJumping", false);
        // Add any other animator parameters that need resetting to their default/idle state
    }
}
