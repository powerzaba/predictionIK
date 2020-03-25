using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    private readonly float[] _timeSample;
    private readonly AnimationClip _clip;

    public EventManager(AnimationClip clip, float[] time)
    {
        _timeSample = time;
        _clip = clip;
    }

    public void InsertGroundedCurve(int[] rightGroundedData, int[] leftGroundedData)
    {
        AnimationCurve leftFootCurve;
        AnimationCurve rightFootCurve;

        var left = Array.ConvertAll(leftGroundedData, x => (float)x);
        var right = Array.ConvertAll(rightGroundedData, x => (float)x);

        List<Keyframe> leftKeys = GenerateKeyFrames(left);
        List<Keyframe> rightKeys = GenerateKeyFrames(right);

        leftFootCurve = new AnimationCurve(leftKeys.ToArray());
        rightFootCurve = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftFootCurve", leftFootCurve);
        _clip.SetCurve("", typeof(Animator), "RightFootCurve", rightFootCurve);
    }

    public void InsertFlightTimeCurve(float[] rightFlightData, float[] leftFlightData)
    {
        AnimationCurve leftFootCurve;
        AnimationCurve rightFootCurve;

        List<Keyframe> leftKeys = GenerateContinuousFrames(leftFlightData);
        List<Keyframe> rightKeys = GenerateContinuousFrames(rightFlightData);

        leftFootCurve = new AnimationCurve(leftKeys.ToArray());
        rightFootCurve = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftFlightCurve", leftFootCurve);
        _clip.SetCurve("", typeof(Animator), "RightFlightCurve", rightFootCurve);

    }

    public void InsertDisplacementX(float[] rightFootDis, float[] leftFootDis)
    {
        AnimationCurve leftFootCurveX;        
        AnimationCurve rightFootCurveX;        

        List<Keyframe> leftKeys = GenerateContinuousFrames(leftFootDis);
        List<Keyframe> rightKeys = GenerateContinuousFrames(rightFootDis);

        leftFootCurveX = new AnimationCurve(leftKeys.ToArray());
        rightFootCurveX = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftDisplacementX", leftFootCurveX);        
        _clip.SetCurve("", typeof(Animator), "RightDisplacementX", rightFootCurveX);        
    }

    public void InsertDisplacementZ(float[] rightFootDis, float[] leftFootDis)
    {
        AnimationCurve leftFootCurveZ;
        AnimationCurve rightFootCurveZ;

        List<Keyframe> leftKeys = GenerateContinuousFrames(leftFootDis);
        List<Keyframe> rightKeys = GenerateContinuousFrames(rightFootDis);

        leftFootCurveZ = new AnimationCurve(leftKeys.ToArray());
        rightFootCurveZ = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftDisplacementZ", leftFootCurveZ);
        _clip.SetCurve("", typeof(Animator), "RightDisplacementZ", rightFootCurveZ);
    }

    public void InsertStepLength(float[] rightLength, float[] leftLength)
    {
        AnimationCurve leftStrideLength;
        AnimationCurve rightStrideLength;

        List<Keyframe> leftKeys = GenerateKeyFrames(leftLength);
        List<Keyframe> rightKeys = GenerateKeyFrames(rightLength);

        leftStrideLength = new AnimationCurve(leftKeys.ToArray());
        rightStrideLength = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftStrideLength", leftStrideLength);
        _clip.SetCurve("", typeof(Animator), "RightStrideLength", rightStrideLength);
    }

    private List<Keyframe> GenerateKeyFrames(float[] data)
    {
        var footKeys = new List<Keyframe>();
        var isIdle = true;
        const float infinity = Mathf.Infinity;
        
        for (var i = 1; i < data.Length; i ++)
        {
            if (data[i] == data[i - 1]) continue;
            
            isIdle = false;
            var value = data[i];
            var prevValue = data[i - 1];
            var time = _timeSample[i];

            if (value != 0)
            {
                footKeys.Add(new Keyframe(time, value, infinity, infinity));
                footKeys.Add(new Keyframe(time - 0.001f, 0, infinity, infinity));
            }
            else
            {
                footKeys.Add(new Keyframe(time, 0, infinity, infinity));
                footKeys.Add(new Keyframe(time - 0.001f, prevValue, infinity, infinity));
            }
        }

        if (isIdle)        
            footKeys.Add(new Keyframe(0, data[0], Mathf.Infinity, Mathf.Infinity));        

        return footKeys;
    }

    private List<Keyframe> GenerateContinuousFrames(float[] data)
    {
        var footKeys = new List<Keyframe>();
        var isIdle = true;
        const float infinity = Mathf.Infinity;
        
        for (var i = 1; i < data.Length; i ++)
        {
            if (data[i] == data[i - 1]) continue;
            
            isIdle = false;
            var value = data[i];
            var prevValue = data[i - 1];
            var time = _timeSample[i];
            
            footKeys.Add(new Keyframe(time, value, infinity, infinity));
            footKeys.Add(new Keyframe(time - 0.001f, prevValue, infinity, infinity));
        }

        if (isIdle)        
            footKeys.Add(new Keyframe(0, data[0], Mathf.Infinity, Mathf.Infinity));        

        return footKeys;
    }
}

