using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeetManager : MonoBehaviour
{
    private Animator _animator;
    private GameObject _model;
    private CharacterController _characterController;
    private LocomotionController _locomotionScript;
    private FeetController _feetController;
    private FeetController _leftFeetController;
    private FeetPredictor _predictor;
    
    //test hip
    private Vector3 prevHipPosition;
    private float prevHipY;
    private Vector3 prevRightPos;
    private Vector3 prevLeftPos;
    
    private float prevRightPosY;
    private float prevLeftPosY;
    [SerializeField] private LayerMask environmentLayer;
    //test feet height
    [Range (0, 1f)]
    [SerializeField] private float feetHeight = 0;
    
    [Range (0, 1f)]
    [SerializeField] private float followCurveTh = 0.02f;
    
    [Range (0, 1f)]
    [SerializeField] private float TH = 0.02f;

    private Quaternion rightRotationIK;
    private Quaternion leftRotationIK;

    private Vector3 rightPositionIK;
    private Vector3 leftPositionIK;

    private bool firstTime = true;
    private float time;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _model = this.gameObject;
        _locomotionScript = GetComponent<LocomotionController>();
        _feetController = new FeetController(_animator);
        _leftFeetController = new FeetController(_animator);
        _predictor = new FeetPredictor(_animator);
        _characterController = GetComponent<CharacterController>();

        //StateManager.GetDataFromAnimator(_animator);
    }

    void Update()
    {
          
    }

    private void OnAnimatorIK(int layerIndex)
    {
        StateManager.UpdateState(_animator, _characterController);
        StateManager.UpdateModelDirection(_model);
        _predictor.UpdateState();
        _predictor.PredictFeetPosition();
        var currentRight = StateManager.rightFootPosition;
        var currentLeft = StateManager.leftFootPosition;

        //MovePelvisHeight(currentRight, currentLeft);

        
        //TODO: currently doing the boxcasting just for the right foot
        var rightFrom = _predictor.rightShadowPosition;
        var rightTo = _predictor.predictedRightFootPosition;
        rightFrom.y += feetHeight;
        rightTo.y += feetHeight;
        
        var leftFrom = _predictor.leftShadowPosition;
        var leftTo = _predictor.predictedLeftFootPosition;
        leftFrom.y += feetHeight;
        leftTo.y += feetHeight;

        _feetController.CreateBoxCast(rightFrom, rightTo);
        _feetController.CreateBoxCast(rightFrom, rightTo);    

        
        _leftFeetController.CreateBoxCast(leftFrom, leftTo);

        _feetController.GetProjectionOnCurve(HumanBodyBones.RightFoot, currentRight);
        _leftFeetController.GetProjectionOnCurve(HumanBodyBones.LeftFoot, currentLeft);
        
        rightPositionIK = GetGroundPoint(currentRight, true);
        leftPositionIK = GetGroundPoint(currentLeft, false);
        
        //TODO: test for curve following for feet
        if (!StateManager.rightFootGround)
        {
            var shadowY = _predictor.rightShadowPosition.y;
            var predictedY = _predictor.predictedRightFootPosition.y;
            
            //check how similar the two altitude need to be, so that the 
            //curve doesn't get followed while on flat surface
            var diff = currentRight.y - currentLeft.y;
            if (Mathf.Abs(shadowY - predictedY) > followCurveTh || _feetController.midPointHit)
            {
                _feetController.MoveFeetAlongCurve(HumanBodyBones.RightFoot, AvatarIKGoal.RightFoot, currentRight);
            }
            
            //test interpolate quaternion
            var lerpTime = StateManager.rightFlightTime;
            //var result = Quaternion.Lerp(currentRot, _predictor.predictedRightRotation, time / lerpTime);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _predictor.rightFlightDuration/lerpTime);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, _predictor.predictedRightRotation * _animator.GetIKRotation(AvatarIKGoal.RightFoot));
        }
        else
        {
            // MoveFeetToIkPoint(AvatarIKGoal.RightFoot, rightPositionIK, rightRotationIK, ref prevRightPos.y);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, rightPositionIK);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, rightRotationIK * _animator.GetIKRotation(AvatarIKGoal.RightFoot));
        }
        
        //TODO: test for curve following for feet
        if (!StateManager.leftFootGround)
        {
            var shadowY = _predictor.leftShadowPosition.y;
            var predictedY = _predictor.predictedLeftFootPosition.y;
            
            //check how similar the two altitude need to be, so that the 
            //curve doesn't get followed while on flat surface
            if (Mathf.Abs(shadowY - predictedY) > followCurveTh || _leftFeetController.midPointHit)
            {
                _leftFeetController.MoveFeetAlongCurve(HumanBodyBones.LeftFoot, AvatarIKGoal.LeftFoot, currentLeft);
            }
            
            //test interpolate quaternion
            var lerpTime = StateManager.leftFlightTime;
            //var result = Quaternion.Lerp(currentRot, _predictor.predictedRightRotation, time / lerpTime);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _predictor.leftFlightDuration/lerpTime);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, _predictor.predictedLeftRotation * _animator.GetIKRotation(AvatarIKGoal.LeftFoot));
        }
        else
        {
            //MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, correctPosition, leftRotationIK, ref prevLeftPos.y);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftPositionIK);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftRotationIK * _animator.GetIKRotation(AvatarIKGoal.LeftFoot));
        }
        
        
        //prevRightPos = currentRight;
        //prevLeftPos = currentLeft;
    }
    
    void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder, ref float lastFootPositionY) {
        var targetIkPosition = _animator.GetIKPosition(foot);

        if (positionIkHolder != Vector3.zero) {
            targetIkPosition = this.gameObject.transform.InverseTransformPoint(targetIkPosition);
            positionIkHolder = this.gameObject.transform.InverseTransformPoint(positionIkHolder);

            var yVariable = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, 0.2f); //speed feet to IK
            targetIkPosition.y += yVariable;
            lastFootPositionY = yVariable;

            targetIkPosition = this.gameObject.transform.TransformPoint(targetIkPosition);
            //_animator.SetIKRotation(foot, rotationIkHolder * _animator.GetIKRotation(foot));
        }
        _animator.SetIKPosition(foot, targetIkPosition);
    }

    private void MovePelvisHeight(Vector3 right, Vector3 left) {

        if (right == Vector3.zero || left == Vector3.zero || Math.Abs(prevHipY) < 0.05f) {
            prevHipY = _animator.bodyPosition.y;
            return;
        }
        
        var position = this.gameObject.transform.position;
        var leftOffsetPosition = left.y - position.y;
        var rightOffsetPosition = right.y - position.y;
        
        var totalOffset = (leftOffsetPosition < rightOffsetPosition) ? leftOffsetPosition : rightOffsetPosition;
        var newPelvisPosition = _animator.bodyPosition + Vector3.up * totalOffset;
        newPelvisPosition.y = Mathf.Lerp(prevHipY, newPelvisPosition.y, 0.4f) - TH;        
        _animator.bodyPosition = newPelvisPosition;
        
        prevHipY = _animator.bodyPosition.y;
        
    }
    
    private Vector3 GetGroundPoint(Vector3 predictedPosition, bool right)
    {
        RaycastHit hit;
        var groundPoint = Vector3.zero;
        var skyPosition = predictedPosition + Vector3.up * 1.2f;
        Debug.Log(skyPosition);

        Debug.DrawLine(skyPosition, skyPosition + Vector3.down * 2f, Color.magenta);
        if (Physics.Raycast(skyPosition, Vector3.down, out hit, 2f, environmentLayer))
        {
            
            groundPoint = hit.point;
            groundPoint.y += feetHeight;
            
            //test rotation all the time like old system
            var rotAxis = Vector3.Cross(Vector3.up, hit.normal);
            var angle = Vector3.Angle(Vector3.up, hit.normal);
            var rotation = Quaternion.AngleAxis(angle, rotAxis);
            if (right)
            {
                rightRotationIK = rotation;
            }
            else
            {
                leftRotationIK = rotation;
            }
        }
        
        return groundPoint;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Gizmos.color = Color.red;
        // if (StateManager.rightFootGround)
        //     Gizmos.DrawCube(StateManager.rightFootPosition, new Vector3(0.2f,0.2f,0.2f));
        //
        //     Gizmos.color = Color.blue;
        // if (StateManager.leftFootGround)
        //     Gizmos.DrawSphere(StateManager.leftFootPosition, 0.1f);

        //Draw the current Direction
        Gizmos.color = Color.green;
        var current = StateManager.currentPosition;
        var direction = StateManager.currentDirection;
        if (current != Vector3.zero)
        {           
            Gizmos.DrawLine(current, (current + direction * 1.1f));
        }
    
        //Draw the current root position
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(StateManager.currentPosition, 0.05f);
        
        //Draw the right shadow position
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_predictor.rightShadowPosition, 0.05f);
        
        //Draw the left shadow position
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(_predictor.leftShadowPosition, 0.05f);
        
        //Draw the predicted right foot position
        Gizmos.color = Color.green;
        var pRight = _predictor.predictedRightFootPosition;
        Gizmos.DrawSphere(pRight, 0.05f);

        //Draw the predicted left foot position
        Gizmos.color = Color.green;
        var pLeft = _predictor.predictedLeftFootPosition;
        Gizmos.DrawSphere(pLeft, 0.05f);
        
        //Draw the previous positions
        // Color lightRed = new Color(244, 141, 112);
        // Color lightBlue = new Color(104, 172, 221);
        //
        // Gizmos.color = lightRed;
        // Vector3 prevleft = _predictor.previousFootprintLeft;
        // Gizmos.DrawSphere(prevleft, 0.1f);
        //
        // Gizmos.color = lightBlue;
        // Vector3 prevRight = _predictor.previousFootprintRight;
        // Gizmos.DrawSphere(prevRight, 0.1f);


        //drawing the curve for both feet
        Gizmos.color = Color.red;
        var matrix = _feetController.matrix;
        var step = _feetController._localTargetPosition.z / 100;
        var start = 0f;
        var endPoint = 0f;
        for (int i = 0; i < 100; i++)
        {
            var globalStart = matrix.MultiplyPoint3x4(new Vector3(0, endPoint, start));
            start += step;
            endPoint = (float)_feetController._curve.Interpolate(start);
            var globalEnd = matrix.MultiplyPoint3x4(new Vector3(0, endPoint, start));
            
            Gizmos.DrawLine(globalStart, globalEnd);
        }
        
        //drawing the highest mid point
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(matrix.MultiplyPoint3x4(_feetController._midPoint), 0.05f);
        
        //drawing the cube for casting
        var currentMatrix = Gizmos.matrix;
        // Gizmos.color = Color.red;
        // Gizmos.matrix = matrix;
        // var c = matrix.inverse.MultiplyPoint3x4(_feetController.center);
        // Gizmos.DrawCube(c, _feetController.half * 2);
        // Gizmos.matrix = currentMatrix;
        
        //Draw right foot projection
        Gizmos.color = Color.magenta;
        currentMatrix = Gizmos.matrix;
        Gizmos.matrix = matrix;
        var projectionPos = _feetController.localFootProjection;
        Gizmos.DrawSphere(projectionPos, 0.05f);
        Gizmos.matrix = currentMatrix;
        
        //Draw newglobalPosition
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_feetController.newGlobalPosition, 0.05f);

        //Draw curve for the left foot
        Gizmos.color = Color.red;
        var leftMatrix = _leftFeetController.matrix;
        var leftStep = _feetController._localTargetPosition.z / 100;
        var leftStart = 0f;
        var leftEndPoint = 0f;
        for (int i = 0; i < 100; i++)
        {
            var globalStart = leftMatrix.MultiplyPoint3x4(new Vector3(0, leftEndPoint, leftStart));
            leftStart += leftStep;
            leftEndPoint = (float)_feetController._curve.Interpolate(leftStart);
            var globalEnd = leftMatrix.MultiplyPoint3x4(new Vector3(0, leftEndPoint, leftStart));
            
            Gizmos.DrawLine(globalStart, globalEnd);
        }
        
        // //drawing the highest mid point
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(leftMatrix.MultiplyPoint3x4(_feetController._midPoint), 0.05f);
        
        // //drawing the cube for casting
        // Gizmos.color = Color.red;
        // Gizmos.matrix = leftMatrix;
        // var leftC = leftMatrix.inverse.MultiplyPoint3x4(_feetController.center);
        // Gizmos.DrawCube(leftC, _feetController.half * 2);
        
        //Draw current position
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_feetController.newGlobalPosition, 0.05f);
        Gizmos.DrawSphere(_leftFeetController.newGlobalPosition, 0.05f);
    }

    void OnGUI()
    {        
        GUI.Label(new Rect(0, 60, 200, 20), "Current Flight Time :" + StateManager.currentFlightTime);
        GUI.Label(new Rect(0, 90, 200, 20), "Next Foot Time :" + _predictor.nextRightFootTime);
        GUI.Label(new Rect(0, 120, 200, 20), "Time :" + Time.time);
        GUI.Label(new Rect(0, 150, 200, 20), "Current Velocity :" + StateManager.currentVelocity);
        GUI.Label(new Rect(0, 180, 200, 20), "Current Duration :" + _predictor.rightFlightDuration);
    }
}
