using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AnimationClipSample
{
    private AnimationClip _clip;
    private List<AnimationClip> _errorList;
    private int _sampleNumber;
    private float _groundLevel;
    private string _logPath;
    private float[] _timeSample;

    private (float[] val, float[] times) rightSampleX;
    private (float[] val, float[] times) rightSampleY;
    private (float[] val, float[] times) rightSampleZ;

    private (float[] val, float[] times) leftSampleX;
    private (float[] val, float[] times) leftSampleY;
    private (float[] val, float[] times) leftSampleZ;

    //TODO: getting root position over time
    private (float[] val, float[] times) rootSampleX;
    private (float[] val, float[] times) rootSampleY;

    private Vector3[] _rightLegSample;
    private Vector3[] _leftLegSample;
    private Vector3[] _rootPositionSample;
    private Quaternion[] _rootRotationSample;
    

    //TODO set threshold values for ground level from the editor
    private float thresholdGround = 0.1f;
    private int thresholdCheck = 15;


    public AnimationClipSample(AnimationClip clip, int sampleNumber, ref List<AnimationClip> errorList)
    {
        _clip = clip;
        _sampleNumber = sampleNumber;
        _errorList = errorList;
        _logPath = Directory.GetCurrentDirectory() + @"\Assets\AnimationLogData\";
        _timeSample = new float[_sampleNumber];

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

    //TODO: remove the log from this function
    private Vector3[] CreateVectorSample(EditorCurveBinding[] bindings, bool shouldLog, string name)
    {
        float x = 0;
        float y = 0;
        float z = 0;
        float time = 0;
        var xCurve = AnimationUtility.GetEditorCurve(_clip, bindings[0]);
        var yCurve = AnimationUtility.GetEditorCurve(_clip, bindings[1]);
        var zCurve = AnimationUtility.GetEditorCurve(_clip, bindings[2]);

        Vector3[] positionArray = new Vector3[_sampleNumber];
      
        for (int i = 0; i < _sampleNumber; i++)
        {
            time = _timeSample[i];
            x = xCurve.Evaluate(time);
            y = yCurve.Evaluate(time);
            z = zCurve.Evaluate(time);
            positionArray[i] = new Vector3(x, y, z);
        }

        if (shouldLog)
        {
            string[] content = Array.ConvertAll(positionArray, e => e.ToString("F8"));
            LogData(_clip.name + name + "_Position", content);
        }

        return positionArray;
    }

    private Quaternion[] CreateQuaternionSample(EditorCurveBinding[] bindings, bool shouldLog, string name)
    {
        float x = 0;
        float y = 0;
        float z = 0;
        float w = 0;
        float time = 0;
        var xCurve = AnimationUtility.GetEditorCurve(_clip, bindings[0]);
        var yCurve = AnimationUtility.GetEditorCurve(_clip, bindings[1]);
        var zCurve = AnimationUtility.GetEditorCurve(_clip, bindings[2]);
        var wCurve = AnimationUtility.GetEditorCurve(_clip, bindings[3]);

        Quaternion[] rotationArray = new Quaternion[_sampleNumber];

        for (int i = 0; i < _sampleNumber; i++)
        {
            time = _timeSample[i];
            x = xCurve.Evaluate(time);
            y = yCurve.Evaluate(time);
            z = zCurve.Evaluate(time);
            w = wCurve.Evaluate(time);
            rotationArray[i] = new Quaternion(x, y, z, w);
        }

        if (shouldLog)
        {
            string[] content = Array.ConvertAll(rotationArray, e => e.ToString("F8"));
            LogData(_clip.name + name + "_Rotation", content);
        }

        return rotationArray;
    }

    private void Sample(bool shouldLog)
    {
        var bindingList = AnimationUtility.GetCurveBindings(_clip);
        EditorCurveBinding[] posBinding = new EditorCurveBinding[3];
        EditorCurveBinding[] rotBinding = new EditorCurveBinding[4];

        for (int i = 0; i < bindingList.Length; i++)
        {
            if (bindingList[i].propertyName == "RightFootT.x")
            {
                Array.Copy(bindingList, i, posBinding, 0, 3);                
                _rightLegSample = CreateVectorSample(posBinding, shouldLog, "rightLeg");
            }
            else if (bindingList[i].propertyName == "LeftFootT.x")
            {
                Array.Copy(bindingList, i, posBinding, 0, 3);
                _leftLegSample = CreateVectorSample(posBinding, shouldLog, "leftLeg");
            } 
            else if (bindingList[i].propertyName == "RootQ.x")
            {
                Array.Copy(bindingList, i, rotBinding, 0, 4);                                
                _rootRotationSample = CreateQuaternionSample(rotBinding, shouldLog, "root");
            } 
            else if (bindingList[i].propertyName == "RootT.x")
            {
                Array.Copy(bindingList, i, posBinding, 0, 3);
                _rootPositionSample = CreateVectorSample(posBinding, shouldLog, "root");
            }
        }

        ConvertFromLocalToWorld();
        _groundLevel = (_rightLegSample[0].y <= _leftLegSample[0].y) ? _rightLegSample[0].y : _leftLegSample[0].y;
        _groundLevel *= (1 + thresholdGround);                
    }

    private void ConvertFromLocalToWorld()
    {
        GameObject empty = new GameObject();
        for (int i = 0; i < _sampleNumber; i++)
        {
            _rootPositionSample[i].x = 0;
            _rootPositionSample[i].z = 0;

            empty.transform.position = _rootPositionSample[i];
            empty.transform.rotation = _rootRotationSample[i];

            _rightLegSample[i] = empty.transform.TransformPoint(_rightLegSample[i]);            
            _leftLegSample[i] = empty.transform.TransformPoint(_leftLegSample[i]);
        }

        string[] content = Array.ConvertAll(_rightLegSample, e => e.ToString("F8").Replace("(", "").Replace(")", ""));        
        LogData(_clip.name + "convertedRIGHT", content);
    }

    private void SampleAnimation(bool shouldLog)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(_clip))
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(_clip, binding);
            if (binding.propertyName == "RightFootT.x")
            {
                rightSampleX = SampleAndLog(_clip, curve, "_rightFootX", shouldLog);
            }
            if (binding.propertyName == "RightFootT.z")
            {
                rightSampleZ = SampleAndLog(_clip, curve, "_rightFootZ", shouldLog);
            }
            if (binding.propertyName == "RightFootT.y")
            {
                rightSampleY = SampleAndLog(_clip, curve, "_rightFootY", shouldLog);
            }
            if (binding.propertyName == "LeftFootT.x")
            {
                leftSampleX = SampleAndLog(_clip, curve, "_leftFootX", shouldLog);
            }
            if (binding.propertyName == "LeftFootT.z")
            {
                leftSampleZ = SampleAndLog(_clip, curve, "_leftFootZ", shouldLog);
            }
            if (binding.propertyName == "LeftFootT.y")
            {
                leftSampleY = SampleAndLog(_clip, curve, "_leftFootY", shouldLog);
            }
            //ROOT STUFF
            if (binding.propertyName == "RootT.x")
            {
                rootSampleX = SampleAndLog(_clip, curve, "_rootX", shouldLog);
            }
            if (binding.propertyName == "RootT.z")
            {
                rootSampleY = SampleAndLog(_clip, curve, "_rootY", shouldLog);
            }
        }

        _groundLevel = (rightSampleY.val[0] <= leftSampleY.val[0]) ? rightSampleY.val[0] : leftSampleY.val[0];
        _groundLevel *= (1 + thresholdGround);
    }

    private (float[] val, float[] times) SampleAndLog(AnimationClip clip, AnimationCurve curve, string name, bool shouldLog)
    {
        float duration = curve.keys[curve.length - 1].time;
        float step = duration / _sampleNumber;
        float start = 0f;
        float[] valueArray = new float[_sampleNumber];
        float[] timeArray = new float[_sampleNumber];

        for (int i = 0; i < _sampleNumber; i++)
        {
            float val = curve.Evaluate(start);
            valueArray[i] = val;
            timeArray[i] = start;
            start += step;
        }

        if (shouldLog)
            LogData(clip.name + name, Array.ConvertAll(valueArray, e => e.ToString()));

        return (valueArray, timeArray);
    }

    private (float[] val, float[] times) Sample(AnimationClip clip, AnimationCurve curve, string name, bool shouldLog)
    {
        float duration = curve.keys[curve.length - 1].time;
        float step = duration / _sampleNumber;
        float start = 0f;


        float[] valueArray = new float[_sampleNumber];
        float[] timeArray = new float[_sampleNumber];

        for (int i = 0; i < _sampleNumber; i++)
        {
            float val = curve.Evaluate(start);
            valueArray[i] = val;
            timeArray[i] = start;
            start += step;
        }

        if (shouldLog)
            LogData(clip.name + name, Array.ConvertAll(valueArray, e => e.ToString()));

        return (valueArray, timeArray);
    }

    private void LogData(string fileName, string[] content)
    {
        string path = _logPath + fileName + ".txt";
        System.IO.File.WriteAllLines(path, content);
    }

    public void AnalyzeAnimation(bool shoudLog)
    {
        (int lLiftIndex, float lLiftTime, int lStrikeIndex, float lStrikeTime) lkeyTimes;
        (int rLiftIndex, float rLiftTime, int rStrikeIndex, float rStrikeTime) rkeyTimes;

        SampleAnimation(shoudLog);
        Sample(shoudLog);

        if (_clip.averageSpeed.x > _clip.averageSpeed.z)
        {
            rkeyTimes = ReturnKeyTimes(rightSampleX, rightSampleY, _clip);
            lkeyTimes = ReturnKeyTimes(leftSampleX, leftSampleY, _clip);
        }
        else
        {
            rkeyTimes = ReturnKeyTimes(rightSampleZ, rightSampleY, _clip);
            lkeyTimes = ReturnKeyTimes(leftSampleZ, leftSampleY, _clip);

        }

        float rDeltaX = rightSampleX.val[rkeyTimes.rLiftIndex] - rightSampleX.val[rkeyTimes.rStrikeIndex];
        float rDeltaZ = rightSampleZ.val[rkeyTimes.rLiftIndex] - rightSampleZ.val[rkeyTimes.rStrikeIndex];

        float lDeltaX = leftSampleX.val[lkeyTimes.lLiftIndex] - leftSampleX.val[lkeyTimes.lStrikeIndex];
        float lDeltaZ = leftSampleZ.val[lkeyTimes.lLiftIndex] - leftSampleZ.val[lkeyTimes.lStrikeIndex];

        double stepLength = Math.Sqrt(Math.Pow(rDeltaX, 2) + Math.Pow(rDeltaZ, 2));
        double LstepLegth = Math.Sqrt(Math.Pow(lDeltaX, 2) + Math.Pow(lDeltaZ, 2));
        float averageStepLength = (float)(stepLength + LstepLegth) / 2;

        //TODO: flight time calculation tests
        //TODO: Sending the right flightTime instead of the average
        //TODO: Calculate the flightTime in a better way
        float rightFlightTime = Math.Abs(rkeyTimes.rLiftTime - rkeyTimes.rStrikeTime);
        float leftFlightTime = Math.Abs(lkeyTimes.lLiftTime - lkeyTimes.lStrikeTime);
        float averageFlightTime = (rightFlightTime + leftFlightTime) / 2;
        //AnimationUtility.SetAnimationEvents(_clip, CreateEvents(lkeyTimes, rkeyTimes, averageStepLength));
        Vector2 rightFootStrikePosition = new Vector2(rightSampleX.val[rkeyTimes.rStrikeIndex], rightSampleZ.val[rkeyTimes.rStrikeIndex]);
        Vector2 rootPositionAtRightStrikeTime = new Vector2(rootSampleX.val[rkeyTimes.rStrikeIndex], rootSampleY.val[rkeyTimes.rStrikeIndex]);

        Vector2 displacement = rightFootStrikePosition - rootPositionAtRightStrikeTime;
        string dis = rightFootStrikePosition.ToString();

        AnimationUtility.SetAnimationEvents(_clip, CreateEvents(lkeyTimes, rkeyTimes, rightFlightTime, dis));
    }

    private AnimationEvent[] CreateEvents((int lLiftIndex, float lLiftTime, int lStrikeIndex, float lStrikeTime) lkeyTimes,
                                          (int rLiftIndex, float rLiftTime, int rStrikeIndex, float rStrikeTime) rkeyTimes,
                                           float stepLength, string dis)
    {
        AnimationEvent[] evt = new AnimationEvent[4];
        evt[0] = new AnimationEvent
        {
            time = lkeyTimes.lLiftTime,
            stringParameter = dis,
            functionName = "LeftFootLift",
            floatParameter = stepLength
        };
        evt[1] = new AnimationEvent
        {
            time = lkeyTimes.lStrikeTime,
            stringParameter = "L_strike",
            functionName = "LeftFootStrike",
            floatParameter = stepLength
        };
        evt[2] = new AnimationEvent
        {
            time = rkeyTimes.rLiftTime,
            stringParameter = "R_lift",
            functionName = "RightFootLift",
            floatParameter = stepLength
        };
        evt[3] = new AnimationEvent
        {
            time = rkeyTimes.rStrikeTime,
            stringParameter = "R_strike",
            functionName = "RightFootStrike",
            floatParameter = stepLength
        };

        return evt;
    }

    private (int, float, int, float) ReturnKeyTimes((float[] val, float[] times) axisOfMovement, (float[] val, float[] times) yAxis, AnimationClip clip)
    {
        (int[] peaks, int[] valleys) = ReturnPeaksAndValleys(axisOfMovement);
        int liftIndex = 0;
        float liftTime;
        int strikeIndex = 0;
        float strikeTime;

        //TODO: Add proper error conditions for adding a clip to the error list;
        if (peaks.Length <= 0 || valleys.Length <= 0 || peaks.Length > 2 || valleys.Length > 2)
        {
            if (_errorList != null)
            {
                _errorList.Add(_clip);
            }
            return (0, 0f, 0, 0f);
        }

        float minVal = float.MaxValue;
        foreach (int peak in peaks)
        {
            int finalLiftIndex = (peak + thresholdCheck) % _sampleNumber;

            for (int i = peak; i < finalLiftIndex; i++)
            {
                float yVal = yAxis.val[i];

                if (i == finalLiftIndex - 1 && yVal <= minVal)
                {
                    strikeIndex = i;
                    minVal = yVal;
                    break;
                }

                if (yVal <= _groundLevel && yVal <= minVal)
                {
                    strikeIndex = i;
                    minVal = yVal;
                    break;
                }
            }
        }

        minVal = float.MaxValue;
        foreach (int valley in valleys)
        {
            //TODO: clean this up
            decimal a = Math.Floor((decimal)(valley - thresholdCheck) / _sampleNumber);
            int finalLiftIndex = (int)(valley - thresholdCheck) - (_sampleNumber * (int)a);

            for (int i = valley; i > finalLiftIndex; i--)
            {
                float yVal = yAxis.val[i];

                if (i == finalLiftIndex + 1 && yVal <= minVal)
                {
                    liftIndex = i;
                    minVal = yVal;
                    break;
                }

                if (yVal <= _groundLevel && yVal <= minVal)
                {
                    liftIndex = i;
                    minVal = yVal;
                    break;
                }
            }
        }

        liftTime = axisOfMovement.times[liftIndex];
        strikeTime = axisOfMovement.times[strikeIndex];

        return (liftIndex, liftTime, strikeIndex, strikeTime);
    }

    private (int[] peaks, int[] valleys) ReturnPeaksAndValleys((float[] val, float[] times) axisOfMovement)
    {
        List<int> peaks = new List<int>();
        List<int> valleys = new List<int>();

        for (int i = 1; i < _sampleNumber - 1; i++)
        {
            float a = axisOfMovement.val[i - 1];
            float b = axisOfMovement.val[i];
            float c = axisOfMovement.val[i + 1];
            if (isPeak(a, b, c)) { peaks.Add(i); }
            if (isValley(a, b, c)) { valleys.Add(i); }
        }

        return (peaks.ToArray(), valleys.ToArray());
    }

    private bool isPeak(float a, float b, float c)
    {
        return (a < b) && (c < b);
    }

    private bool isValley(float a, float b, float c)
    {
        return (a > b) && (c > b);
    }    

    private (int[] peaks, int[] valleys) GetPeaksAndValleys(Vector3[] legSample)                                       
    {
        List<int> peaks = new List<int>();
        List<int> valleys = new List<int>();
        float[] axisOfMovement = new float[_sampleNumber];
        for (int i = 0; i < legSample.Length; i++)
        {
            axisOfMovement[i] = ()legSample[i].z;
        }

        for (int i = 1; i < _sampleNumber - 1; i++)
        {
            float a = legSample[i - 1].z;
            float b = legSample[i].z;
            float c = legSample[i + 1].z;
            if (isPeak(a, b, c)) { peaks.Add(i); }
            if (isValley(a, b, c)) { valleys.Add(i); }
        }

        return (peaks.ToArray(), valleys.ToArray());
    }
}



