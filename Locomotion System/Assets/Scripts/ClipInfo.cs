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
    private float ground = 0.01f;
    private bool shouldLog = false;

    public static AnimationClip _idle;

    [MenuItem("Window/Clip Info")]
    static void Init()
    {
        GetWindow(typeof(ClipInfo));
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("Sample Number");
        sampleNumber = EditorGUILayout.IntField(sampleNumber);

        EditorGUILayout.LabelField("Ground Treshold");
        ground = EditorGUILayout.FloatField(ground);

        EditorGUILayout.LabelField("Time Treshold");
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

        if (clipList != null)
        {
            foreach (AnimationClip clip in clipList)
            {
                AnimationUtility.SetAnimationEvents(clip, empty);
            }
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
        Analyze();
    }

    private void LoadClip()
    {
        var list = Resources.LoadAll("AnimationClips", typeof(AnimationClip));
        clipList = new AnimationClip[list.Length];
        Array.Copy(list, clipList, list.Length);
    }

    private void Analyze()
    {       
        foreach (AnimationClip clip in clipList)
        {
            if (clip.name == idleClip.name)
            {
                //ADD RELATIVE POSITION FOR IDLE ANIMATION
                //ADD FLIGHT TIME AS WELL LOL
                _idle = clip;
                continue;
            }

            if (!clip.humanMotion)
            {                
                nonHumanoidList.Add(clip);
                continue;
            }

            var animationSample = new AnimationAnalyzer(clip, sampleNumber, ref errorList, ground, treshold);
            animationSample.AnalyzeAnimation(shouldLog);            
        }
    }
}