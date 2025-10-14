using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Combat Input Values")]
		public bool primaryAttack;
		public bool secondaryAttack;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnPrimaryAttack(InputValue value)
		{
			PrimaryAttackInput(value.isPressed);
		}

		public void OnSecondaryAttack(InputValue value)
		{
			SecondaryAttackInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void PrimaryAttackInput(bool newPrimaryAttackState)
		{
			primaryAttack = newPrimaryAttackState;
		}

		public void SecondaryAttackInput(bool newSecondaryAttackState)
		{
			secondaryAttack = newSecondaryAttackState;
		}

		private void Start()
		{
			SetCursorState(cursorLocked);
			
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
			// Ensure PlayerInput is enabled after scene transitions
			ValidateInputSystem();
#endif
		}

		private void Update()
		{
			// Toggle cursor lock with ESC key
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				cursorLocked = !cursorLocked;
				SetCursorState(cursorLocked);
			}
			
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
			// Check if input system needs re-initialization (happens after scene transitions)
			if (Time.frameCount % 60 == 0) // Check every 60 frames to avoid performance issues
			{
				ValidateInputSystem();
			}
#endif
		}

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		private void ValidateInputSystem()
		{
			PlayerInput playerInput = GetComponent<PlayerInput>();
			if (playerInput != null)
			{
				// Re-enable if disabled
				if (!playerInput.enabled)
				{
					playerInput.enabled = true;
				}
				
				// Force keyboard and mouse control scheme
				if (playerInput.currentControlScheme != "KeyboardMouse")
				{
					playerInput.SwitchCurrentControlScheme("KeyboardMouse", Keyboard.current, Mouse.current);
				}
				
				// Ensure actions are enabled
				if (playerInput.actions != null && !playerInput.actions.enabled)
				{
					playerInput.actions.Enable();
				}
			}
		}
#endif

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;
		}
	}
}