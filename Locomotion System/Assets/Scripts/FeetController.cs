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
    public Vector3 center { get; private set; }
    public Vector3 half { get; private set; }
    public Vector3 localUp { get; private set; }
    private readonly double[] _inputX = new double[3];
    private readonly double[] _inputY = new double[3];
    
    public Vector3 localTargetPosition;
    
    public CubicSpline curve;
    public Vector3 midPoint;
    
    //Test projection
    public Vector3 localFootProjection; 
    
    //test pos
    public Vector3 newGlobalPosition;
    public bool midPointHit = false;
    
    //Adding variables for catmull rom spline
    public Vector3 highestHitPoint;
    public Vector3 secondHighestHitPoint;
    public CatmullRom catmullRomSpline;
    
    //todo: fix this please hahahah
    public Vector3[] threeHits = new Vector3[3];
    public Vector3[] fourHits = new Vector3[4];


    public FeetController(Animator animator)
    {
        _animator = animator;
    }
    
    public void CreateBoxCast(Vector3 from, Vector3 to, float midPointHeight, float heightTh, float curveTh, bool catmull)
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
            LayerMask mask = LayerMask.GetMask("Default");
            hit = Physics.BoxCastAll(center, half,-localUp, rotation, 2f, mask);
            
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

            midPoint = (to + from) / 2;
            //TODO: remember this number 0.05
            midPoint = midPoint + rotation * Vector3.up * midPointHeight;
            midPoint = matrix.inverse.MultiplyPoint3x4(midPoint);
        
            localTargetPosition = matrix.inverse.MultiplyPoint3x4(to);

            if (!catmull)
            {
                if (hit[0].point != Vector3.zero)
                {
                    var highPoint = matrix.inverse.MultiplyPoint3x4(hit[0].point);
                    if (Math.Abs(highPoint.z - localTargetPosition.z) > curveTh)
                    {
                        if (highPoint.y > midPoint.y)
                        {
                            highestHitPoint = highPoint;
                            midPointHit = true;
                        }
                        else
                        {
                            highestHitPoint = midPoint;
                            midPointHit = false;
                        }
                    }
                }
        
                _inputX[0] = 0;
                _inputX[1] = highestHitPoint.z;
                _inputX[2] = localTargetPosition.z;


                _inputY[0] = 0;
                _inputY[1] = highestHitPoint.y + heightTh;
                _inputY[2] = localTargetPosition.y;
            
                curve = CreateCurve(_inputX, _inputY);
            }
            else
            {
                if (hit[0].point != Vector3.zero)
                {
                    var highPoint = matrix.inverse.MultiplyPoint3x4(hit[0].point);
                    
                    if (highPoint.y > midPoint.y)
                    {
                        highestHitPoint = highPoint;
                        midPointHit = true;
                    }
                    else
                    {
                        highestHitPoint = midPoint;
                        midPointHit = false;
                    }

                    if (hit.Length > 1)
                    {
                        var secondPoint = matrix.inverse.MultiplyPoint3x4(hit[1].point);

                        if (secondPoint.y > midPoint.y)
                        {
                            secondHighestHitPoint = secondPoint;
                        }
                        else
                        {
                            secondHighestHitPoint = midPoint;
                        }
                    }
                }
                
                threeHits[0] = Vector3.zero;
                threeHits[1] = highestHitPoint + new Vector3(0, heightTh, 0);
                threeHits[2] = localTargetPosition;
                catmullRomSpline = new CatmullRom(threeHits);
                
                // if (highestHitPoint == secondHighestHitPoint)
                // {
                //     threeHits[0] = Vector3.zero;
                //     threeHits[1] = highestHitPoint + new Vector3(0, heightTh, 0);
                //     threeHits[2] = localTargetPosition;
                //     catmullRomSpline = new CatmullRom(threeHits);
                // }
                // else
                // {
                //     fourHits[0] = Vector3.zero;
                //     fourHits[1] = highestHitPoint + new Vector3(0, heightTh, 0);;
                //     fourHits[2] = secondHighestHitPoint + new Vector3(0, heightTh, 0);;
                //     fourHits[3] = localTargetPosition;
                //     catmullRomSpline = new CatmullRom(fourHits);
                // }
            }
        }
    }
    
    private CubicSpline CreateCurve(double[] x, double[] y)
    {
        //CubicSpline curve = CubicSpline.InterpolateNatural(x, y);
        // var curve = CubicSpline.InterpolateNaturalInplace(x, y);
        CubicSpline curve = CubicSpline.InterpolateNaturalSorted(x, y);
        return curve;
    }

    public void GetProjectionOnCurve(Vector3 position)
    {
        localFootProjection = matrix.inverse.MultiplyPoint3x4(position);
    }

    public void MoveFeetAlongCurve(HumanBodyBones foot, AvatarIKGoal goal, Vector3 currentPosition, float speed)
    {
        var step = speed * Time.deltaTime;
        var newYlocalValue = (float)curve.Interpolate(localFootProjection.z);
        newGlobalPosition = matrix.MultiplyPoint3x4(new Vector3(0, newYlocalValue, localFootProjection.z));
        // newGlobalPosition = currentPosition;
        // // float yVar = Mathf.Lerp(currentPosition.y, newYGlobalValues.y, 0.5f);
        
        // newGlobalPosition.y = newYGlobalValues.y;
        
        
        var yVar = Mathf.MoveTowards(currentPosition.y, newGlobalPosition.y, step);
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
    
    public void MoveFeetCatmull(AvatarIKGoal goal, Vector3 currentPosition, float speed)
    {
        var step = speed * Time.deltaTime;
        //to add parameters from function call
        var newLocalPos = catmullRomSpline.Interpolate(localFootProjection, 0, 0.5f);
        newGlobalPosition = matrix.MultiplyPoint3x4(new Vector3(0, newLocalPos.y, localFootProjection.z));
        // newGlobalPosition = matrix.MultiplyPoint3x4(newLocalPos);
        
        var yVar = Mathf.MoveTowards(currentPosition.y, newGlobalPosition.y, step);
        newGlobalPosition.y = yVar;
        _animator.SetIKPositionWeight(goal, 1);
        _animator.SetIKPosition(goal, newGlobalPosition);
        
        // var test = new Vector3(0, newGlobalPosition.y, currentPosition.z);
        // var newY = Vector3.MoveTowards(currentPosition, test, step);
        // test.y = newY.y;
        // _animator.SetIKPositionWeight(goal, 1);
        // _animator.SetIKPosition(goal, test);
    }
}
