using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

/// Manages switching between Third Person and RTS camera modes
/// Preserves both camera setups and allows seamless switching
public class CameraModeManager : MonoBehaviour
{
    [Header("Camera Mode")]
    [SerializeField] private CameraMode currentMode = CameraMode.ThirdPerson;
    [SerializeField] private KeyCode toggleModeKey = KeyCode.V;
    
    [Header("Camera References")]
    [SerializeField] private GameObject thirdPersonCamera;
    [SerializeField] private GameObject rtsCamera;
    
    [Header("Player References")]
    [SerializeField] private GameObject playerCharacter;
    [SerializeField] private ThirdPersonController thirdPersonController;
    
    [Header("RTS Camera Settings")]
    [SerializeField] private Vector3 rtsCameraOffset = new Vector3(0, 25, -15);
    [SerializeField] private Vector3 rtsCameraRotation = new Vector3(60, 0, 0);
    
    [Header("UI Feedback")]
    [SerializeField] private bool showModeChangeMessage = true;
    [SerializeField] private float messageDisplayTime = 2f;
    
    private RTSCameraController rtsCameraController;
    private Camera mainCamera;
    private string currentModeMessage = "";
    private float messageTimer = 0f;
    private bool isRTSModeActive = false;
    
    void OnEnable()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-find player and cameras when scene changes
        playerCharacter = null;
        thirdPersonCamera = null;
        thirdPersonController = null;
        
        // Reset to third person mode on scene load and ensure player input is enabled
        currentMode = CameraMode.ThirdPerson;
        isRTSModeActive = false;
        
