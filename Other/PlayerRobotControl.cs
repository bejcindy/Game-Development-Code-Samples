using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine.UI;

public class PlayerRobotControl : MonoBehaviour
{
    [Header("Robot")]
    public Transform currentRobot;
    public GameObject bRobot;
    public Transform connectableRobot;
    public RobotControl rc;
    public Animator robotAnim;
    public bool inRobot;
    public bool connectReady;


    [Header("References")]
    public Rigidbody rb;
    public MazePlayerMovement mpm;
    public LookTargetController lookTarget;
    public SoulState soulState;
    public int bSoulEncounters = 0;
    public Animator bridgeAnim;
    public Animator bridgeCenterAnim;
    Transform soulParticle;
    Transform soulParticle2;
    public GameObject shadow;

    [Header("KeyCode")]
    public KeyCode robotKey = KeyCode.F;


    [Header("Camera")]
    public GameObject playerCam;
    public GameObject playerFreeLook;


    [Header("UI")]
    public GameObject drivingButton;
    public Sprite doubleJumpButton;
    public Sprite dashButton;
    public Animator abilityAnim;
    public Sprite[] abilityIcons;
    public Image abilityImage;
    public int stage;

    public bool isBlackSoul;

    bool camTransitioned;
    GameObject lowPriorityFreeLook;
    string activateSound;
    string robotBreakSound = "event:/LevelOne/NPC/NPCBreak";

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        soulParticle = transform.GetChild(0);
        soulParticle2 = soulParticle.GetChild(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerCam.GetComponent<CinemachineBrain>().IsBlending)
        {
            camTransitioned = true;
        }
        if(camTransitioned && !playerCam.GetComponent<CinemachineBrain>().IsBlending)
        {
            if (lowPriorityFreeLook)
            {
                lowPriorityFreeLook.SetActive(false);
            }
            camTransitioned = false;
        }

        //if (inRobot && rc)
        //{
        //    if (!playerCam.GetComponent<CinemachineBrain>().IsBlending)
        //    {
        //        playerFreeLook.SetActive(false);
        //    }
        //    //Debug.Log("robo cam: " + rc.robotCam.GetComponent<CinemachineBrain>().IsBlending);
        //}
        if (currentRobot != null)
        {
            rc = currentRobot.GetComponent<RobotControl>();
        }

        if (connectReady)
        {
            if (Input.GetKeyDown(robotKey))
            {
                if (!isBlackSoul)
                {
                    soulState.destroyCounting = false;
                    if (mpm.doubleJumpEnabled)
                    {
                        bridgeAnim.enabled = true;
                        bridgeCenterAnim.enabled = true;
                    }
                    robotAnim = connectableRobot.parent.GetChild(1).GetChild(0).gameObject.GetComponent<Animator>();
                    robotAnim.speed = 1;
                    connectReady = false;
                    drivingButton.SetActive(false);
                    StartCoroutine(DriveRobot(connectableRobot.parent));
                    shadow.SetActive(false);
                }
                else
                {
                    StartCoroutine(BlackSoulRobotConnect());
                    connectReady = false;
                    drivingButton.SetActive(false);
                }


            }
        }

