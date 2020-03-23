using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EventManager
{
    private float[] _timeSample;
    private AnimationClip _clip;

    public EventManager(AnimationClip clip, float[] time)
    {
        _timeSample = time;
        _clip = clip;
    }

    /*public void InsertFeetCurve((int[] strikeIndex, int[] liftIndex) leftKeyIndexes,
                                (int[] strikeIndex, int[] liftIndex) rightKeyIndexes)
    {
        AnimationCurve leftFootCurve;
        AnimationCurve rightFootCurve;

        //List<Keyframe> leftKeys = GenerateKeyFrmes(leftKeyIndexes);
        //List<Keyframe> rightKeys = GenerateKeyFrmes(rightKeyIndexes);

        //leftFootCurve = new AnimationCurve(leftKeys.ToArray());
        //rightFootCurve = new AnimationCurve(rightKeys.ToArray());

        //_clip.SetCurve("", typeof(Animator), "LeftFootCurve", leftFootCurve);
        //_clip.SetCurve("", typeof(Animator), "RightFootCurve", rightFootCurve);
    }*/

    public void InsertFeetCurve(int[] rightGroundedData, int[] leftGroundedData)
    {
        AnimationCurve leftFootCurve;
        AnimationCurve rightFootCurve;

        List<Keyframe> leftKeys = GenerateKeyFrames(leftGroundedData);
        List<Keyframe> rightKeys = GenerateKeyFrames(rightGroundedData);

        leftFootCurve = new AnimationCurve(leftKeys.ToArray());
        rightFootCurve = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftFootCurve", leftFootCurve);
        _clip.SetCurve("", typeof(Animator), "RightFootCurve", rightFootCurve);
    }

    private List<Keyframe> GenerateKeyFrames(int[] groundedData)
    {
        List<Keyframe> footKeys = new List<Keyframe>();
        bool idle = true;
        
        for (int i = 1; i < groundedData.Length; i ++)
        {
            if (groundedData[i] != groundedData[i - 1])
            {
                idle = false;
                if (groundedData[i] == 1)
                {
                    footKeys.Add(new Keyframe(_timeSample[i], 0, Mathf.Infinity, Mathf.Infinity));
                    footKeys.Add(new Keyframe(_timeSample[i] + 0.001f, 1, Mathf.Infinity, Mathf.Infinity));
                }
                else
                {
                    footKeys.Add(new Keyframe(_timeSample[i], 1, Mathf.Infinity, Mathf.Infinity));
                    footKeys.Add(new Keyframe(_timeSample[i] + 0.001f, 0, Mathf.Infinity, Mathf.Infinity));
                }
            }
        }

        if (idle)
            footKeys.Add(new Keyframe(0, 1, Mathf.Infinity, Mathf.Infinity));

        return footKeys;
    }   

    /*private List<Keyframe> GenerateKeyFrmes((int[] strikeIndexes, int[] liftIndexes) foot)
    {
        List<Keyframe> footKeys = new List<Keyframe>();
        var len = foot.strikeIndexes.Length;

        if (_timeSample[foot.strikeIndexes[0]] <= _timeSample[foot.liftIndexes[0]])
        {

            footKeys.Add(new Keyframe(0, 1.5f, 0, 20));
            footKeys.Add(new Keyframe(_clip.length, 1.5f, -20, 20));

        }
        else
        {
            footKeys.Add(new Keyframe(0, 1, -20, 0));
            footKeys.Add(new Keyframe(_clip.length, 1, 0, 20));
        }

        for (int i = 0; i < len; i++)
        {
            footKeys.Add(new Keyframe(_timeSample[foot.strikeIndexes[i]], 1, -20, 0));
            footKeys.Add(new Keyframe(_timeSample[foot.liftIndexes[i]], 1, 0, 20));
        }

        return footKeys;
    }*/

    public void InsertAnimationEvents(string displacementVector, float flightTime)
    {
        AnimationEvent[] evt = new AnimationEvent[1];

        evt[0] = new AnimationEvent
        {
            time = 0,
            stringParameter = displacementVector,
            floatParameter = flightTime
            
        };

        AnimationUtility.SetAnimationEvents(_clip, evt);
    }
}

