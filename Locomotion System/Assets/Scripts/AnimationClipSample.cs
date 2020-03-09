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

    private Sampler _sampler;


    //TODO set threshold values for ground level from the editor
    private float thresholdGround = 0.1f;
    private int thresholdCheck = 5;


    public AnimationClipSample(AnimationClip clip, int sampleNumber, ref List<AnimationClip> errorList)
    {
        _clip = clip;
        _sampleNumber = sampleNumber;
        _errorList = errorList;
        _logPath = Directory.GetCurrentDirectory() + @"\Assets\AnimationLogData\";
        _timeSample = new float[_sampleNumber];
        _sampler = new Sampler(_clip, _sampleNumber);
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

    private void LogData(string fileName, string[] content)
    {
        string path = _logPath + fileName + ".txt";
        System.IO.File.WriteAllLines(path, content);
    }

    public void AnalyzeAnimation(bool shoudLog)
    {
        (int lLiftIndex, float lLiftTime, int lStrikeIndex, float lStrikeTime) lkeyTimes;
        (int rLiftIndex, float rLiftTime, int rStrikeIndex, float rStrikeTime) rkeyTimes;

        //TESTING UNDERNEATH
        SampleAnimation(shoudLog);
        _sampler.Sample(shoudLog);


        if (shoudLog)
        {
            _sampler.LogData();
        }

        var curreGL = _groundLevel;
        _groundLevel = 0 + 0.01f;        
        var left = GetKeyTimes(_sampler._leftFootPos);
        var right = GetKeyTimes(_sampler._rightFootPos);
        _groundLevel = curreGL;

        List<Keyframe> ks = new List<Keyframe>();
        AnimationCurve testCurve;
        for (int i = 0; i < left.strikeIndexes.Length; i++)
        {
            
            ks.Add(new Keyframe(_timeSample[left.strikeIndexes[i]], 1, 0, 20));            
        }
        for (int i = 0; i < left.liftIndexes.Length; i++)
        {
            ks.Add(new Keyframe(_timeSample[left.liftIndexes[i]], 1, -20, 0));
        }
        testCurve = new AnimationCurve(ks.ToArray());
        _clip.SetCurve("", typeof(Animator), "PORCODIO", testCurve);

        ////////TESTING ABOVE
        ///

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

    private float UpdateGroundLevel()
    {
        float groundLevel = 0;
        if (_sampler != null)
        {
            var leftFoot = _sampler._leftFootPos;
            var rightFoot = _sampler._rightFootPos;
            groundLevel = (leftFoot.y[0] < rightFoot.y[0]) ?
                            leftFoot.y[0] : rightFoot.y[0];
        }
        return groundLevel;
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

    private (int[] peaks, int[] valleys) GetPeaksAndValleys(LegPositionInformation leg)
    {
        List<int> peaks = new List<int>();
        List<int> valleys = new List<int>();
        float[] axisOfMovement;

        axisOfMovement = (_clip.averageSpeed.x > _clip.averageSpeed.z) ? leg.x : leg.z;

        for (int i = 1; i < _sampleNumber - 1; i++)
        {
            float a = axisOfMovement[i - 1];
            float b = axisOfMovement[i];
            float c = axisOfMovement[i + 1];
            if ((a < b) && (c < b)) { peaks.Add(i); }
            if ((a > b) && (c > b)) { valleys.Add(i); }
        }

        return (peaks.ToArray(), valleys.ToArray());
    }

    private (int[] strikeIndexes, int[] liftIndexes) GetKeyTimes(LegPositionInformation leg)       
    {
        List<int> correctPeaks = new List<int>();
        List<int> correctValleys = new List<int>();
        (int[] peaks, int[] valleys) pv;

        pv = GetPeaksAndValleys(leg);

        //TODO: Add proper error conditions for adding a clip to the error list;
        if (pv.peaks.Length <= 0 || pv.valleys.Length <= 0 ||
           (pv.peaks.Length != pv.valleys.Length)) 
        {
            if (_errorList != null)
            {
                _errorList.Add(_clip);
            }
            return (correctPeaks.ToArray(), correctValleys.ToArray());
        }

        foreach (int peak in pv.peaks)
        {
            var currentPeak = peak;
            for (int i = 0; i <= thresholdCheck; i++)
            {
                if (i == thresholdCheck)
                {
                    correctPeaks.Add(currentPeak);
                    break;
                }
                if (leg.y[currentPeak] <= _groundLevel)
                {
                    correctPeaks.Add(currentPeak);
                    break;
                }
                currentPeak++;
                if (currentPeak >= _sampleNumber) { currentPeak = 0; }
            }
        }

        foreach (int valley in pv.valleys)
        {
            var currentValley = valley;
            for (int i = 0; i <= thresholdCheck; i++)
            {
                if (i == thresholdCheck)
                {
                    correctValleys.Add(currentValley);
                    break;
                }
                if (leg.y[currentValley] <= _groundLevel)
                {
                    correctValleys.Add(currentValley);
                    break;
                }
                currentValley--;
                if (currentValley < 0) { currentValley = _sampleNumber - 1; }
            }
        }

        return (correctPeaks.ToArray(), correctValleys.ToArray());
    }
}



