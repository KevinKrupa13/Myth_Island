using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class PlayerController : NetworkBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private float animationSmoothTime = 0.1f;
    [SerializeField]
    private float animationPlayTransition = 0.1f;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private float groundDrag;
    [SerializeField]
    private float jumpForce;
    [SerializeField]
    private float jumpCooldown;

    [Header("Crouching")]
    [SerializeField]
    private float crouchHeight;

    [Header("Movement Speeds")]
    [SerializeField]
    private float sprintSpeed;
    [SerializeField]
    private float crouchSpeed;

    [Header("Keybinds")]
    [SerializeField]
    private KeyCode jumpKey = KeyCode.Space;
    [SerializeField]
    private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField]
    private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField]
    private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField]
    private KeyCode shootKey = KeyCode.Mouse0;

    [Header("Ground Check")]
    [SerializeField]
    private float playerHeight;
    [SerializeField]
    private LayerMask whatIsGround;

    [Header("Aim")]
    [SerializeField]
    private Transform aimTarget;
    [SerializeField]
    private float aimDistance = 1f;
    [SerializeField]
    private Canvas hipCanvas;
    [SerializeField]
    private Canvas aimCanvas;

    [Header("Camera")]
    [SerializeField]
    private Transform cameraPosition;
    [SerializeField]
    private GameObject cameraObject;

    [Header("Mouse")]
    [SerializeField]
    private float mouseSensitivity = 100f;

    [Header("Orientation")]
    [SerializeField]
    private Transform orientation;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;
    CapsuleCollider capsule;
    Animator playerAnim;
    int jumpAnimation;
    int moveXAnimationParameterID;
    int moveZAnimationparameterID;
    int stateAnimationParameterID;
    Vector2 currentAnimationBlend;
    Vector2 animationVelocity;

    // Camera Constants
    float xRotation = 0f;
    float YRotation = 0f;
    Camera cam;
    AudioListener aud;

    float startHeight;
    Vector3 startCenter;

    bool readyToJump = false;
    bool grounded;

    enum state
    {
        walking,
        sprinting,
        crouching,
        jumping,
    };

    state currState = state.walking;
    state prevState = state.walking;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerAnim = GetComponent<Animator>();
        cam = cameraObject.GetComponent<Camera>();
        aud = cameraObject.GetComponent<AudioListener>();

        moveXAnimationParameterID = Animator.StringToHash("MoveX");
        moveZAnimationparameterID = Animator.StringToHash("MoveZ");
        stateAnimationParameterID = Animator.StringToHash("State");
        jumpAnimation = Animator.StringToHash("Pistol Jump");
        rb.freezeRotation = true;
        readyToJump = true;
        aimCanvas.enabled = false;
        hipCanvas.enabled = true;
        startHeight = capsule.height;
        startCenter = capsule.center;
    }

    private void Update()
    {
        // ground check
        if (!IsOwner) return;
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();
        UpdateCamera();
        UpdateRemyRotation();
        Aim();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        MovePlayer();
        AnimatePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump

        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        if (Input.GetKeyDown(sprintKey))
        {
            currState = state.sprinting;
        }
        if (Input.GetKeyUp(sprintKey))
        {
            currState = state.walking;
        }
        //Crouching Controls
        if (Input.GetKeyDown(crouchKey))
        {
            currState = state.crouching;
            capsule.height = crouchHeight;
            capsule.center = new Vector3(0, (capsule.height * 0.5f) - 0.2f);
        }
        else if (Input.GetKeyUp(crouchKey))
        {
            currState = state.walking;
            capsule.height = startHeight;
            capsule.center = startCenter;
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        Vector2 input = new(horizontalInput, verticalInput);
        currentAnimationBlend = Vector2.SmoothDamp(currentAnimationBlend, input, ref animationVelocity, animationSmoothTime);
        float horizontalMoveDirection = horizontalInput == 0 ? 0f : currentAnimationBlend.x;
        float verticalMoveDirection = verticalInput == 0 ? 0f : currentAnimationBlend.y;
        moveDirection = orientation.forward * verticalMoveDirection + orientation.right * horizontalMoveDirection;

        // on ground
        if (grounded)
        {
            if (currState == state.sprinting)
            {
                rb.AddForce(moveDirection.normalized * sprintSpeed * 10f, ForceMode.Force);
            }
            else if (currState == state.crouching)
            {
                rb.AddForce(moveDirection.normalized * crouchSpeed * 10f, ForceMode.Force);
            }
            else
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }

        }

        // in air
        else if (!grounded)
        {
            if (prevState == state.sprinting) {
                rb.AddForce(moveDirection.normalized * sprintSpeed * 10f, ForceMode.Force);
            }

            if (prevState == state.walking) {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }
        }

    }

    private void AnimatePlayer()
    {
        if (!IsOwner) return;
        playerAnim.SetFloat(moveXAnimationParameterID, currentAnimationBlend.x);
        playerAnim.SetFloat(moveZAnimationparameterID, currentAnimationBlend.y);
        playerAnim.SetFloat(stateAnimationParameterID, (int)currState);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        prevState = currState;
        currState = state.jumping;
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        playerAnim.CrossFade(jumpAnimation, animationPlayTransition);
    }

    private void ResetJump()
    {
        currState = prevState;
        readyToJump = true;
    }

    private void UpdateCamera() {
        if (!IsOwner) return;
        cam.enabled = true;
        aud.enabled = true;
        aimTarget.position = cam.transform.position + cam.transform.forward * aimDistance;
        cameraObject.transform.position = cameraPosition.position;
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
    
        //control rotation around x axis (Look up and down)
        xRotation -= mouseY;
    
        //we clamp the rotation so we cant Over-rotate (like in real life)
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
    
        //control rotation around y axis (Look up and down)
        YRotation += mouseX;
    
        //applying both rotations
        cameraObject.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        //transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void Aim() {
        if (!IsOwner) return;
        if (Input.GetKeyDown(aimKey)) {
            aimCanvas.enabled = true;
            hipCanvas.enabled = false;
        }

        if (Input.GetKeyUp(aimKey)) {
            aimCanvas.enabled = false;
            hipCanvas.enabled = true;
        }
    }

    private void UpdateRemyRotation() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
 
        //control rotation around x axis (Look up and down)
        xRotation -= mouseY;
 
        //we clamp the rotation so we cant Over-rotate (like in real life)
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
 
        //control rotation around y axis (Look up and down)
        YRotation += mouseX;
 
        //applying both rotations
        transform.localRotation = Quaternion.Euler(0f, YRotation, 0f);
        //transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }
}
