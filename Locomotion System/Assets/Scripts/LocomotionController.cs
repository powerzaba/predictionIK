using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotionController : MonoBehaviour
{
    #region Variables
    public Quaternion lastRotation;

    CharacterController characterController;
    GameObject model;

    //IK Testing Variables
    private Vector3 rightFootPosition, leftFootPosition, leftFootIkPosition, rightFootIkPosition;
    private Quaternion leftFootIkRotation, rightFootIkRotation;
    private float lastPelvisPositionY, lastRightFootPositionY, lastLeftFootPositionY;
    [Header("Feet Grounder")]
    public bool enableFeetIk = true;
    [Range(0, 2)][SerializeField]private float heightFromGroundRaycast = 0f;
    [Range(0, 2)] [SerializeField] private float raycastDownDistance = 1.5f;
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private float pelvisOffset = 0f;
    [Range(0, 1)] [SerializeField] private float pelvisUpAndDownSpeed = 0.28f;
    [Range(0, 1)] [SerializeField] private float feetToIkPositionSpeed = 0.5f;

    public string leftFootAnimVariableName = "LeftFootCurve";
    public string rightFootAnimVariableName = "RightFootCurve";
    public float pelvisModifier = 1f;

    public bool useIkFeature = false;
    public bool showSolverDebug = true;

    //TODO: change this to private/protected provide accessories
    public float speed = 0.0f;
    public float dampTime = 0.1f;
    private Animator animator;
    public Vector3 moveDirection = Vector3.zero;
    private Vector3 fallVector = Vector3.zero;
    public Transform cameraRelative;


    //gravity stuff
    public float gravity = 9.8f;
    public float fallingSpeed = 5f;

    #endregion 

    // Start is called before the first frame update
    void Start()
    {
        model = this.gameObject;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        lastRotation = model.transform.rotation;        
    }


    // Update is called once per frame
    void Update()
    {
        if (characterController.isGrounded)
        {
            fallVector.y = 0.0f;
        }
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        speed = new Vector2(inputX, inputZ).magnitude;
        if (speed >= 0)
        {
            speed = 0;
        }
        
        animator.SetFloat("Speed", speed, dampTime, Time.deltaTime);
        Vector3 moveX = Camera.main.transform.right * inputX;
        Vector3 moveZ = Camera.main.transform.forward * inputZ;
        moveDirection = moveX + moveZ;
        moveDirection.y = 0.0f;
        
        if (moveDirection != Vector3.zero) {
            Quaternion newRotation = Quaternion.Lerp(lastRotation, Quaternion.LookRotation(moveDirection), 0.1f);
            model.transform.rotation = newRotation;
            //model.transform.rotation = Quaternion.Lerp(model.transform.rotation, Quaternion.LookRotation(moveDirection), 14);            
        }

        fallVector.y -= gravity * Time.deltaTime;
        characterController.Move(fallVector * Time.deltaTime);

        lastRotation = model.transform.rotation;
    }
   
    #region FeetGrounding

    /// <summary>
    /// Updating the target for the feet and adjusting their positions.
    /// </summary>
    private void FixedUpdate() {

        if (enableFeetIk) {

            if (animator == null)
                return;

            AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
            AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

            //raycast to the ground to find positions
            FeetPositionSolver(rightFootPosition, ref rightFootIkPosition, ref rightFootIkRotation); //handle solver for right foot
            FeetPositionSolver(leftFootPosition, ref leftFootIkPosition, ref leftFootIkRotation); //handle solver for left foot


        }
    }

    private void OnAnimatorIK(int layerIndex) {       
        if (enableFeetIk) {

            if (animator == null)
                return;


            MovePelvisHeight();

            //right foot ik position and rotation -- utilise pro feature
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);            
            
            if (useIkFeature) {
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
            }

            MoveFeetToIkPoint(AvatarIKGoal.RightFoot, rightFootIkPosition, rightFootIkRotation, ref lastRightFootPositionY);

            //left foot ik position and rotation -- utilise pro feature
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
           
            if (useIkFeature) {
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            }

            MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, leftFootIkPosition, leftFootIkRotation, ref lastLeftFootPositionY);                        
        }
    }

    #endregion

    #region FeetGroundingMethods

    void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder, ref float lastFootPositionY) {
        Vector3 targetIkPosition = animator.GetIKPosition(foot);

        if (positionIkHolder != Vector3.zero) {
            targetIkPosition = model.transform.InverseTransformPoint(targetIkPosition);
            positionIkHolder = model.transform.InverseTransformPoint(positionIkHolder);

            float yVariable = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, feetToIkPositionSpeed);
            targetIkPosition.y += yVariable;
            lastFootPositionY = yVariable;

            targetIkPosition = model.transform.TransformPoint(targetIkPosition);
            animator.SetIKRotation(foot, rotationIkHolder * animator.GetIKRotation(foot));
        }
        animator.SetIKPosition(foot, targetIkPosition);
    }

    private void MovePelvisHeight() {

        if (rightFootIkPosition == Vector3.zero || leftFootIkPosition == Vector3.zero || lastPelvisPositionY == 0.0f) {
            lastPelvisPositionY = animator.bodyPosition.y;
            return;
        }

        float leftOffsetPosition = leftFootIkPosition.y - model.transform.position.y;
        float rightOffsetPosition = rightFootIkPosition.y - model.transform.position.y;

        float totalOffset = (leftOffsetPosition < rightOffsetPosition) ? leftOffsetPosition : rightOffsetPosition;

        Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * totalOffset/pelvisOffset;
        newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed);        
        animator.bodyPosition = newPelvisPosition;
        
        lastPelvisPositionY = animator.bodyPosition.y;
    }

    private void FeetPositionSolver(Vector3 fromSkyPosition, ref Vector3 feetIkPositions, ref Quaternion feetIkRotations) {
        //raycast handling section 
        RaycastHit feetOutHit;

        if (showSolverDebug)
            Debug.DrawLine(fromSkyPosition, fromSkyPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast), Color.yellow);

        if (Physics.Raycast(fromSkyPosition, Vector3.down, out feetOutHit, raycastDownDistance + heightFromGroundRaycast, environmentLayer)) {
            //finding our feet ik positions from the sky position
            //feetIkPositions = fromSkyPosition;
            //feetIkPositions.y = feetOutHit.point.y + pelvisOffset;
            Vector3 rotAxis = Vector3.Cross(Vector3.up, feetOutHit.normal);
            float angle = Vector3.Angle(Vector3.up, feetOutHit.normal);
            Quaternion rot = Quaternion.AngleAxis(angle, rotAxis);
            feetIkPositions = feetOutHit.point;
            //feetIkRotations = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal);
            feetIkRotations = rot;

            return;
        }

        feetIkPositions = Vector3.zero; //it didn't work :(

    }

    private void AdjustFeetTarget(ref Vector3 feetPositions, HumanBodyBones foot) {
        feetPositions = animator.GetBoneTransform(foot).position;
        //feetPositions.y = model.transform.position.y + heightFromGroundRaycast;
        feetPositions.y += heightFromGroundRaycast;
        

    }

    #endregion
}
