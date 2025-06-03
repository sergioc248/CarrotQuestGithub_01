using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalePowerupController : MonoBehaviour
{
    [Header("Scale Powerup Settings")]
    public string requiredItemTag = "ScalePowerUp";      // The collectible tag
    public KeyCode activationKey = KeyCode.E;            // Key to trigger powerup
    public Vector3 scaledSize = new Vector3(2f, 2f, 2f);
    public float powerupDuration = 5f;
    public float slowMotionFactor = 0.5f;

    [Header("Destruction Area Effect")]
    [Tooltip("Radius around the impact point to affect other destructibles.")]
    public float areaDestructionRadius = 1.5f;
    [Tooltip("Layer(s) that destructible objects are on.")]
    public LayerMask destructibleLayerMask; // Assign this in the Inspector!

    [Header("References")]
    [Tooltip("Drag in the GameObject that has your CollectibleManagerScript on it.")]
    public GameObject collectibleManagerObject;
    private CollectibleManagerScript inventory;
    private bool isReady = false;

    private Transform playerTransform;
    private PlayerController playerController;
    private CharacterController characterController;

    public bool IsScaledUp { get; private set; }

    void Start()
    {
        if (collectibleManagerObject != null)
            inventory = collectibleManagerObject.GetComponent<CollectibleManagerScript>();
        if (inventory == null)
            Debug.LogError("[ScalePowerupController] No CollectibleManagerScript found.");

        var playerGO = this.gameObject; // This script must be on the Player object
        playerTransform = playerGO.transform;
        playerController = playerGO.GetComponent<PlayerController>();
        characterController = playerGO.GetComponent<CharacterController>();
        if (playerController == null)
            Debug.LogError("[ScalePowerupController] No PlayerController on Player.");
        if (characterController == null)
            Debug.LogError("[ScalePowerupController] No CharacterController on Player (needed for OnControllerColliderHit).");
        
        IsScaledUp = false;
        Debug.Log("[ScalePowerupController] Initialized. IsScaledUp: " + IsScaledUp, gameObject);

        // Check if destructibleLayerMask is unassigned (it defaults to 0 which is 'Nothing')
        if (destructibleLayerMask.value == 0) // LayerMask default is 'Nothing'
        {
            Debug.LogWarning("[ScalePowerupController] Destructible Layer Mask is not set in the Inspector. Area destruction might not find any objects. Please assign the layer(s) your destructible objects are on.", this);
            // Optionally, set it to 'Default' or 'Everything' if you want a fallback, but Inspector assignment is better.
            // destructibleLayerMask = LayerMask.GetMask("Default"); 
        }
    }

    void Update()
    {
        if (!isReady)
        {
            if (inventory != null && inventory.SpecialInventoryContains(requiredItemTag))
                isReady = true;
            else
                return;
        }

        if (Input.GetKeyDown(activationKey))
        {
            if (!IsScaledUp)
            {
                Debug.Log("[ScalePowerupController] Activation key pressed. Attempting to activate.", gameObject);
                if (inventory != null && inventory.RemoveSpecialItem(requiredItemTag))
                {
                    isReady = false;
                    StartCoroutine(ActivateScalePowerup());
                }
                else if (inventory == null)
                {
                    Debug.LogError("[ScalePowerupController] Inventory is null, cannot remove item or activate.", gameObject);
                }
                else
                {
                    Debug.LogWarning("[ScalePowerupController] Failed to remove required item from inventory, cannot activate.", gameObject);
                }
            }
            else
            {
                Debug.Log("[ScalePowerupController] Activation key pressed, but already scaled up.", gameObject);
            }
        }
    }

    private IEnumerator ActivateScalePowerup()
    {
        Debug.Log("[ScalePowerupController] ActivateScalePowerup coroutine START. Setting IsScaledUp = true.", gameObject);
        inventory?.StartPowerUpUIEffect(requiredItemTag);
        IsScaledUp = true;

        Vector3 originalScale = playerTransform.localScale;
        float origWalk = playerController.walkSpeed;
        float origSprint = playerController.sprintSpeed;

        playerTransform.localScale = scaledSize;
        playerController.walkSpeed = origWalk * slowMotionFactor;
        playerController.sprintSpeed = origSprint * slowMotionFactor;
        playerController.SetInvulnerable(true);

        yield return new WaitForSeconds(powerupDuration);

        playerTransform.localScale = originalScale;
        playerController.walkSpeed = origWalk;
        playerController.sprintSpeed = origSprint;
        playerController.SetInvulnerable(false);
        
        IsScaledUp = false;
        inventory?.StopPowerUpUIEffect(requiredItemTag);
        Debug.Log("[ScalePowerupController] ActivateScalePowerup coroutine END. IsScaledUp is now: " + IsScaledUp, gameObject);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (IsScaledUp)
        {
            DestructibleByScale directlyHitDestructible = hit.gameObject.GetComponent<DestructibleByScale>();
            if (directlyHitDestructible != null && !directlyHitDestructible.IsBrokenApart)
            {
                Debug.Log($"[ScalePowerupController] Directly hit a destructible object ({hit.gameObject.name}) while scaled up! Calling TriggerDestruction.", gameObject);
                directlyHitDestructible.TriggerDestruction(hit.point, playerTransform.position);
                
                // Now check for nearby destructibles for area effect
                CheckForAreaDamage(hit.point); 
            }
        }
    }

    void CheckForAreaDamage(Vector3 impactPoint)
    {
        Debug.Log($"[ScalePowerupController] Checking for area damage around {impactPoint} with radius {areaDestructionRadius}", gameObject);
        Collider[] nearbyColliders = Physics.OverlapSphere(impactPoint, areaDestructionRadius, destructibleLayerMask);

        if (nearbyColliders.Length == 0)
        {
            Debug.Log("[ScalePowerupController] Area damage found no colliders on the specified layer(s).", gameObject);
        }

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            DestructibleByScale nearbyDestructible = nearbyCollider.GetComponent<DestructibleByScale>();
            if (nearbyDestructible != null && !nearbyDestructible.IsBrokenApart)
            {
                Debug.Log($"[ScalePowerupController] Area damage: Found nearby destructible ({nearbyCollider.gameObject.name}). Triggering destruction.", gameObject);
                // For pieces from area damage, the "hitPoint" is conceptually the center of the AoE impact
                // and playerPosition is still the source of the main force.
                nearbyDestructible.TriggerDestruction(impactPoint, playerTransform.position);
            }
        }
    }
}