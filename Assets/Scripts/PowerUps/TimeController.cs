using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Header("TimeController Settings")]
    public float slowMotionScale = 0.25f;
    public float slowMotionDuration = 5f;
    public string requiredItemTag = "ClockSlowTime";  // the item you consume

    [Header("Inventory Manager (assign the GO, not the component)")]
    [Tooltip("Drag in the GameObject that has your CollectibleManagerScript on it.")]
    public GameObject inventoryManagerObject;
    private CollectibleManagerScript inventory;

    // all ISlowable objects (enemies, platforms, etc.)
    private List<ISlowable> slowables;

    private bool isReady = false;

    void Start()
    {
        // Grab the component off the assigned GameObject
        if (inventoryManagerObject != null)
        {
            inventory = inventoryManagerObject.GetComponent<CollectibleManagerScript>();
            if (inventory == null)
                Debug.LogError($"TimeController: No CollectibleManagerScript on '{inventoryManagerObject.name}'");
        }

        // Find and cache everything in the scene that implements ISlowable
        slowables = FindObjectsByType<MonoBehaviour>
                        (
                            FindObjectsInactive.Exclude,
                            FindObjectsSortMode.None
                        )
                        .OfType<ISlowable>()
                        .ToList();

        // Then log each one individually:
        foreach (var s in slowables)
        {
            // If your ISlowable implementers are MonoBehaviours, you can cast back to MonoBehaviour
            var mb = s as MonoBehaviour;
            if (mb != null)
            {
                Debug.Log($"[TimeController]  • Slowable: {mb.gameObject.name} (type: {s.GetType().Name})");
            }
            else
            {
                // Fallback if you have some non‐MonoBehaviour implementer of ISlowable
                Debug.Log($"[TimeController]  • Slowable (unknown GO): {s.GetType().Name}");
            }
        }
    }

    void Update()
    {
        // Wait until the player actually has the item in inventory
        if (!isReady)
        {
            if (inventory != null && inventory.SpecialInventoryContains(requiredItemTag))
                isReady = true;
            else
                return;
        }

        // Press V to trigger slow‐motion on just the slowables
        if (Input.GetKeyDown(KeyCode.V))
        {
            // Remove it from inventory (hides UI icon)
            inventory?.RemoveSpecialItem(requiredItemTag);

            isReady = false;

            StartCoroutine(ActivateSlowMotion());
        }
    }

    private IEnumerator ActivateSlowMotion()
    {
        // Notify CollectibleManager to start UI effect
        inventory?.StartPowerUpUIEffect(requiredItemTag);

        // Apply slow‐motion scale to each
        foreach (var s in slowables)
            s.SetTimeScale(slowMotionScale);

        // Wait real‐time, unaffected by any per‐object scales
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        // Restore normal speed
        foreach (var s in slowables)
            s.SetTimeScale(1f);
        
        // Notify CollectibleManager to stop UI effect
        inventory?.StopPowerUpUIEffect(requiredItemTag);
    }
}
