using UnityEngine;
using VInspector;
using DG.Tweening;
using Mirror;
using FMOD.Studio;
using FMODUnity;

public class IKFootSolver : NetworkBehaviour
{
    private bool canGrab;
    [Tab("Feet")]
    [SerializeField] private bool enableFootIK = false;
    [SerializeField] private Transform leftFootTarget, rightFootTarget;
    [SerializeField] private Transform raycastPoint;
    [SerializeField] private float footSpacing, stepHeight, stepDistance, lerpSpeed;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject dustParticle;
    private int stepCount;
    private Vector3 yOffset = new(0, 0.2f, 0);

    [Tab("Hands")]
    [SerializeField] private Transform rightHandTarget, leftHandTarget;
    [SerializeField] private Transform rightHoldingPos, leftHoldingPos;

    [Tab("Jump Settings")]
    [SerializeField] private Vector3 jumpFootOffset = new Vector3(0f, 0.8f, -0.5f);
    [SerializeField] private float jumpFootSplay = 0.05f;
    [SerializeField] private float jumpRotationAngle = 30f;
    [SerializeField] private float jumpFeetVerticalAngle = 30f;
    [SerializeField] private float jumpResetDuration = 0.15f;
    [SerializeField] private float jumpAnimDuration = 0.4f;
    [SerializeField] private float jumpReturnDuration = 0.2f;

    private float timerL, timerR;
    private bool rightFootMoved;
    private Vector3 leftOldPos, leftNewPos, rightOldPos, rightNewPos;
    private Vector3 moveDirection;

    private bool calculateFoot;
    private Rigidbody rb;
    private LayerMask groundLayers;

    private bool jumping;
    private bool leftRotating, rightRotating;
    Vector3 leftStartLocalRot, rightStartLocalRot;
    Vector3 leftStartLocalPos, rightStartLocalPos;
    private float leftRotAngle, rightRotAngle;

    // For tracking movement when rb is kinematic (client-side)
    private Vector3 lastPosition;
    private Vector3 calculatedVelocity;

    // Flag to disable ground detection and let feet drop (e.g., when in mud)
    private bool disableGroundDetection = false;

    /// <summary>
    /// When true, feet will drop down instead of detecting ground surfaces.
    /// Used for mud/water/sinking scenarios.
    /// </summary>
    public bool DisableGroundDetection
    {
        get => disableGroundDetection;
        set => disableGroundDetection = value;
    }
    //For using mud footsteps or not
    public bool inMud { get; set; }
    EventInstance mudFootstepInstance;
    public bool hasPlayedMudFootstep;
    //set by playermovment script
    PlayerMovement playerMovement;

    // Start is called before the first frame update
    void Start()
    {
        rb = transform.GetComponentInParent<Rigidbody>();
        groundLayers = LayerMask.GetMask("Ground", "StaticObstacles");
        leftStartLocalRot = leftFootTarget.localEulerAngles;
        rightStartLocalRot = rightFootTarget.localEulerAngles;

        leftStartLocalPos = leftFootTarget.localPosition;
        rightStartLocalPos = rightFootTarget.localPosition;

        lastPosition = transform.position;

        mudFootstepInstance = AudioManager.Instance.CreateInstance(FMODEvents.Instance.PlayerFootstep_Mud);
        RuntimeManager.AttachInstanceToGameObject(mudFootstepInstance, gameObject);
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        EventsMaster.OnPlayerStateChange += OnPlayerStateChange;
        canGrab = PlayerStateMachine.playerCurrentState is CurrentState.Carry
               or CurrentState.Drop
               or CurrentState.Cutscene
               or CurrentState.LevelIntro
               or CurrentState.Loading
               or CurrentState.Repair
               or CurrentState.Retrieve;
    }

    private void OnDisable()
    {
        EventsMaster.OnPlayerStateChange -= OnPlayerStateChange;
        mudFootstepInstance.release();
    }

