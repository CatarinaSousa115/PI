using UnityEngine;

namespace MuseumGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float sprintSpeed = 5.5f;
        public float jumpHeight = 1.1f;
        public float gravity = -20f;

        [Header("Look")]
        public Transform cameraRoot;
        public float lookSensitivity = 2f;
        public float maxLookAngle = 80f;
        public bool lockCursorOnPlay = true;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (cameraRoot == null && Camera.main != null)
                cameraRoot = Camera.main.transform;
        }

        void Start()
        {
            if (lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void Update()
        {
            HandleLook();
            HandleMovement();
            HandleCursorToggle();
        }

        private void HandleLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

            if (cameraRoot != null)
                cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;

            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;

                if (Input.GetButtonDown("Jump"))
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * currentSpeed;
            velocity.y = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(0) && lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
