using UnityEngine;
public class RopeBridge : MonoBehaviour
{
    [Header("Bridge Physics Settings")]
    [Tooltip("The prefab for a single wooden plank.")]
    public GameObject plankPrefab;

    [Tooltip("How many planks make up the bridge? (Fewer planks = stiffer bridge, e.g., 25-35).")]
    public int plankCount = 40;

    [Tooltip("Mass of each plank. Higher values (e.g. 2.0 - 5.0) handle heavy vehicles better.")]
    public float plankMass = 3.0f;

    [Tooltip("Linear drag/damping on planks to absorb impact vibrations.")]
    public float linearDamping = 1.0f;

    [Tooltip("Angular drag/damping on planks to prevent sudden flipping.")]
    public float angularDamping = 2.0f;

    [Header("Tension & Rigidity Settings")]
    [Tooltip("If true, adds stiffening DistanceJoints across planks to act as support cables.")]
    public bool enableSupportCables = true;

    [Tooltip("Distance joint max distance tolerance scale (1.0 = exact length, >1.0 allows mild sag).")]
    public float tensionStiffness = 1.0f;

    [Tooltip("Number of planks to skip for support cable connection (e.g. 2 connects plank i to i+2).")]
    public int cableSkipInterval = 2;

    [Header("Global Physics Tweaks")]
    [Tooltip("Automatically increases Physics2D solver iterations for tighter joint constraints.")]
    public bool boostPhysicsIterations = true;

    public int solverVelocityIterations = 25;
    public int solverPositionIterations = 25;

    [Header("Anchor Post Prefabs")]
    [Tooltip("Prefab for the start anchor post (Left cliff).")]
    public GameObject startPostPrefab;

    [Tooltip("Prefab for the end anchor post (Right cliff). Optional; falls back to startPostPrefab if unassigned.")]
    public GameObject endPostPrefab;

    [Tooltip("Offset post position relative to the bridge anchor point (e.g., Y offset to sit on ground).")]
    public Vector2 postOffset = new Vector2(0f, 0.5f);

    [Header("Top Rope Settings")]
    [Tooltip("Enable top rope rendering.")]
    public bool enableVisualRope = true;

    [Tooltip("Assign your rope Sprite texture here. Will automatically tile along the cable.")]
    public Sprite ropeSprite;

    [Tooltip("Thickness of the rope cable.")]
    public float ropeWidth = 0.12f;

    [Tooltip("How much the rope sags down in the middle between top posts (0 = straight line, 0.5 = slight curve).")]
    public float ropeSag = 0.4f;

    [Tooltip("Smoothness curve resolution for the sag.")]
    public int ropeSegments = 20;

    [Tooltip("Local offset on Start Post top where the rope attaches (e.g., Y = 1.2).")]
    public Vector2 startPostRopeOffset = new Vector2(0f, 1.2f);

    [Tooltip("Local offset on End Post top where the rope attaches (e.g., Y = 1.2).")]
    public Vector2 endPostRopeOffset = new Vector2(0f, 1.2f);

    private Transform startPostTransform;
    private Transform endPostTransform;
    private LineRenderer ropeLineRenderer;
    private SpriteRenderer dummyColorReceiver;

