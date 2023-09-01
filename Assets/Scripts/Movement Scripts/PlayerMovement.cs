using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour {
    [Header("Animation")]
    public Animator playerAnim;

    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;

    [Header("Crouching")]

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

    bool readyToJump = false;
    bool sprinting = false;
    bool walking = false;
    bool jumping = false;
    bool crouching = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        rb.freezeRotation = true;
        readyToJump = true;
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
        
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        sprinting = Input.GetKey(sprintKey);

        if (Input.GetKeyDown(crouchKey)) {
            crouching = true;
        } else if (Input.GetKeyUp(crouchKey)) {
            crouching = false;
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if(grounded) {
            if (sprinting && !crouching) {
                rb.AddForce(moveDirection.normalized * sprintSpeed * 10f, ForceMode.Force);
            } else if (crouching && !sprinting) {
                rb.AddForce(moveDirection.normalized * crouchSpeed * 10f, ForceMode.Force);
            } else {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            }
            
        }

        // in air
        else if(!grounded) {
            if (sprinting) {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * sprintSpeed * airMultiplier, ForceMode.Force);
            } else {
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
            }
        }
    }

    private void AnimatePlayer() {

        if (!IsOwner) return;
        if (verticalInput > 0 && !playerAnim.GetBool("sprint")) {
            playerAnim.SetTrigger("jog_forward");
            playerAnim.ResetTrigger("idle");
            walking = true;
        }
        if (verticalInput < 0) {
            playerAnim.SetTrigger("jog_backward");
            playerAnim.ResetTrigger("idle");
            walking = true;
        }
        if (verticalInput == 0) {
            playerAnim.SetTrigger("idle");
            playerAnim.ResetTrigger("jog_forward");
            playerAnim.ResetTrigger("jog_backward");
            walking = false;
        }
        if (horizontalInput > 0) {
            playerAnim.SetTrigger("strafe_right");
            playerAnim.ResetTrigger("idle");
        }
        if (horizontalInput < 0) {
            playerAnim.SetTrigger("strafe_left");
            playerAnim.ResetTrigger("idle");
        }
        if (horizontalInput == 0) {
            playerAnim.SetTrigger("idle");
            playerAnim.ResetTrigger("strafe_left");
            playerAnim.ResetTrigger("strafe_right");
        }
        if (walking && !playerAnim.GetBool("jog_backward")) {
            if (sprinting) {
                playerAnim.SetTrigger("sprint");
                playerAnim.ResetTrigger("jog_forward");
            }
            if (!sprinting) {
                playerAnim.SetTrigger("jog_forward");
                playerAnim.ResetTrigger("sprint");
            }
        }
        if (jumping) {
            playerAnim.SetTrigger("jump");
            playerAnim.ResetTrigger("idle");
            playerAnim.ResetTrigger("jog_forward");
            playerAnim.ResetTrigger("jog_backward");
            playerAnim.ResetTrigger("sprint");
        }
        if (!jumping) {
            playerAnim.ResetTrigger("jump");
        }

    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if(flatVel.magnitude > moveSpeed)
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
