using UnityEngine;

public class Snowman : MonoBehaviour
{
    [Header("Snowman Parts")]
    public Rigidbody2D headRB;
    public Rigidbody2D bodyRB;

    private bool isHit = false;

    void Start()
    {
        // Keep them frozen in place on the hill so they don't roll away early
        if (headRB != null) headRB.isKinematic = true;
        if (bodyRB != null) bodyRB.isKinematic = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isHit) return;

        // Did something tagged "Player" hit us?
        if (other.CompareTag("Player"))
        {
            // STOP! Is the "Player" just our own Head or Body? If so, ignore it!
            if (headRB != null && other.gameObject == headRB.gameObject) return;
            if (bodyRB != null && other.gameObject == bodyRB.gameObject) return;

            isHit = true;

            // 1. Unfreeze the pieces so they react to natural physics
            if (headRB != null) headRB.isKinematic = false;
            if (bodyRB != null) bodyRB.isKinematic = false;

            // 2. Unparent them so they exist independently in the world
            if (headRB != null) headRB.transform.SetParent(null);
            if (bodyRB != null) bodyRB.transform.SetParent(null);

            // 3. Clean up the loose pieces after a few seconds so they don't lag the game
            if (headRB != null) Destroy(headRB.gameObject, 4f);
            if (bodyRB != null) Destroy(bodyRB.gameObject, 4f);

            // 4. Destroy the empty parent object
            Destroy(gameObject);
        }
    }
}