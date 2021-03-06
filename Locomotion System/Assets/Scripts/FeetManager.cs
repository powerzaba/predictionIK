﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
    private float _prevHipY;
    private Vector3 _prevRightPos;
    private Vector3 _prevLeftPos;

    private Quaternion _prevRightRot;
    private Quaternion _prevLeftRot;
    
    [SerializeField] private LayerMask environmentLayer;
    //test feet height
    [Range (0, 1f)]
    [SerializeField] private float feetHeight = 0;
    
    [Range (0, 1f)]
    [SerializeField] private float followCurveTh = 0.02f;
    
    [Range (0, 1f)]
    [SerializeField] private float TH = 0.02f;
    
    [Range (0, 1f)]
    [SerializeField] private float midPointHeight = 0.01f;
    
    [Range (0, 1f)]
    [SerializeField] private float curveHeightTh = 0.01f;

    private Quaternion rightRotationIK;
    private Quaternion leftRotationIK;

    private Vector3 rightPositionIK;
    private Vector3 leftPositionIK;

    private bool firstTime = true;
    private float time;

    public Transform rightFootT;
    public Transform leftFootT;

    public float stepSpeed;
    public float stepCurveSpeed;
    public float pelvisSpeed;
    public float curveTh;

    private Vector3 _prevHipPos;
    private bool _isRightBehind;
    
    //catmull test
    public bool useCatmull = false;
    public float tension = 0f;
    public float alpha = 0.5f;
    [Range (0, 1f)]
    [SerializeField] private float curveStartEndHeight = 0;
    
    
    void Start()
    {
        _animator = GetComponent<Animator>();
        _model = this.gameObject;
        _locomotionScript = GetComponent<LocomotionController>();
        _feetController = new FeetController(_animator);
        _leftFeetController = new FeetController(_animator);
        _predictor = new FeetPredictor(_animator);
        _characterController = GetComponent<CharacterController>();

        _prevRightPos = rightFootT.transform.position;
    }

    void Update()
    {
        //NOW IT'S CORRECTLY UPDATED AFTER THE IK PASS
        _prevRightPos = rightFootT.position;
        _prevLeftPos = leftFootT.position;
        _prevRightRot = rightFootT.rotation;
        _prevLeftRot = leftFootT.rotation;

        var localRight = gameObject.transform.InverseTransformPoint(_prevRightPos);
        var localLeft = gameObject.transform.InverseTransformPoint(_prevLeftPos);
        _isRightBehind = (localRight.z <= localLeft.z);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        StateManager.UpdateState(_animator, gameObject);
        StateManager.UpdateModelDirection(_model);
        _predictor.UpdateState();
        _predictor.PredictFeetPosition();
        var currentRight = StateManager.rightFootPosition;
        var currentLeft = StateManager.leftFootPosition;

        var r = _predictor.predictedRightFootPosition;
        r.y += feetHeight;
        var l = _predictor.predictedLeftFootPosition;
        l.y += feetHeight;
        
        var rightPass = (StateManager.rightFootGround) ? rightPositionIK : r;
        var leftPass = (StateManager.leftFootGround) ? leftPositionIK : l;
        
        // MovePelvis(prevRightPos, prevLeftPos);
        // MovePelvis(rightPositionIK, leftPositionIK);
        MovePelvis(rightPass, leftPass);
        // _prevHipPos = _animator.bodyPosition;
        
        var rightFrom = _predictor.rightShadowPosition;
        var rightTo = _predictor.predictedRightFootPosition;
        rightFrom.y += curveStartEndHeight;
        rightTo.y += curveStartEndHeight;
        
        var leftFrom = _predictor.leftShadowPosition;
        var leftTo = _predictor.predictedLeftFootPosition;
        leftFrom.y += curveStartEndHeight;
        leftTo.y += curveStartEndHeight;

        _feetController.CreateBoxCast(rightFrom, rightTo, midPointHeight, curveHeightTh, curveTh, useCatmull);
        _leftFeetController.CreateBoxCast(leftFrom, leftTo, midPointHeight, curveHeightTh, curveTh, useCatmull);

        _feetController.GetProjectionOnCurve(currentRight);
        _leftFeetController.GetProjectionOnCurve(currentLeft);
        
        rightPositionIK = GetGroundPoint(currentRight, true);
        leftPositionIK = GetGroundPoint(currentLeft, false);
        
        if (!StateManager.rightFootGround)
        {
            var shadowY = _predictor.rightShadowPosition.y;
            var predictedY = _predictor.predictedRightFootPosition.y;
            
            //check how similar the two altitude need to be, so that the 
            //curve doesn't get followed while on flat surface
            if (Mathf.Abs(shadowY - predictedY) > followCurveTh || _feetController.midPointHit)
            {
                if (!useCatmull)
                {
                    _feetController.MoveFeetAlongCurve(HumanBodyBones.RightFoot, AvatarIKGoal.RightFoot, _prevRightPos, stepCurveSpeed);                    
                }
                else
                {
                    //do nothing for now
                    _feetController.MoveFeetCatmull(AvatarIKGoal.RightFoot, _prevRightPos, stepCurveSpeed);
                }
            }
            
            //test interpolate quaternion
            var lerpTime = StateManager.rightFlightTime;
            //var result = Quaternion.Lerp(currentRot, _predictor.predictedRightRotation, time / lerpTime);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _predictor.rightFlightDuration/lerpTime);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, _predictor.predictedRightRotation * _animator.GetIKRotation(AvatarIKGoal.RightFoot));
        }
        else
        {
            //TODO: IK ONLY THE Y POSITION PLS, NOT THE FULL POSITION
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            var step =  stepSpeed * Time.deltaTime; // calculate distance to move
            var targetIk = Vector3.MoveTowards(_prevRightPos, rightPositionIK, step);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, targetIk);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, rightRotationIK * _animator.GetIKRotation(AvatarIKGoal.RightFoot));
        }
        
        if (!StateManager.leftFootGround)
        {
            var shadowY = _predictor.leftShadowPosition.y;
            var predictedY = _predictor.predictedLeftFootPosition.y;
            
            //check how similar the two altitude need to be, so that the 
            //curve doesn't get followed while on flat surface
            if (Mathf.Abs(shadowY - predictedY) > followCurveTh || _leftFeetController.midPointHit)
            {
                if (!useCatmull)
                {
                    _leftFeetController.MoveFeetAlongCurve(HumanBodyBones.LeftFoot, AvatarIKGoal.LeftFoot, _prevLeftPos, stepCurveSpeed);    
                }
                else
                {
                    //do nothing for now   
                    _leftFeetController.MoveFeetCatmull(AvatarIKGoal.LeftFoot, _prevLeftPos, stepCurveSpeed);    
                }
            }
            
            //test interpolate quaternion
            var lerpTime = StateManager.leftFlightTime;
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _predictor.leftFlightDuration/lerpTime);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, _predictor.predictedLeftRotation * _animator.GetIKRotation(AvatarIKGoal.LeftFoot));
        }
        else
        {
            //TODO: IK ONLY THE Y POSITION PLS, NOT THE FULL POSITION
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            var step =  stepSpeed * Time.deltaTime; // calculate distance to move
            var targetIk = Vector3.MoveTowards(_prevLeftPos, leftPositionIK, step);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, targetIk);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftRotationIK * _animator.GetIKRotation(AvatarIKGoal.LeftFoot));
        }
    }
    
    void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 positionIkHolder, Quaternion rotationIkHolder, ref float lastFootPositionY) {
        var targetIkPosition = _animator.GetIKPosition(foot);

        if (positionIkHolder != Vector3.zero) {
            targetIkPosition = this.gameObject.transform.InverseTransformPoint(targetIkPosition);
            positionIkHolder = this.gameObject.transform.InverseTransformPoint(positionIkHolder);

            var yVariable = Mathf.Lerp(lastFootPositionY, positionIkHolder.y, 0.4f); //speed feet to IK
            targetIkPosition.y += yVariable;
            lastFootPositionY = yVariable;

            targetIkPosition = this.gameObject.transform.TransformPoint(targetIkPosition);
            //_animator.SetIKRotation(foot, rotationIkHolder * _animator.GetIKRotation(foot));
        }
        _animator.SetIKPosition(foot, targetIkPosition);
    }

    private void MovePelvis(Vector3 right, Vector3 left) {

        if (right == Vector3.zero || left == Vector3.zero || _prevHipPos == Vector3.zero)
        {
            _prevHipPos = _animator.bodyPosition;
            return;
        }
        
        var position = gameObject.transform.position.y;
        var leftOffset = left.y - position;
        var rightOffset = right.y - position;
        var step = pelvisSpeed * Time.deltaTime;
        var offset = (leftOffset < rightOffset) ? leftOffset : rightOffset;
        
        //test if both are in the air
        // if (!StateManager.leftFootGround && !StateManager.rightFootGround)
        // {
        //     _prevHipPos = _animator.bodyPosition;
        //     return;
        // }
        
        if (leftOffset <= rightOffset)
        {
            if (!StateManager.leftFootGround)
            {
                if (!_isRightBehind)
                {
                    offset = rightOffset;
                }
                else
                {
                    offset = leftOffset;
                }
            }
            else
            {
                offset = leftOffset;
            }
        }
        else
        {
            if (!StateManager.rightFootGround)
            {
                if (_isRightBehind)
                {
                    offset = leftOffset;
                }
                else
                {
                    offset = rightOffset;
                }
            }
            else
            {
                offset = rightOffset;
            }
        }
        
        var newBodyPosition = _animator.bodyPosition + Vector3.up * (offset - feetHeight);
        newBodyPosition.y = Mathf.MoveTowards(_prevHipPos.y, newBodyPosition.y, step);
        _animator.bodyPosition = newBodyPosition;
        _prevHipPos = _animator.bodyPosition;

        // var newBodyPosition = _animator.bodyPosition + Vector3.up * (offset - feetHeight);
        // newBodyPosition.y = Mathf.MoveTowards(_prevHipPos.y, newBodyPosition.y, step);
        // _animator.bodyPosition = newBodyPosition;
        // _prevHipPos = _animator.bodyPosition;


        // if (rightFootIkPosition == Vector3.zero || leftFootIkPosition == Vector3.zero || lastPelvisPositionY == 0.0f) {
        //     lastPelvisPositionY = animator.bodyPosition.y;
        //     return;
        // }
        //
        // float leftOffsetPosition = leftFootIkPosition.y - model.transform.position.y;
        // float rightOffsetPosition = rightFootIkPosition.y - model.transform.position.y;
        //
        // float totalOffset = (leftOffsetPosition < rightOffsetPosition) ? leftOffsetPosition : rightOffsetPosition;
        //
        // // Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * totalOffset/pelvisOffset;
        // Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * totalOffset;
        // newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed);        
        // animator.bodyPosition = newPelvisPosition;
        //
        // lastPelvisPositionY = animator.bodyPosition.y;

        //OLD STUFF OMG
        // var position = gameObject.transform.position;
        // var position = _prevHipPos;
        // var leftOffset = left.y - position.y;
        // var rightOffset = right.y - position.y;
        //
        // var smallestOffset = (leftOffset < rightOffset) ? leftOffset : rightOffset;
        // var newPelvisPosition = _animator.bodyPosition + Vector3.up * smallestOffset;
        // newPelvisPosition.y = Mathf.MoveTowards(_prevHipPos.y, newPelvisPosition.y, pelvisSpeed) - TH;
        // // newPelvisPosition.y = Mathf.Lerp(prevHipY, newPelvisPosition.y, 0.4f) - TH;      
        // _animator.bodyPosition = newPelvisPosition;
        //
        // prevHipY = _animator.bodyPosition.y;
    }
    
    private Vector3 GetGroundPoint(Vector3 predictedPosition, bool right)
    {
        RaycastHit hit;
        var groundPoint = Vector3.zero;
        var skyPosition = predictedPosition + Vector3.up * 1.2f;

        Debug.DrawLine(skyPosition, skyPosition + Vector3.down * 2f, Color.magenta);
        //test OMG
        var currentDirection = StateManager.currentDirection;
        var rot = Quaternion.LookRotation(currentDirection);
        var a1 = predictedPosition + rot * new Vector3(0.05f, 0, 0.05f);
        var a2 = predictedPosition + rot * new Vector3(-0.05f, 0, 0.05f);
        var a3 = predictedPosition + rot * new Vector3(-0.05f, 0, -0.05f);
        var a4 = predictedPosition + rot * new Vector3(0.05f, 0, -0.05f);
        var sky1 = _predictor.GetSkyPosition(1.2f, a1);
        var sky2 = _predictor.GetSkyPosition(1.2f, a2);
        var sky3 = _predictor.GetSkyPosition(1.2f, a3);
        var sky4 = _predictor.GetSkyPosition(1.2f, a4);
        var hit1 = Physics.Raycast(sky1, Vector3.down, out var h1,4f, environmentLayer);
        var hit2 = Physics.Raycast(sky2, Vector3.down, out var h2,4f, environmentLayer);
        var hit3 = Physics.Raycast(sky3, Vector3.down, out var h3,4f, environmentLayer);
        var hit4 = Physics.Raycast(sky4, Vector3.down, out var h4,4f, environmentLayer);
        Debug.DrawLine(sky1, sky1 + Vector3.down * 1.2f, Color.yellow);
        Debug.DrawLine(sky2, sky2 + Vector3.down * 1.2f, Color.yellow);
        Debug.DrawLine(sky3, sky3 + Vector3.down * 1.2f, Color.yellow);
        Debug.DrawLine(sky4, sky4 + Vector3.down * 1.2f, Color.yellow);
        
        var test = new float[4];
        test[0] = h1.point.y;
        test[1] = h2.point.y;
        test[2] = h3.point.y;
        test[3] = h4.point.y;
        var res = _predictor.GetPopularElement(test);
        
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
        
        return new Vector3(predictedPosition.x, res, predictedPosition.z);
        return groundPoint;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

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
        Gizmos.color = new Color(0f, 0f, 0.25f);
        Gizmos.DrawSphere(_predictor.rightShadowPosition, 0.05f);
        
        //Draw the left shadow position
        Gizmos.color = new Color(0.32f, 0f, 0.32f);
        Gizmos.DrawSphere(_predictor.leftShadowPosition, 0.05f);
        
        //Draw the predicted right foot position
        Gizmos.color = Color.blue;
        var pRight = _predictor.predictedRightFootPosition;
        Gizmos.DrawSphere(pRight, 0.05f);

        //Draw the predicted left foot position
        Gizmos.color = Color.magenta;
        var pLeft = _predictor.predictedLeftFootPosition;
        Gizmos.DrawSphere(pLeft, 0.05f);

        //drawing the curve for both feet
        var matrix = _feetController.matrix;

        if (!useCatmull)
        {
            Gizmos.color = Color.red;
            var step = _feetController.localTargetPosition.z / 100;
            var start = 0f;
            var endPoint = 0f;
            for (int i = 0; i < 100; i++)
            {
                var globalStart = matrix.MultiplyPoint3x4(new Vector3(0, endPoint, start));
                start += step;
                endPoint = (float)_feetController.curve.Interpolate(start);
                var globalEnd = matrix.MultiplyPoint3x4(new Vector3(0, endPoint, start));
            
                Gizmos.DrawLine(globalStart, globalEnd);
            }    
        }
        else
        {
            //DRAWING CATMULL RIGHT
            Gizmos.color = Color.blue;
            var step = _feetController.localTargetPosition.z / 100;
            var start = Vector3.zero;
            var end = start + new Vector3(0, 0, step);
            for (int i = 0; i < 100; i++)
            {
                var a = _feetController.catmullRomSpline.Interpolate(end, tension, alpha);
                var globalA = matrix.MultiplyPoint3x4(a);
                var globalStart = matrix.MultiplyPoint3x4(start);
                Gizmos.DrawLine(globalStart, globalA);
                start = a;    
                end += new Vector3(0, 0, step);
            }
        }
        
        
        // //drawing the highest mid point
        // Gizmos.color = Color.black;
        // Gizmos.DrawSphere(matrix.MultiplyPoint3x4(_feetController._midPoint), 0.05f);
        
        //drawing the cube for casting
        var currentMatrix = Gizmos.matrix;
        Gizmos.color = Color.red;
        Gizmos.matrix = matrix;
        var c = matrix.inverse.MultiplyPoint3x4(_feetController.center);
        Gizmos.DrawCube(c, _feetController.half * 2);
        Gizmos.matrix = currentMatrix;
        
        //Draw right foot projection
        Gizmos.color = Color.magenta;
        currentMatrix = Gizmos.matrix;
        Gizmos.matrix = matrix;
        var projectionPos = _feetController.localFootProjection;
        Gizmos.DrawSphere(projectionPos, 0.05f);
        Gizmos.matrix = currentMatrix;

        //Draw curve for the left foot
        var leftMatrix = _leftFeetController.matrix;

        if (!useCatmull)
        {
            Gizmos.color = Color.red;
            var leftStep = _leftFeetController.localTargetPosition.z / 100;
            var leftStart = 0f;
            var leftEndPoint = 0f;
            for (int i = 0; i < 100; i++)
            {
                var globalStart = leftMatrix.MultiplyPoint3x4(new Vector3(0, leftEndPoint, leftStart));
                leftStart += leftStep;
                leftEndPoint = (float)_leftFeetController.curve.Interpolate(leftStart);
                var globalEnd = leftMatrix.MultiplyPoint3x4(new Vector3(0, leftEndPoint, leftStart));
            
                Gizmos.DrawLine(globalStart, globalEnd);
            }    
        }
        else
        {
            //DRAWING CATMULL LEFT
            Gizmos.color = Color.blue;
            var step = _leftFeetController.localTargetPosition.z / 100;
            var start = Vector3.zero;
            var end = start + new Vector3(0, 0, step);
            for (int i = 0; i < 100; i++)
            {
                var a = _leftFeetController.catmullRomSpline.Interpolate(end, tension, alpha);
                var globalA = leftMatrix.MultiplyPoint3x4(a);
                var globalStart = leftMatrix.MultiplyPoint3x4(start);
                Gizmos.DrawLine(globalStart, globalA);
                start = a;    
                end += new Vector3(0, 0, step);
            }
        }

        // //drawing the highest mid point
        // Gizmos.color = Color.black;
        // Gizmos.DrawSphere(leftMatrix.MultiplyPoint3x4(_feetController._midPoint), 0.05f);
        
        // //drawing the cube for casting
        // Gizmos.color = Color.red;
        // currentMatrix = Gizmos.matrix;
        // Gizmos.matrix = leftMatrix;
        // var leftC = leftMatrix.inverse.MultiplyPoint3x4(_feetController.center);
        // Gizmos.DrawCube(leftC, _feetController.half * 2);
        // Gizmos.matrix = currentMatrix;
        
        //Draw current position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_feetController.newGlobalPosition, 0.05f);
        Gizmos.DrawSphere(_leftFeetController.newGlobalPosition, 0.05f);

        Gizmos.color = new Color(0.41f, 0.79f, 1f);
        Gizmos.DrawSphere(rightPositionIK, 0.05f);
        
        Gizmos.color = new Color(0.41f, 0.79f, 1f);
        Gizmos.DrawSphere(leftPositionIK, 0.05f);
        
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(_prevHipPos, 0.05f);

        Gizmos.color = Color.blue;
        Gizmos.DrawCube(gameObject.transform.position, new Vector3(0.1f, 0.1f, 0.1f));

        DrawHitPoints(matrix, leftMatrix);
    }

    private void DrawHitPoints(Matrix4x4 matrix, Matrix4x4 leftMatrix)
    {
        Gizmos.color = Color.red;
        var curr = Gizmos.matrix;
        Gizmos.matrix = matrix;
        Gizmos.DrawSphere(_feetController.highestHitPoint, 0.05f);
        Gizmos.matrix = curr;
        
        Gizmos.color = Color.green;
        curr = Gizmos.matrix;
        Gizmos.matrix = leftMatrix;
        Gizmos.DrawSphere(_leftFeetController.highestHitPoint, 0.05f);
        Gizmos.matrix = curr;
    }

    private void OnGUI()
    {        
        GUI.Label(new Rect(0, 60, 200, 20), "Current Flight Time :" + StateManager.currentFlightTime);
        GUI.Label(new Rect(0, 90, 200, 20), "Next Foot Time :" + _predictor.nextRightFootTime);
        GUI.Label(new Rect(0, 120, 200, 20), "Time :" + Time.time);
        GUI.Label(new Rect(0, 150, 200, 20), "Current Velocity :" + StateManager.currentVelocity);
        GUI.Label(new Rect(0, 180, 200, 20), "Current Duration :" + _predictor.rightFlightDuration);
        
        GUI.Label(new Rect(400, 60, 200, 60), "RIGHT :" + StateManager.rightFootGround);
        GUI.Label(new Rect(400, 120, 200, 60), "LEFT :" + StateManager.leftFootGround);
    }
}
