// CarrotOverflow.cs
using UnityEngine;
using System.Collections;

public class CarrotOverflow : MonoBehaviour
{
    [Header("Original Movement Speed")]
    private PlayerController playerController;

    private float originalWalkSpeed;
    private float originalSprintSpeed;
    private float originalJumpHeight;

    [Header("OverFlow Setting")]
    public KeyCode activationKey = KeyCode.Q; // Public keybind for activation
    private bool isBoostActive = false;

    public float OverFlowTime = 2f;
    public float WalkMultiplayer = 2f;
    public float SprintMultiplayer = 2f;
    public float JumpMultiplayer = 2f;

    [Header("References")]
    [Tooltip("Drag the GameObject with CollectibleManagerScript here.")]
    public CollectibleManagerScript collectibleManager; // Assign in Inspector

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (collectibleManager == null)
        {
            Debug.LogError("CarrotOverflow: CollectibleManagerScript not assigned!");
        }
    }
    void Update()
    {
        bool qPressed = Input.GetKeyDown(activationKey);
        bool conditionsMet = false;
        bool allCarrotsGathered = false;

        if (collectibleManager != null)
        {
            allCarrotsGathered = collectibleManager.AreAllNormalCollectiblesGathered();
            conditionsMet = qPressed && !isBoostActive && allCarrotsGathered;

            if(qPressed) // Log specifically when Q is pressed
            {
                Debug.Log($"[CarrotOverflow] Q pressed. CollectibleManager assigned: {collectibleManager != null}, IsBoostActive: {isBoostActive}, AllCarrotsGathered: {allCarrotsGathered}");
                Debug.Log($"[CarrotOverflow] collectibleManager.itemsCollected: {collectibleManager.itemsCollected}, collectibleManager.totalItemsScene: {collectibleManager.totalItemsScene}"); // Assuming these are public or have getters for debug, if not, this specific line might error or need adjustment
            }
        }
        else if (qPressed) // Log if Q pressed but manager is null
        {
             Debug.Log("[CarrotOverflow] Q pressed, but CollectibleManager is NULL.");
        }

        // Check if collectibleManager is assigned before using it
        if (collectibleManager != null && 
            qPressed && 
            !isBoostActive && 
            allCarrotsGathered)
        {
            Debug.Log("[CarrotOverflow] All conditions MET. Activating boost!");
            StartCoroutine(ActivateBoost());
        }
    }

    IEnumerator ActivateBoost()
    {
        isBoostActive = true;

        // Notify CollectibleManager to start carrot UI effect and reset carrots
        collectibleManager?.ActivateCarrotOverflowVisuals();

        // Guardamos los valores originales
        originalWalkSpeed = playerController.walkSpeed;
        originalSprintSpeed = playerController.sprintSpeed;
        originalJumpHeight = playerController.jumpHeight;

        // Duplicamos los valores
        playerController.walkSpeed *= WalkMultiplayer;
        playerController.sprintSpeed *= SprintMultiplayer;
        playerController.jumpHeight *= JumpMultiplayer;

        yield return new WaitForSeconds(OverFlowTime);

        // Restauramos los valores
        playerController.walkSpeed = originalWalkSpeed;
        playerController.sprintSpeed = originalSprintSpeed;
        playerController.jumpHeight = originalJumpHeight;

        // Notify CollectibleManager to stop carrot UI effect
        collectibleManager?.DeactivateCarrotOverflowVisuals();

        isBoostActive = false;
    }
}