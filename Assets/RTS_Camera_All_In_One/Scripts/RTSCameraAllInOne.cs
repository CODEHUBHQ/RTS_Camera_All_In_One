// unity
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CODEHUB.RTSCameraAllInOne
{
    public class RTSCameraAllInOne : MonoBehaviour
    {
        #region Camera Settings

        [Header("References")]
        [Space()]
        [SerializeField] private Camera mainCamera;

        [Header("Settings")]
        [Space()]
        [SerializeField] private bool usePhysicsScene = false;

        [Header("Movement Settings")]
        [Space()]
        [SerializeField] private bool useKeyboardInput = true;
        [SerializeField, Tooltip("speed with keyboard movement")] private float keyboardMovementSpeed = 5f;

        [Header("ScreenEdge Settings")]
        [Space()]
        [SerializeField] private bool useScreenEdgeInput = true;
        [SerializeField] private float screenEdgeBorder = 25f;
        [SerializeField, Tooltip("speed with screen edge movement")] private float screenEdgeMovementSpeed = 3f;

        [Header("Pan Settings")]
        [Space()]
        [SerializeField] private bool usePanning = true;
        [SerializeField] private float panningSpeed = 10f;

        [Header("MapLimit Settings")]
        [Space()]
        [SerializeField] private bool limitMap = true;
        [SerializeField, Tooltip("x limit of map")] private float limitX = 50f;
        [SerializeField, Tooltip("z limit of map")] private float limitY = 50f;

        [Header("Rotation Settings")]
        [Space()]
        [SerializeField] private bool useRotationLimit = true;
        [SerializeField] private Vector2 rotationLimitX = new Vector2(15f, 90);
        [SerializeField] private Vector2 rotationLimitY = new Vector2(-360f, 360f);
        [SerializeField, Tooltip("X-axis camera position offset value.")] private float offsetX = -15;
        [SerializeField, Tooltip("Z-axis camera position offset value.")] private float offsetZ = -15;

        [Header("Keyboard Rotation Settings")]
        [Space()]
        [SerializeField] private bool useKeyboardRotation = true;
        [SerializeField] private Key rotateRightKey = Key.X;
        [SerializeField] private Key rotateLeftKey = Key.Z;
        [SerializeField] private float keyboardRotationSpeed = 50f;

        [Header("Mouse Rotation Settings")]
        [Space()]
        [SerializeField] private bool useMouseRotation = true;
        [SerializeField] private float mouseRotationSpeed = 10f;

        [Header("Height Settings")]
        [Space()]
        [SerializeField] private bool autoHeight = true;
        [SerializeField] private float heightDampening = 0.0f;

        [SerializeField] private LayerMask groundMask = -1;

        [Header("Zoom Settings")]
        [Space()]
        [SerializeField] private bool zoomInvert = true;
        [SerializeField] private bool usePivot = true;
        [SerializeField] private float pivotMinHeightRange = 5.0f;
        [SerializeField] private float pivotTargetAngle = 25.0f;
        [SerializeField] private float pivotSpeed = 25.0f;
        [SerializeField, Tooltip("The height that the main camera starts with.")] private float initialZoomHeight = 18f;
        [SerializeField, Tooltip("max zoom height")] private float maxZoomHeight = 25f;
        [SerializeField, Tooltip("min zoom height")] private float minZoomHeight = 5f;
        [SerializeField, Tooltip("Defines the initial rotation of the main camera.")]
        private Vector3 initialEulerAngles = new Vector3(45.0f, 45.0f, 0.0f);

        [Header("Keyboard Zoom Settings")]
        [Space()]
        [SerializeField] private bool useKeyboardZoom = true;
        [SerializeField] private Key zoomInKey = Key.E;
        [SerializeField] private Key zoomOutKey = Key.Q;
        [SerializeField] private float keyboardZoomSensitivity = 2f;

        [Header("ScrollWheel Zoom Settings")]
        [Space()]
        [SerializeField] private bool useScrollwheelZoom = true;
        [SerializeField] private float scrollWheelZoomSensitivity = 25f;

        #endregion

        private Controls controls;

        private PhysicsScene physicsScene;

        private Vector2 keybaordInput = Vector2.zero;
        private Vector3 currMoveDirection = Vector3.zero;
        private Vector3 lastMoveDirection = Vector3.zero;
        private Vector3 currPanDirection = Vector3.zero;
        private Vector3 lastPanDirection = Vector3.zero;
        private Vector3 zoomDirection = Vector3.zero;
        private Vector3 lastCamPos = Vector3.zero;
        private Vector2 currRotationValue = Vector2.zero;
        private Vector2 lastRotationValue = Vector2.zero;
        private Vector3 rotateAroundCenter = Vector3.zero;

        private float zoomValue;
        private float lastZoomValue;
        private float zoomSpeed;
        private float lastOffset;
        private float accumOffset;
        private float rotationSpeed;

        private bool isEnabled = true;
        private bool isPivoting;
        private bool isResettingPivoting;
        private bool isAddingOffset;

        private void Awake()
        {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, initialZoomHeight, mainCamera.transform.position.z);
        }

        private void Start()
        {
            controls = new Controls();

            controls.Player.Move.performed += SetKeyboardInput;
            controls.Player.Move.canceled += SetKeyboardInput;

            controls.Enable();
        }

        private void LateUpdate()
        {
            CameraUpdate();
        }

        private void SetKeyboardInput(InputAction.CallbackContext ctx)
        {
            keybaordInput = ctx.ReadValue<Vector2>();
        }

        /// <summary>
        /// handle camera movement, zoom and rotation
        /// </summary>
        private void CameraUpdate()
        {
            if (!Application.isFocused) return;
            if (!isEnabled) return;

            HandleCameraMovement();
            HandleCameraZoom();
            HandleCameraRotation();
        }

        private void HandleCameraMovement()
        {
            if (useKeyboardInput)
            {
                currMoveDirection.x = keybaordInput.x;
                currMoveDirection.z = keybaordInput.y;

                lastMoveDirection = Vector3.Lerp(lastMoveDirection, currMoveDirection, keyboardMovementSpeed);

                SetPosition(lastMoveDirection, keyboardMovementSpeed);
            }

            if (useScreenEdgeInput && !Mouse.current.middleButton.isPressed && !EventSystem.current.IsPointerOverGameObject())
            {
                currPanDirection = Vector3.zero;

                var mousePosition = Mouse.current.position.ReadValue();

                if (mousePosition.x <= screenEdgeBorder && mousePosition.x >= 0.0f)
                {
                    currPanDirection.x = -1f;
                }
                else if (mousePosition.x >= Screen.width - screenEdgeBorder && mousePosition.x <= Screen.width)
                {
                    currPanDirection.x = 1.0f;
                }

                if (mousePosition.y <= screenEdgeBorder && mousePosition.y >= 0.0f)
                {
                    currPanDirection.z = -1.0f;
                }
                else if (mousePosition.y >= Screen.height - screenEdgeBorder && mousePosition.y <= Screen.height)
                {
                    currPanDirection.z = 1.0f;
                }

                lastPanDirection = Vector3.Lerp(lastPanDirection, currPanDirection, screenEdgeMovementSpeed);

                SetPosition(lastPanDirection, screenEdgeMovementSpeed);
            }

            if (usePanning && Mouse.current.middleButton.isPressed && Mouse.current.delta.ReadValue() != Vector2.zero)
            {
                currPanDirection = Vector3.zero;

                var mouseAxis = Mouse.current.delta.ReadValue();

                currPanDirection = new Vector3(-mouseAxis.x, 0f, -mouseAxis.y);

                lastPanDirection = Vector3.Lerp(lastPanDirection, currPanDirection, panningSpeed);

                SetPosition(lastPanDirection, panningSpeed);
            }
        }

        private void SetPosition(Vector3 lastDirection, float speed)
        {
            var angle = Quaternion.Euler(new Vector3(0f, mainCamera.transform.eulerAngles.y, 0f));
            var position = mainCamera.transform.position + angle * lastDirection * speed * Time.deltaTime;

            var positionX = !limitMap ? position.x : Mathf.Clamp(position.x, limitX, limitY);
            var positionZ = !limitMap ? position.z : Mathf.Clamp(position.z, limitX, limitY);

            mainCamera.transform.position = new Vector3(positionX, position.y, positionZ);
        }

        private void HandleCameraZoom()
        {
            zoomValue = 0.0f;

            if (useKeyboardZoom && Mouse.current.scroll.ReadValue() == Vector2.zero)
            {
                zoomValue += ZoomDirection * Time.deltaTime * keyboardZoomSensitivity * (zoomInvert ? -1.0f : 1.0f);
                zoomSpeed = keyboardZoomSensitivity;
            }

            if (useScrollwheelZoom)
            {
                zoomValue += Mouse.current.scroll.ReadValue().y * Time.deltaTime * scrollWheelZoomSensitivity * (zoomInvert ? -1.0f : 1.0f);
                zoomSpeed = scrollWheelZoomSensitivity;
            }

            var screenPoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0.0f);

            ScreenPointToTerrainPoint(screenPoint, out Vector3 middleScreenTerrainPosition);
            ScreenPointToTerrainPoint(Mouse.current.position.ReadValue(), out Vector3 mouseTerrainPosition);

            zoomDirection = (mainCamera.transform.position - mouseTerrainPosition).normalized;

            HandleNearMinHeightPivot();
            ApplyTransformZoom(middleScreenTerrainPosition);
        }

        private void HandleNearMinHeightPivot()
        {
            if (!usePivot) return;

            var currHeight = mainCamera.transform.position.y;

            var isZoomingOut = Mouse.current.scroll.ReadValue().y == (zoomInvert ? -1.0f : 1.0f);
            var isZoomingIn = Mouse.current.scroll.ReadValue().y == (zoomInvert ? 1.0f : -1.0f);

            if (currHeight < minZoomHeight + pivotMinHeightRange && isZoomingOut)
            {
                lastCamPos = mainCamera.transform.position;

                isResettingPivoting = true;
                isPivoting = false;
            }

            if (currHeight < minZoomHeight + pivotMinHeightRange && isZoomingIn)
            {
                lastCamPos = mainCamera.transform.position;

                isResettingPivoting = false;
                isPivoting = true;
            }

            if (isPivoting && currHeight >= minZoomHeight + pivotMinHeightRange)
            {
                isPivoting = false;
            }

            if (isResettingPivoting && currHeight >= minZoomHeight + pivotMinHeightRange)
            {
                isResettingPivoting = false;
            }

            if (isResettingPivoting)
            {
                var nextEulerAngles = mainCamera.transform.rotation.eulerAngles;

                nextEulerAngles.x = Mathf.Lerp(nextEulerAngles.x, initialEulerAngles.x, pivotSpeed * Time.deltaTime);

                mainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);

                lastCamPos.y = Mathf.Lerp(lastCamPos.y, minZoomHeight + pivotMinHeightRange + 1f, pivotSpeed * Time.deltaTime);

                mainCamera.transform.position = lastCamPos;

                if (nextEulerAngles.x == initialEulerAngles.x)
                {
                    isPivoting = false;
                    isResettingPivoting = false;
                }
            }

            if (isPivoting)
            {
                var nextEulerAngles = mainCamera.transform.rotation.eulerAngles;

                nextEulerAngles.x = Mathf.Lerp(nextEulerAngles.x, pivotTargetAngle, pivotSpeed * Time.deltaTime);

                mainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);

                lastCamPos.y = Mathf.Lerp(lastCamPos.y, minZoomHeight, pivotSpeed * Time.deltaTime);

                mainCamera.transform.position = lastCamPos;

                if (nextEulerAngles.x == pivotTargetAngle)
                {
                    isPivoting = false;
                    isResettingPivoting = false;
                }
            }
        }

        private void ApplyTransformZoom(Vector3 middleScreenTerrainPosition)
        {
            var targetDirection = Vector3.zero;
            var lastCamPos = mainCamera.transform.position;

            // Handling terrain height offset elevation
            if (autoHeight && middleScreenTerrainPosition.y >= heightDampening)
            {
                if (!isAddingOffset || (lastOffset <= 0.0f && (middleScreenTerrainPosition.y - heightDampening - accumOffset) > 0.5f))
                {
                    lastOffset = (middleScreenTerrainPosition.y - heightDampening - accumOffset);
                    accumOffset += lastOffset;
                    isAddingOffset = true;
                }
            }
            else if (isAddingOffset)
            {
                lastOffset = accumOffset - lastOffset;
                accumOffset = 0;
                isAddingOffset = false;
            }

            if (lastOffset > 0.0f)
            {
                var change = Time.deltaTime * zoomSpeed;
                lastOffset -= change;
                targetDirection = (isAddingOffset ? -1.0f : 1.0f) * change * mainCamera.transform.forward;
            }

            // Handling actual zooming in/out
            lastZoomValue = Mathf.Lerp(lastZoomValue, zoomValue, zoomSpeed);

            targetDirection += zoomSpeed * Time.deltaTime * lastZoomValue * zoomDirection;

            mainCamera.transform.position += targetDirection;

            // Apply zooming limit
            if (mainCamera.transform.position.y < minZoomHeight || mainCamera.transform.position.y > maxZoomHeight)
            {
                lastCamPos.y = Mathf.Clamp(lastCamPos.y, minZoomHeight, maxZoomHeight);
                mainCamera.transform.position = lastCamPos;
            }
        }

        private bool ScreenPointToTerrainPoint(Vector3 pos, out Vector3 hitPos)
        {
            hitPos = default;

            var fwd = mainCamera.transform.TransformDirection(Vector3.forward);
            var ray = mainCamera.ScreenPointToRay(pos);

            if (usePhysicsScene && physicsScene != null)
            {
                if (physicsScene.Raycast(ray.origin, fwd, out RaycastHit physicsSceneHit, Mathf.Infinity, groundMask))
                {
                    hitPos = physicsSceneHit.point;

                    return true;
                }

                return false;
            }

            if (Physics.Raycast(ray.origin, fwd, out RaycastHit physicsHit, Mathf.Infinity, groundMask))
            {
                hitPos = physicsHit.point;

                return true;
            }

            return false;
        }

        private void HandleCameraRotation()
        {
            currRotationValue = Vector2.zero;

            if (useKeyboardRotation)
            {
                currRotationValue.x = RotationDirection;
                rotationSpeed = keyboardRotationSpeed;
                //transform.Rotate(Vector3.up, RotationDirection * Time.deltaTime * rotationSped, Space.World);
            }

            if (useMouseRotation && Mouse.current.rightButton.isPressed)
            {
                var mouseAxis = Mouse.current.delta.ReadValue();

                currRotationValue.x = mouseAxis.x;
                rotationSpeed = mouseRotationSpeed;
                //mainCamera.transform.Rotate(Vector3.up, mouseAxis.x * Time.deltaTime * mouseRotationSpeed, Space.World);
            }

            // Smoothly update the last rotation value towards the current one
            lastRotationValue = Vector2.Lerp(lastRotationValue, currRotationValue, rotationSpeed);

            var nextEulerAngles = mainCamera.transform.rotation.eulerAngles;

            // The position that the camera will be rotating around is the world position of the middle of the screen.
            // Only update it when the player is not actively rotating the camera..
            if (currRotationValue == Vector2.zero)
            {
                var screenPoint = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0.0f);

                rotateAroundCenter = MainCameraScreenToWorldPoint(screenPoint, applyOffset: false);
            }

            // orbit horizontally
            mainCamera.transform.RotateAround(rotateAroundCenter, Vector3.up, lastRotationValue.x * rotationSpeed * Time.deltaTime);

            // orbit vertically
            mainCamera.transform.RotateAround(rotateAroundCenter, mainCamera.transform.TransformDirection(Vector3.right), lastRotationValue.y * rotationSpeed * Time.deltaTime);

            nextEulerAngles = mainCamera.transform.eulerAngles;

            // Limit the y/x euler angless if that's enabled
            if (useRotationLimit)
            {
                nextEulerAngles.x = Mathf.Clamp(nextEulerAngles.x, rotationLimitX.x, rotationLimitX.y);
            }

            if (useRotationLimit)
            {
                nextEulerAngles.y = Mathf.Clamp(nextEulerAngles.y, rotationLimitY.x, rotationLimitY.y);
            }

            mainCamera.transform.rotation = Quaternion.Euler(nextEulerAngles);
        }

        private Vector3 MainCameraScreenToWorldPoint(Vector3 position, bool applyOffset = true)
        {
            position.z = mainCamera.transform.position.y;

            Vector3 worldPosition;

            if (usePhysicsScene)
            {
                var fwd = mainCamera.transform.TransformDirection(Vector3.forward);

                if (physicsScene.Raycast(mainCamera.ScreenPointToRay(position).origin, fwd, out RaycastHit physicsSceneHit, Mathf.Infinity, groundMask))
                {
                    worldPosition = physicsSceneHit.point;
                }
                else
                {
                    worldPosition = mainCamera.ScreenToWorldPoint(position);
                }
            }
            else
            {
                if (Physics.Raycast(mainCamera.ScreenPointToRay(position), out RaycastHit hit, Mathf.Infinity, groundMask))
                {
                    worldPosition = hit.point;
                }
                else
                {
                    worldPosition = mainCamera.ScreenToWorldPoint(position);
                }
            }

            var currOffsetX = (offsetX * mainCamera.transform.position.y) / initialZoomHeight;
            var currOffsetZ = (offsetZ * mainCamera.transform.position.y) / initialZoomHeight;

            return applyOffset ? new Vector3(worldPosition.x - currOffsetX, worldPosition.y, worldPosition.z - currOffsetZ) : worldPosition;
        }

        #region Getters And Setters

        private int ZoomDirection
        {
            get
            {
                var zoomIn = Keyboard.current[zoomInKey].isPressed;
                var zoomOut = Keyboard.current[zoomOutKey].isPressed;

                if (zoomIn && zoomOut)
                {
                    return 0;
                }
                else if (!zoomIn && zoomOut)
                {
                    return 1;
                }
                else if (zoomIn && !zoomOut)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int RotationDirection
        {
            get
            {
                bool rotateRight = Keyboard.current[rotateRightKey].isPressed;
                bool rotateLeft = Keyboard.current[rotateLeftKey].isPressed;

                if (rotateLeft && rotateRight)
                {
                    return 0;
                }
                else if (rotateLeft && !rotateRight)
                {
                    return -1;
                }
                else if (!rotateLeft && rotateRight)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public void SetPhysicsScene(PhysicsScene physicsScene)
        {
            this.physicsScene = physicsScene;
        }

        public void SetIsEnabled(bool isEnabled)
        {
            this.isEnabled = isEnabled;
        }

        #endregion
    }
}
