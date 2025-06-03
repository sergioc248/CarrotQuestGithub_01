using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;

public class CollectibleManagerScript : MonoBehaviour
{
    [Header("SecuenciaMovimiento")]
    public float amplitud = 0.25f;
    public float floatSpeed = 2f;
    public float rotationSpeed = 45f;
    [Tooltip("The local Y coordinate around which collectibles will bob, relative to their parent.")]
    public float bobbingBaseLocalY = 0.25f;

    [Header("SistemaRecoleccion")]
    public TextMeshProUGUI itemCounter;
    public string collectibleTag = "Collectible";
    public int totalItemsScene = 0;
    public int itemsCollected = 0;

    [Header("Lista de objetos")]
    public List<Transform> collectibles = new List<Transform>();
    private Dictionary<Transform, Vector3> initialLocalPositions = new Dictionary<Transform, Vector3>();

    [Header("Special Collectibles")]
    public List<Transform> specialCollectibles = new List<Transform>();
    private Dictionary<Transform, Vector3> specialInitialLocalPositions = new Dictionary<Transform, Vector3>();
    private List<string> specialInventory = new List<string>();

    [System.Serializable]
    public class SpecialItemSlot
    {
        [Tooltip("The exact tag that your special‐pickup prefab carries.")]
        public string itemTag;
        [Tooltip("The UI Image you want to enable/disable when that tag is in inventory.")]
        public Image uiImage;
        // Internal state for managing effects
        internal Coroutine activeEffectCoroutine = null;
        internal Vector3 originalScale = Vector3.one;
        internal Quaternion originalRotation = Quaternion.identity;
    }
    [Header("Special UI Slots")]
    public List<SpecialItemSlot> specialItemSlots = new List<SpecialItemSlot>();

    [Header("UI PowerUp Effects")]
    public float uiEffectSpinSpeed = 180f; // Degrees per second
    public float uiEffectPulseScaleFactor = 1.15f; // e.g., 1.15 for 15% larger
    public float uiEffectPulseFrequency = 1.5f; // Pulses per second

    // --- SPECIAL ITEM TAGS ---
    [Header("Slow-Time Controller")]
    [Tooltip("Give the exact tag name that your 'clock' pickup uses.")]
    public string slowTimeTag = "ClockSlowTime";
    public TimeController timeController;

    [Header("ScaleUp Controller")]
    // public string scaleUpTag = "ScalePowerUp"; // This line was made redundant previously
    public ScalePowerupController scaleController;

    [Header("Win Condition")]
    [Tooltip("The tag of the special collectible that triggers the win condition (e.g., GoldenCarrot).")]
    public string winCollectibleTag = "GoldenCarrot";
    [Tooltip("Drag your Win Screen UI GameObject (e.g., the Canvas or Panel for the win screen) here.")]
    public GameObject winScreenPanel; // Assign in Inspector

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Gather ANY GameObject in the scene tagged "Collectible"
        collectibles = GameObject.FindGameObjectsWithTag(collectibleTag)
                                  .Select(go => go.transform)
                                  .ToList();

        totalItemsScene = collectibles.Count;

        // Disable powerup controllers until we pick them up:
        timeController.enabled = false;
        scaleController.enabled = false;

        // Make sure inventory/UI starts empty:
        specialInventory.Clear();

        foreach (var slot in specialItemSlots)
        {
            if (slot.uiImage != null)
            {
                slot.originalScale = slot.uiImage.rectTransform.localScale;
                slot.originalRotation = slot.uiImage.rectTransform.localRotation;
            }
        }

        // ----- Normal collectibles setup -----
        foreach (var obj in collectibles)
        {
            if (obj == null) continue;

            // 1) Remember its original local position
            initialLocalPositions[obj] = obj.localPosition;

            // 2) Ensure it has a collider (and make it a trigger)
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
                col = obj.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            // 3) Ensure exactly one CollectibleDetectorScript, and initialize it
            var detector = obj.GetComponent<CollectibleDetectorScript>();
            if (detector == null)
                detector = obj.gameObject.AddComponent<CollectibleDetectorScript>();
            detector.Init(this);
        }

        // ----- Special collectibles setup -----
        foreach (var obj in specialCollectibles)
        {
            if (obj == null) continue;

            // 1) Remember its original local position
            specialInitialLocalPositions[obj] = obj.localPosition;

            // 2) Ensure it has a collider (and make it a trigger)
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
                col = obj.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;

            // 3) Ensure exactly one CollectibleDetectorScript, and initialize it
            var detector = obj.GetComponent<CollectibleDetectorScript>();
            if (detector == null)
                detector = obj.gameObject.AddComponent<CollectibleDetectorScript>();
            detector.Init(this);
        }

