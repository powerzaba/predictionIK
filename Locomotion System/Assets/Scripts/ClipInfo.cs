using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

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
        //AnalyzeData();
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


    private void AnalyzeData()
    {
        /*
        Tuple<float[], float[]> sampleX = null;
        Tuple<float[], float[]> sampleZ = null;
        Tuple<float[], float[]> sampleY = null;

        Tuple<float[], float[]> LsampleX = null;
        Tuple<float[], float[]> LsampleZ = null;
        Tuple<float[], float[]> LsampleY = null;

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

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                if (binding.propertyName == "RightFootT.x")
                {
                    sampleX = SampleAndLog(clip, curve, "_rightFootX", shouldLog);
                }
                if (binding.propertyName == "RightFootT.z")
                {
                    sampleZ = SampleAndLog(clip, curve, "_rightFootZ", shouldLog);
                }
                if (binding.propertyName == "RightFootT.y")
                {
                    sampleY = SampleAndLog(clip, curve, "_rightFootY", shouldLog);
                }
                if (binding.propertyName == "LeftFootT.x")
                {
                    LsampleX = SampleAndLog(clip, curve, "_leftFootX", shouldLog);
                }
                if (binding.propertyName == "LeftFootT.z")
                {
                    LsampleZ = SampleAndLog(clip, curve, "_leftFootZ", shouldLog);
                }
                if (binding.propertyName == "LeftFootT.y")
                {
                    LsampleY = SampleAndLog(clip, curve, "_leftFootY", shouldLog);
                }
            }

            Tuple<int, float, int, float> RkeyTimesAndIndexes;
            Tuple<int, float, int, float> LkeyTimesAndIndexes;
            (float[] peaks, float[] valleys) test;
            //TODO: Change FindLiftOffStrikeTimes signature so that it can accept the Y values;
            //TODO: Change the signature so that it can also take Tuples directly instead of single Items
            if (clip.averageSpeed.x > clip.averageSpeed.z)
            {
                //RkeyTimesAndIndexes = FindLiftOffStrikeTimes(sampleX.Item1, sampleX.Item2, clip);
                //LkeyTimesAndIndexes = FindLiftOffStrikeTimes(LsampleX.Item1, LsampleX.Item2, clip);
                ReturnKeyTimes(sampleX.ToValueTuple<float[], float[]>(), sampleY.ToValueTuple<float[], float[]>(), clip);
                test = ReturnPeaksAndValleys(sampleX.ToValueTuple<float[], float[]>());
            }
            else
            {
                //RkeyTimesAndIndexes = FindLiftOffStrikeTimes(sampleZ.Item1, sampleZ.Item2, clip);
                //LkeyTimesAndIndexes = FindLiftOffStrikeTimes(LsampleZ.Item1, LsampleZ.Item2, clip);                
                ReturnKeyTimes(sampleZ.ToValueTuple<float[], float[]>(), sampleY.ToValueTuple<float[], float[]>(), clip);
                test = ReturnPeaksAndValleys(sampleZ.ToValueTuple<float[], float[]>());
            }

            Debug.Log(clip.name + " -  peaks: " + test.peaks.Length);
            foreach (float thingy in test.peaks) Debug.Log(thingy);
            Debug.Log(clip.name + " -  valleys: " + test.valleys.Length);
            foreach (float thingy in test.valleys) Debug.Log(thingy);

            //TODO: Go over whatever you did here
            /*
            float RflightTime = Math.Abs(RkeyTimesAndIndexes.Item4 - RkeyTimesAndIndexes.Item2);
            float LflightTime = Math.Abs(LkeyTimesAndIndexes.Item4 - LkeyTimesAndIndexes.Item2);
            float averageFlightTime = (RflightTime + LflightTime) / 2;
            float groundTime = clip.length - averageFlightTime;

            double deltaX = sampleX.Item1[RkeyTimesAndIndexes.Item1] - sampleX.Item1[RkeyTimesAndIndexes.Item3];
            double deltaZ = sampleZ.Item1[RkeyTimesAndIndexes.Item1] - sampleZ.Item1[RkeyTimesAndIndexes.Item3];
            
            double LdeltaX = LsampleX.Item1[LkeyTimesAndIndexes.Item1] - LsampleX.Item1[LkeyTimesAndIndexes.Item3];
            double LdeltaZ = LsampleZ.Item1[LkeyTimesAndIndexes.Item1] - LsampleZ.Item1[LkeyTimesAndIndexes.Item3];

            double stepLength = Math.Sqrt(Math.Pow(deltaX, 2) + Math.Pow(deltaZ, 2));
            double LstepLegth = Math.Sqrt(Math.Pow(LdeltaX, 2) + Math.Pow(LdeltaZ, 2));
            double averageStepLength = (stepLength + LstepLegth) / 2;

            AnimationUtility.SetAnimationEvents(clip, CreateEvents(LkeyTimesAndIndexes, RkeyTimesAndIndexes));

            Debug.Log("Step Length is: " + clip.name + " - " + averageStepLength);
            Debug.Log("Total Clip Time: " + clip.length);
            Debug.Log("Flight Time: " + averageFlightTime);
            Debug.Log("Ground Time: " + groundTime);
            
        }
        */
    }

    private AnimationEvent[] CreateEvents(Tuple<int, float, int, float> LkeyTimes, Tuple<int, float, int, float> RkeyTimes)
    {
        AnimationEvent[] evt = new AnimationEvent[4];
        evt[0] = new AnimationEvent { time = LkeyTimes.Item2, stringParameter = "L_liftOff" };
        evt[1] = new AnimationEvent { time = LkeyTimes.Item4, stringParameter = "L_strike" };
        evt[2] = new AnimationEvent { time = RkeyTimes.Item2, stringParameter = "R_liftOff" };
        evt[3] = new AnimationEvent { time = RkeyTimes.Item4, stringParameter = "R_strike" };

        return evt;
    }

    private Tuple<int, float, int, float> FindLiftOffStrikeTimes(float[] val, float[] time, AnimationClip clip)
    {
        float min = float.MaxValue;
        float max = float.MinValue;
        float minTime = 0f;
        float maxTime = 0f;
        int minIndex = 0;
        int maxIndex = 0;

        for (int i = 0; i < sampleNumber; i++)
        {
            if (val[i] < min)
            {
                min = val[i];
                minTime = time[i];
                minIndex = i;
            }
            else if (val[i] > max)
            {
                max = val[i];
                maxTime = time[i];
                maxIndex = i;
            }
        }

        //minTime += (float)(val[minIndex] * treshold);
        //maxTime += (float)(val[maxIndex] * treshold);
        minTime *= (1 + treshold);
        maxTime *= (1 + treshold);
        return Tuple.Create(minIndex, minTime, maxIndex, maxTime);
    }
}