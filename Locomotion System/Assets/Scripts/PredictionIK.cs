using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredictionIK : MonoBehaviour
{

    private Animator animator;
    private GameObject model;
    private CharacterController characterController;
    LocomotionController locomotionScript;
    
    //Foot variables
    private bool rightFootGrounded = true;
    private bool leftFootGrounded = true;
    private Vector3 currentLeftFootPos;
    private Vector3 currentRightFootPos;

    private Vector3 currentRootPos;
    private Quaternion currentRootRot;

    private Vector3 predictedRootPos;
    private Vector3 prevRightFootPos;
    private Vector3 prevLeftFootPos;

    private Vector3 direction;
    private Vector3 currentPos;
    private Vector3 prevPos;

    //test
    private Vector3 predictedCharacterPosition;
    private Vector3 predictedRightFootPosition;
    private Vector3 predictedLeftFootPosition;
    public float stepy = 1.1f;
    private float time = 0f;
    private bool shouldStartTimer = false;
    private Vector3 currentVelocity = Vector3.zero;

    private float footprintTime = 0f;

    private Dictionary<string, float> flightTimeCollection = new Dictionary<string, float>();


    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        locomotionScript = GetComponent<LocomotionController>();
        model = this.gameObject;
        
        foreach (AnimationClip ac in animator.runtimeAnimatorController.animationClips)
        {            
            RetrieveAnimationFlightTime(ac);
        }

        prevPos = characterController.transform.position;
        prevPos.y += 0.5f;
    }

    void Update()
    {
        currentRightFootPos = animator.GetBoneTransform(HumanBodyBones.RightToes).position;
        currentLeftFootPos = animator.GetBoneTransform(HumanBodyBones.LeftToes).position;

        currentRootPos = animator.rootPosition;

        if (locomotionScript.speed <= 0.05)
        {
            //leftFootGrounded = true;
            //rightFootGrounded = true;                             
            //animator.SetBool("Stop", true);            
        }
        else
        {
            animator.SetBool("Stop", false);
        }

        if (shouldStartTimer)
        {
            time += Time.deltaTime;
        }
        else
        {
            time = 0f;
        }
        
        CalculateDirection();
        PredictFeetPosition();

        prevPos = currentPos;
        if (rightFootGrounded)
        {
            prevRightFootPos = currentRightFootPos;
        }
        if (leftFootGrounded)
        {
            prevLeftFootPos = currentLeftFootPos;
        }                
    }


    private void PredictFeetPosition()
    {
        float currentFlightTime = GetCurrentFlightTime();
        //TODO just for right at the moment;
        Vector3 disRootToRight = new Vector3(0.054771f, -0.41804f, 0.7325f);
        Vector3 disRootToLeft = new Vector3(-0.021762f, 0f, -0.091565f);

        Vector3 _currentVelocity = GetCurrentVelocity();
        Vector3 currentDirection = direction;        
        //currentDirection.y = 0;
        float currentTime = Time.time;
        float remainingFlightTime = currentFlightTime - time;
        float nextFootprintTime = currentTime + remainingFlightTime;
        footprintTime = remainingFlightTime;
        _currentVelocity = locomotionScript.lastRotation * _currentVelocity;

        Matrix4x4 test = Matrix4x4.TRS(currentRootPos, currentRootRot, Vector3.one);
        disRootToRight = test.MultiplyVector(disRootToRight);

        //disRootToRight = locomotionScript.lastRotation * disRootToRight;
        //disRootToRight = currentRootRot * disRootToRight;
        disRootToLeft = locomotionScript.lastRotation * disRootToLeft;


        currentVelocity = _currentVelocity;

        predictedRootPos = currentRootPos + (_currentVelocity * (nextFootprintTime - currentTime));
        //predictedRootPos.y = 0f;
        predictedRightFootPosition = predictedRootPos + disRootToRight;
        predictedLeftFootPosition = predictedRootPos + disRootToLeft;

        
        //predictedCharacterPosition = prevPos + currentDirection;        
    }

    private void CalculateDirection()
    {
        currentPos = characterController.transform.position;
        //currentPos.y += 0.5f;
        Vector3 diff = currentPos - prevPos;
        diff = diff.normalized;
        //direction = currentPos + (diff * 1.1f);
        direction = diff;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        currentRootRot = animator.rootRotation;
        //currentRootPos = characterController.transform.position;
        //currentRootPos.y = 0f;
    }

    private void RetrieveAnimationFlightTime(AnimationClip clip)
    {
        AnimationEvent[] evt = clip.events;        
        if (evt.Length != 0)
        {
            float flightTime = evt[0].floatParameter;
            flightTimeCollection.Add(clip.name, flightTime);
        }
        else
        {
            flightTimeCollection.Add(clip.name, 0f);
        }
    }

    private void LockFoot(AvatarIKGoal foot, Vector3 position)
    {
        animator.SetIKPosition(foot, position);
        animator.SetIKPositionWeight(foot, 1);
    }

    private void UnlockFoot(AvatarIKGoal foot)
    {
        animator.SetIKPositionWeight(foot, 0);
    }

    #region AnimationEventTest

    public void LeftFootLift(AnimationEvent evt)
    {
        if (evt.animatorClipInfo.weight > 0.5)
            leftFootGrounded = false;
    }
    public void RightFootLift(AnimationEvent evt)
    {
        if (evt.animatorClipInfo.weight > 0.5)
        {
            rightFootGrounded = false;
            shouldStartTimer = true;
        }
            
    }
    public void LeftFootStrike(AnimationEvent evt)
    {
        if (evt.animatorClipInfo.weight > 0.5)
            leftFootGrounded = true;
    }
    public void RightFootStrike(AnimationEvent evt)
    {
        if (evt.animatorClipInfo.weight > 0.5)
        {
            rightFootGrounded = true;
            shouldStartTimer = false;
        }
            
           
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(prevLeftFootPos, 0.1f);        

        Gizmos.color = Color.blue;                
        Gizmos.DrawSphere(prevRightFootPos, 0.1f);        

        Gizmos.color = Color.green;
        Gizmos.DrawLine(currentPos , (currentPos + direction * 1.1f));

       
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(predictedRootPos, new Vector3(0.1f, 0.1f, 0.1f));
        
        
        Gizmos.color = Color.green;
        Gizmos.DrawCube(predictedRightFootPosition, new Vector3(0.1f, 0.1f, 0.1f));
        //Gizmos.DrawCube(predictedLeftFootPosition, new Vector3(0.1f, 0.1f, 0.1f));

        Gizmos.color = Color.green;
        Vector3 test = direction;
        test.y = 0;
        Gizmos.DrawLine(prevRightFootPos, prevRightFootPos + direction * 1.1f);

        /*Matrix4x4 cubeTransform = Matrix4x4.TRS(currentRootPos, currentRootRot, Vector3.one);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.color = Color.cyan;
        Gizmos.matrix *= cubeTransform;
        Gizmos.DrawCube(Vector3.zero, new Vector3(0.1f, 0.2f, 0.1f));
        Vector3 disRootToRight = new Vector3(0.054771f, -0.4f, 0.7325f);
        Gizmos.DrawCube(disRootToRight, new Vector3(0.1f, 0.2f, 0.1f));
        Gizmos.matrix = old;*/

        //TODO: testing vector rotation for centre of mass
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(currentRootPos, new Vector3(0.1f, 0.1f, 0.1f));
    }

    private float GetCurrentFlightTime()
    {
        float flightTime = 0f;

        foreach (AnimatorClipInfo info in animator.GetCurrentAnimatorClipInfo(0))
        {
            flightTime += flightTimeCollection[info.clip.name] * info.weight;
        }

        return flightTime;
    }

    private Vector3 GetCurrentVelocity()
    {
        Vector3 velocity = Vector3.zero;

        foreach (AnimatorClipInfo info in animator.GetCurrentAnimatorClipInfo(0))
        {
            velocity += info.clip.averageSpeed * info.weight;
        }

        return velocity;
    }

    void OnGUI()
    {
        foreach (AnimatorClipInfo info in animator.GetCurrentAnimatorClipInfo(0))
        {
            //Output the current Animation name and length to the screen            
            GUI.Label(new Rect(0, 0, 200, 20), "Clip Name : " + info.clip.name);
            GUI.Label(new Rect(0, 30, 200, 20), "Weight : " + info.weight);
        }
        GUI.Label(new Rect(0, 60, 200, 20), "Current Flight Time :" + GetCurrentFlightTime());
        GUI.Label(new Rect(0, 90, 200, 20), "Next Foot Time :" + footprintTime);
        GUI.Label(new Rect(0, 120, 200, 20), "Time :" + Time.time);
        GUI.Label(new Rect(0, 150, 200, 20), "Current Velocity :" + currentVelocity);

        GUI.Label(new Rect(0, 180, 200, 20), "Current pos :" + currentRootPos);
        GUI.Label(new Rect(0, 210, 200, 20), "Predicted pos :" + predictedRootPos);
    }

    #endregion
}
