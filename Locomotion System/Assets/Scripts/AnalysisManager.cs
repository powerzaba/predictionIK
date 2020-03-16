using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnalysisManager 
{
    private List<AnimationAnalyzer> _analyzers;
    private AnimationClip[] _clipList;
    private string _path;
    
    public AnalysisManager(float sampleNumber, ref List<AnimationClip> errorList, float ground, float treshold)
    {

    }

    public void LoadAnimationClips()
    {
        var list = Resources.LoadAll("AnimationClips", typeof(AnimationClip));
        _clipList = new AnimationClip[list.Length];
        Array.Copy(list, _clipList, list.Length);
    }

    public void AnalyzeAnimations()
    {
        foreach (var clip in _clipList)
        {
            var animationAnalyzer = new AnimationAnalyzer(clip, sampleNumber)
        }
    }
}