    public void BuildBridge(Vector2 startPos, Vector2 endPos)
    {
        if (boostPhysicsIterations)
        {
            Physics2D.velocityIterations = Mathf.Max(Physics2D.velocityIterations, solverVelocityIterations);
            Physics2D.positionIterations = Mathf.Max(Physics2D.positionIterations, solverPositionIterations);
        }

        Vector2 bridgeVector = endPos - startPos;
        float totalDistance = bridgeVector.magnitude;
        Vector2 direction = bridgeVector.normalized;
        float plankLength = totalDistance / plankCount;

        Rigidbody2D[] plankBodies = new Rigidbody2D[plankCount];

        Rigidbody2D startRb = CreateAnchorPost(startPos, true);
        startPostTransform = startRb.transform;
        Rigidbody2D previousRb = startRb;

        for (int i = 0; i < plankCount; i++)
        {
            Vector2 pos = startPos + direction * (plankLength * i + plankLength / 2f);

            GameObject plank = Instantiate(plankPrefab, pos, Quaternion.identity, transform);
            plank.name = $"Plank_{i}";

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            plank.transform.rotation = Quaternion.Euler(0, 0, angle);

            Rigidbody2D plankRb = plank.GetComponent<Rigidbody2D>();
            if (plankRb == null)
            {
                plankRb = plank.AddComponent<Rigidbody2D>();
            }

            plankRb.mass = plankMass;
            plankRb.linearDamping = linearDamping;
            plankRb.angularDamping = angularDamping;
            plankRb.interpolation = RigidbodyInterpolation2D.Interpolate;

            HingeJoint2D hinge = plank.AddComponent<HingeJoint2D>();
            hinge.enableCollision = false;
            hinge.autoConfigureConnectedAnchor = false;
            hinge.anchor = new Vector2(-plankLength / 2f, 0f);

            if (i == 0)
            {
                hinge.connectedBody = startRb;
                hinge.connectedAnchor = startRb.transform.InverseTransformPoint(startPos);
            }
            else
            {
                hinge.connectedBody = previousRb;
                hinge.connectedAnchor = new Vector2(plankLength / 2f, 0f);
            }

            previousRb = plankRb;
            plankBodies[i] = plankRb;
        }

        Rigidbody2D endRb = CreateAnchorPost(endPos, false);
        endPostTransform = endRb.transform;

        HingeJoint2D finalHinge = previousRb.gameObject.AddComponent<HingeJoint2D>();
        finalHinge.enableCollision = false;
        finalHinge.autoConfigureConnectedAnchor = false;
        finalHinge.connectedBody = endRb;
        finalHinge.anchor = new Vector2(plankLength / 2f, 0f);
        finalHinge.connectedAnchor = endRb.transform.InverseTransformPoint(endPos);

        if (enableSupportCables && plankCount > 1)
        {
            for (int i = 0; i < plankCount - 1; i++)
            {
                DistanceJoint2D distJoint = plankBodies[i].gameObject.AddComponent<DistanceJoint2D>();
                distJoint.enableCollision = false;
                distJoint.autoConfigureConnectedAnchor = false;
                distJoint.autoConfigureDistance = false;
                distJoint.connectedBody = plankBodies[i + 1];

                distJoint.anchor = new Vector2(plankLength / 2f, 0f);
                distJoint.connectedAnchor = new Vector2(-plankLength / 2f, 0f);
                distJoint.distance = 0f;
                distJoint.maxDistanceOnly = true;
            }

            if (cableSkipInterval > 1)
            {
                for (int i = 0; i < plankCount - cableSkipInterval; i++)
                {
                    DistanceJoint2D cableJoint = plankBodies[i].gameObject.AddComponent<DistanceJoint2D>();
                    cableJoint.enableCollision = false;
                    cableJoint.autoConfigureConnectedAnchor = false;
                    cableJoint.autoConfigureDistance = false;
                    cableJoint.connectedBody = plankBodies[i + cableSkipInterval];

                    cableJoint.anchor = Vector2.zero;
                    cableJoint.connectedAnchor = Vector2.zero;

                    float spanDistance = plankLength * cableSkipInterval * tensionStiffness;
                    cableJoint.distance = spanDistance;
                    cableJoint.maxDistanceOnly = true;
                }
            }
        }

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < allColliders.Length; i++)
        {
            for (int j = i + 1; j < allColliders.Length; j++)
            {
                Physics2D.IgnoreCollision(allColliders[i], allColliders[j]);
            }
        }

        if (enableVisualRope)
        {
            SetupRopeRenderer();
        }
    }

    private void SetupRopeRenderer()
    {
        GameObject ropeObject = new GameObject("TopRope_LineRenderer");
        ropeObject.transform.SetParent(transform, false);

        ropeLineRenderer = ropeObject.AddComponent<LineRenderer>();
        
        dummyColorReceiver = ropeObject.AddComponent<SpriteRenderer>();

        Material spriteMat = new Material(Shader.Find("Sprites/Default"));
        if (ropeSprite != null)
        {
            spriteMat.mainTexture = ropeSprite.texture;
        }
        ropeLineRenderer.material = spriteMat;
        ropeLineRenderer.textureMode = LineTextureMode.Tile;

        ropeLineRenderer.startWidth = ropeWidth;
        ropeLineRenderer.endWidth = ropeWidth;
        ropeLineRenderer.useWorldSpace = true;
        ropeLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ropeLineRenderer.receiveShadows = false;
        ropeLineRenderer.sortingOrder = 5;

        ropeLineRenderer.positionCount = ropeSegments + 1;

        UpdateRopePositions();
    }

    private void LateUpdate()
    {
        if (!enableVisualRope || ropeLineRenderer == null || startPostTransform == null || endPostTransform == null) return;

        if (dummyColorReceiver != null)
        {
            ropeLineRenderer.startColor = dummyColorReceiver.color;
            ropeLineRenderer.endColor = dummyColorReceiver.color;
        }

        UpdateRopePositions();
    }

    private void UpdateRopePositions()
    {
        Vector3 startPoint = startPostTransform.TransformPoint(startPostRopeOffset);
        Vector3 endPoint = endPostTransform.TransformPoint(endPostRopeOffset);

        for (int i = 0; i <= ropeSegments; i++)
        {
            float t = (float)i / ropeSegments;
            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);

            float sagOffset = 4f * t * (1f - t) * ropeSag;
            point.y -= sagOffset;

            ropeLineRenderer.SetPosition(i, point);
        }
    }

    private Rigidbody2D CreateAnchorPost(Vector2 anchorPos, bool isStart)
    {
        GameObject postPrefabToUse = isStart ? startPostPrefab : (endPostPrefab != null ? endPostPrefab : startPostPrefab);
        GameObject postObject;

        if (postPrefabToUse != null)
        {
            postObject = Instantiate(postPrefabToUse, anchorPos + postOffset, Quaternion.identity, transform);
            postObject.name = isStart ? "StartAnchorPost" : "EndAnchorPost";

            if (!isStart && endPostPrefab == null)
            {
                Vector3 scale = postObject.transform.localScale;
                scale.x *= -1f;
                postObject.transform.localScale = scale;
            }
        }
        else
        {
            postObject = new GameObject(isStart ? "StartAnchor" : "EndAnchor");
            postObject.transform.parent = transform;
            postObject.transform.position = anchorPos;
        }

        Rigidbody2D rb = postObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = postObject.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Static;

        return rb;
    }
}