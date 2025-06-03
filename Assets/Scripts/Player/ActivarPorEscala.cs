using UnityEngine;

public class ActivarPorEscala : MonoBehaviour
{
    public GameObject objetoA;
    public Transform objetoB; // Puede ser GameObject también, pero usamos Transform para leer escala

    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        objetoA.SetActive(false);
        Debug.Log("Objeto inicial inactivo");
    }

    void Update()
    {
        if (objetoB.localScale.x > 1.9f)
        {
            if (!objetoA.activeSelf)
            {
                objetoA.SetActive(true);
                Debug.Log("Objeto Activo");
            }
                
        }
        else
        {
            if (objetoA.activeSelf)
            {
                objetoA.SetActive(false);
            }                
        }
    }
}

