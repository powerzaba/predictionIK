using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EventManager
{
    private float[] _timeSample;
    private AnimationClip _clip;

    public EventManager(ref AnimationClip clip, float[] time)
    {
        _timeSample = time;
        _clip = clip;
    }

    public void InsertFeetCurve((int[] strikeIndex, int[] liftIndex) leftKeyIndexes,
                                (int[] strikeIndex, int[] liftIndex) rightKeyIndexes)
    {
        AnimationCurve leftFootCurve;
        AnimationCurve rightFootCurve;

        List<Keyframe> leftKeys = GenerateKeyFrmes(leftKeyIndexes);
        List<Keyframe> rightKeys = GenerateKeyFrmes(rightKeyIndexes);

        leftFootCurve = new AnimationCurve(leftKeys.ToArray());
        rightFootCurve = new AnimationCurve(rightKeys.ToArray());

        _clip.SetCurve("", typeof(Animator), "LeftFootCurve", leftFootCurve);
        _clip.SetCurve("", typeof(Animator), "RightFootCurve", rightFootCurve);
    }

    private List<Keyframe> GenerateKeyFrmes((int[] strikeIndexes, int[] liftIndexes) foot)
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
    }

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

    public void RemoveEvents()
    {
        if (_clip != null)
        {
            AnimationEvent[] empty = new AnimationEvent[1];
            AnimationUtility.SetAnimationEvents(_clip, empty);
        }
    }

    public void RemoveCurves()
    {
        if (_clip != null)
        {
            _clip.SetCurve("", typeof(Animator), "LeftFootCurve", null);
            _clip.SetCurve("", typeof(Animator), "RightFootCurve", null);
        }       
    }
}

