using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AnalysisManager 
{
    public List<AnimationClip> errorList { get; private set; }
    private AnimationClip[] _clipList;
    
    public AnalysisManager()
    {
        errorList = new List<AnimationClip>();
    }

    private void LoadAnimationClips()
    {
        var list = Resources.LoadAll("AnimationClips", typeof(AnimationClip));
        _clipList = new AnimationClip[list.Length];
        Array.Copy(list, _clipList, list.Length);
    }

    public void AnalyzeAnimations(int sampleNumber, float velocityTh, int smoothingTh)
    {
        LoadAnimationClips();

        foreach (var clip in _clipList)
        {
            if (!clip.isHumanMotion)
            {
                errorList.Add(clip);
                continue;
            }                

            var animationAnalyzer = new AnimationAnalyzer(clip, sampleNumber);
            animationAnalyzer.AnalyzeAnimation(velocityTh, smoothingTh);
        }
    }

    //TODO: TO modify
    public void RemoveCurves()
    {
        if (_clipList == null)
        {
            LoadAnimationClips();
        }

        foreach (var clip in _clipList)
        {
            //Remove grounded information curves
            clip.SetCurve("", typeof(Animator), "LeftFootCurve", null);
            clip.SetCurve("", typeof(Animator), "RightFootCurve", null);
            
            //Remove flight time curves
            clip.SetCurve("", typeof(Animator), "LeftFlightCurve", null);
            clip.SetCurve("", typeof(Animator), "RightFlightCurve", null);
            
            //Remove displacement curves, X
            clip.SetCurve("", typeof(Animator), "LeftDisplacementX", null);
            clip.SetCurve("", typeof(Animator), "RightDisplacementX", null);
            
            //Remove displacement curves, Z
            clip.SetCurve("", typeof(Animator), "LeftDisplacementZ", null);
            clip.SetCurve("", typeof(Animator), "RightDisplacementZ", null);
            
            //Remove step length curves
            clip.SetCurve("", typeof(Animator), "LeftStrideLength", null);
            clip.SetCurve("", typeof(Animator), "RightStrideLength", null);
        }
    }
}
