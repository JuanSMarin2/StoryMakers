using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CharacterDraggable : MonoBehaviour
{
    [Header("Drag")]
    [SerializeField] private bool isDraggable = true;
    [SerializeField] private bool allowMoveX = true;
    [SerializeField] private bool allowMoveY = true;
    [SerializeField] private bool debugLogs = false;

    [Header("Index [copy, character]")]
    [SerializeField] private int copyIndex;
    [SerializeField] private int characterIndex;

    private Camera cachedCamera;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private bool isDragging;
    private int activeFingerId = -1;
    private float lastDragLogTime;

    private Rigidbody cachedRigidbody;
    private bool hasPendingRigidbodyMove;
    private Vector3 pendingRigidbodyPosition;

    public bool IsDraggable
    {
        get { return isDraggable; }
        set
        {
            isDraggable = value;
            if (!isDraggable)
            {
                EndDrag();
            }
        }
    }

    public int CopyIndex
    {
        get { return copyIndex; }
    }

    public int CharacterIndex
    {
        get { return characterIndex; }
    }

    public void SetDragIndex(int newCopyIndex, int newCharacterIndex)
    {
        copyIndex = Mathf.Max(0, newCopyIndex);
        characterIndex = Mathf.Max(0, newCharacterIndex);
    }

    private void Awake()
    {
        cachedCamera = Camera.main;
        cachedRigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        EndDrag();
    }

    private void Update()
    {
        if (!isDraggable)
        {
            return;
        }

        if (Touchscreen.current != null)
        {
            HandleTouchDrag();

            if (isDragging || IsAnyTouchActive())
            {
                return;
            }
        }

        HandleMouseDrag();
    }

    private void FixedUpdate()
    {
        if (!hasPendingRigidbodyMove || cachedRigidbody == null)
        {
            return;
        }

        if (cachedRigidbody.isKinematic)
        {
            cachedRigidbody.position = pendingRigidbodyPosition;
        }
        else
        {
            cachedRigidbody.MovePosition(pendingRigidbodyPosition);
        }

#if UNITY_6000_0_OR_NEWER
        cachedRigidbody.linearVelocity = Vector3.zero;
#else
        cachedRigidbody.velocity = Vector3.zero;
#endif
        cachedRigidbody.angularVelocity = Vector3.zero;

        hasPendingRigidbodyMove = false;
    }

    private void HandleMouseDrag()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (mouse.leftButton.wasReleasedThisFrame && activeFingerId == -1)
            {
                EndDrag();
            }

            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryBeginDrag(mouse.position.ReadValue(), -1);
        }

        if (isDragging && activeFingerId == -1 && mouse.leftButton.isPressed)
        {
            DragToScreenPosition(mouse.position.ReadValue());
        }

        if (mouse.leftButton.wasReleasedThisFrame && activeFingerId == -1)
        {
            EndDrag();
        }
    }

    private void HandleTouchDrag()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return;
        }

        var touches = touchscreen.touches;
        bool activeFingerFound = false;
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            if (!touch.press.isPressed && !touch.press.wasReleasedThisFrame)
            {
                continue;
            }

            int fingerId = touch.touchId.ReadValue();
            Vector2 touchPosition = touch.position.ReadValue();
            bool isOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);

            if (!isDragging)
            {
                if (touch.press.isPressed && !isOverUi)
                {
                    TryBeginDrag(touchPosition, fingerId);
                    if (isDragging)
                    {
                        return;
                    }
                }

                continue;
            }

            if (fingerId != activeFingerId)
            {
                continue;
            }

            activeFingerFound = true;

            if (touch.press.isPressed && !isOverUi)
            {
                DragToScreenPosition(touchPosition);
            }

            if (touch.press.wasReleasedThisFrame)
            {
                EndDrag();
            }

            return;
        }

        if (isDragging && !activeFingerFound)
        {
            EndDrag();
        }
    }

    private bool IsAnyTouchActive()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return false;
        }

        var touches = touchscreen.touches;
        for (int i = 0; i < touches.Count; i++)
        {
            if (touches[i].press.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    private void TryBeginDrag(Vector2 screenPosition, int fingerId)
    {
        Camera cam = GetCamera();
        if (cam == null)
        {
            LogDebug("TryBeginDrag: no active camera found.");
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit))
        {
            LogDebug(string.Format("TryBeginDrag: raycast miss at screen {0}.", screenPosition));
            return;
        }

        LogDebug(string.Format("TryBeginDrag: raycast hit {0}.", hit.transform.name));

        if (hit.transform != transform && !hit.transform.IsChildOf(transform))
        {
            LogDebug(string.Format("TryBeginDrag: hit {0} is not this object {1}.", hit.transform.name, transform.name));
            return;
        }

        // Keep drag mapped to screen movement by using a plane facing the active camera.
        dragPlane = new Plane(-cam.transform.forward, transform.position);

        float enter;
        if (!dragPlane.Raycast(ray, out enter))
        {
            LogDebug("TryBeginDrag: plane intersection failed.");
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        dragOffset = transform.position - hitPoint;
        isDragging = true;
        activeFingerId = fingerId;
        LogDebug(string.Format("Drag started for {0} | copy={1} character={2} finger={3}.", transform.name, copyIndex, characterIndex, activeFingerId));
    }

    private void DragToScreenPosition(Vector2 screenPosition)
    {
        Camera cam = GetCamera();
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(screenPosition);
        float enter;
        if (!dragPlane.Raycast(ray, out enter))
        {
            LogDebug("DragToScreenPosition: plane intersection failed while dragging.");
            return;
        }

        Vector3 target = ray.GetPoint(enter) + dragOffset;
        Vector3 current = transform.position;

        if (!allowMoveX)
        {
            target.x = current.x;
        }

        if (!allowMoveY)
        {
            target.y = current.y;
        }

        target.z = current.z;

        if (cachedRigidbody != null)
        {
            pendingRigidbodyPosition = target;
            hasPendingRigidbodyMove = true;
        }
        else
        {
            transform.position = target;
        }

        if (Time.unscaledTime - lastDragLogTime >= 0.15f)
        {
            lastDragLogTime = Time.unscaledTime;
            LogDebug(string.Format("Dragging {0} -> position {1}.", transform.name, transform.position));
        }
    }

    private Camera GetCamera()
    {
        if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
        {
            return cachedCamera;
        }

        cachedCamera = Camera.main;
        if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
        {
            return cachedCamera;
        }

        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam != null && cam.isActiveAndEnabled && cam.gameObject.activeInHierarchy)
            {
                cachedCamera = cam;
                return cachedCamera;
            }
        }

        return cachedCamera;
    }

    private void EndDrag()
    {
        if (isDragging)
        {
            LogDebug(string.Format("Drag ended for {0} at {1}.", transform.name, transform.position));
        }

        isDragging = false;
        activeFingerId = -1;
        hasPendingRigidbodyMove = false;
    }

    private void LogDebug(string message)
    {
        if (!debugLogs)
        {
            return;
        }

        Debug.Log(string.Format("CharacterDraggable [{0},{1}] {2}", copyIndex, characterIndex, message), this);
    }
}
