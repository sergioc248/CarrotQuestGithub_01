using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LiveSystem : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 3;
    private int currentLives;

    public Transform respawnPoint;
    public TextMeshProUGUI livesText;

    [Header("UI Screens")]
    [Tooltip("Drag your Death Screen UI GameObject (e.g., the Canvas or Panel for the death screen) here.")]
    public GameObject deathScreenPanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentLives = maxLives;
        UpdateLivesUI();
        if (respawnPoint == null)
            respawnPoint = this.transform;

        if (deathScreenPanel != null) deathScreenPanel.SetActive(false); // Ensure it's off at start
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLivesUI();
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = $"{currentLives}/{maxLives}";
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deadzone"))
        {
            currentLives--;
            Debug.Log("Vidas restantes: " + currentLives);

            if (currentLives <= 0)
            {
                ShowDeathScreen();
            }
            else
            {
                transform.position = respawnPoint.position;
            }
        }      
    }

    void ShowDeathScreen()
    {
        if (deathScreenPanel != null)
        {
            deathScreenPanel.SetActive(true);
        }
        Time.timeScale = 0f; // Pause the game
        Cursor.lockState = CursorLockMode.None; // Unlock cursor
        Cursor.visible = true; // Show cursor
        // Optionally, disable player input here if PlayerController has a method for it
        PlayerController playerController = GetComponent<PlayerController>();
        if(playerController != null) 
        {
            playerController.enabled = false;
            playerController.ResetAnimatorToIdle(); // Call the new reset method
        }

        TimeController timeController = GetComponent<TimeController>(); // Or FindObjectOfType<TimeController>();
        if(timeController != null) timeController.enabled = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Unpause
        Cursor.lockState = CursorLockMode.Locked; // Re-lock cursor
        Cursor.visible = false; // Hide cursor
        // Re-enable player controller if it was disabled
        PlayerController playerController = GetComponent<PlayerController>();
        if(playerController != null) playerController.enabled = true;

        TimeController timeController = GetComponent<TimeController>(); // Or FindObjectOfType<TimeController>();
        if(timeController != null) timeController.enabled = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload current scene
    }
}
