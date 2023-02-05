using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;


public enum ThrowableVeggie
{
    NONE,
    POTATO,
    CARROT,
    BEETROOT
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController2D : MonoBehaviour
{
    // Move player in 2D space
    public float maxSpeed = 3.4f;
    public float jumpHeight = 6.5f;
    public float gravityScale = 1.5f;

    bool facingRight = true;
    float moveDirection = 0;
    public bool isGrounded = false;
    Rigidbody2D r2d;
    CapsuleCollider2D mainCollider;
    Transform t;
    public Animator animator;
    private Quaternion initFacing;
    private Vector2 move_vector;
    private bool jump;
    public Transform throwTransform;
    public GameObject carrotFab;
    public GameObject potatoFab;
    public GameObject beetrootFab;
    public Transform carrotIcon;
    public Transform potatoIcon;
    public Transform beetrootIcon;

	public int digForce = 1;
    public GameObject vegObject;
    Collider2D triggeringCollider;
    public ThrowableVeggie activeVeggie;
    public bool infiniteAmmo;
    public float stunTime;
    public float maxStunTime = 2f;
    public int playerIndex;
    public int deathCounter;
    public static Dictionary<int, string> indexToColor = new()
    {
        { 0, "pink" },
        { 1, "yellow" },
        { 2, "blue" },
        { 3, "purple" }
    };
    public static Dictionary<int, Color> indexToColorName = new()
    {
        { 0, new Color(255,105,180) },
        { 1, Color.yellow },
        { 2, Color.blue },
        { 3, new Color(147,112,219) }
    };
    // Use this for initialization
    void Start()
    {
        t = transform;
        r2d = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<CapsuleCollider2D>();

        r2d.freezeRotation = true;
        r2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        r2d.gravityScale = gravityScale;
        facingRight = t.localScale.x > 0;
        vegObject = null;
        initFacing = transform.rotation;

        var input = GetComponent<PlayerInput>();
        playerIndex = input.playerIndex;
        string color = indexToColor[input.playerIndex];
        transform.name = $"player-{color}";
        animator = transform.GetChild(input.playerIndex).GetChild(0).GetComponent<Animator>();
        RegisterCinemachine();
    }

    private void RegisterCinemachine()
    {
        var go = GameObject.Find("TargetGroup");
        if(!go) return;
        var ctg = go.GetComponent<CinemachineTargetGroup>();
        var ct = new CinemachineTargetGroup.Target
        {
            target = transform,
            radius = 1,
            weight = 1
        };
        var targets = ctg.m_Targets.ToList();
        targets.Add(ct);
        ctg.m_Targets = targets.ToArray();
    }

    public void Movement(InputAction.CallbackContext ctx)
    {
        if(t==null) return;
        move_vector = ctx.ReadValue<Vector2>();
        moveDirection = move_vector.x;

        if (moveDirection != 0)
        {
            if (moveDirection > 0 && !facingRight)
            {
                facingRight = true;
                var localScale = t.localScale;
                localScale = new Vector3(Mathf.Abs(localScale.x), localScale.y, transform.localScale.z);
                t.localScale = localScale;
                animator.SetBool ("facingRight", facingRight);
                transform.Rotate(Vector3.up, -60.0f);
            }
            if (moveDirection < 0 && facingRight)
            {
                facingRight = false;
                var localScale = t.localScale;
                localScale = new Vector3(-Mathf.Abs(localScale.x), localScale.y, localScale.z);
                t.localScale = localScale;
                animator.SetBool ("facingRight", facingRight);
                transform.Rotate(Vector3.up, 60.0f);
            }
        }
        else
        {
            transform.rotation = initFacing;
        }
    }
    
    public void Interact(InputAction.CallbackContext ctx)
    {
        if(!ctx.started) return;

        digForVeggie();
    }
    
    public void Jump(InputAction.CallbackContext ctx)
    {
        if (isGrounded)
        {
            r2d.velocity = new Vector2(r2d.velocity.x, jumpHeight);
        }
    }
    
    public void Shoot(InputAction.CallbackContext ctx)
    {
        if(!ctx.started) return;
        var direction = facingRight ? Vector2.right : Vector2.left;
        GameObject fab;
        Vector2 direction_vector;
        float power = 500f;

        if (infiniteAmmo) activeVeggie = (ThrowableVeggie) Random.Range(1, 3);
        
        switch (activeVeggie)
        {
            case ThrowableVeggie.NONE:
                return;
            case ThrowableVeggie.POTATO:
                fab = potatoFab;
                power = 800F;
                direction_vector = (direction.normalized + Vector2.up) * 200;
                break;
            case ThrowableVeggie.CARROT:
                fab = carrotFab;
                power = 200f;
                direction_vector = (direction.normalized) * 350;
                break;
            case ThrowableVeggie.BEETROOT:
                power = 800F;
                fab = beetrootFab;
                direction_vector = Vector2.up * 350;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        var veggie = Instantiate(fab);
        var explosion = veggie.GetComponent<ExplodeOnCollision>();
        explosion.power = power;
        
        animator.SetTrigger("isThrowing");
        if (veggie.GetComponent<Throwable>()) veggie.GetComponent<Throwable>().SetOwer(gameObject);
        veggie.transform.position = throwTransform.position;
        veggie.transform.rotation = facingRight ? Quaternion.Euler(0,0,90) : Quaternion.Euler(0,0,-90);
        var rg = veggie.GetComponent<Rigidbody2D>();
        rg.AddForce(direction_vector);

        activeVeggie = ThrowableVeggie.NONE;
        beetrootIcon.gameObject.SetActive(false);
        carrotIcon.gameObject.SetActive(false);
        potatoIcon.gameObject.SetActive(false);
    }
    private void digForVeggie()
    {
        if(triggeringCollider && !vegObject)
        {
            Vegetable veg = triggeringCollider.GetComponent<Vegetable>();
            if (veg)
            {
                animator.SetTrigger("isDigging");
                
                vegObject = veg.DigIt(digForce);

                if (vegObject) pickVeggie(veg);
            }
        }
    }

    private void pickVeggie(Vegetable veggie)
    {
        activeVeggie = veggie.type;
        beetrootIcon.gameObject.SetActive(veggie.type == ThrowableVeggie.BEETROOT);
        carrotIcon.gameObject.SetActive(veggie.type == ThrowableVeggie.CARROT);
        potatoIcon.gameObject.SetActive(veggie.type == ThrowableVeggie.POTATO);

        GameObject.Destroy(veggie.gameObject);
    }

    void Update()
    {
        stunTime -= Time.deltaTime;
    }
    
    void FixedUpdate()
    {
        Bounds colliderBounds = mainCollider.bounds;
        float colliderRadius = mainCollider.size.x * 0.4f * Mathf.Abs(transform.localScale.x);
        Vector3 groundCheckPos = colliderBounds.min + new Vector3(colliderBounds.size.x * 0.5f, colliderRadius * 0.9f, 0);
        // Check if player is grounded
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckPos, colliderRadius);
        //Check if any of the overlapping colliders are not player collider, if so, set isGrounded to true
        isGrounded = false;
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != mainCollider)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        // Apply movement velocity
        if(stunTime < 0) r2d.velocity = new Vector2((moveDirection) * maxSpeed, r2d.velocity.y);

        //Animator updates
        if (Mathf.Abs(r2d.velocity.x) > 0.001f ) animator.SetBool ("isRunning", true);
        else animator.SetBool ("isRunning", false);

        if (r2d.velocity.y > 0.01f && !isGrounded) 
        {
            animator.SetBool ("isJumping", true);
        }
        else 
        {
            animator.SetBool("isJumping", false);
        }
        
        
        if (r2d.velocity.y < -0.01f && !isGrounded) 
        {
            animator.SetBool ("isFalling", true);
        }
        else 
        {
            animator.SetBool("isFalling", false);
        }

        // Simple debug
        Debug.DrawLine(groundCheckPos, groundCheckPos - new Vector3(0, colliderRadius, 0), isGrounded ? Color.green : Color.red);
        Debug.DrawLine(groundCheckPos, groundCheckPos - new Vector3(colliderRadius, 0, 0), isGrounded ? Color.green : Color.red);

    }

    private void OnTriggerStay2D(Collider2D other)
    {
        triggeringCollider = other;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        triggeringCollider = null;
    }
}