    private void OnPlayerStateChange(PlayerState newState)
    {
        canGrab = newState is CarryState
                || newState is DropState
                || newState is CutsceneState
                || newState is LevelIntroState
                || newState is LoadingState
                || newState is RepairState
                || newState is RetrieveState;
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate velocity from position delta (works for both server and client)
        CalculateVelocity();

        if (!enableFootIK) return;

        if (calculatedVelocity.magnitude > 0.1f)
        {
            calculateFoot = true;
        }
        else
        {
            calculateFoot = false;
        }

        moveDirection = calculatedVelocity.normalized;

        if (jumping)
            return;

        FootCalculation();
    }

    /// <summary>
    /// Calculates velocity from position changes.
    /// Works regardless of whether rb is kinematic (client) or not (server).
    /// </summary>
    private void CalculateVelocity()
    {
        // Use rb.velocity if available and not kinematic
        if (rb != null && !rb.isKinematic)
        {
            calculatedVelocity = rb.velocity;
        }
        else
        {
            // Calculate velocity from position delta (for kinematic/client-side)
            calculatedVelocity = (transform.position - lastPosition) / Time.deltaTime;
        }

        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (canGrab)
        {
            rightHandTarget.position = rightHoldingPos.position;
            leftHandTarget.position = leftHoldingPos.position;
            rightHandTarget.rotation = rightHoldingPos.rotation;
            leftHandTarget.rotation = leftHoldingPos.rotation;
        }
        else
        {
            if (!Physics.Raycast(rightHandTarget.position, Vector3.down, 0.2f, groundLayers))
            {
                rightHandTarget.position += new Vector3(0, -9.81f, 0) * Time.deltaTime;
                rightHandTarget.localRotation = Quaternion.RotateTowards(rightHandTarget.localRotation, Quaternion.identity, Time.deltaTime * 100);
            }
            if (!Physics.Raycast(leftHandTarget.position, Vector3.down, 0.2f, groundLayers))
            {
                leftHandTarget.position += new Vector3(0, -9.81f, 0) * Time.deltaTime;
                leftHandTarget.localRotation = Quaternion.RotateTowards(leftHandTarget.localRotation, Quaternion.identity, Time.deltaTime * 100);
            }
        }
    }

    private void FootCalculation()
    {
        // If ground detection is disabled (e.g., in mud), let feet drop
        if (disableGroundDetection)
        {
            DropFeet();
            if (inMud)
            {
                PlayMudFootstep();
            }
            else
            {
                StopMudFootstep();
            }
            return;
        }
        StopMudFootstep();

        if (canGrab)
        {
            if (rightFootMoved)
                MoveLeftFoot();
            else
                MoveRightFoot();
        }
        else
        {
            if (!Physics.Raycast(rightFootTarget.position, Vector3.down, 0.2f, groundLayers))
                rightFootTarget.position += new Vector3(0, -9.81f, 0) * Time.deltaTime;
            if (!Physics.Raycast(leftFootTarget.position, Vector3.down, 0.2f, groundLayers))
                leftFootTarget.position += new Vector3(0, -9.81f, 0) * Time.deltaTime;
        }
    }

    void PlayMudFootstep()
    {
        if (mudFootstepInstance.isValid())
        {
            if (playerMovement.IsMovingWithInput())
            {
                if (!hasPlayedMudFootstep)
                {
                    mudFootstepInstance.start();
                    hasPlayedMudFootstep = true;
                }
            }
            else
            {
                if (hasPlayedMudFootstep)
                {
                    mudFootstepInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                    hasPlayedMudFootstep = false;
                }
            }
        }
    }

    void StopMudFootstep()
    {
        if (mudFootstepInstance.isValid() && hasPlayedMudFootstep)
        {
            mudFootstepInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            hasPlayedMudFootstep = false;
        }
    }

