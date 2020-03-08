﻿using System;
using System.IO;
using UnityEditor;
using UnityEngine;

class Sampler
{
    private AnimationClip _clip;
    private int _sampleNumber;
    private LegPositionInformation _leftFootPos { get; set; }
    private LegPositionInformation _rightFootPos { get; set; }
    private RootInformation _rootInfo { get; set; }
    private float[] _timeSample;

    public Sampler(AnimationClip clip, int sampleNumber)
    {
        _clip = clip;
        _sampleNumber = sampleNumber;

        SampleTime();
    }

    private void SampleTime()
    {
        float duration = _clip.length;
        float step = duration / _sampleNumber;
        float currentTime = 0f;

        for (int i = 0; i < _sampleNumber; i++)
        {
            _timeSample[i] = currentTime;
            currentTime += step;
        }
    }

    public void LogData()
    {
        string path = Directory.GetCurrentDirectory() + @"\Assets\AnimationLogData\"
                                                      + _clip.name;
        string rightFootPath = path + "_Right_Position";
        string leftFootPath = path + "_Left_Position";
        string rootPositionPath = path + "_Root_Position";
        string rootRotationPath = path + "_Root_ROtation";
        System.IO.File.WriteAllLines(rightFootPath, _rightFootPos.getStringPosition());
        System.IO.File.WriteAllLines(leftFootPath, _leftFootPos.getStringPosition());
        System.IO.File.WriteAllLines(rootPositionPath, _rootInfo.getStringPosition());
        System.IO.File.WriteAllLines(rootRotationPath, _rootInfo.getStringRotation());
    }

    private LegPositionInformation CreatePositionStruct(EditorCurveBinding[] bindings)
    {
        LegPositionInformation posInfo = new LegPositionInformation();
        posInfo.position = new Vector3[_sampleNumber];
        posInfo.x = new float[_sampleNumber];
        posInfo.y = new float[_sampleNumber];
        posInfo.z = new float[_sampleNumber];

        var xCurve = AnimationUtility.GetEditorCurve(_clip, bindings[0]);
        var yCurve = AnimationUtility.GetEditorCurve(_clip, bindings[1]);
        var zCurve = AnimationUtility.GetEditorCurve(_clip, bindings[2]);
        float time = 0;
        float x = 0;
        float y = 0;
        float z = 0;


        for (int i = 0; i < _sampleNumber; i++)
        {
            time = _timeSample[i];
            x = xCurve.Evaluate(time);
            y = yCurve.Evaluate(time);
            z = zCurve.Evaluate(time);
            posInfo.x[i] = x;
            posInfo.y[i] = y;
            posInfo.z[i] = z;
            posInfo.position[i] = new Vector3(x, y, z);
        }

        return posInfo;
    }

    private RootInformation CreateRootInfoStruct(EditorCurveBinding[] posBindings,
                                                 EditorCurveBinding[] rotBindings)
    {
        RootInformation rootInfo = new RootInformation();
        rootInfo.position = new Vector3[_sampleNumber];
        rootInfo.rotation = new Quaternion[_sampleNumber];

        var xPosCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[0]);
        var yPosCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[1]);
        var zPosCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[2]);
        var xCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[0]);
        var yCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[1]);
        var zCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[2]);
        var wCurve = AnimationUtility.GetEditorCurve(_clip, rotBindings[3]);
        float time = 0;
        float x, y, z;
        float q_x, q_y, q_z, q_w; ;

        for (int i = 0; i < _sampleNumber; i++)
        {
            time = _timeSample[i];
            x = xPosCurve.Evaluate(time);
            y = yPosCurve.Evaluate(time);
            z = zPosCurve.Evaluate(time);
            q_x = xCurve.Evaluate(time);
            q_y = yCurve.Evaluate(time);
            q_z = zCurve.Evaluate(time);
            q_w = wCurve.Evaluate(time);
            rootInfo.rotation[i] = new Quaternion(q_x, q_y, q_z, q_w);
            rootInfo.position[i] = new Vector3(x, y, z);

        }

        return rootInfo;
    }

    public void Sample(bool shouldLog)
    {
        var bindingList = AnimationUtility.GetCurveBindings(_clip);
        EditorCurveBinding[] posBinding = new EditorCurveBinding[3];
        EditorCurveBinding[] rotBinding = new EditorCurveBinding[4];

        for (int i = 0; i < bindingList.Length; i++)
        {
            if (bindingList[i].propertyName == "RightFootT.x")
            {
                Array.Copy(bindingList, i, posBinding, 0, 3);
                _rightFootPos = CreatePositionStruct(posBinding);
            }
            else if (bindingList[i].propertyName == "LeftFootT.x")
            {
                Array.Copy(bindingList, i, posBinding, 0, 3);
                _leftFootPos = CreatePositionStruct(posBinding);
            }
            else if (bindingList[i].propertyName == "RootQ.x")
            {
                Array.Copy(bindingList, i, rotBinding, 0, 4);
                Array.Copy(bindingList, i + 4, posBinding, 0, 3);
                _rootInfo = CreateRootInfoStruct(posBinding, rotBinding);
            }
        }

        ConvertFromLocalToWorld();
        //_groundLevel = (_rightLegSample[0].y <= _leftLegSample[0].y) ? _rightLegSample[0].y : _leftLegSample[0].y;
        //_groundLevel *= (1 + thresholdGround);
    }

    private void ConvertFromLocalToWorld()
    {
        GameObject empty = new GameObject();
        for (int i = 0; i < _sampleNumber; i++)
        {
            _rootInfo.position[i].x = 0;
            _rootInfo.position[i].z = 0;

            empty.transform.position = _rootInfo.position[i];
            empty.transform.rotation = _rootInfo.rotation[i];

            Vector3 rightPos = empty
                               .transform
                               .TransformPoint(_rightFootPos.position[i]);
            Vector3 leftPos = empty
                               .transform
                               .TransformPoint(_leftFootPos.position[i]);
            _rightFootPos.setPosition(rightPos, i);
            _leftFootPos.setPosition(leftPos, i);
        }
    }
}

public struct RootInformation
{
    public Vector3[] position;
    public Quaternion[] rotation;

    public string[] getStringPosition()
    {
        return Array.ConvertAll(position, e => e.ToString("F8")
                                                .Replace("(", "")
                                                .Replace(")", ""));
    }

    public string[] getStringRotation()
    {

        return Array.ConvertAll(rotation, e => e.ToString("F8")
                                                .Replace("(", "")
                                                .Replace(")", ""));
    }
}

public struct LegPositionInformation
{
    public float[] x;
    public float[] y;
    public float[] z;
    public Vector3[] position;

    public void setPosition(Vector3 newPos, int index)
    {
        x[index] = newPos.x;
        y[index] = newPos.y;
        z[index] = newPos.z;
        position[index] = newPos;
    }

    public string[] getStringPosition()
    {
        return Array.ConvertAll(position, e => e.ToString("F8")
                                                .Replace("(", "")
                                                .Replace(")", ""));
    }
}