using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MathNet.Numerics.Interpolation;

public class FeetController
{
    private Animator _animator;
    private RaycastHit[] hit { get; set; }
    private Quaternion rotation { get; set; }
    public Matrix4x4 matrix { get; private set; }
    private Vector3 perpendicular { get; set; }
    public Vector3 center { get; private set; }
    public Vector3 half { get; private set; }
    public Vector3 localUp { get; private set; }
    private Vector3 highestPoint { get; set; }
    private readonly double[] _inputX = new double[3];
    private readonly double[] _inputY = new double[3];
    
    public Vector3 _localMidPosition;
    public Vector3 _localTargetPosition;
    
    public CubicSpline _curve;
    public Vector3 _midPoint;
    
    //Test projection
    public Vector3 localFootProjection; 
    
    //test pos
    public Vector3 newGlobalPosition;
    public bool midPointHit = false;
    
    
    public FeetController(Animator animator)
    {
        _animator = animator;
    }
    
    public void CreateBoxCast(Vector3 from, Vector3 to, float midPointHeight, float heightTh, float curveTh)
    {
        if (from != to)
        {
            var relativePosition = to - from;
            var distanceHalf = relativePosition.magnitude / 2;
            
            rotation = Quaternion.LookRotation(relativePosition, Vector3.up);
            matrix = Matrix4x4.TRS(from, rotation, Vector3.one);
            center = matrix.MultiplyPoint3x4(new Vector3(0, 1f, distanceHalf));
            half = new Vector3(0.1f, 0.1f, distanceHalf);
            localUp = rotation * Vector3.up;
            hit = Physics.BoxCastAll(center, half,-localUp, rotation);
            
            //TODO: to optimize eventually
            for (int i = 0; i < hit.Length; i++)
            {
                hit[i].point = matrix.inverse.MultiplyPoint3x4(hit[i].point);
            }

            hit = hit.OrderByDescending(h => h.point.y).ToArray();

            for (int i = 0; i < hit.Length; i++)
            {
                hit[i].point = matrix.MultiplyPoint3x4(hit[i].point);
            }

            _midPoint = (to + from) / 2;
            //TODO: remember this number 0.05
            _midPoint = _midPoint + rotation * Vector3.up * midPointHeight;
            _midPoint = matrix.inverse.MultiplyPoint3x4(_midPoint);
        
            _localTargetPosition = matrix.inverse.MultiplyPoint3x4(to);
        
            if (hit[0].point != Vector3.zero)
            {
                _localMidPosition = matrix.inverse.MultiplyPoint3x4(hit[0].point);
                if (Math.Abs(_localMidPosition.z - _localTargetPosition.z) > curveTh)
                {
                    if (_localMidPosition.y > _midPoint.y)
                    {
                        _midPoint = _localMidPosition;
                        midPointHit = true;
                    }
                    else
                    {
                        midPointHit = false;
                    }
                }
            }
        
            _inputX[0] = 0;
            _inputX[1] = _midPoint.z;
            _inputX[2] = _localTargetPosition.z;


            _inputY[0] = 0;
            _inputY[1] = _midPoint.y + heightTh;
            _inputY[2] = _localTargetPosition.y;
            
            _curve = CreateCurve(_inputX, _inputY);
            
        }
    }
    
    private CubicSpline CreateCurve(double[] x, double[] y)
    {
        //CubicSpline curve = CubicSpline.InterpolateNatural(x, y);
        // var curve = CubicSpline.InterpolateNaturalInplace(x, y);
        CubicSpline curve = CubicSpline.InterpolateNaturalSorted(x, y);
        return curve;
    }

    public void GetProjectionOnCurve(HumanBodyBones foot, Vector3 position)
    {
        localFootProjection = matrix.inverse.MultiplyPoint3x4(position);
        localFootProjection.x = 0f;
        localFootProjection.y = 0f;
    }

    public void MoveFeetAlongCurve(HumanBodyBones foot, AvatarIKGoal goal, Vector3 currentPosition, float speed)
    {
        var step = speed * Time.deltaTime;
        var newYlocalValue = (float)_curve.Interpolate(localFootProjection.z);
        newGlobalPosition = matrix.MultiplyPoint3x4(new Vector3(0, newYlocalValue, localFootProjection.z));
        // newGlobalPosition = currentPosition;
        // // float yVar = Mathf.Lerp(currentPosition.y, newYGlobalValues.y, 0.5f);
        var yVar = Mathf.MoveTowards(currentPosition.y, newGlobalPosition.y, step);
        // newGlobalPosition.y = newYGlobalValues.y;
        newGlobalPosition.y = yVar;
        _animator.SetIKPositionWeight(goal, 1);
        _animator.SetIKPosition(goal, newGlobalPosition);
        
        // var currentGlobalPosition = _animator.GetBoneTransform(foot).position;
        // var newYlocalValue = (float)_curve.Interpolate(localFootProjection.z);
        // var newYGlobalValues = matrix.MultiplyPoint3x4(new Vector3(0, newYlocalValue, localFootProjection.z));
        //
        // float yVariable = Mathf.Lerp(prevPositionY, newYGlobalValues.y, 0.5f);
        // currentGlobalPosition.y += yVariable;
        // prevPositionY = yVariable;
        //
        // newGlobalPosition = currentGlobalPosition;
        // newGlobalPosition.y = newYGlobalValues.y;
        // _animator.SetIKPositionWeight(goal, 1);
        // _animator.SetIKPosition(goal, currentGlobalPosition);
    }
}
