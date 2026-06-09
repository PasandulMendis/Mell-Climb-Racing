using UnityEngine;

public class RopeBridge : MonoBehaviour
{
    [Header("Bridge Settings")]
    [Tooltip("The prefab for a single wooden plank.")]
    public GameObject plankPrefab;
    [Tooltip("How many planks make up the bridge? More planks = smoother but heavier.")]
    public int plankCount = 12;

    // Called automatically by the MapGenerator
    public void BuildBridge(Vector2 startPos, Vector2 endPos)
    {
        // Calculate the exact distance and direction to span the gap
        Vector2 bridgeVector = endPos - startPos;
        float distance = bridgeVector.magnitude;
        Vector2 direction = bridgeVector.normalized;
        
        float plankLength = distance / plankCount;
        
        // 1. CREATE THE START ANCHOR (Unmovable point on the left cliff)
        GameObject startAnchor = new GameObject("StartAnchor");
        startAnchor.transform.parent = transform;
        startAnchor.transform.position = startPos;
        Rigidbody2D startRb = startAnchor.AddComponent<Rigidbody2D>();
        startRb.bodyType = RigidbodyType2D.Static;
        
        Rigidbody2D previousRb = startRb;
        
        // 2. SPAWN AND CHAIN THE PLANKS
        for (int i = 0; i < plankCount; i++)
        {
            // Position plank exactly in the middle of its calculated segment
            Vector2 pos = startPos + direction * (plankLength * i + plankLength / 2f);
            
            GameObject plank = Instantiate(plankPrefab, pos, Quaternion.identity, transform);
            
            // Point the plank towards the end anchor
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            plank.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Rigidbody2D plankRb = plank.GetComponent<Rigidbody2D>();
            // Give planks a bit of mass so the bridge sags naturally
            plankRb.mass = 0.5f;
            
            // Connect to the previous plank (or the start anchor)
            HingeJoint2D hinge = plank.AddComponent<HingeJoint2D>();
            hinge.connectedBody = previousRb;
            hinge.autoConfigureConnectedAnchor = true;
            
            // The anchor point on THIS plank is on its left edge
            hinge.anchor = new Vector2(-plankLength / 2f, 0); 

            // Disable collisions between connected joints to prevent violent jittering
            hinge.enableCollision = false;

            previousRb = plankRb;
        }
        
        // 3. CREATE THE END ANCHOR (Unmovable point on the right cliff)
        GameObject endAnchor = new GameObject("EndAnchor");
        endAnchor.transform.parent = transform;
        endAnchor.transform.position = endPos;
        Rigidbody2D endRb = endAnchor.AddComponent<Rigidbody2D>();
        endRb.bodyType = RigidbodyType2D.Static;
        
        // Add one final hinge to the very last plank to attach it to the right cliff
        HingeJoint2D finalHinge = previousRb.gameObject.AddComponent<HingeJoint2D>();
        finalHinge.connectedBody = endRb;
        finalHinge.autoConfigureConnectedAnchor = true;
        finalHinge.anchor = new Vector2(plankLength / 2f, 0); // Right edge of the last plank

        // 4. BULLETPROOF PHYSICS FIX: Prevent the planks from fighting each other
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < allColliders.Length; i++)
        {
            for (int j = i + 1; j < allColliders.Length; j++)
            {
                Physics2D.IgnoreCollision(allColliders[i], allColliders[j]);
            }
        }
    }
}