using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List

[RequireComponent(typeof(Collider))] // Ensure it has a collider
public class DestructibleByScale : MonoBehaviour
{
    [Header("Piece Destruction Physics")]
    public float pieceExplosionForce = 25f; // Increased default example
    public float pieceExplosionRadius = 3f; 
    public float pieceUpwardsModifier = 0.75f;
    public float pieceTorqueStrength = 40f;

    [Header("Piece Fade & Destroy")]
    public float timeToStartPieceFade = 1.5f; 
    public float pieceFadeDuration = 1.5f;    
    
    public bool IsBrokenApart { get; private set; } // Made public for checking

    // No longer need these for the parent object if we are breaking into pieces
    // private Rigidbody rb;
    // private Material originalMaterial;
    // private Color originalColor;
    // private Renderer objectRenderer;

    void Start()
    {
        IsBrokenApart = false; // Ensure it's initialized
        // The main object (this one) likely just acts as a trigger and container.
        // Individual pieces will handle their own physics and visuals upon destruction.
        // We might want to ensure child pieces are correctly set up (e.g., have colliders) here if needed.
        Debug.Log($"[DestructibleByScale] {gameObject.name} initialized. Waiting for scaled player collision.", gameObject);
    }

    // REMOVED OnCollisionEnter - Player will call TriggerDestruction directly
    // void OnCollisionEnter(Collision collision) { ... }

    // Make this public so Player/ScalePowerupController can call it
    public void TriggerDestruction(Vector3 hitPoint, Vector3 playerPosition)
    {
        if (IsBrokenApart) return; 
        Debug.Log($"[DestructibleByScale] {gameObject.name} TriggerDestruction (breaking into pieces) called by player.", gameObject);
        IsBrokenApart = true;

        // Optional: Disable the main collider on this parent object if it has one and could interfere
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;

        // Find all child transforms that represent pieces. 
        // This example assumes direct children with renderers are pieces.
        // You might need a more specific way to identify pieces (e.g., tag, layer, or a specific component).
        foreach (Transform pieceTransform in transform) // Iterates direct children
        {
            HandlePiece(pieceTransform.gameObject, hitPoint, playerPosition);
        }
        
        // Destroy the original parent object after a short delay, or immediately if preferred
        // This allows pieces to become fully independent.
        Destroy(gameObject, 0.1f); 
    }

    void HandlePiece(GameObject piece, Vector3 hitPoint, Vector3 playerPosition)
    {
        Renderer pieceRenderer = piece.GetComponent<Renderer>();
        if (pieceRenderer == null) 
        {
            // If a child doesn't have a renderer, maybe it's a container for more pieces?
            // Or just skip it if we only care about visible pieces.
            // For this example, let's see if it has children with renderers.
            foreach(Transform subPieceTransform in piece.transform)
            {
                if(subPieceTransform.GetComponent<Renderer>() != null)
                {
                    HandlePiece(subPieceTransform.gameObject, hitPoint, playerPosition);
                }
            }
            if (!piece.name.ToLower().Contains("caja")) Destroy(piece,pieceFadeDuration+timeToStartPieceFade + 0.5f); // Destroy empty parents of pieces as well unless it is the main model
            return; // Skip this child if it has no renderer itself but process its children
        }

        // Make the piece an independent object in the scene hierarchy
        piece.transform.SetParent(null, true); // true to keep world position

        Rigidbody pieceRb = piece.GetComponent<Rigidbody>();
        if (pieceRb == null) pieceRb = piece.AddComponent<Rigidbody>();
        pieceRb.isKinematic = false;
        pieceRb.useGravity = true;
        
        // Ensure piece has its own collider for physics interactions
        Collider pieceCollider = piece.GetComponent<Collider>();
        if (pieceCollider == null) 
        {
            // Add a simple BoxCollider if none exists. For complex shapes, consider MeshCollider (convex).
            MeshFilter mf = piece.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                // Attempt to add a MeshCollider, make it convex for dynamic Rigidbody interaction
                MeshCollider mc = piece.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true; 
            }
            else
            {
                piece.AddComponent<BoxCollider>(); // Fallback to BoxCollider
            }
            Debug.LogWarning($"[DestructibleByScale] Added Collider to piece {piece.name} as none was found.", piece);
        }
        else if(pieceCollider is MeshCollider meshCol && !meshCol.convex)
        {
            // If it already has a MeshCollider but it's not convex, it won't collide well with other dynamic rigidbodies
            Debug.LogWarning($"[DestructibleByScale] Piece {piece.name} has a non-convex MeshCollider. Physics interactions might be limited. Consider making it convex or using primitive colliders.", piece);
        }

        // Apply explosion force to the piece
        // Using AddExplosionForce is good for a shattering effect from a point
        pieceRb.AddExplosionForce(pieceExplosionForce, playerPosition, pieceExplosionRadius, pieceUpwardsModifier, ForceMode.Impulse);
        // Add some random torque
        pieceRb.AddTorque(Random.insideUnitSphere * pieceTorqueStrength, ForceMode.Impulse);

        StartCoroutine(FadeOutAndDestroyPiece(piece, pieceRenderer));
    }

    private IEnumerator FadeOutAndDestroyPiece(GameObject pieceInstance, Renderer pieceRenderer)
    {
        yield return new WaitForSeconds(timeToStartPieceFade);

        Material materialInstance = null;
        Color pieceOriginalColor = Color.white;

        if (pieceRenderer != null && pieceRenderer.material != null)
        {
            materialInstance = new Material(pieceRenderer.material);
            pieceRenderer.material = materialInstance;
            pieceOriginalColor = materialInstance.color;

            float elapsedTime = 0f;
            while (elapsedTime < pieceFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(pieceOriginalColor.a, 0f, elapsedTime / pieceFadeDuration);
                materialInstance.color = new Color(pieceOriginalColor.r, pieceOriginalColor.g, pieceOriginalColor.b, alpha);
                yield return null;
            }
        }
        else 
        {
            // If no renderer/material, just wait out the duration
            yield return new WaitForSeconds(pieceFadeDuration);
        }
        
        if (pieceInstance != null) Destroy(pieceInstance);
    }
} 