        if (inRobot)
        {
            transform.localPosition = currentRobot.GetComponent<RobotControl>().drivingPos;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("RobotConnect"))
        {
            
            connectReady = true;
            if (!other.transform.parent.name.Contains("Black"))
            {
                connectableRobot = other.transform;
                
            }
            else
            {
                isBlackSoul = true;
                connectableRobot = other.transform;
            }
            GetRobotKey();

        }
    }

    public IEnumerator BlackSoulRobotConnect()
    {
        soulState.RechargePlayer();
        FMODUnity.RuntimeManager.PlayOneShot(robotBreakSound, transform.position);
        connectableRobot.transform.parent.GetComponent<RobotExploder>().enabled = true;
        switch (stage)
        {
            case 0:
                mpm.doubleJumpEnabled = true;
                break;
            case 1:
                mpm.dashEnabled = true;
                mpm.doubleJumpEnabled = false;
                break;
            default:
                break;
        }

        connectableRobot.gameObject.SetActive(false);
        
        if (!abilityAnim.isActiveAndEnabled)
        {
            abilityAnim.enabled = true;
        }
        else
        {
            abilityAnim.SetBool("Hide", true);
            yield return new WaitForSeconds(2.0f);
            abilityImage.sprite = abilityIcons[stage];
            abilityAnim.SetBool("Hide", false);
            abilityAnim.SetBool("Show", true);
        }
        stage++;




    }

    public void GetOffRobot()
    {
        lookTarget.player = transform;
        inRobot = false;
        PlayerCameraSwitch();
        DestroyLastRobot();
        //Invoke(nameof(DestroyLastRobot), 1f);
        soulParticle.localScale = Vector3.Lerp(soulParticle.localScale, soulParticle.localScale *  2f, 2f);
        soulParticle2.localScale = Vector3.Lerp(soulParticle2.localScale, soulParticle2.localScale * 2f, 2f);
        //StartCoroutine(LerpToSeat(rc.landingPos, 0.25f, 2f));

    }

    private void GetRobotKey()
    {
        if (!isBlackSoul)
        {
            if (mpm.doubleJumpEnabled)
            {
                robotKey = KeyCode.Q;
                drivingButton.GetComponent<Image>().sprite = doubleJumpButton;
                activateSound = "event:/LevelTwo/SFX/JumpingRobot/JumpingEnter";
                //drivingButton.GetComponent<TextMeshProUGUI>().text = "Q";
            }

            else if (mpm.dashEnabled)
            {
                robotKey = KeyCode.E;
                drivingButton.GetComponent<Image>().sprite = dashButton;
                activateSound = "event:/LevelTwo/SFX/DashingRobot/DashingEnter";
                //drivingButton.GetComponent<TextMeshProUGUI>().text = "E";
            }

            else if (mpm.wallClimbingEnabled)
            {
                robotKey = KeyCode.C;
                //drivingButton.GetComponent<TextMeshProUGUI>().text = "C";
            }

            else if (mpm.flyingEnabled)
            {
                robotKey = KeyCode.Z;
                //drivingButton.GetComponent<TextMeshProUGUI>().text = "Z";
            }
        }
        else
        {
            switch (stage)
            {
                case 0:
                    robotKey = KeyCode.Q;
                    drivingButton.GetComponent<Image>().sprite = doubleJumpButton;
                    break;
                case 1:
                    robotKey = KeyCode.E;
                    drivingButton.GetComponent<Image>().sprite = dashButton;
                    break;
            }
        }

        drivingButton.SetActive(true);

        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("RobotConnect"))
        {
            drivingButton.SetActive(false);
            connectableRobot = null;
            connectReady = false;
        }
    }


    private IEnumerator DriveRobot(Transform robot)
    {
        FMODUnity.RuntimeManager.PlayOneShot(activateSound, robot.position);
        //setting the robot we are using
        currentRobot = robot;
        transform.parent = currentRobot;
        EnableRobotAbility(robot);

        //turn off player self movement and physics
        GetComponent<SphereCollider>().enabled = false;
        GetComponent<MazePlayerMovement>().enabled = false;
        rb.isKinematic = true;
        rc = currentRobot.GetComponent<RobotControl>();
        lookTarget.roboting = true;
        StartCoroutine(LerpToSeat(rc.drivingPos, 0.25f, 0.5f));
        yield return new WaitForSeconds(0.5f);
        Invoke(nameof(RobotCameraSwitch), .25f);
    }


    private void EnableRobotAbility(Transform robot)
    {
        MazePlayerMovement rmpm = robot.gameObject.GetComponent<MazePlayerMovement>();
        Debug.Log(robot.gameObject);
        if (mpm.doubleJumpEnabled)
            rmpm.doubleJumpEnabled = true;
        else
            rmpm.doubleJumpEnabled = false;
        if (mpm.dashEnabled)
            rmpm.dashEnabled = true;
        else
            rmpm.dashEnabled = false;
        if (mpm.wallClimbingEnabled)
            rmpm.wallClimbingEnabled = true;
        else
            rmpm.wallClimbingEnabled = false;
        if (mpm.flyingEnabled)
            rmpm.flyingEnabled = true;
        else
            rmpm.flyingEnabled = false;
    }

    IEnumerator LerpToSeat(Vector3 targetPos, float duration, float scaleChange)
    {

        soulParticle.localScale = Vector3.Lerp(soulParticle.localScale, soulParticle.localScale * scaleChange, 2f);
        soulParticle2.localScale = Vector3.Lerp(soulParticle2.localScale, soulParticle2.localScale * scaleChange, 2f);
        float time = 0;
        Vector3 startPos = transform.localPosition;
        
        

        while (time < duration)
        {
            transform.localPosition = Vector3.Lerp(startPos, targetPos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = targetPos;

    }

    private void RobotCameraSwitch()
    {
        //rc.robotFreeLook.GetComponent<CinemachineFreeLook>().Priority = 1;
        //playerFreeLook.GetComponent<CinemachineFreeLook>().Priority = 0;
        inRobot = true;
        rc.connectSetUp = true;
        //rc.robotCam.SetActive(true);
        //rc.robotFreeLook.SetActive(true);
        //playerCam.SetActive(false);
        //added
        rc.robotFreeLook.GetComponent<CinemachineFreeLook>().Priority = 1;
        playerFreeLook.GetComponent<CinemachineFreeLook>().Priority = 0;
        rc.robotFreeLook.SetActive(true);
        lowPriorityFreeLook = playerFreeLook;
        //playerFreeLook.SetActive(false);

        lookTarget.player = connectableRobot.parent;
    }

    private void PlayerCameraSwitch()
    {
        lookTarget.roboting = false;
        rc.robotFreeLook.GetComponent<CinemachineFreeLook>().Priority = 0;
        playerFreeLook.GetComponent<CinemachineFreeLook>().Priority = 1;
        playerFreeLook.SetActive(true);
        lowPriorityFreeLook = rc.robotFreeLook;
        

    }

    private void DestroyLastRobot()
    {
        soulState.destroyCounting = true;
        transform.parent = null;
        rb.isKinematic = false;
        GetComponent<SphereCollider>().enabled = true;
        GetComponent<MazePlayerMovement>().enabled = true;
        bRobot.transform.parent = null;
        bRobot.SetActive(true);
        currentRobot.gameObject.SetActive(false);
        currentRobot = null;
    }
}
