using System;
using System.Collections.Generic;
using UnityEngine;

public class FeetPredictor
{
    private Animator _animator;    
    public Vector3 predictedRightFootPosition { get; private set; }
    public Vector3 predictedLeftFootPosition { get; private set; }
    public Vector3 previousFootprintRight { get; private set; }
    public Vector3 previousFootprintLeft { get; private set; }
    public Vector3 predictedRootPositionRight { get; private set; }
    public Vector3 predictedRootPositionLeft { get; private set; }

    public float rightFlightDuration = 0f;
    public float leftFlightDuration = 0f;

    public float nextRightFootTime;
    
    public Vector3 rightShadowPosition;
    public Vector3 leftShadowPosition;
    
    //test predicted rotation
    public Quaternion predictedRightRotation { get; private set; }
    public Quaternion predictedLeftRotation { get; private set; }

    public FeetPredictor(Animator animator)
    {
        _animator = animator;
    }

    public void UpdateState()
    {
        if (!StateManager.rightFootGround)
        {
            rightFlightDuration += Time.deltaTime;
        }
        else
        {
            previousFootprintRight = StateManager.rightFootPosition;
            rightFlightDuration = 0f;
        }

        if (!StateManager.leftFootGround)
        {
            leftFlightDuration += Time.deltaTime;
        }
        else
        {
            previousFootprintLeft = StateManager.leftFootPosition;
            leftFlightDuration = 0f;
        }
    }

    public void PredictFeetPosition()
    {
        var currentDirection = StateManager.currentDirection;

        Quaternion currentRotation = Quaternion.LookRotation(currentDirection);
        var rightFlightTime = StateManager.currentFlightTime;
        var leftFlightTime = StateManager.leftFlightTime;
        
        //var currentVelocity = currentRotation * StateManager.currentVelocity;

        var currentVelocity = StateManager.currentVelocity * currentDirection;
        var currentTime = Time.time;
        
        var rightRemainingTime = rightFlightTime - rightFlightDuration;
        var leftRemainingTime = leftFlightTime - leftFlightDuration;
        
        var nextRightFootprintTime = currentTime + rightRemainingTime;
        var nextLeftFootprintTime = currentTime + leftRemainingTime;

        nextRightFootTime = nextRightFootprintTime;

        Vector3 rightDis = currentRotation * StateManager.currentRightDis;
        Vector3 leftDis = currentRotation * StateManager.currentLeftDis;

        predictedRootPositionRight = StateManager.currentPosition + (currentVelocity * (nextRightFootprintTime - currentTime));
        predictedRootPositionLeft = StateManager.currentPosition + (currentVelocity * (nextLeftFootprintTime - currentTime));

        //Test offset
        Vector3 offset = currentRotation * new Vector3(0, 0, 0);

        //test physics
        predictedRightFootPosition = predictedRootPositionRight + rightDis;
        predictedRightFootPosition = GetGroundPoint(predictedRightFootPosition, true, currentRotation);

        predictedLeftFootPosition = predictedRootPositionLeft + leftDis + offset;
        predictedLeftFootPosition = GetGroundPoint(predictedLeftFootPosition, false, currentRotation);

        var rightStride = StateManager.rightStride;
        var leftStride = StateManager.leftStride;
        
        rightShadowPosition = predictedRightFootPosition - currentRotation * new Vector3(0, 0, rightStride);
        leftShadowPosition = predictedLeftFootPosition - currentRotation * new Vector3(0, 0, leftStride);

        rightShadowPosition = GroundPoint(rightShadowPosition);
        leftShadowPosition = GroundPoint(leftShadowPosition);
    }

    private Vector3 GetGroundPoint(Vector3 predictedPosition, bool right, Quaternion rot)
    {
        var groundPoint = Vector3.zero;
        var skyPosition = predictedPosition + Vector3.up * 1.2f;
        
        LayerMask mask = LayerMask.GetMask("Default");

        Debug.DrawLine(skyPosition, skyPosition + Vector3.down * 1.2f, Color.yellow);

        if (Physics.Raycast(skyPosition, Vector3.down, out var hit, 4f, mask))
        {
            groundPoint = hit.point;
            var rotAxis = Vector3.Cross(Vector3.up, hit.normal);
            var angle = Vector3.Angle(Vector3.up, hit.normal);
            var rotation = Quaternion.AngleAxis(angle, rotAxis);
            if (right)
            {
                predictedRightRotation = rotation;

            }
            else
            {
                predictedLeftRotation = rotation;
            }
            
        }
        
        return groundPoint;
    }

    private Vector3 GroundPoint(Vector3 predictedPosition)
    {
        var groundPoint = Vector3.zero;
        var skyPosition = predictedPosition + Vector3.up * 1.2f;
        
        LayerMask mask = LayerMask.GetMask("Default");
        
        Debug.DrawLine(skyPosition, skyPosition + Vector3.down * 1.2f, Color.yellow);
        if (Physics.Raycast(skyPosition, Vector3.down, out var hit, 4f, mask))
        {
            groundPoint = hit.point;
        }
        
        return groundPoint;
    }
}
