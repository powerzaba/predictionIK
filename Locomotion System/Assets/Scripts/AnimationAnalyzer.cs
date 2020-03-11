using System.Collections.Generic;

using UnityEngine;

public class AnimationAnalyzer
{
    private AnimationClip _clip;
    private List<AnimationClip> _errorList;
    private int _sampleNumber;
    private float _groundLevel;
    private float[] _timeSample;
    private Sampler _sampler;
    private EventManager _eventManager;

    //TODO set threshold values for ground level from the editor
    private float _thresholdGround = 0.01f;
    private int _thresholdCheck = 5;


    public AnimationAnalyzer(AnimationClip clip, int sampleNumber, ref List<AnimationClip> errorList, 
                             float groundTh, float timeTh)
    {
        _clip = clip;
        _sampleNumber = sampleNumber;
        _errorList = errorList;
        _sampler = new Sampler(_clip, _sampleNumber);
        _timeSample = _sampler._timeSample;
        _eventManager = new EventManager(ref _clip, _timeSample);
        _thresholdCheck = (int) Mathf.Floor(_sampleNumber * timeTh);
        _thresholdGround = groundTh;
        _groundLevel = 0 + _thresholdGround;
    }

    public void AnalyzeAnimation(bool shouldLog)
    {
        _sampler.Sample(shouldLog);
 

        if (shouldLog)
            _sampler.LogData();

        var leftKeyTimes = GetKeyTimes(_sampler._leftFootPos);
        var rightKeyTimes = GetKeyTimes(_sampler._rightFootPos);

        _eventManager.InsertFeetCurve(leftKeyTimes, rightKeyTimes);

        float leftFlightTime = GetFlightTime(leftKeyTimes);
        float rightFlightTime = GetFlightTime(rightKeyTimes);
        float averageFlightTime = (leftFlightTime + rightFlightTime) / 2;

        Vector3 rightDis = GetDisplacementVector(rightKeyTimes.strikeIndexes, _sampler._rightFootPos);
        Vector3 leftDis = GetDisplacementVector(leftKeyTimes.strikeIndexes, _sampler._leftFootPos);

        string stringRight = rightDis.ToString("F8")
                                     .Replace("(", "")
                                     .Replace(")", "");

        string stringLeft = leftDis.ToString("F8")
                                   .Replace("(", "")
                                   .Replace(")", "");

        string disVec = stringRight + "#" + stringLeft;
        _eventManager.InsertAnimationEvents(disVec, averageFlightTime);
    }   

    public void RemoveEvents()
    {
        _eventManager.RemoveCurves();
        _eventManager.RemoveEvents();
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
            for (int i = 0; i <= _thresholdCheck; i++)
            {
                if (i == _thresholdCheck)
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
            for (int i = 0; i <= _thresholdCheck; i++)
            {
                if (i == _thresholdCheck)
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

    private Vector3 GetDisplacementVector(int[] strikeIndexes, LegPositionInformation leg)
    {
        var index = strikeIndexes[0];
        Vector3 displacementVector = new Vector3(leg.x[index], 0, leg.z[index]);

        return displacementVector;
    }

    private float GetFlightTime((int[] strikeIndexes, int[] liftIndexes) foot)
    {
        float flightTime = 0;
        float strikeTime = _timeSample[foot.strikeIndexes[0]];          
        float liftTime = _timeSample[foot.liftIndexes[0]];
        float endTime = _clip.length;

        if (strikeTime < liftTime)
        {
            if (foot.strikeIndexes.Length > 1)
            {
                strikeTime = _timeSample[foot.strikeIndexes[1]];
                flightTime = strikeTime - liftTime;
            }
            else
            {
                flightTime = endTime - liftTime + strikeTime;
            }
        }
        else
        {
            flightTime = strikeTime - liftTime;
        }
        return flightTime;
    }
}



