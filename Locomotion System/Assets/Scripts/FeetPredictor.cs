﻿using System;
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
        
        var currentVelocity = currentRotation * StateManager.currentVelocity;

        // var currentVelocity = StateManager.currentVelocity * currentDirection;
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
        Debug.Log("CURRENT DIRECTION: " + currentRotation);
        Debug.Log("CURRENT POS: " + StateManager.currentPosition);
        Debug.Log("current velocity: " + currentVelocity);
        Debug.Log("nextright footprint time: " + nextRightFootprintTime);
        Debug.Log("Current TIME: " + currentTime);
        Debug.Log("Inside the predictor ROOT class is: " + predictedRootPositionRight);


        //Test offset
        Vector3 offset = currentRotation * new Vector3(0, 0, 0);

        //test physics
        predictedRightFootPosition = predictedRootPositionRight + rightDis;
        predictedRightFootPosition = GetGroundPoint(predictedRightFootPosition, true, currentRotation);
        Debug.Log("Inside the predictor class is: " + predictedRightFootPosition);

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
        var a1 = predictedPosition + rot * new Vector3(0.05f, 0, 0.05f);
        var a2 = predictedPosition + rot * new Vector3(-0.05f, 0, 0.05f);
        var a3 = predictedPosition + rot * new Vector3(-0.05f, 0, -0.05f);
        var a4 = predictedPosition + rot * new Vector3(0.05f, 0, -0.05f);
        var sky1 = GetSkyPosition(1.2f, a1);
        var sky2 = GetSkyPosition(1.2f, a2);
        var sky3 = GetSkyPosition(1.2f, a3);
        var sky4 = GetSkyPosition(1.2f, a4);
        var hit1 = Physics.Raycast(sky1, Vector3.down, out var h1,4f, mask);
        var hit2 = Physics.Raycast(sky2, Vector3.down, out var h2,4f, mask);
        var hit3 = Physics.Raycast(sky3, Vector3.down, out var h3,4f, mask);
        var hit4 = Physics.Raycast(sky4, Vector3.down, out var h4,4f, mask);
        Debug.DrawLine(sky1, sky1 + Vector3.down * 1.2f, Color.yellow);
        Debug.DrawLine(sky2, sky2 + Vector3.down * 1.2f, Color.yellow);
        Debug.DrawLine(sky3, sky3 + Vector3.down * 1.2f, Color.yellow);
        Debug.DrawLine(sky4, sky4 + Vector3.down * 1.2f, Color.yellow);
        
        var test = new float[4];
        test[0] = h1.point.y;
        test[1] = h2.point.y;
        test[2] = h3.point.y;
        test[3] = h4.point.y;
        var res = GetPopularElement(test);

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
        
        return new Vector3(predictedPosition.x, res, predictedPosition.z);
        return groundPoint;
    }
    
    public float GetPopularElement(float[] a)
    {
        float count = 1, tempCount;
        float popular = a[0];
        float temp = 0;
        for (int i = 0; i < (a.Length - 1); i++)
        {
            temp = a[i];
            tempCount = 0;
            for (int j = 1; j < a.Length; j++)
            {
                if (temp == a[j])
                    tempCount++;
            }
            if (tempCount > count)
            {
                popular = temp;
                count = tempCount;
            }
        }
        return popular;
    }

    public Vector3 GetSkyPosition(float height, Vector3 pos)
    {
        return pos + Vector3.up * height;
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
