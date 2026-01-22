using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Controller2D target;
    public float verticalOffset;
    public float lookAheadDstX;
    public float lookSmoothTimeX;
    public float verticalSmoothTime;
    public Vector2 focusAreaSize;

    FocusArea focusArea;

    float currentLookAheadX;
    float targetLookAheadX;
    float lookAheadDirX;
    float smoothLookVelocityX;
    float smoothVelocityY;

    bool lookAheadStopped;

    public Camera camera;

    [SerializeField] private float zoomStep = 0.1f;
    [SerializeField] private float defaultZ = -10f; // normal camera distance (z)

    private enum ZoomState { None, ZoomingOut, ZoomingBackIn }
    private ZoomState zoomState = ZoomState.None;

    private float zoomTargetZ;

    void Start()
    {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
        camera = GetComponent<Camera>();

        // Force initial camera position to normal zoom
        transform.position = new Vector3(
            target.transform.position.x,
            target.transform.position.y,
            defaultZ
        );
    }

    private void OnEnable()
    {
        // Keep whatever Z the camera already has
        transform.position = new Vector3(
            target.transform.position.x,
            target.transform.position.y,
            transform.position.z
        );
    }

    void LateUpdate()
    {
        focusArea.Update(target.collider.bounds);

        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        if (focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0)
            {
                lookAheadStopped = false;
                targetLookAheadX = lookAheadDirX * lookAheadDstX;
            }
            else
            {
                if (!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDstX - currentLookAheadX) / 4f;
                }
            }
        }

        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);
        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
        focusPosition += Vector2.right * currentLookAheadX;

        Vector3 pos = transform.position;
        pos.x = focusPosition.x;
        pos.y = focusPosition.y;

        // 1) If zoomOut is active, it ALWAYS wins (even if we were zooming back in)
        if (target.zoneInfo.zoomOut != 0f)
        {
            // Cancel any "exit zone" behavior because we are actively in a zoom zone again
            target.zoneInfo.exitZone = false;

            zoomState = ZoomState.ZoomingOut;
            zoomTargetZ = defaultZ * target.zoneInfo.zoomOut;
            pos.z = Mathf.MoveTowards(pos.z, zoomTargetZ, zoomStep);
        }
        // 2) Otherwise, if we're exiting, zoom back to default
        else if (target.zoneInfo.exitZone)
        {
            zoomState = ZoomState.ZoomingBackIn;
            zoomTargetZ = defaultZ;

            pos.z = Mathf.MoveTowards(pos.z, zoomTargetZ, zoomStep);

            if (Mathf.Approximately(pos.z, zoomTargetZ))
            {
                zoomState = ZoomState.None;
                target.zoneInfo.exitZone = false; // done returning
                // NOTE: no Reset() here, so we don't wipe zoomOut if a new zone sets it next frame
            }
        }
        // 3) No zooming requested --> maintain default if not mid-anim
        else if (zoomState == ZoomState.None)
        {
            pos.z = defaultZ;
        }

        transform.position = pos;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.centre, focusAreaSize);
    }

    struct FocusArea
    {
        public Vector2 centre;
        public Vector2 velocity;
        float left, right;
        float top, bottom;

        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
        }

        public void Update(Bounds targetBounds)
        {
            float shiftX = 0;
            if (targetBounds.min.x < left) shiftX = targetBounds.min.x - left;
            else if (targetBounds.max.x > right) shiftX = targetBounds.max.x - right;
            left += shiftX;
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < bottom) shiftY = targetBounds.min.y - bottom;
            else if (targetBounds.max.y > top) shiftY = targetBounds.max.y - top;
            top += shiftY;
            bottom += shiftY;

            centre = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(shiftX, shiftY);
        }
    }
}