        // Wait and find all references in the coroutine (don't call InitializeReferences here)
        StartCoroutine(EnablePlayerInputAfterSceneLoad());
    }
    
    private IEnumerator EnablePlayerInputAfterSceneLoad()
    {
        // Wait a bit longer for scene to fully load
        yield return new WaitForSeconds(0.2f);
        
        // Try multiple times to find player
        int attempts = 0;
        while (playerCharacter == null && attempts < 10)
        {
            // Try by tag first
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                playerCharacter = taggedPlayer;
                break;
            }
            
            // Try by ThirdPersonController component
            ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
            if (controller != null)
            {
                playerCharacter = controller.gameObject;
                break;
            }
            
            // Try by name
            playerCharacter = GameObject.Find("PlayerArmature");
            if (playerCharacter == null)
            {
                playerCharacter = GameObject.Find("Player");
            }
            
            if (playerCharacter != null) break;
            
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
        
        // If still not found, try one more aggressive search
        if (playerCharacter == null)
        {
            // Find any object with ThirdPersonController
            ThirdPersonController[] controllers = FindObjectsOfType<ThirdPersonController>();
            if (controllers.Length > 0)
            {
                playerCharacter = controllers[0].gameObject;
            }
        }
        
        // CRITICAL: Re-find ALL components from the player
        if (playerCharacter != null)
        {
            // Find third person controller
            thirdPersonController = playerCharacter.GetComponent<ThirdPersonController>();
            
            // Find third person camera
            Camera[] cameras = playerCharacter.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam.gameObject.name.Contains("Camera") || cam.gameObject.name.Contains("Follow"))
                {
                    thirdPersonCamera = cam.gameObject;
                    break;
                }
            }
            
            // If still no camera found, try main camera
            if (thirdPersonCamera == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    thirdPersonCamera = mainCam.gameObject;
                }
            }
        }
        
        // Position RTS camera above the player for the new scene
        if (rtsCamera != null && playerCharacter != null)
        {
            Vector3 playerWorldPos = playerCharacter.transform.position;
            rtsCamera.transform.position = playerWorldPos + rtsCameraOffset;
            rtsCamera.transform.rotation = Quaternion.Euler(rtsCameraRotation);
        }
        
        // Refresh RTS camera boundaries again after player is found and camera is positioned
        if (rtsCameraController != null)
        {
            rtsCameraController.RefreshBoundaries();
        }
        
        // EXPLICITLY enable all player components before calling EnableThirdPersonMode
        if (playerCharacter != null)
        {
            // Enable third person controller
            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = true;
            }
            
            // Enable StarterAssets input component
            var starterInput = playerCharacter.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (starterInput != null)
            {
                starterInput.enabled = true;
            }
            
            // Enable PlayerInput component (Unity Input System)
            #if ENABLE_INPUT_SYSTEM
            var playerInput = playerCharacter.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }
            #endif
            
            // Enable character controller
            var charController = playerCharacter.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = true;
            }
            
            // Enable animator
            var animator = playerCharacter.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
            }
            
            // Re-enable rigidbody physics if present
            var rb = playerCharacter.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
        
        // Enable third person mode to restore all player input (calls the same logic again to be safe)
        EnableThirdPersonMode();
    }
    
    void Awake()
    {
        // Make this manager persist across scenes
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        InitializeReferences();
    }
    
    void InitializeReferences()
    {
        mainCamera = Camera.main;
        
        // Auto-find player in scene if not assigned
        if (playerCharacter == null)
        {
            FindPlayerInScene();
        }
        
        // Auto-find third person camera if not assigned
        if (thirdPersonCamera == null && playerCharacter != null)
        {
            // Look for camera in player hierarchy
            Camera[] cameras = playerCharacter.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                if (cam.gameObject.name.Contains("Camera") || cam.gameObject.name.Contains("Follow"))
                {
                    thirdPersonCamera = cam.gameObject;
                    break;
                }
            }
            
            // If still not found, use main camera
            if (thirdPersonCamera == null && mainCamera != null)
            {
                thirdPersonCamera = mainCamera.gameObject;
            }
        }
        
        // Setup RTS camera if not assigned
        if (rtsCamera == null)
        {
            SetupRTSCamera();
        }
        else
        {
            rtsCameraController = rtsCamera.GetComponent<RTSCameraController>();
            if (rtsCameraController == null)
            {
                rtsCameraController = rtsCamera.AddComponent<RTSCameraController>();
            }
        }
        
        // Get third person controller reference
        if (thirdPersonController == null && playerCharacter != null)
        {
            thirdPersonController = playerCharacter.GetComponent<ThirdPersonController>();
        }
        
        // Set initial mode - only call SetCameraMode if starting in RTS, otherwise just ensure RTS is disabled
        if (currentMode == CameraMode.RTS)
        {
            SetCameraMode(CameraMode.RTS);
        }
        else
        {
            // Just ensure RTS camera is disabled, don't touch player components
            if (rtsCamera != null)
            {
                Camera rtsCam = rtsCamera.GetComponent<Camera>();
                if (rtsCam != null) rtsCam.enabled = false;
                
                // Remove AudioListener from RTS camera if it exists
                AudioListener rtsListener = rtsCamera.GetComponent<AudioListener>();
                if (rtsListener != null) Destroy(rtsListener);
                
                if (rtsCameraController != null)
                    rtsCameraController.enabled = false;
            }
            isRTSModeActive = false;
        }
    }
    
    void FindPlayerInScene()
    {
        // Try to find by tag
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            playerCharacter = taggedPlayer;
            return;
        }
        
        // Try to find by ThirdPersonController component
        ThirdPersonController controller = FindObjectOfType<ThirdPersonController>();
        if (controller != null)
        {
            playerCharacter = controller.gameObject;
            return;
        }
        
        // Try to find by name
        playerCharacter = GameObject.Find("PlayerArmature");
        if (playerCharacter == null)
        {
            playerCharacter = GameObject.Find("Player");
        }
    }
    
    void Update()
    {
        // Don't allow RTS mode in ManagerScene and keep cursor visible
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "ManagerScene")
        {
            // Always keep cursor visible and unlocked in ManagerScene
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        
        // Toggle camera mode
        if (Input.GetKeyDown(toggleModeKey))
        {
            ToggleCameraMode();
        }
        
        // Block player input when in RTS mode
        if (isRTSModeActive && playerCharacter != null)
        {
            // Re-find third person controller if null
            if (thirdPersonController == null)
            {
                thirdPersonController = playerCharacter.GetComponent<ThirdPersonController>();
            }
            
            // Force disable ALL player components every frame
            var starterInput = playerCharacter.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (starterInput != null && starterInput.enabled)
            {
                starterInput.enabled = false;
            }
            
            #if ENABLE_INPUT_SYSTEM
            var playerInput = playerCharacter.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null && playerInput.enabled)
            {
                playerInput.enabled = false;
            }
            #endif
            
            var charController = playerCharacter.GetComponent<CharacterController>();
            if (charController != null && charController.enabled)
            {
                charController.enabled = false;
            }
            
            if (thirdPersonController != null && thirdPersonController.enabled)
            {
                thirdPersonController.enabled = false;
            }
            
            var animator = playerCharacter.GetComponent<Animator>();
            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
            
            var rb = playerCharacter.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
            }
        }
        
        // Update message timer
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
        }
    }
    
    void SetupRTSCamera()
    {
        // Create RTS camera GameObject as root object (not parented)
        rtsCamera = new GameObject("RTS Camera");
        
        // Mark as DontDestroyOnLoad so it persists across scenes
        DontDestroyOnLoad(rtsCamera);
        
        // Add camera component
        Camera rtsCam = rtsCamera.AddComponent<Camera>();
        rtsCam.enabled = false;
        
        // Position will be set when switching to RTS mode based on player position
        // For now, just set rotation
        rtsCamera.transform.rotation = Quaternion.Euler(rtsCameraRotation);
        
        // Add RTS controller
        rtsCameraController = rtsCamera.AddComponent<RTSCameraController>();
        
        // Don't add AudioListener - third person camera keeps it
    }
    
    public void ToggleCameraMode()
    {
        CameraMode newMode = currentMode == CameraMode.ThirdPerson ? CameraMode.RTS : CameraMode.ThirdPerson;
        SetCameraMode(newMode);
    }
    
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        
        switch (mode)
        {
            case CameraMode.ThirdPerson:
                EnableThirdPersonMode();
                break;
                
            case CameraMode.RTS:
                EnableRTSMode();
                break;
        }
        
        // Show mode change message
        if (showModeChangeMessage)
        {
            currentModeMessage = $"Camera Mode: {mode}";
            messageTimer = messageDisplayTime;
        }
    }
    
    void EnableThirdPersonMode()
    {
        isRTSModeActive = false;
        
        // Enable third person camera
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.SetActive(true);
            Camera tpCam = thirdPersonCamera.GetComponent<Camera>();
            if (tpCam != null) tpCam.enabled = true;
            
            AudioListener tpListener = thirdPersonCamera.GetComponent<AudioListener>();
            if (tpListener != null) tpListener.enabled = true;
        }
        
        // Disable RTS camera
        if (rtsCamera != null)
        {
            Camera rtsCam = rtsCamera.GetComponent<Camera>();
            if (rtsCam != null) rtsCam.enabled = false;
            
            AudioListener rtsListener = rtsCamera.GetComponent<AudioListener>();
            if (rtsListener != null) rtsListener.enabled = false;
            
            rtsCameraController.enabled = false;
        }
        
        // Enable ALL player input and movement components
        if (playerCharacter != null)
        {
            // Enable third person controller
            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = true;
            }
            
            // Enable StarterAssets input component
            var starterInput = playerCharacter.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (starterInput != null)
            {
                starterInput.enabled = true;
            }
            
            // Enable PlayerInput component (Unity Input System)
            #if ENABLE_INPUT_SYSTEM
            var playerInput = playerCharacter.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }
            #endif
            
            // Enable character controller
            var charController = playerCharacter.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = true;
            }
            
            // Enable animator
            var animator = playerCharacter.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
            }
            
            // Re-enable rigidbody physics if present
            var rb = playerCharacter.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
        
        // Lock cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void EnableRTSMode()
    {
        isRTSModeActive = true;
        
        // Disable third person camera (but keep its AudioListener active)
        if (thirdPersonCamera != null)
        {
            Camera tpCam = thirdPersonCamera.GetComponent<Camera>();
            if (tpCam != null) tpCam.enabled = false;
        }
        
        // Enable RTS camera (without AudioListener)
        if (rtsCamera != null)
        {
            // Position RTS camera directly above player's current position FIRST
            if (playerCharacter != null)
            {
                // Get player's world position (works even if nested under terrain)
                Vector3 playerWorldPos = playerCharacter.transform.position;
                
                // Set camera world position
                rtsCamera.transform.position = playerWorldPos + rtsCameraOffset;
                rtsCamera.transform.rotation = Quaternion.Euler(rtsCameraRotation);
            }
            
            Camera rtsCam = rtsCamera.GetComponent<Camera>();
            if (rtsCam != null) rtsCam.enabled = true;
            
            // Make sure RTS camera doesn't have AudioListener
            AudioListener rtsListener = rtsCamera.GetComponent<AudioListener>();
            if (rtsListener != null) Destroy(rtsListener);
            
            rtsCameraController.enabled = true;
        }
        
        // Disable ALL player input and movement components
        if (playerCharacter != null)
        {
            // Disable third person controller
            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = false;
            }
            
            // Disable StarterAssets input component
            var starterInput = playerCharacter.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (starterInput != null)
            {
                starterInput.enabled = false;
            }
            
            // Disable PlayerInput component (Unity Input System)
            #if ENABLE_INPUT_SYSTEM
            var playerInput = playerCharacter.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
            #endif
            
            // Disable character controller to prevent physics movement
            var charController = playerCharacter.GetComponent<CharacterController>();
            if (charController != null)
            {
                charController.enabled = false;
            }
            
            // Disable animator to freeze animations
            var animator = playerCharacter.GetComponent<Animator>();
            if (animator != null)
            {
                animator.enabled = false;
            }
            
            // Disable rigidbody if present
            var rb = playerCharacter.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
        
        // Show cursor for RTS controls
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void OnGUI()
    {
        if (messageTimer > 0f && showModeChangeMessage)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            
            // Add shadow for better visibility
            float yPos = Screen.height - 100;
            GUI.color = Color.black;
            GUI.Label(new Rect(Screen.width / 2 - 149, yPos + 1, 300, 50), currentModeMessage, style);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 150, yPos, 300, 50), currentModeMessage, style);
        }
        
        // Show toggle hint
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 14;
        hintStyle.normal.textColor = new Color(1, 1, 1, 0.7f);
        GUI.Label(new Rect(10, Screen.height - 30, 300, 20), 
            $"Press [{toggleModeKey}] to toggle camera mode", hintStyle);
    }
    
    // Public getters
    public CameraMode GetCurrentMode() => currentMode;
    public bool IsRTSMode() => currentMode == CameraMode.RTS;
    public bool IsThirdPersonMode() => currentMode == CameraMode.ThirdPerson;
    
    void OnDrawGizmos()
    {
        // Visualize RTS camera position in Scene view
        if (rtsCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rtsCamera.transform.position, 2f);
            Gizmos.DrawLine(rtsCamera.transform.position, rtsCamera.transform.position + rtsCamera.transform.forward * 10f);
        }
        
        if (playerCharacter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerCharacter.transform.position, 1f);
            
            // Draw line from player to where RTS camera should be
            Vector3 targetPos = playerCharacter.transform.position + rtsCameraOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerCharacter.transform.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, 1.5f);
        }
    }
}
