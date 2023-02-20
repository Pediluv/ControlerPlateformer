using System.Collections;
using System.ComponentModel;
using TreeEditor;
using UnityEditor.Search;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Parameters")]
    [Tooltip("Abusez pas vous comprenez qand même ce qu'est une vitesse de déplacement")]
    public float moveSpeed;
    [Tooltip("pariel sur la puissance du saut, la puissance du saut est plus basse que la vitesse de déplacement car elle utilise une échelle différente du SmoothDamp)")]
    public float jumpForce;    
    private float movehorizontal;
    private float movevertical;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded;
    [Tooltip("Règle l'accélération du SmoothDamp (n'y touchez pas trop c'est chiant à régler :')")]
    [SerializeField] private float SDOffset;
    [Tooltip("Leger délai pour sauter après une chutte d'une plateforme")]
    [SerializeField] private float coyoteTime;
    private float coyoteTimeCounter;
    [Tooltip("Permet de sauter légèrement avant d'avoir touché le sol")]
    [SerializeField] private float jumpBufferTime;
    private float jumpBufferTimeCounter;
    

    [Header("Dash Parameters")]
    [Tooltip("La touche du Dash/autre action")]
    [SerializeField] private KeyCode dashKey = KeyCode.CapsLock;
    [Tooltip("Puissance du Dash")]
    [SerializeField] private float dashPower;
    [Tooltip("Durée du Dash, il vaut mieux la laisser faible, sinon le perso ne déscent plus pendant un moment")]
    [SerializeField] private float dashTime;
    [Tooltip("Le cooldown du Dash")]
    [SerializeField] private float dashCooldown;
    private bool canDash = true;
    private bool isDashing = false;
    [Tooltip("La Trail pendant le Dash")]
    [SerializeField] private TrailRenderer trail;

    [Header("Planer Parameters")]
    [Tooltip("PAS TOUCHE !!!")]
    [SerializeField] private Vector2 directionRay;
    [Tooltip("Permet de régler la hauteur minimale du perso nécaissaire pour planer")]
    [SerializeField] private float sizeRay;
    private RaycastHit2D hitground;
    private bool cannotFly;
    [Tooltip("Place la vitesse du RigidBody à 0 à chaque frame pour ralentir le perso en lui forçant une vitesse aditionnée à la gravité")]
    [SerializeField] private float SlowFall;
    //permet de déclancher le planage en double clic de saut
    private bool canPlane = false;

    /*[Header("WallJump Parameters")]
    [Tooltip("Vitesse de déscente des murs quand accroché aux murs")]
    [SerializeField] private float grabSpeed;
    [Tooltip("vitesse horizontale lorque le perso se propulse à l'aide du mur")]
    [SerializeField] private Vector2 powerWallJump;
    [Tooltip("Permet de faire un Wall Jump après avoir laché le grab")]
    [SerializeField] private float wallJumpBuffer;
    private float wallJumpBufferCounter;
    [SerializeField] private float wallJumpDuration;
    private float wallJumpDirection;
    private bool isWallGrab;
    private bool isWallJump;
    private bool isCloseToWall;
    [Tooltip("Zone de détection du mur de droite")]
    [SerializeField] private Transform wallCheck;
    [Tooltip("Le rayon des zones de détection des murs")]
    [SerializeField] private float wallCheckRadius;
    [Tooltip("La CollisionLayer des murs")]
    [SerializeField] private LayerMask WallCollisionLayer;
    //[Tooltip("Touche pour se déplacer vers la gauche (uniquement utilisée pour les WallJumps")]
    //[SerializeField] private KeyCode leftKey = KeyCode.Q;
    //[Tooltip("Touche pour se déplacer vers la droite (uniquement utilisée pour les WallJumps")]
    //[SerializeField] private KeyCode rightKey = KeyCode.D;
    //[Tooltip("Vitesse verticale quand collé à un mur")]
    //[SerializeField] private float wallJumpForce;
    //[Tooltip("léger recul quand le perso monte le mur à chaque saut, ajoute un peu de réalisme")]
    //[SerializeField] private float recoilWallJump;
    //private RaycastHit2D wallJumpLeft;
    //private RaycastHit2D wallJumpRight;*/

    [Header("Ground Check Parameters")]
    [Tooltip("Zone de détection du sol")]
    [SerializeField] private Transform groundCheck;
    [Tooltip("Rayon de la zone de détection su sol")]
    [SerializeField] private float groundCheckRadius;
    [Tooltip("Cette CollisionLayer est la même que celle utilisée pour le flyCheck du planage !")]
    [SerializeField] private LayerMask GroundCollisionLayers;

    [Header("Components")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    private bool isFacingRight = true;

    public static PlayerMovement instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Il y a plus d'une instance PlayerMovement dans la scène");
            return;
        }

        instance = this;
    }

    void Update()
    {
        //Bool de vérification de collision au sol
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, GroundCollisionLayers);

        //Bool de vrification de collision pour le fly
        hitground = Physics2D.Raycast(transform.position, directionRay, sizeRay, GroundCollisionLayers);
        cannotFly = hitground.collider;

        //Bool de vérification de collision aux murs
        //isCloseToWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, WallCollisionLayer);

        //Raycast de verification de collision aux murs pour le Wall Jump
        //wallJumpLeft = Physics2D.Raycast(transform.position, new Vector2(-1, 0), 2, WallCollisionLayer);
        //wallJumpRight = Physics2D.Raycast(transform.position, new Vector2(1, 0), 2, WallCollisionLayer);

        //Valeur des vitesses 
        movehorizontal = Input.GetAxisRaw("Horizontal") * moveSpeed * Time.fixedDeltaTime;

        //Appel des méthodes de mouvements
        Moveperso(movehorizontal);
        Jump();
        //WallGrab();
        //WallJump();
        Planer();
        if (Input.GetKeyDown(dashKey) && canDash)
        {
            StartCoroutine(Dash());
        }
        Flip();
    }

    void Moveperso(float _horiz)
    {
        Vector3 baseMoveVelocity = new Vector2(_horiz, rb.velocity.y);
        rb.velocity = Vector3.SmoothDamp(rb.velocity, baseMoveVelocity, ref velocity, SDOffset);
    }

    void Jump()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }

        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimeCounter = jumpBufferTime;
        }

        else
        {
            jumpBufferTimeCounter -= Time.deltaTime;
        }

        if (coyoteTimeCounter > 0f && jumpBufferTimeCounter > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferTimeCounter = 0f;
        }

        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
            coyoteTimeCounter = 0f;
        }
    }

    /*void Dash()
    {
        Vector2 dashDir = new Vector2(dashPower * Mathf.Sign(rb.velocity.x), dashPowerCompensation);
        if (canDash >= dashCooldown)
        {
            canDash = dashCooldown;
        }

        else
        {
            canDash += Time.deltaTime;
        }

        if (Input.GetKeyDown(dashKey) && canDash == dashCooldown)
        {
            rb.AddForce(dashDir);
            canDash = 0f;
        }
    }*/

    private IEnumerator Dash()
    {
        //start of Dash
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        trail.emitting = true;
        rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * dashPower, 0f);
        yield return new WaitForSeconds(dashTime);
        //end of Dash
        isDashing = false;
        rb.gravityScale = originalGravity;
        trail.emitting = false;
        //enable Dash after cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void Planer()
    {
        if (cannotFly)
        {
            canPlane = false;
        }
        
        Vector3 flyVelocity = new Vector2(movehorizontal, SlowFall);
        if (Input.GetButton("Jump") && canPlane)
        {
            rb.velocity = Vector3.SmoothDamp(rb.velocity, flyVelocity, ref velocity, SDOffset);
        }

        if (Input.GetButtonUp("Jump") && !cannotFly)
        {
            canPlane = true;
        }
    }

    /*void LeftWallJumpOld()
    {
        Vector3 grabVelocity = new Vector2(movehorizontal, grabSpeed);
        if (Input.GetButton("Horizontal") && isCloseToLeftWall && !cannotFly)
        {
            rb.velocity = Vector3.SmoothDamp(rb.velocity, grabVelocity, ref velocity, SDOffset);
        }

        if (Input.GetKeyUp(leftKey))
        {
            wallJumpBufferCounter = wallJumpBuffer; 
        }

        if (wallJumpLeft.collider)
        {
            wallJumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetKey(leftKey) && isCloseToLeftWall && !cannotFly && Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector2(recoilWallJump, wallJumpForce);
            wallJumpBufferCounter = wallJumpBuffer;
        }

        if (wallJumpBufferCounter >= 0f && wallJumpLeft.collider && !cannotFly && Input.GetButtonDown("Jump") && Input.GetKey(rightKey))
        {
            rb.velocity = new Vector2(propulseWallJump, wallJumpForce*2);
            wallJumpBufferCounter = wallJumpBuffer;
        }
    }*/

    /*void RightWallJumpOld()
    {
        Vector3 grabVelocity = new Vector2(movehorizontal, grabSpeed);
        if (Input.GetButton("Horizontal") && isCloseToRightWall && !cannotFly)
        {
            rb.velocity = Vector3.SmoothDamp(rb.velocity, grabVelocity, ref velocity, SDOffset);
        }

        if (Input.GetKeyUp(rightKey))
        {
            wallJumpBufferCounter = wallJumpBuffer;
        }

        if (wallJumpRight.collider)
        {
            wallJumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetKey(rightKey) && isCloseToRightWall && !cannotFly && Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector2(-recoilWallJump, wallJumpForce);
            wallJumpBufferCounter = wallJumpBuffer;
        }

        if (wallJumpBufferCounter >= 0f && isCloseToRightWall && !cannotFly && Input.GetButtonDown("Jump") && Input.GetKey(leftKey))
        {
            rb.velocity = new Vector2(-propulseWallJump, wallJumpForce * 2);
            wallJumpBufferCounter = wallJumpBuffer;
        }
    }*/

    /*void WallGrab()
    {
        if (isCloseToWall && !isGrounded && movehorizontal != 0f)
        {
            isWallGrab = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -grabSpeed, float.MaxValue));
        }
        else
        {
            isWallGrab = false;
        }
    }*/

    /*void WallJump()
    {
        if (isWallGrab)
        {
            isWallJump = false;
            wallJumpDirection = -transform.localScale.x;
            wallJumpBufferCounter = wallJumpBuffer;
        }
        else
        {
            wallJumpBufferCounter -= Time.deltaTime;
        }

        if(Input.GetButtonDown("Jump") && wallJumpBufferCounter > 0f)
        {
            isWallJump = true;
            rb.velocity = new Vector2(wallJumpDirection * powerWallJump.x, powerWallJump.y);
            wallJumpBufferCounter = 0f;

            if (transform.localScale.x != wallJumpDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpDuration);
        }
    }*/

    /*void StopWallJumping()
    {
        isWallJump = false;
    }*/

    void Flip()
    {
        if (isFacingRight && movehorizontal < 0f || !isFacingRight && movehorizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private void OnDrawGizmos()
    {
        //isGrounded
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.DrawLine(groundCheck.position, GetComponentInParent<Transform>().position);

        //cannotFly vercion Raycast
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, hitground.point);

        //isCloseToWall
        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
    }
}