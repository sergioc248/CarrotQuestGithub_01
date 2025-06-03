using UnityEngine;

public enum MovementAxis { Vertical_Y, Horizontal_X, Horizontal_Z }

public class MovingPlatformScript : MonoBehaviour, ISlowable
{
    [Header("Configuración de la plataforma")]
    [Tooltip("Eje a lo largo del cual se moverá la plataforma.")]
    [SerializeField] private MovementAxis movementAxis = MovementAxis.Vertical_Y;
    [Tooltip("Distancia mínima de movimiento desde el punto inicial en la dirección negativa del eje seleccionado (normalmente 0 o un valor negativo).")]
    [SerializeField] private float minExtent = -2f; // e.g., 0 for vertical, or -5 for horizontal left
    [Tooltip("Distancia máxima de movimiento desde el punto inicial en la dirección positiva del eje seleccionado.")]
    [SerializeField] private float maxExtent = 2f; // e.g., 5 for vertical up, or 5 for horizontal right
    [Tooltip("Velocidad del movimiento en unds / s")]
    [SerializeField] private float speed = 2f;
    
    private float baseSpeed;
    private bool movingTowardsMaxExtent = true;
    private Vector3 initialPosition; // This will be the center of the oscillation path

    void Awake()
    {
        baseSpeed = speed;
        // Store the initial configured position as the center of the path
        initialPosition = transform.position; 
    }

    void Start()
    {
        // Decide random initial direction
        movingTowardsMaxExtent = Random.value > 0.5f;

        Vector3 axisVector;
        switch (movementAxis)
        {
            case MovementAxis.Horizontal_X: axisVector = Vector3.right; break;
            case MovementAxis.Horizontal_Z: axisVector = Vector3.forward; break;
            default: axisVector = Vector3.up; break; // Vertical_Y
        }

        // Calculate a random starting point along the path
        float randomLerpFactor = Random.Range(0f, 1f);
        Vector3 startOffset = axisVector * Mathf.Lerp(minExtent, maxExtent, randomLerpFactor);
        transform.position = initialPosition + startOffset;

        // Ensure initialPosition used by FixedUpdate and Gizmos refers to the configured center, not this randomized start
        // The initialPosition field is already set in Awake, so it correctly holds the center.
    }

    void FixedUpdate()
    {
        Vector3 movementDirectionVector;
        float currentAxisPosition;
        float targetMinPosOnAxis;
        float targetMaxPosOnAxis;

        switch (movementAxis)
        {
            case MovementAxis.Horizontal_X:
                movementDirectionVector = Vector3.right;
                currentAxisPosition = transform.position.x;
                targetMinPosOnAxis = initialPosition.x + minExtent;
                targetMaxPosOnAxis = initialPosition.x + maxExtent;
                break;
            case MovementAxis.Horizontal_Z:
                movementDirectionVector = Vector3.forward;
                currentAxisPosition = transform.position.z;
                targetMinPosOnAxis = initialPosition.z + minExtent;
                targetMaxPosOnAxis = initialPosition.z + maxExtent;
                break;
            default: // MovementAxis.Vertical_Y
                movementDirectionVector = Vector3.up;
                currentAxisPosition = transform.position.y;
                targetMinPosOnAxis = initialPosition.y + minExtent;
                targetMaxPosOnAxis = initialPosition.y + maxExtent;
                break;
        }

        Vector3 movement = movingTowardsMaxExtent ? movementDirectionVector : -movementDirectionVector;
        movement *= speed * Time.fixedDeltaTime;
        transform.Translate(movement);

        // Update currentAxisPosition after moving
        switch (movementAxis)
        {
            case MovementAxis.Horizontal_X: currentAxisPosition = transform.position.x; break;
            case MovementAxis.Horizontal_Z: currentAxisPosition = transform.position.z; break;
            default: currentAxisPosition = transform.position.y; break;
        }

        if (movingTowardsMaxExtent && currentAxisPosition > targetMaxPosOnAxis)
        {
            SetPositionOnAxis(targetMaxPosOnAxis);
            movingTowardsMaxExtent = false;
        }
        else if (!movingTowardsMaxExtent && currentAxisPosition < targetMinPosOnAxis)
        {
            SetPositionOnAxis(targetMinPosOnAxis);
            movingTowardsMaxExtent = true;
        }
    }

    void SetPositionOnAxis(float axisValue)
    {
        Vector3 newPos = transform.position;
        switch (movementAxis)
        {
            case MovementAxis.Horizontal_X: newPos.x = axisValue; break;
            case MovementAxis.Horizontal_Z: newPos.z = axisValue; break;
            default: newPos.y = axisValue; break; // Vertical_Y
        }
        transform.position = newPos;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 startPoint = Application.isPlaying ? initialPosition : transform.position;
        Vector3 minPointOffset, maxPointOffset;

        switch (movementAxis)
        {
            case MovementAxis.Horizontal_X:
                minPointOffset = Vector3.right * minExtent;
                maxPointOffset = Vector3.right * maxExtent;
                break;
            case MovementAxis.Horizontal_Z:
                minPointOffset = Vector3.forward * minExtent;
                maxPointOffset = Vector3.forward * maxExtent;
                break;
            default: // Vertical_Y
                minPointOffset = Vector3.up * minExtent;
                maxPointOffset = Vector3.up * maxExtent;
                break;
        }

        Vector3 gizmoMinPoint = startPoint + minPointOffset;
        Vector3 gizmoMaxPoint = startPoint + maxPointOffset;

        Gizmos.DrawLine(gizmoMinPoint, gizmoMaxPoint);
        Gizmos.DrawWireSphere(gizmoMinPoint, 0.2f);
        Gizmos.DrawWireSphere(gizmoMaxPoint, 0.2f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Good practice to check tag
        {
            other.transform.SetParent(transform);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.SetParent(null);
        }
    }

    public void SetTimeScale(float scale)
    {
        speed = baseSpeed * scale;
    }
}
