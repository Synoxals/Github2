using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem Dust;
    public ParticleSystem WallDust;

    [Header("Movement")]
    private Rigidbody2D rb;
    public float speed = 10f;
    public float airSpeed = 7f;

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    private float x;
    private float y;

    private bool doubleJump;
    [SerializeField] private bool isFacingRight = false;

    private bool canDash = true;
    private bool isDashing;

    [Header("Wall Sliding")]
    [SerializeField] private bool isWallSliding;
    [SerializeField] private float wallSlidingSpeed = 10f;

    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    
    [Header("Wall Jumping")]
    [SerializeField] private float wallJumpingDuration = 0.4f;
    [SerializeField] private Vector2 wallJumpingPower = new Vector2(5f, 8f);

    private float dashingPower = 24f;
    private float dashingTime = 0.2f;
    private float dashingCooldown = 1f;

    [Header("References")]
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] public Camera MainCam;

    public Transform height;
    private bool previousGrounded = false;
    private bool previousWalled = false;

    [Range(1, 10)]
    public float jumpVelocity;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        //Stops all input during dashes
        if (isDashing)
        {
            return;
        }

        //Retrieves unity built in axes and assigns them to variables x and y
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        Vector2 dir = new Vector2(x, y);

        //Runs the basic movement functions
        Walk(dir);
        WallSlide();
        WallJump();

        //Makes sure the character doesnt flip if they are currently wall jumping
        if (!isWallJumping)
        {
            Flip();
        }

        if (!isWallJumping)
        {
            rb.velocity = new Vector2(x * speed, rb.velocity.y);
        }

        //Reads the input to start a dash
        if (Input.GetKeyDown(KeyCode.Q) && canDash)
        {
            StartCoroutine(Dash());
        }

        //Resets the double jump boolean after landing
        if (isGrounded() && !Input.GetButton("Jump"))
        {
            doubleJump = false;
        }

        //Reads the input for a jump or double jump by checking if they are either on the ground or if they currently have a jump left
        if (Input.GetButtonDown ("Jump") && (isGrounded() || doubleJump))
        {
            rb.velocity = Vector2.up * jumpVelocity;

            doubleJump = !doubleJump;
        }

        //Triggers the stretch animation if the player jumps from the ground
        if (Input.GetButtonDown ("Jump") && isGrounded())
        {
            height.GetComponent<Animator>().SetTrigger("Stretch");
            CreateDust();
        }

        if (IsWalled() && !previousWalled)
        {
            StartCoroutine(Freeze());
        }


        //Checks if the player was previously on the ground in the last frame and if they are currently. If they weren't previously grounded but are on the ground now, the game knows they just landed this frame, so this statement triggers the squash animation.
        if (isGrounded() && !previousGrounded)
        {
            height.GetComponent<Animator>().SetTrigger("Squash");
        }

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }
        previousGrounded = isGrounded();
        previousWalled = IsWalled();
    }

    private bool isGrounded()
    {
        if (Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer))
        {
            return true;
        }
        else if (Physics2D.OverlapCircle(groundCheck.position, 0.2f, platformLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
        
    }
    private bool IsWalled()
    {
        if (Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer))
        {
            return true;
        }
        else if (Physics2D.OverlapCircle(wallCheck.position, 0.2f, platformLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void WallSlide()
    {
        if (IsWalled() && !isGrounded())
        {
            isWallSliding = true;
            CreateWallDust();
            Vector2 velStore = rb.velocity;
            StartCoroutine(Freeze());
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(velStore.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void Walk(Vector2 dir)
    {
        if (!isWallJumping)
        {
            rb.velocity = (new Vector2(dir.x * speed, rb.velocity.y));
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x; 
            wallJumpingCounter = wallJumpingTime;
            speed = airSpeed;


            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            CreateDust();
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);


            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }
    private void Flip()
    {
        if (isFacingRight && x < 0f || !isFacingRight && x > 0f && (!isWallJumping))
        {
            if (isGrounded()) { CreateDust(); }
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private IEnumerator Freeze()
    {
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.1f);
    }


    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(x * dashingPower, y * dashingPower);
        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }
    private void CreateDust()
    {
        Dust.Play();
    }
    
    private void CreateWallDust()
    {
        WallDust.Play();
    }

}
