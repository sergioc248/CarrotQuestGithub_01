using System.Threading.Tasks;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Setup")]
    public Transform player;
    public Transform cameraTarget;
    public Vector3 shoulderOffset = new Vector3(0.3f, 1.7f, -2f);
    public float followSpeed = 10f;
    public float rotationSpeed = 5f;
    public float mouseSensitivity = 0.5f;

    [Header("Orbita")]
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch;
    private PlayerController playerController;
    private Transform mainCamera;

    void Start()
    {
        playerController = player.GetComponent<PlayerController>();
        mainCamera = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        HandleInput();
        UpdateCameraPosition();
        // Reset cameraTarget rotation so it doesnt rotate with the player
        cameraTarget.rotation = Quaternion.identity;
    }

    void HandleInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX * rotationSpeed;
        pitch -= mouseY * rotationSpeed;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = cameraTarget.position + rotation * shoulderOffset;
        
        // Smoothly move camera to target position
        mainCamera.position = Vector3.Lerp(mainCamera.position, targetPosition, followSpeed * Time.deltaTime);
        
        // Make camera look at target
        mainCamera.LookAt(cameraTarget);
    }
}
