using UnityEngine;

/// RTS-style camera controller with WASD movement, edge scrolling, and zoom
/// Adapted from VG2 implementation with enhanced features
public class RTSCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float edgeScrollSpeed = 15f;
    [SerializeField] private float edgeScrollBorderSize = 10f;
    [SerializeField] private bool enableEdgeScrolling = true;
    
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float currentZoom = 50f; // Start fully zoomed out
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Z;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.C;
    
    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private bool autoDetectBoundaries = true;
    [SerializeField] private string boundaryTag = "Boundary";
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = 1 << 3; // Default to Ground layer (layer 3)
    [SerializeField] private float groundCheckDistance = 100f;
    [SerializeField] private float heightOffset = 10f;
    
    private Vector3 movement;
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }
        
        // Set ground layer to "Ground" if not already set
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }
        
        // Auto-detect boundaries from tagged cubes
        if (autoDetectBoundaries)
        {
            DetectBoundariesFromCubes();
        }
        
        // Set initial zoom
        UpdateZoom(currentZoom);
    }
    
    void OnEnable()
    {
        // Refresh boundaries when camera becomes active (e.g., after scene transition)
        if (autoDetectBoundaries)
        {
            DetectBoundariesFromCubes();
        }
        // Don't clamp position here - let CameraModeManager position the camera first
        // Boundaries will be enforced in Update/HandleMovement
    }
    
    /// <summary>
    /// Re-detect boundaries - useful for scene transitions
    /// </summary>
    public void RefreshBoundaries()
    {
        if (autoDetectBoundaries)
        {
            DetectBoundariesFromCubes();
        }
    }
    
    void DetectBoundariesFromCubes()
    {
        GameObject[] boundaryCubes = GameObject.FindGameObjectsWithTag(boundaryTag);
        
        if (boundaryCubes.Length > 0)
        {
            // Find the innermost edges of all boundary cubes (closest to play area)
            float minX = float.MinValue;
            float maxX = float.MaxValue;
            float minZ = float.MinValue;
            float maxZ = float.MaxValue;
            
            foreach (GameObject cube in boundaryCubes)
            {
                Collider col = cube.GetComponent<Collider>();
                if (col != null)
                {
                    Bounds bounds = col.bounds;
                    
                    // Get the innermost edges (use Max for min, Min for max)
                    minX = Mathf.Max(minX, bounds.min.x);
                    maxX = Mathf.Min(maxX, bounds.max.x);
                    minZ = Mathf.Max(minZ, bounds.min.z);
                    maxZ = Mathf.Min(maxZ, bounds.max.z);
                }
            }
            
            // Fallback: if boundaries weren't set properly, use a large area
            if (minX == float.MinValue) minX = -100f;
            if (maxX == float.MaxValue) maxX = 100f;
            if (minZ == float.MinValue) minZ = -100f;
            if (maxZ == float.MaxValue) maxZ = 100f;
            
            // Ensure boundaries are valid (min < max)
            if (minX > maxX)
            {
                float temp = minX;
                minX = maxX;
                maxX = temp;
            }
            if (minZ > maxZ)
            {
                float temp = minZ;
                minZ = maxZ;
                maxZ = temp;
            }
            
            // Add small padding to keep camera inside the play area
            float padding = 2f;
            minBounds = new Vector2(minX + padding, minZ + padding);
            maxBounds = new Vector2(maxX - padding, maxZ - padding);
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
    }
    
    void HandleMovement()
    {
        Vector3 keyboardMovement = Vector3.zero;
        Vector3 edgeMovement = Vector3.zero;
        
        // Get camera forward/right on XZ plane (ignore Y rotation)
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();
        
        // Arrow Keys Movement
        if (Input.GetKey(KeyCode.UpArrow))
            keyboardMovement += forward;
        if (Input.GetKey(KeyCode.DownArrow))
            keyboardMovement -= forward;
        if (Input.GetKey(KeyCode.LeftArrow))
            keyboardMovement -= right;
        if (Input.GetKey(KeyCode.RightArrow))
            keyboardMovement += right;
        
        // Edge Scrolling
        if (enableEdgeScrolling)
        {
            Vector3 mousePos = Input.mousePosition;
            
            if (mousePos.x < edgeScrollBorderSize)
                edgeMovement -= right;
            if (mousePos.x > Screen.width - edgeScrollBorderSize)
                edgeMovement += right;
            if (mousePos.y < edgeScrollBorderSize)
                edgeMovement -= forward;
            if (mousePos.y > Screen.height - edgeScrollBorderSize)
                edgeMovement += forward;
        }
        
        // Normalize movements separately to prevent faster diagonal movement
        if (keyboardMovement.magnitude > 1f)
            keyboardMovement.Normalize();
        if (edgeMovement.magnitude > 1f)
            edgeMovement.Normalize();
        
        // Combine movements with their respective speeds
        movement = keyboardMovement * moveSpeed + edgeMovement * edgeScrollSpeed;
        
        // Apply movement
        Vector3 newPosition = transform.position + movement * Time.deltaTime;
        
        // Keep movement on XZ plane
        newPosition.y = transform.position.y;
        
        // Apply boundaries
        if (useBoundaries)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, minBounds.x, maxBounds.x);
            newPosition.z = Mathf.Clamp(newPosition.z, minBounds.y, maxBounds.y);
        }
        
        // Always apply the movement
        transform.position = newPosition;
        
        // Optional: Ground check to adjust height (if ground layer is set)
        if (groundLayer != 0)
        {
            Vector3 checkPosition = newPosition + Vector3.up * heightOffset;
            if (Physics.Raycast(checkPosition, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
            {
                // Optionally adjust Y position based on terrain height
                // Uncomment if you want camera to follow terrain height:
                // transform.position = new Vector3(newPosition.x, hit.point.y + heightOffset, newPosition.z);
            }
        }
    }
    
    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (scrollInput != 0f)
        {
            float newZoom = currentZoom - scrollInput * zoomSpeed;
            newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
            
            // Check if new zoom would put camera through ground
            Vector3 testPosition = transform.position;
            float angle = Vector3.Angle(transform.forward, Vector3.down);
            float testHeight = newZoom * Mathf.Sin(angle * Mathf.Deg2Rad);
            testPosition.y = testHeight;
            
            // Raycast down to check ground distance
            if (Physics.Raycast(testPosition, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            {
                float distanceToGround = testPosition.y - hit.point.y;
                
                // Don't allow zoom if camera would be less than 2 units above ground
                if (distanceToGround < 2f)
                {
                    return; // Block this zoom
                }
            }
            
            currentZoom = newZoom;
            UpdateZoom(currentZoom);
        }
    }
    
    void UpdateZoom(float zoom)
    {
        // Adjust camera position based on zoom
        // This assumes the camera is looking down at an angle
        Vector3 direction = transform.forward;
        float angle = Vector3.Angle(direction, Vector3.down);
        
        // Calculate height based on zoom and angle
        float height = zoom * Mathf.Sin(angle * Mathf.Deg2Rad);
        float distance = zoom * Mathf.Cos(angle * Mathf.Deg2Rad);
        
        Vector3 localPos = transform.localPosition;
        localPos.y = height;
        transform.localPosition = localPos;
    }
    
    void HandleRotation()
    {
        if (!enableRotation) return;
        
        float rotation = 0f;
        
        if (Input.GetKey(rotateLeftKey))
            rotation = -rotationSpeed * Time.deltaTime;
        if (Input.GetKey(rotateRightKey))
            rotation = rotationSpeed * Time.deltaTime;
        
        if (rotation != 0f)
        {
            transform.Rotate(Vector3.up, rotation, Space.World);
        }
    }
    
    /// Set the camera boundaries dynamically
    public void SetBoundaries(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBoundaries = true;
    }
    /// Disable camera boundaries
    public void DisableBoundaries()
    {
        useBoundaries = false;
    }
    
    /// Focus camera on a specific position
    public void FocusOnPosition(Vector3 position)
    {
        Vector3 newPos = transform.position;
        newPos.x = position.x;
        newPos.z = position.z;
        
        if (useBoundaries)
        {
            newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
            newPos.z = Mathf.Clamp(newPos.z, minBounds.y, maxBounds.y);
        }
        
        transform.position = newPos;
    }
    
    void OnDrawGizmos()
    {
        // Visualize boundaries in Scene view
        if (useBoundaries)
        {
            Gizmos.color = Color.yellow;
            
            // Draw boundary rectangle at ground level
            float y = 0f;
            Vector3 corner1 = new Vector3(minBounds.x, y, minBounds.y);
            Vector3 corner2 = new Vector3(maxBounds.x, y, minBounds.y);
            Vector3 corner3 = new Vector3(maxBounds.x, y, maxBounds.y);
            Vector3 corner4 = new Vector3(minBounds.x, y, maxBounds.y);
            
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);
            
            // Draw vertical lines to make it more visible
            Gizmos.DrawLine(corner1, corner1 + Vector3.up * 10f);
            Gizmos.DrawLine(corner2, corner2 + Vector3.up * 10f);
            Gizmos.DrawLine(corner3, corner3 + Vector3.up * 10f);
            Gizmos.DrawLine(corner4, corner4 + Vector3.up * 10f);
        }
    }
}
