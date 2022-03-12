using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private Transform playerCamera;

    // Head Controls
    [Header("Head Controls")]
    [Range(0.1f, 10f)]
    public float headSensitivity = 2;
    [SerializeField] private float mouseX = 0;
    [SerializeField] private float mouseY = 0;

    // Body Controls
    [Header("Body Controls")]
    [Range(0.1f, 1f)]
    public float bodySensitivity = 0.5f;
    [SerializeField] private float yaw = 0;
    [SerializeField] private float pitch = 0;
    [SerializeField] private float roll = 0;

    // Movement Controls
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20;
    [SerializeField] private float velocity;
    [SerializeField] private bool movementLockToCamera = true;
    private float moveForward;
    private float moveRight;
    private Vector3 moveDirection;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCamera = GameObject.Find("Player Camera").GetComponent<Transform>();
    }

    
    void Update()
    {
        Movement();
        HeadControl();
        BodyControl();
        DebugInputs();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }
    private void HeadControl()
    {
        mouseX += Input.GetAxis("Mouse X") * headSensitivity;
        mouseY -= Input.GetAxis("Mouse Y") * headSensitivity;

        playerCamera.localEulerAngles = new Vector2(mouseY, mouseX);

    }

    private void BodyControl()
    {
        pitch = Input.GetAxis("Numpad Pitch") * bodySensitivity;
        yaw = Input.GetAxis("Numpad Yaw") * bodySensitivity;
        roll = Input.GetAxis("Numpad Roll") * bodySensitivity;

        transform.Rotate(new Vector3(-pitch, yaw, -roll));
    }

    private void Movement()
    {
        // WASD
        moveForward = Input.GetAxis("Vertical");
        moveRight = Input.GetAxis("Horizontal");

        if (!movementLockToCamera)
            moveDirection = transform.forward * moveForward + transform.right * moveRight;
        else
            moveDirection = playerCamera.forward * moveForward + playerCamera.right * moveRight;

        velocity = rb.velocity.magnitude;
    }

    private void MovePlayer()
    {
        rb.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Acceleration);
    }

    private void DebugInputs()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (movementLockToCamera)
            {
                transform.rotation = playerCamera.rotation;
                mouseX = 0;
                mouseY = 0;
            }
            movementLockToCamera = movementLockToCamera ? false : true;
        }
    }
}