    /// <summary>
    /// Lets feet drop down naturally (used when in mud or other non-walkable surfaces)
    /// </summary>
    private void DropFeet()
    {
        // Gradually move feet to default hanging position below the player
        Vector3 leftTargetPos = transform.TransformPoint(leftStartLocalPos - new Vector3(0, 0.3f, 0));
        Vector3 rightTargetPos = transform.TransformPoint(rightStartLocalPos - new Vector3(0, 0.3f, 0));

        leftFootTarget.position = Vector3.Lerp(leftFootTarget.position, leftTargetPos, Time.deltaTime * 5f);
        rightFootTarget.position = Vector3.Lerp(rightFootTarget.position, rightTargetPos, Time.deltaTime * 5f);

        // Reset rotation to neutral
        leftFootTarget.localRotation = Quaternion.Lerp(leftFootTarget.localRotation, Quaternion.Euler(leftStartLocalRot), Time.deltaTime * 5f);
        rightFootTarget.localRotation = Quaternion.Lerp(rightFootTarget.localRotation, Quaternion.Euler(rightStartLocalRot), Time.deltaTime * 5f);
    }

    private void MoveLeftFoot()
    {
        rightFootTarget.position = rightNewPos;

        if (calculateFoot)
        {
            Vector3 targetRaycastPos = raycastPoint.position + -transform.right * footSpacing + moveDirection * stepDistance;
            int multiplier;

            // Ensure the foot doesn't cross over
            Vector3 footOffset = rightFootTarget.position - leftFootTarget.position;
            Vector3 projectedOffset = Vector3.Project(footOffset, moveDirection);

            // If the distance is too small, prioritize a larger step to maintain spacing
            if (projectedOffset.magnitude < stepDistance / 2)
                multiplier = 1;
            else
                multiplier = 2;

            // Prevent feet from crossing when moving sideways
            float sideDistance = Vector3.Dot(footOffset, transform.right);
            if (Mathf.Abs(sideDistance) < footSpacing * 0.8f) // Adjust threshold if needed
            {
                targetRaycastPos -= transform.right * Mathf.Sign(sideDistance) * footSpacing * 0.5f;
            }

            RaycastHit hit;
            if (Physics.Raycast(targetRaycastPos, Vector3.down, out hit, Mathf.Infinity, groundLayer))
            {
                if (Vector3.Distance(leftNewPos, hit.point) > stepDistance * multiplier)
                {
                    timerL = 0;
                    leftNewPos = hit.point + yOffset;
                    leftRotAngle = Vector3.SignedAngle(Vector3.up, hit.normal, leftFootTarget.right);
                }
            }
        }

        if (timerL < 1)
        {
            Vector3 footPos = Vector3.Lerp(leftOldPos, leftNewPos, timerL);
            footPos.y += Mathf.Sin(timerL * Mathf.PI) * stepHeight;
            leftFootTarget.position = footPos;
            if (!leftRotating)
            {
                leftRotating = true;

                leftFootTarget.DOLocalRotate(new Vector3(leftRotAngle, 0, 0) + leftStartLocalRot, 1f).SetEase(Ease.OutBack).OnComplete(() => leftRotating = false);
            }
            timerL += Time.deltaTime * lerpSpeed;
        }
        else if (timerL != 10)
        {
            leftOldPos = leftNewPos;
            Instantiate(dustParticle, leftFootTarget.position - new Vector3(0, 0, 0.2f), transform.rotation * Quaternion.Euler(0, 180, 0));

            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerFootstep, leftFootTarget.position);
            stepCount++;
            rightFootMoved = false;
            timerL = 10;
        }

    }
    void MoveRightFoot()
    {
        leftFootTarget.position = leftNewPos;

        if (calculateFoot)
        {
            Vector3 targetRaycastPos = raycastPoint.position + transform.right * footSpacing + moveDirection * stepDistance;
            int multiplier;

            // Ensure the foot doesn't cross over
            Vector3 footOffset = leftFootTarget.position - rightFootTarget.position;
            Vector3 projectedOffset = Vector3.Project(footOffset, moveDirection);

            // If the distance is too small, prioritize a larger step to maintain spacing
            if (projectedOffset.magnitude < stepDistance / 2)
                multiplier = 1;
            else
                multiplier = 2;

            // Prevent feet from crossing when moving sideways
            float sideDistance = Vector3.Dot(footOffset, transform.right);
            if (Mathf.Abs(sideDistance) < footSpacing * 0.8f) // Adjust threshold if needed
            {
                targetRaycastPos += transform.right * Mathf.Sign(sideDistance) * footSpacing * 0.5f;
            }

            RaycastHit hit;
            if (Physics.Raycast(targetRaycastPos, Vector3.down, out hit, Mathf.Infinity, groundLayer))
            {
                if (Vector3.Distance(rightNewPos, hit.point) > stepDistance * multiplier)
                {
                    timerR = 0;
                    rightNewPos = hit.point + yOffset;
                    rightRotAngle = Vector3.SignedAngle(Vector3.up, hit.normal, rightFootTarget.right);
                }
            }
        }

        if (timerR < 1)
        {
            Vector3 footPos = Vector3.Lerp(rightOldPos, rightNewPos, timerR);
            footPos.y += Mathf.Sin(timerR * Mathf.PI) * stepHeight;
            rightFootTarget.position = footPos;
            if (!rightRotating)
            {
                rightRotating = true;
                rightFootTarget.DOLocalRotate(new Vector3(rightRotAngle, 0, 0) + rightStartLocalRot, 1f).SetEase(Ease.OutBack).OnComplete(() => rightRotating = false);
            }
            timerR += Time.deltaTime * lerpSpeed;
        }
        else if (timerR != 10)
        {
            rightOldPos = rightNewPos;
            Instantiate(dustParticle, rightFootTarget.position - new Vector3(0, 0, 0.2f), transform.rotation * Quaternion.Euler(0, 180, 0));
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerFootstep, rightFootTarget.position);
            stepCount++;
            rightFootMoved = true;
            timerR = 10;
        }
    }

    public void Jump()
    {
        jumping = true;

        // Use stored start positions as the baseline (scale-independent)
        Vector3 leftOriginalLocalPos = leftStartLocalPos;
        Vector3 rightOriginalLocalPos = rightStartLocalPos;
        Vector3 leftOriginalLocalRot = leftStartLocalRot;
        Vector3 rightOriginalLocalRot = rightStartLocalRot;

        // Calculate jump target positions using offsets instead of magic numbers
        Vector3 leftTargetLocalPos = leftOriginalLocalPos + jumpFootOffset + new Vector3(-jumpFootSplay, 0, 0);
        Vector3 rightTargetLocalPos = rightOriginalLocalPos + jumpFootOffset + new Vector3(jumpFootSplay, 0, 0);

        Vector3 leftTargetLocalRot = leftOriginalLocalRot + new Vector3(jumpFeetVerticalAngle, -jumpRotationAngle, 0);
        Vector3 rightTargetLocalRot = rightOriginalLocalRot + new Vector3(jumpFeetVerticalAngle, jumpRotationAngle, 0);

        // First lerp feet back to original positions to prevent snapping, then animate the jump
        leftFootTarget.DOLocalMove(leftOriginalLocalPos, jumpResetDuration).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                leftFootTarget.DOLocalMove(leftTargetLocalPos, jumpAnimDuration).SetEase(Ease.OutExpo)
                    .OnComplete(() => leftFootTarget.DOLocalMove(leftOriginalLocalPos, jumpReturnDuration).SetEase(Ease.InExpo));
            });

        rightFootTarget.DOLocalMove(rightOriginalLocalPos, jumpResetDuration).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                rightFootTarget.DOLocalMove(rightTargetLocalPos, jumpAnimDuration).SetEase(Ease.OutExpo)
                    .OnComplete(() => rightFootTarget.DOLocalMove(rightOriginalLocalPos, jumpReturnDuration).SetEase(Ease.InExpo)
                        .OnComplete(() => jumping = false));
            });

        leftFootTarget.DOLocalRotate(leftOriginalLocalRot, jumpResetDuration).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                leftFootTarget.DOLocalRotate(leftTargetLocalRot, jumpAnimDuration).SetEase(Ease.OutCirc)
                    .OnComplete(() => leftFootTarget.DOLocalRotate(leftOriginalLocalRot, jumpReturnDuration).SetEase(Ease.OutBack));
            });

        rightFootTarget.DOLocalRotate(rightOriginalLocalRot, jumpResetDuration).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                rightFootTarget.DOLocalRotate(rightTargetLocalRot, jumpAnimDuration).SetEase(Ease.OutCirc)
                    .OnComplete(() => rightFootTarget.DOLocalRotate(rightOriginalLocalRot, jumpReturnDuration).SetEase(Ease.OutBack));
            });
    }

    public void ResetFeet()
    {
        // leftFootTarget.localPosition = leftStartLocalPos;
        // rightFootTarget.localPosition = rightStartLocalPos;        

        // Vector3 leftRaycastPos = new Vector3(transform.position.x + leftStartLocalPos.x, transform.position.y, transform.position.z + leftStartLocalPos.z);
        // Vector3 rightRaycastPos = new Vector3(transform.position.x + rightStartLocalPos.x, transform.position.y, transform.position.z + rightStartLocalPos.z);


        Vector3 leftRaycastPos = transform.TransformPoint(leftStartLocalPos);
        Vector3 rightRaycastPos = transform.TransformPoint(rightStartLocalPos);
        leftRaycastPos = new Vector3(leftRaycastPos.x, transform.position.y, leftRaycastPos.z);
        rightRaycastPos = new Vector3(rightRaycastPos.x, transform.position.y, rightRaycastPos.z);

        RaycastHit hit;
        if (Physics.Raycast(leftRaycastPos, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            leftFootTarget.position = hit.point + yOffset + new Vector3(0, transform.position.y - hit.point.y, 0);
            leftOldPos = leftFootTarget.position - new Vector3(0, transform.position.y - hit.point.y, 0);
            leftNewPos = leftOldPos;
            // leftFootTarget.position = hit.point + yOffset;

            leftRotAngle = Vector3.SignedAngle(Vector3.up, hit.normal, leftFootTarget.right);
        }
        if (Physics.Raycast(rightRaycastPos, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {

            rightFootTarget.position = hit.point + yOffset + new Vector3(0, transform.position.y - hit.point.y, 0);
            rightOldPos = rightFootTarget.position - new Vector3(0, transform.position.y - hit.point.y, 0);
            rightNewPos = rightOldPos;
            // rightFootTarget.position = hit.point + yOffset;

            rightRotAngle = Vector3.SignedAngle(Vector3.up, hit.normal, rightFootTarget.right);
        }



        leftFootTarget.localEulerAngles = new Vector3(leftRotAngle, 0, 0) + leftStartLocalRot;
        rightFootTarget.localEulerAngles = new Vector3(rightRotAngle, 0, 0) + rightStartLocalRot;

        timerR = 10;
        timerL = 10;
    }

    public int GetStepCount()
    {
        return stepCount;
    }

    public void SetStepCount(int count)
    {
        stepCount = count;
    }

    public void SetLeftHoldingPos(Transform lPos)
    {
        leftHoldingPos = lPos;
    }

    public void SetRightHoldingPos(Transform rPos)
    {
        rightHoldingPos = rPos;
    }


    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(leftNewPos, .1f);
        Gizmos.DrawSphere(rightNewPos, .1f);

    }
}