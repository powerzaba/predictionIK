using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeetPredictor 
{
    private Animator _animator;
    public Vector3 predictedRightFootPosition { get; private set; }
    public Vector3 predictedLeftFootPosition { get; private set; }

    public Vector3 previousFootprintRight { get; private set; }
    public Vector3 previousFootprintLeft { get; private set; }

    public Vector3 predictefRootPositionRight { get; private set; }
    public Vector3 predictedRootPositionLeft { get; private set; }

    public float rightFlightDuration = 0f;
    private float leftFlightDuration = 0f;

    public float nextRightFootTime;

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
        if (currentDirection != Vector3.zero)
        {
            Quaternion currentRotation = Quaternion.LookRotation(currentDirection);
            var currentFlightTime = StateManager.currentFlightTime;
            var currentVelocity = currentRotation * StateManager.currentVelocity;
            var currentTime = Time.time;
            var rightRemainingTime = currentFlightTime - rightFlightDuration;
            var nextRightFootprintTime = currentTime + rightRemainingTime;
            nextRightFootTime = nextRightFootprintTime;

            var leftRemainingTime = currentFlightTime - leftFlightDuration;
            var nextLeftFootprintTime = currentTime + leftRemainingTime;

            Vector3 rightDis = currentRotation * StateManager.currentRightDis;
            Vector3 leftDis = currentRotation * StateManager.currentLeftDis;

            predictefRootPositionRight = StateManager.currentPosition + (currentVelocity * (nextRightFootprintTime - currentTime));
            predictedRootPositionLeft = StateManager.currentPosition + (currentVelocity * (nextLeftFootprintTime - currentTime));

            //Test offset
            Vector3 offset = currentRotation * new Vector3(0, 0, 0.2f);

            //test physics

            predictedRightFootPosition = predictefRootPositionRight + rightDis + offset;
            predictedRightFootPosition = GetGroundPoint(predictedRightFootPosition);


            predictedLeftFootPosition = predictedRootPositionLeft + leftDis + offset;
            predictedLeftFootPosition = GetGroundPoint(predictedLeftFootPosition);
        }               
    }

    private Vector3 GetGroundPoint(Vector3 predicredPosition)
    {
        Vector3 groundPoint = Vector3.zero;
        RaycastHit hit;
        Vector3 skyPosition = predicredPosition + Vector3.up * 1.2f;

        Debug.DrawLine(skyPosition, skyPosition + Vector3.down * 1.2f, Color.yellow);

        if (Physics.Raycast(skyPosition, Vector3.down, out hit))
        {
            groundPoint = hit.point;
        }

        return groundPoint;
    }
}
