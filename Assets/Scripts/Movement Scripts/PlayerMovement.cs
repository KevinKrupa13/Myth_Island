using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;

public class PlayerMovement : NetworkBehaviour
{

    [Header("Animation")]
    [SerializeField]
    private float animationSmoothTime = 0.1f;
    [SerializeField]
    private float animationPlayTransition = 0.1f;

    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;

    [Header("Crouching")]
    public float crouchHeight;
    private float startHeight;
    private Vector3 startCenter;

    [Header("Movement Speeds")]
    public float sprintSpeed;
    public float crouchSpeed;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Objects")]
    public Transform orientation;

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

    bool readyToJump = false;

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
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerAnim = GetComponent<Animator>();

        moveXAnimationParameterID = Animator.StringToHash("MoveX");
        moveZAnimationparameterID = Animator.StringToHash("MoveZ");
        stateAnimationParameterID = Animator.StringToHash("State");
        jumpAnimation = Animator.StringToHash("Pistol Jump");
        rb.freezeRotation = true;
        readyToJump = true;
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
        print(currState);
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
}
