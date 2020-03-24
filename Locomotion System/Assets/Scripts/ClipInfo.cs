using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ClipInfo : EditorWindow
{
    private AnimationClip _idleClip;
    private List<AnimationClip> _errorList;
    private AnalysisManager _analysisManager;
    private int _sampleNumber = 100;
    private float _velocityTh = 1.5f;
    private int _smoothTh = 4;
    private float _groundTh = 0.01f;
    private string _path = Directory.GetCurrentDirectory() + @"\Assets\Scripts\AnimData.txt";

    [MenuItem("Window/Clip Info")]
    static void Init()
    {
        GetWindow(typeof(ClipInfo));
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("Sample Number");
        _sampleNumber = EditorGUILayout.IntField(_sampleNumber);

        EditorGUILayout.LabelField("Ground Treshold");
        _groundTh = EditorGUILayout.FloatField(_groundTh);

        EditorGUILayout.LabelField("Velocity Treshold");
        _velocityTh = EditorGUILayout.FloatField(_velocityTh);

        EditorGUILayout.LabelField("Idle Animation");
        _idleClip = EditorGUILayout.ObjectField(_idleClip, typeof(AnimationClip), false) as AnimationClip;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Analyze Animation"))
        {
            AnalyzeAnimation();
        }

        if (GUILayout.Button("Reset"))
        {
            Reset();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void Reset()
    {
        if (_analysisManager != null)
        {
            _analysisManager.RemoveCurves();
        }        
    }

    private void AnalyzeAnimation()
    {        
        if (_idleClip == null)
        {
            EditorUtility.DisplayDialog("Missing Idle", "Please select an Idle Animation", "ok");
            return;
        }

        _analysisManager = new AnalysisManager();
        _analysisManager.AnalyzeAnimations(_sampleNumber, _velocityTh, _smoothTh);
    }
}
