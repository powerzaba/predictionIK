using System.Collections.Generic;

using UnityEngine;

public static class StateManager
{
    public static FootData leftFoot = new FootData();
    public static FootData rightFoot = new FootData();

    public static bool rightFootGround = true;
    public static bool leftFootGround = true;
    
    public static float rightPreviousVal = 1;
    public static float leftPreviousVal = 1;

    public static Vector3 rightFootPosition = Vector3.zero;
    public static Vector3 leftFootPosition = Vector3.zero;
    
    public static float rightStride;
    public static float leftStride;

    public static Vector3 currentDirection = Vector3.zero;
    public static Vector3 currentPosition = Vector3.zero;
    public static Vector3 previousPosition = currentPosition;

    public static float currentFlightTime = 0f;
    public static float rightFlightTime = 0f;
    public static float leftFlightTime = 0f;
    
    public static Vector3 currentRightDis = Vector3.zero;
    public static Vector3 currentLeftDis = Vector3.zero;
    //public static Vector3 currentVelocity = Vector3.zero;
    public static float currentVelocity;
    public static Vector3 currentDirectionModel;
    
    public static void UpdateState(Animator animator, GameObject model)
    {
        UpdateFeetStatus(animator);
        UpdateDirectionAndVelocity(animator, model);
        UpdateFeetData(animator);
    }

    public static void UpdataFeetData(Animator animator, float ok)
    {
        var rightFlight = animator.GetFloat("RightFlightCurve");
        var leftFlight = animator.GetFloat("LeftFlightCurve");

        var rightX = animator.GetFloat("RightDisplacementX");
        var rightZ = animator.GetFloat("RightDisplacementZ");

        var leftX = animator.GetFloat("LeftDisplacementX");
        var leftZ = animator.GetFloat("LeftDisplacementZ");

        var rightStride = animator.GetFloat("RightStrideLength");
        var leftStride = animator.GetFloat("LeftStrideLength");
    }

    public static void UpdateFeetData(Animator animator)
    {
        Vector3 velocity = Vector3.zero;

        var rightFlight = animator.GetFloat("RightFlightCurve");
        var leftFlight = animator.GetFloat("LeftFlightCurve");

        var rightDisplacementX = animator.GetFloat("RightDisplacementX");
        var rightDisplacementZ = animator.GetFloat("RightDisplacementZ");

        var leftDisplacementX = animator.GetFloat("LeftDisplacementX");
        var leftDisplacementZ = animator.GetFloat("LeftDisplacementZ");
        
        rightStride = animator.GetFloat("RightStrideLength");
        leftStride = animator.GetFloat("LeftStrideLength");

        foreach (var info in animator.GetCurrentAnimatorClipInfo(0))
        {            
            velocity += info.clip.averageSpeed * info.weight;
        }

        currentFlightTime = rightFlight;
        rightFlightTime = rightFlight;
        leftFlightTime = leftFlight;
        currentRightDis = new Vector3(rightDisplacementX, 0, rightDisplacementZ);
        currentLeftDis = new Vector3(leftDisplacementX, 0, leftDisplacementZ);
        //currentVelocity = velocity;
    }

    public static void UpdateFeetStatus(Animator animator)
    {
        var rightValue = animator.GetFloat("RightFootCurve");
        var leftValue = animator.GetFloat("LeftFootCurve");

        rightFootPosition = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        leftFootPosition = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
        
        
        if (rightPreviousVal != 0 && rightValue == 0)
        {
            rightFootGround = false;
        }
        
        if (rightValue == 1)
        {
            rightFootGround = true;
        }
        
        if (leftPreviousVal != 0 && leftValue == 0)
        {
            leftFootGround = false;
        }
        
        if (leftValue == 1)
        {
            leftFootGround = true;
        }
        
        rightPreviousVal = rightValue;
        leftPreviousVal = leftValue;
    }

    public static void UpdateDirectionAndVelocity(Animator animator, GameObject model)
    {
        // currentPosition = animator.bodyPosition;
        currentPosition = model.transform.position;
        currentPosition.y = GroundPoint(currentPosition).y;
        //currentPosition.y = 0;
        currentVelocity = Vector3.Distance(previousPosition, currentPosition) / Time.deltaTime;
        Vector3 diff = (currentPosition - previousPosition).normalized;
        diff.y = 0f;

        currentDirection = diff;
        previousPosition = currentPosition;
    }   
    
    private static Vector3 GroundPoint(Vector3 predictedPosition)
    {
        var groundPoint = Vector3.zero;
        LayerMask mask = LayerMask.GetMask("Default");
        
        Debug.DrawLine(predictedPosition, predictedPosition + Vector3.down * 2f, Color.yellow);
        if (Physics.Raycast(predictedPosition, Vector3.down, out var hit, 2f, mask))
        {
            groundPoint = hit.point;
        }
        
        return groundPoint;
    }
    
    public static void UpdateModelDirection(GameObject model)
    {
        currentDirectionModel = model.transform.forward;
    }
}

public struct FootData
{
    public bool isGrounded;
    public float currentFlightTime;
    public float currentStrideLegth;
    public Vector3 currentPosition;
    public Vector3 currentDisplacement;    
}