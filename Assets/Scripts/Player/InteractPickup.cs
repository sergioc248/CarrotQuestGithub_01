using UnityEngine;
using System.Collections;

public class InteractPickup : MonoBehaviour
{
    [Header("Configuración de interacción")]
    public KeyCode activationKey = KeyCode.E;
    public float interactionDistance = 2f;

    [Header("Escala del personaje")]
    public Vector3 scaledSize = new Vector3(2f, 2f, 2f);
    public float scaleDuration = 5f;




    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player != null && Vector3.Distance(player.position, transform.position) <= interactionDistance)
        {
            if (Input.GetKeyDown(activationKey))
            {
                StartCoroutine(ScalePlayerTemporarily(player));

            }
        }
    }

    IEnumerator ScalePlayerTemporarily(Transform playerTransform)
    {
        Vector3 originalScale = playerTransform.localScale;
        playerTransform.localScale = scaledSize;

        yield return new WaitForSeconds(scaleDuration);

        playerTransform.localScale = originalScale;
    }
}
