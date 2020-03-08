using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ClipInfo : EditorWindow
{
    private AnimationClip[] clipList;
    private AnimationClip idleClip;
    private List<AnimationClip> errorList;
    private List<AnimationClip> nonHumanoidList;
    private int sampleNumber = 100;
    private float treshold = 0.1f;

    private bool shouldLog = false;

    [MenuItem("Window/Clip Info")]
    static void Init()
    {
        GetWindow(typeof(ClipInfo));
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("Sample Number");
        sampleNumber = EditorGUILayout.IntField(sampleNumber);

        EditorGUILayout.LabelField("Treshold Value");
        treshold = EditorGUILayout.FloatField(treshold);

        EditorGUILayout.LabelField("Idle Animation");
        idleClip = EditorGUILayout.ObjectField(idleClip, typeof(AnimationClip), false) as AnimationClip;

        shouldLog = EditorGUILayout.Toggle("Log Data", shouldLog);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Analyze Animation"))
        {
            AnalyzeMotion();
        }

        if (GUILayout.Button("Reset"))
        {
            Reset();
        }
        EditorGUILayout.EndHorizontal();

        if (nonHumanoidList != null)
        {
            foreach (AnimationClip clip in nonHumanoidList)
            {
                EditorGUILayout.LabelField(clip.name + ": is not a Humanoid Animation, cannot analyze");
            }
        }

        if (errorList != null)
        {
            foreach (AnimationClip clip in errorList)
            {
                EditorGUILayout.LabelField(clip.name + ": could not analyze motion");
            }
        }
    }

    private void Reset()
    {
        AnimationEvent[] empty = new AnimationEvent[0];

        LoadClip();
        foreach (AnimationClip clip in clipList)
        {
            AnimationUtility.SetAnimationEvents(clip, empty);
        }

        if (errorList != null)
        {
            errorList.Clear();
        }

        if (nonHumanoidList != null)
        {
            nonHumanoidList.Clear();
        }
    }

    private void AnalyzeMotion()
    {
        LoadClip();
        Ann();
    }

    private void LoadClip()
    {
        var list = Resources.LoadAll("AnimationClips", typeof(AnimationClip));
        clipList = new AnimationClip[list.Length];
        Array.Copy(list, clipList, list.Length);
    }

    private void Ann()
    {
        foreach (AnimationClip clip in clipList)
        {
            if (clip.name == idleClip.name)
            {
                continue;
            }

            if (!clip.humanMotion)
            {
                nonHumanoidList.Add(clip);
                continue;
            }

            AnimationClipSample animationSample = new AnimationClipSample(clip, sampleNumber, ref errorList);
            animationSample.AnalyzeAnimation(shouldLog);
        }
    }
}