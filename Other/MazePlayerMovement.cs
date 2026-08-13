using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MazePlayerMovement : MonoBehaviour
{
    public bool isBlackSoul;
    public float gravityMultiplier;

    [Header("Movement")]
    public float moveSpeed;
    public float walkSpeed;
    public float dashSpeed;
    public float groundDrag;
    public float walkSpeedVal;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    public bool readyToJump;
    public bool midAirJump;

    public float forwardJumpReducer;

    public float dampenRate;


    [Header("Dashing")]
    public bool dashing;
    public Transform orientation;

    [Header("Flying")]
    public HelicopterController flycontroller;
    float horizontalInput;
    float verticalInput;
    float rotateRate = .25f;
    int raycastLayerMask = 1 << 13;
    int groundLayerMask = 1 << 12;



    Vector3 moveDirection;
    Vector3 dampenDirection;
    Rigidbody rb;

    [Header("KeyBinding")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;


    [Header("Restraints")]
    public bool doubleJumpEnabled;
    public bool dashEnabled;
    public bool wallClimbingEnabled;
    public bool flyingEnabled;
    public bool isRobot;

    [Header("Refernces")]
    public Animator robotAnim;
    public Animator firstBridge;
    public SoulState soulState;
    public Vector3 groundNormal;
    public CinemachineBrain mainCamBrain;
    public GameObject endFade;


    [Header("Hint UI")]
    public Animator doubleJumpHint;
    public Animator dashHint;


    [Header("FMOD")]
    private FMOD.Studio.EventInstance playerMove;
    private FMOD.Studio.EventInstance wheelAmbiance;
    private string playerJump;
    private string playerDash;
    private string playerLand;
    private string brickHit = "event:/LevelTwo/SFX/Bricks/BrickHit";
    private string dashImpact = "event:/LevelTwo/SFX/Bricks/BrickImpact";

    float g;
    float GMultiplier = 1f;
    bool contactGround;
    bool changeGravity;
    public bool climbing;
    //reset rotation and gravity back to before wallclimb
    bool reset;
    DoubleElevator currentDE;
    float originalJumpForce;
    [SerializeField]
    bool freezeGDir;
    Vector3 BSEleNormal;
    Vector3 ElevatorUp;
    bool playedGroundSound;
    bool exitScene;

    private void Awake()
    {
        ElevatorUp = GameObject.Find("BSEleTrigger").transform.up;
        originalJumpForce = jumpForce;
        soulState = gameObject.GetComponent<SoulState>();
        flycontroller = gameObject.GetComponent<HelicopterController>();
        GetComponent<Collider>().material = GameObject.Find("LevelSetUpper").GetComponent<Level2SetUp>().noFriction;
        g = -9.81f * GMultiplier;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false;
        if (gravityMultiplier == 0)
        {
            gravityMultiplier = 1f;
        }
        walkSpeedVal = walkSpeed;
        //Debug.Log(gameObject.name + "'s size is " + GetComponent<Collider>().bounds.size);
        //rb.useGravity = false;
    }

    // Start is called before the first frame update
    void Start()
    {

            
    }

    private void OnEnable()
    {
        rb.mass = 1;
        rb.drag = 0;
        rb.angularDrag = 0.05f;
        wheelAmbiance = FMODUnity.RuntimeManager.CreateInstance("event:/LevelTwo/SFX/Ambiance/WheelAmbiance");
        if (!isRobot)
        {
            playerMove = FMODUnity.RuntimeManager.CreateInstance("event:/LevelTwo/SFX/SoulUI/SoulMovement");
            playerJump = "event:/LevelTwo/SFX/SoulUI/SoulJump";
            playerDash = "event:/LevelTwo/SFX/SoulUI/SoulDash";
        }
        else
        {
            if (gameObject.name.Contains("Jumping"))
            {
                playerMove = FMODUnity.RuntimeManager.CreateInstance("event:/LevelTwo/SFX/JumpingRobot/JumpingWalk");
                playerJump = "event:/LevelTwo/SFX/JumpingRobot/JumpingJump";
                playerLand = "event:/LevelTwo/SFX/JumpingRobot/JumpingLand";

            }
            else if (gameObject.name.Contains("Wheel"))
            {
                playerMove = FMODUnity.RuntimeManager.CreateInstance("event:/LevelTwo/SFX/DashingRobot/DashingWalk");
                playerJump = "event:/LevelTwo/SFX/DashingRobot/DashingLand";
                playerDash = "event:/LevelTwo/SFX/DashingRobot/DashingDash";
                playerLand = "event:/LevelTwo/SFX/DashingRobot/DashingLand";
            }
        }
    }

    private void OnDisable()
    {
        playerMove.release();
        wheelAmbiance.release();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(gameObject.name + "'s size is " + GetComponent<Collider>().bounds.size);
        //Debug.Log(angle360(Vector3.up,transform.up,transform.right));
        Vector3 velocityDir = Vector3.ProjectOnPlane(rb.velocity, transform.right);

        Debug.DrawRay(orientation.position, rb.velocity*5f, Color.red);
        grounded = Physics.Raycast(transform.position, -transform.up, playerHeight * 0.5f + 0.2f, whatIsGround);
        PlayerInput();
        SpeedControl();
        if (grounded)
        {
            GetComponent<Collider>().material.frictionCombine = PhysicMaterialCombine.Maximum;
            if (!playedGroundSound && isRobot)
            {
                Debug.Log(playerLand);
                FMODUnity.RuntimeManager.PlayOneShot(playerLand, transform.position);

                //play audio here
                playedGroundSound = true;

            }
        }
        else
        {
            playedGroundSound = false;
            GetComponent<Collider>().material.frictionCombine = PhysicMaterialCombine.Minimum;
        }
        if (grounded && !dashing)
            rb.drag = groundDrag;
        //rb.velocity = rb.velocity - Vector3.Project(rb.velocity, transform.up);

        else
        {
            rb.drag = 0;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, playerHeight * 0.5f + 0.2f, groundLayerMask))
        {
            if (hit.collider.gameObject.CompareTag("Untagged"))
            {
                Debug.DrawRay(transform.position, hit.normal * 10f, Color.green);
                groundNormal = hit.normal;
                climbing = false;
                //if (Vector3.Angle(hit.normal, transform.up) < 50f)
                //{
                    //Physics.gravity = hit.normal * g;
                    //changeGravity = true;
                    //Vector3 newUp = hit.normal;
                    //dampenDirection = -newUp;
                    //Vector3 left = Vector3.Cross(transform.forward, newUp);
                    //Vector3 newForward = Vector3.Cross(newUp, left);
                    //Quaternion newRotation = Quaternion.LookRotation(newForward, newUp);
                    ////transform.rotation = newRotation;
                    //transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotateRate);
                    //Debug.Log(hit.collider.gameObject.layer);
                //}
            }
            

            //Debug.Log(Vector3.Angle(hit.normal, transform.up));
        }


        if (wallClimbingEnabled)
        {
            if (Physics.Raycast(transform.position, -transform.up, out hit, 10f, groundLayerMask))
            {
                if (hit.collider.gameObject.CompareTag("Curve"))
                {
                    climbing = true;
                    Debug.DrawRay(transform.position, hit.normal * 10f, Color.green);
                    groundNormal = -hit.normal;
                    Vector3 newUp = hit.normal;
                    Vector3 left = Vector3.Cross(transform.forward, newUp);
                    Vector3 newForward = Vector3.Cross(newUp, left);
                    Quaternion newRotation = Quaternion.LookRotation(newForward, newUp);
                    //transform.rotation = newRotation;
                    transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotateRate);
                    reset = false;
                }
                //Debug.Log("hit");
            }
            else
            {

                if (!reset)
                {
                    if (Physics.Raycast(transform.position, transform.position, out hit, Mathf.Infinity, raycastLayerMask))
                    {
                        Vector3 newUp = hit.normal;
                        dampenDirection = newUp;
                        Vector3 left = Vector3.Cross(transform.forward, newUp);
                        Vector3 newForward = Vector3.Cross(newUp, left);
                        Quaternion newRotation = Quaternion.LookRotation(newForward, newUp);
                        //transform.rotation = newRotation;
                        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotateRate);
                        climbing = false;
                        reset = true;
                    }
                }
            }
        }

        if (!climbing)
        {

            //lerp rotation back to dampendirection
            if (!isBlackSoul)
            {
                if (Physics.Raycast(transform.position, -transform.up, out hit, Mathf.Infinity, raycastLayerMask))
                {

                    if (Vector3.Angle(hit.normal, transform.up) < 20f)
                    {
                        changeGravity = true;
                        Vector3 newUp = hit.normal;
                        dampenDirection = newUp;
                        Vector3 left = Vector3.Cross(transform.forward, newUp);
                        Vector3 newForward = Vector3.Cross(newUp, left);
                        Quaternion newRotation = Quaternion.LookRotation(newForward, newUp);
                        //transform.rotation = newRotation;
                        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotateRate);
                    }
                    else
                    {
                        changeGravity = false;
                    }


                }
                else
                {
                    changeGravity = false;
                }
            }
            else
            {
                
                if (Physics.Raycast(transform.position, -transform.up, out hit, 50f, whatIsGround))
                {
                    if (hit.transform.name != "centerball")
                    {
                        freezeGDir = true;
                        changeGravity = true;
                        //Vector3 newUp = hit.normal;
                        //Vector3 newUp = transform.position - new Vector3(0, -1300, 0);
                        Vector3 newUp = ElevatorUp;
                        dampenDirection = newUp;
                        Vector3 left = Vector3.Cross(transform.forward, newUp);
                        Vector3 newForward = Vector3.Cross(newUp, left);
                        Quaternion newRotation = Quaternion.LookRotation(newForward, newUp);
                        //transform.rotation = newRotation;
                        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotateRate);
                    }
                    else
                    {
                        freezeGDir = false;
                    }
                        
                }
                else
                {
                    freezeGDir = false;
                }
                if(!freezeGDir)
                {
                    changeGravity = true;
                    //Vector3 newUp = hit.normal;
                    Vector3 newUp = transform.position - new Vector3(0, -1300, 0);
                    dampenDirection = newUp;
                    Vector3 left = Vector3.Cross(transform.forward, newUp);
                    Vector3 newForward = Vector3.Cross(newUp, left);
                    Quaternion newRotation = Quaternion.LookRotation(newForward, newUp);
                    //transform.rotation = newRotation;
                    transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, rotateRate);
                }
            }
        }
        if (dashEnabled && GetComponent<Dashing>()!=null)
        {
            GetComponent<Dashing>().enabled = true;
        }
        else if(GetComponent<Dashing>()!= null)
        {
            GetComponent<Dashing>().enabled = false;
        }


        if (dashing)
            moveSpeed = dashSpeed;
        else
            moveSpeed = walkSpeed;

        
        if(walkSpeed > walkSpeedVal)
        {
            walkSpeed -= 0.5f * Time.deltaTime;
        }
        if(walkSpeed < walkSpeedVal)
        {
            walkSpeed = walkSpeedVal;
        }
        if (currentDE)
        {
            if (currentDE.goingUp)
                jumpForce = originalJumpForce + 10f;
        }
        else
        {
            jumpForce = originalJumpForce;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        if (!isBlackSoul)
        {
            if (!climbing && !dashing) 
            {
                CustomGravity();
                //Debug.Log("not climbing");
            }
            else if(climbing)
            {
                //Debug.Log("climbing");
                WallGravity();
            }
        }
        else
        {
            if (!climbing)
                CustomGravity();
        }
        
        
    }

    private static float GetAngle(Vector2 v1, Vector2 v2)
    {
        var sign = Mathf.Sign(v1.x * v2.y - v1.y * v2.x);
        return Vector2.Angle(v1, v2) * sign;

    }
    float angle360(Vector3 from, Vector3 to, Vector3 right)
    {
        float angle = Vector3.Angle(from, to);
        return (Vector3.Angle(right, to) > 90f) ? 360f - angle : angle;
    }

    void WallGravity()
    {
        rb.AddForce(-groundNormal * g * gravityMultiplier * rb.mass);
    }

    void CustomGravity()
    {
        //Vector3 directionZeroX = Vector3.ProjectOnPlane(transform.position, Vector3.right);
        
        //rb.AddForce(-transform.position.normalized * g * rb.mass);
        if (!isBlackSoul)
        {
            Vector3 directionZeroX = Vector3.ProjectOnPlane(dampenDirection, Vector3.right);
            rb.AddForce(directionZeroX.normalized * g * gravityMultiplier * rb.mass);
        }
        else
        {
            Vector3 direction;
            if (freezeGDir)
                direction = dampenDirection;
            else
                direction = transform.position - new Vector3(0, -1300, 0);
            rb.AddForce(direction.normalized * g * gravityMultiplier * rb.mass);
        }

        //每7.5度改一个gravity和视角
        
        //算transform.up和vector3.up的角度差
        //orientation在外面raycast那边改就行
    }

    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        if(horizontalInput == 0 && verticalInput == 0 && grounded)
        {
            if (isRobot)
            {
                robotAnim.SetBool("Walking", false);
                robotAnim.SetBool("Falling", false);
            }
            playerMove.setPaused(true);
        }
        else if (grounded)
        {
            if (isRobot)
            {
                robotAnim.SetBool("Walking", true);
                robotAnim.SetBool("Falling", false);
            }

            FMOD.Studio.PLAYBACK_STATE state;
            playerMove.getPlaybackState(out state);
            bool isPaused;
            playerMove.getPaused(out isPaused);
            if (isPaused)
            {
                playerMove.setPaused(false);
            }
            else if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
            {
                playerMove.start();
            }
            //playerMove.start();
        }
        else if (!grounded)
        {
            //playerMove.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            playerMove.setPaused(true);
        }

        if (Input.GetKeyDown(jumpKey))
        {

            if (grounded && readyToJump)
            {
                if (isRobot && doubleJumpEnabled)
                {
                    robotAnim.SetBool("Jumping", true);
                    //robotAnim.SetBool("Walking", false);
                }
                readyToJump = false;
                midAirJump = true;
                Jump();
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if (midAirJump && doubleJumpEnabled)
            {
                DoubleJump();
            }
        }

        if (Input.GetKeyUp(jumpKey) && !grounded)
        {
            if (isRobot && doubleJumpEnabled)
            {
                robotAnim.SetBool("Falling", true);
                robotAnim.SetBool("Jumping", false);
            }
        }

        else if(Input.GetKeyDown(jumpKey) && midAirJump && !grounded && doubleJumpEnabled)
        {
            midAirJump = false;
            DoubleJump();
        }

        if (flyingEnabled)
        {
            if (grounded)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    flycontroller.enabled = true;
                    this.enabled = false;
                }
            }
        }
    }

    private void MovePlayer()
    {
        //moveDirection = Vector3.Normalize(orientation.forward * verticalInput + orientation.right * horizontalInput);
        Vector3 rightDirection = Quaternion.AngleAxis(90, transform.up) * orientation.forward;
        //Debug.DrawRay(orientation.position, rightDirection * 5f, Color.yellow);
        moveDirection = Vector3.Normalize(orientation.forward * verticalInput + rightDirection * horizontalInput);
        //Debug.DrawRay(transform.position, Vector3.ProjectOnPlane(moveDirection * moveSpeed * 10f * airMultiplier * forwardJumpReducer, orientation.up), Color.blue);
        Debug.DrawRay(transform.position, moveDirection * 5f, Color.yellow);
        //on ground
        if (grounded)
        {
            rb.AddForce(Vector3.ProjectOnPlane( moveDirection * moveSpeed * 10f,transform.up), ForceMode.Force);
            

        }
        //in air
        else if (!grounded)
        {
            //if (verticalInput > 0)
            //{
            //    rb.AddForce(Vector3.ProjectOnPlane( moveDirection * moveSpeed * 10f * airMultiplier * forwardJumpReducer, transform.up), ForceMode.Force);

            //}
            //else if (verticalInput < 0)
            //{
            //    rb.AddForce(Vector3.ProjectOnPlane( moveDirection * moveSpeed * 10f * airMultiplier,transform.up), ForceMode.Force);
            //}
            rb.AddForce(Vector3.ProjectOnPlane(moveDirection * moveSpeed * 10f * airMultiplier, transform.up), ForceMode.Force);
        }

    }

    private void SpeedControl()
    {
        //Vector3 flatVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 flatVel = Vector3.ProjectOnPlane(rb.velocity, transform.up);
        Vector3 upVel = Vector3.Project(rb.velocity, transform.up);

        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            //rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            rb.velocity = upVel + limitedVel;
        }
    }

    private void Jump()
    {
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        playerMove.setPaused(true);
        Vector3 verticalV = Vector3.ProjectOnPlane(rb.velocity, transform.right);
        Vector3 horizontalV = Vector3.ProjectOnPlane(rb.velocity, transform.up);
        //rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        rb.velocity = horizontalV + transform.up * jumpForce;
        if (!gameObject.name.Contains("Dashing"))
        {
            FMODUnity.RuntimeManager.PlayOneShot(playerJump, transform.position);
        }

    }


    private void DoubleJump()
    {
        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        Vector3 velocityDir = Vector3.ProjectOnPlane(rb.velocity, transform.right);
        Vector3 horizontalV = Vector3.ProjectOnPlane(rb.velocity, transform.up);
        float forceOfAcceleration = Mathf.Abs(g * rb.mass);
        //if (velocityDir.normalized == -transform.up)
        //    rb.AddForce(transform.up * (jumpForce + forceOfAcceleration) * 0.24f, ForceMode.Impulse);
        //else
        //    rb.AddForce(transform.up * jumpForce * 0.24f, ForceMode.Impulse);
        rb.velocity = horizontalV + transform.up * jumpForce;
        if (isRobot && doubleJumpEnabled)
            robotAnim.Play("lemon squeezer|spider_jump", 0, 0.0f);
        FMODUnity.RuntimeManager.PlayOneShot(playerJump, transform.position);

    }
    void ResetJump()
    {
        readyToJump = true;
        if (isRobot && doubleJumpEnabled && grounded)
            robotAnim.SetBool("Falling", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Receiver"))
        {
            soulState.stillInTower = true;
            rb.useGravity = false;
            rb.isKinematic = true;
            //GetComponent<SphereCollider>().enabled = false;
            transform.position = Vector3.Lerp(transform.position, other.transform.parent.position, 1f);
            transform.parent = other.transform.parent;
            //transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(0,0,0), 1f);
            other.transform.parent.gameObject.GetComponent<SoulElevator>().hasSoul = true;
            this.enabled = false;
            
        }

        if (other.gameObject.name.Contains("JumpElevatorTrigger"))
        {
            transform.SetParent(other.transform.parent);
            //jumpForce = 40;
            currentDE = other.transform.parent.GetComponent<DoubleElevator>();
        }

        if (other.gameObject.name.Contains("DoJuHint") && doubleJumpEnabled)
        {
            doubleJumpHint.enabled = true;
            doubleJumpHint.SetBool("Hide", false);
        }

        if(other.gameObject.name.Contains("DashingHintTrigger") && dashEnabled)
        {
            dashHint.enabled = true;
            dashHint.SetBool("Hide", false);
        }
        if (other.transform.parent)
        {
            if (other.transform.parent.name.Contains("bowl_plat"))
            {
                transform.SetParent(other.transform.parent);
            }
        }

        if (other.gameObject.name.Contains("ZoneStart"))
        {
            other.transform.parent.gameObject.GetComponent<ZoneController>().StartPlatforms();
        }

        if (other.gameObject.name.Contains("ZoneStop"))
        {
            other.transform.parent.gameObject.GetComponent<ZoneController>().StopPlatforms();
        }

        if (other.gameObject.name.Contains("EndGameTrigger"))
        {
            endFade.SetActive(true);
            //gameObject.GetComponent<MazePlayerMovement>().enabled = false;
            StartCoroutine("GoToCredit");
            
        }
        if(other.gameObject.name== "BSEleTrigger")
        {
            transform.SetParent(other.transform.parent);
            currentDE = other.transform.parent.GetComponent<DoubleElevator>();
            freezeGDir = true;
        }

        if (other.gameObject.name.Contains("ZoneStop_DashingGround - Wheel"))
        {
            wheelAmbiance.start();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        
        if (other.gameObject.name.Contains("JumpElevatorTrigger"))
        {
            transform.SetParent(null);
            //jumpForce = 30;
            currentDE = null;
        }

        if (other.gameObject.name.Contains("DoJuHint") && doubleJumpEnabled)
        {
            doubleJumpHint.SetBool("Hide", true);
        }

        if (other.gameObject.name.Contains("DashingHintTrigger") && dashEnabled)
        {
            dashHint.SetBool("Hide", true);
        }
        if (other.transform.parent)
        {
            if (other.transform.parent.name.Contains("bowl_plat"))
            {
                transform.SetParent(null);

            }
        }
        if (other.gameObject.name == "BSEleTrigger")
        {
            transform.SetParent(null);
            currentDE = null;
        }
    }

    IEnumerator GoToCredit()
    {
        yield return new WaitForSeconds(3f);
        LoadingSceneManager lsm = GameObject.Find("LoadScene").GetComponent<LoadingSceneManager>();
        lsm.buildIndex = 5;
        lsm.StartCoroutine("LoadScenes");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (dashing)
        {
            //if (collision.gameObject.transform.parent.transform.parent)
            //{
                Debug.Log("has grand father");
                if (collision.gameObject.CompareTag("Box"))
                {
                    WallCollision wc = collision.gameObject.transform.parent.parent.gameObject.GetComponent<WallCollision>();
                    wc.hitByPlayer = true;
                    wc.childHit = collision.rigidbody;
                    Vector3 dir = collision.contacts[0].point - transform.position;
                    wc.forceDirection = -dir.normalized;

                    rb.isKinematic = true;
                    
                }
            //}

            if (collision.gameObject.GetComponent<PhysicSimulator>() && !collision.gameObject.GetComponent<PhysicSimulator>().isRobot)
            {
                Debug.Log("should play sound");
                Debug.Log(collision.transform.position);
                FMODUnity.RuntimeManager.PlayOneShot(dashImpact, transform.position);
            }
            
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (rb.isKinematic)
            rb.isKinematic = false;
    }

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.name.Contains("JumpDeepElevator"))
    //    {
    //        transform.parent = null;
    //    }
    //}

    public void UnlockPlayer()
    {
        GetComponent<SphereCollider>().enabled = true;
        //rb.useGravity = true;
        rb.isKinematic = false;
        transform.parent = null;
    }

}
