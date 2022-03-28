using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private Transform xrOrigin;
    private Transform playerCamera;
    [SerializeField] private Vector3 moveDirection;
    [Range(0f, 10f)]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Vector2 rotationDirection;
    [Range(0f, 1f)]
    public float rotationSpeed = 1f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false;
        rb.drag = 6;
    }

    void Update()
    {

    }

    void FixedUpdate()
    {
        rb.AddForce(moveDirection);
    }

    public void MoveHorizontal(InputAction.CallbackContext context)
    {
        Vector2 direction = -context.ReadValue<Vector2>();
        moveDirection = new Vector3(direction.x, 0, direction.y) * moveSpeed;
    }

    public void Look(InputAction.CallbackContext context)
    {
        rotationDirection = context.ReadValue<Vector2>();
        transform.Rotate(new Vector3(0, rotationDirection.x, rotationDirection.y) * rotationSpeed);
    }
}