        UpdateCounterUI();
        UpdateSpecialUI();
        if (winScreenPanel != null) winScreenPanel.SetActive(false); // Ensure it's off at start
    }

    // Update is called once per frame
    void Update()
    {
        // Animate normal collectibles
        foreach (var obj in collectibles)
        {
            if (obj == null) continue;
            
            if (!initialLocalPositions.TryGetValue(obj, out Vector3 initialLocalPos))
            {
                initialLocalPos = obj.localPosition;
                initialLocalPositions[obj] = initialLocalPos;
            }

            float phase = Time.time * floatSpeed;
            float yDisplacement = Mathf.Sin(phase) * amplitud;

            // Set localPosition based on its own initial local coordinates, with Y bobbing.
            // This ensures it stays 'stuck' to its starting relative XZ on the parent.
            obj.localPosition = new Vector3(initialLocalPos.x, initialLocalPos.y + yDisplacement, initialLocalPos.z);
            
            if (obj.transform.parent == null) 
            {
                obj.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        // Animate special collectibles
        foreach (var obj in specialCollectibles)
        {
            if (obj == null) continue;
            
            if (!specialInitialLocalPositions.TryGetValue(obj, out Vector3 specialInitialLocalPos))
            {
                specialInitialLocalPos = obj.localPosition;
                specialInitialLocalPositions[obj] = specialInitialLocalPos;
            }

            float phase = Time.time * floatSpeed; 
            float yDisplacement = Mathf.Sin(phase) * amplitud;
            
            // Set localPosition based on its own initial local coordinates, with Y bobbing.
            // This ensures it stays 'stuck' to its starting relative XZ on the parent.
            obj.localPosition = new Vector3(specialInitialLocalPos.x, specialInitialLocalPos.y + yDisplacement, specialInitialLocalPos.z);

            if (obj.transform.parent == null)
            {
                obj.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

    public void CollectItem(Transform obj)
    {
        // Normal collectible
        if (collectibles.Contains(obj))
        {
            collectibles.Remove(obj);
            itemsCollected++;
            UpdateCounterUI();
            Destroy(obj.gameObject);
        }
        // Special collectible
        else if (specialCollectibles.Contains(obj))
        {
            specialCollectibles.Remove(obj);

            specialInventory.Add(obj.tag);
            UpdateSpecialUI();
            
            if (timeController != null && obj.CompareTag(slowTimeTag))
            {
                timeController.enabled = true;
            }
            if (scaleController != null && obj.CompareTag(scaleController.requiredItemTag)) 
            {
                scaleController.enabled = true;
            }

            // Check for win condition
            if (obj.CompareTag(winCollectibleTag))
            {
                ShowWinScreen();
                // Optionally destroy the golden carrot, or leave it if the game ends immediately
                // Destroy(obj.gameObject); 
                return; // Stop further processing if win condition met
            }

            Destroy(obj.gameObject);
        }
    }

    void UpdateCounterUI()
    {
        if (itemCounter != null)
        {
            itemCounter.text = $"{itemsCollected} / {totalItemsScene}";
        }
    }

    void UpdateSpecialUI()
    {
        // Turn each slot image on/off based on whether its itemTag is in inventory
        foreach (var slot in specialItemSlots)
        {
            if (slot.uiImage == null) continue;
            bool hasIt = specialInventory.Contains(slot.itemTag);
            slot.uiImage.enabled = hasIt;
        }
    }

    // Removes the named special item (if present), updates its UI, and returns true if removed.
    public bool RemoveSpecialItem(string itemTag)
    {
        bool removed = specialInventory.Remove(itemTag);
        if (removed)
        {
            // DO NOT call UpdateSpecialUI() here. Icon visibility will be managed by StopPowerUpUIEffect and HideConsumedPowerUpIcon.
            // UpdateSpecialUI(); 
            return true;
        }
        return false;
    }
    // Expose a quick check for inventory polling.
    public bool SpecialInventoryContains(string itemTag) => specialInventory.Contains(itemTag);

    public void StartPowerUpUIEffect(string itemTag)
    {
        SpecialItemSlot slot = specialItemSlots.FirstOrDefault(s => s.itemTag == itemTag);
        if (slot != null && slot.uiImage != null)
        {
            if (slot.activeEffectCoroutine != null)
            {
                StopCoroutine(slot.activeEffectCoroutine);
            }
            if(slot.uiImage.rectTransform.localScale != slot.originalScale) slot.originalScale = slot.uiImage.rectTransform.localScale;
            if(slot.uiImage.rectTransform.localRotation != slot.originalRotation) slot.originalRotation = slot.uiImage.rectTransform.localRotation;
            slot.activeEffectCoroutine = StartCoroutine(AnimatePowerUpEffectCoroutine(slot));
        }
    }

    public void StopPowerUpUIEffect(string itemTag)
    {
        SpecialItemSlot slot = specialItemSlots.FirstOrDefault(s => s.itemTag == itemTag);
        if (slot != null && slot.uiImage != null)
        {
            if (slot.activeEffectCoroutine != null)
            {
                StopCoroutine(slot.activeEffectCoroutine);
                slot.activeEffectCoroutine = null;
            }
            // Reset to original state
            slot.uiImage.rectTransform.localScale = slot.originalScale;
            slot.uiImage.rectTransform.localRotation = slot.originalRotation;

            // After stopping the effect, if the item is truly gone from inventory (which it should be),
            // hide the icon. This is a more robust place than HideConsumedPowerUpIcon for general special items.
            if (!SpecialInventoryContains(itemTag)) 
            {
                 slot.uiImage.enabled = false;
            }
        }
    }

    private IEnumerator AnimatePowerUpEffectCoroutine(SpecialItemSlot slot)
    {
        Image uiImage = slot.uiImage;
        RectTransform rectTransform = uiImage.rectTransform;
        while (true)
        {
            rectTransform.Rotate(0, 0, uiEffectSpinSpeed * Time.unscaledDeltaTime);
            float pulse = 1f + (Mathf.Sin(Time.unscaledTime * uiEffectPulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f) * (uiEffectPulseScaleFactor - 1f);
            rectTransform.localScale = slot.originalScale * pulse;
            yield return null;
        }
    }

    [Header("Carrot Overflow UI")]
    public Image normalCarrotUI_Icon;
    private Coroutine carrotOverflowEffectCoroutine = null;
    private Vector3 carrotOriginalScale = Vector3.one;
    private Quaternion carrotOriginalRotation = Quaternion.identity;

    public void ActivateCarrotOverflowVisuals()
    {
        if (normalCarrotUI_Icon != null)
        {
            if(carrotOverflowEffectCoroutine != null) StopCoroutine(carrotOverflowEffectCoroutine);
            carrotOriginalScale = normalCarrotUI_Icon.rectTransform.localScale; 
            carrotOriginalRotation = normalCarrotUI_Icon.rectTransform.localRotation;
            carrotOverflowEffectCoroutine = StartCoroutine(AnimateGenericPowerUpEffectCoroutine(normalCarrotUI_Icon, carrotOriginalScale, carrotOriginalRotation));
        }
        itemsCollected = 0;
        UpdateCounterUI();
    }

    public void DeactivateCarrotOverflowVisuals()
    {
         if (normalCarrotUI_Icon != null)
        {
            if(carrotOverflowEffectCoroutine != null) 
            {
                StopCoroutine(carrotOverflowEffectCoroutine);
                carrotOverflowEffectCoroutine = null;
            }
            normalCarrotUI_Icon.rectTransform.localScale = carrotOriginalScale;
            normalCarrotUI_Icon.rectTransform.localRotation = carrotOriginalRotation;
        }
    }
    
    private IEnumerator AnimateGenericPowerUpEffectCoroutine(Image uiImage, Vector3 originalLocalScale, Quaternion originalLocalRotation)
    {
        RectTransform rectTransform = uiImage.rectTransform;
        while (true)
        {
            rectTransform.Rotate(0, 0, uiEffectSpinSpeed * Time.unscaledDeltaTime);
            float pulse = 1f + (Mathf.Sin(Time.unscaledTime * uiEffectPulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f) * (uiEffectPulseScaleFactor - 1f);
            rectTransform.localScale = originalLocalScale * pulse;
            yield return null;
        }
    }

    public bool AreAllNormalCollectiblesGathered() 
    {
        return itemsCollected >= totalItemsScene && totalItemsScene > 0;
    }

    // This method might still be useful if a power-up icon needs to be hidden for other reasons
    // or if a power-up doesn't use the standard Start/Stop effect model but is consumed.
    public void HideConsumedPowerUpIcon(string itemTag) 
    {
        SpecialItemSlot slot = specialItemSlots.FirstOrDefault(s => s.itemTag == itemTag);
        if (slot != null && slot.uiImage != null)
        {
            slot.uiImage.enabled = false;
        }
    }

    void ShowWinScreen()
    {
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(true);
        }
        Time.timeScale = 0f; // Pause the game
        Cursor.lockState = CursorLockMode.None; // Unlock cursor
        Cursor.visible = true; // Show cursor

        // Disable player input
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if(playerController != null) playerController.enabled = false;

            TimeController timeController = player.GetComponent<TimeController>(); // Assuming it's on the player
            if(timeController != null) timeController.enabled = false;
        }
    }

    // Add a method to be called by the "Play Again" button on the Win Screen
    public void PlayAgainFromWinScreen()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Player controller and TimeController should be re-enabled by scene reload 
        // or explicitly if PlayerController reference was kept and TimeController could be found.
        // For simplicity with scene reload, explicit re-enable here might be redundant 
        // but good for consistency if we weren't reloading immediately.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if(playerController != null) playerController.enabled = true;

            TimeController timeController = player.GetComponent<TimeController>();
            if(timeController != null) timeController.enabled = true;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
