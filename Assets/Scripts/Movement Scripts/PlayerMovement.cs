using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;

    [Header("Crouching")]
    public float crouchHeight;
    private float startHeight;

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

    int moveXAnimationParameterID;
    int moveZAnimationparameterID;

    bool readyToJump = false;
    bool sprinting = false;
    bool walking = false;
    bool jumping = false;
    bool crouching = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        playerAnim = GetComponent<Animator>();

        moveXAnimationParameterID = Animator.StringToHash("MoveX");
        moveZAnimationparameterID = Animator.StringToHash("MoveZ");
        rb.freezeRotation = true;
        readyToJump = true;
        startHeight = capsule.height;
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

        sprinting = Input.GetKey(sprintKey);

        //Crouching Controls
        if (Input.GetKeyDown(crouchKey))
        {
            crouching = true;
            capsule.height = crouchHeight;
            //rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        else if (Input.GetKeyUp(crouchKey))
        {
            capsule.height = startHeight;
            crouching = false;
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if (grounded)
        {
            if (sprinting && !crouching)
            {
                rb.AddForce(moveDirection.normalized * sprintSpeed * 10f, ForceMode.Force);
            }
            else if (crouching && !sprinting)
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
            if (sprinting)
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * sprintSpeed * airMultiplier, ForceMode.Force);
            }
            else
            {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            }
        }
    }

    private void AnimatePlayer()
    {
        if (!IsOwner) return;
        playerAnim.SetFloat(moveXAnimationParameterID, horizontalInput);
        playerAnim.SetFloat(moveZAnimationparameterID, verticalInput);
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

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
        jumping = true;
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        jumping = false;
        readyToJump = true;
    }
}
