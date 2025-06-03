using UnityEngine;

public class CollectibleDetectorScript : MonoBehaviour
{
    private CollectibleManagerScript manager;
    public void Init(CollectibleManagerScript manager)
    {
        this.manager = manager;
    }

    void OnTriggerEnter(Collider other)
    {
        if (manager == null)
        {
            Debug.LogError($"CollectibleDetectorScript on {gameObject.name} does not have its manager initialized. Collection will fail. Ensure CollectibleManagerScript calls Init() on this detector.");
            return; // Can't proceed if manager is not set
        }

        if (other.CompareTag("Player"))
        {
            manager.CollectItem(transform);
        }
    }

}
