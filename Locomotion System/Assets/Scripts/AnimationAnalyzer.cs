using System.Collections.Generic;

using UnityEngine;

public class AnimationAnalyzer
{
    private AnimationClip _clip;
    private int _sampleNumber;
    private float[] _timeSample;
    private Sampler _sampler;
    private EventManager _eventManager;

    public AnimationAnalyzer(AnimationClip clip, int sampleNumber)
    {
        _clip = clip;
        _sampleNumber = sampleNumber;
        _sampler = new Sampler(_clip, _sampleNumber);
        _timeSample = _sampler._timeSample;
        _eventManager = new EventManager(_clip, _timeSample);
    }

    public void AnalyzeAnimation(float velocityTh, int smoothingTh)
    {
        _sampler.Sample();

        var rightGroundTimes = FindGroundTimes(_sampler._rightFootPos.position, velocityTh);
        var leftGroundTimes = FindGroundTimes(_sampler._leftFootPos.position, velocityTh);

        _eventManager.InsertFeetCurve(rightGroundTimes, leftGroundTimes);

        /*var leftKeyTimes = GetKeyTimes(_sampler._leftFootPos, groundTh, velocityTh);
        Debug.Log("IT'S RIGHT");
        var rightKeyTimes = GetKeyTimes(_sampler._rightFootPos, groundTh, velocityTh);

        _eventManager.InsertFeetCurve(leftKeyTimes, rightKeyTimes);

        float leftFlightTime = GetFlightTime(leftKeyTimes);
        float rightFlightTime = GetFlightTime(rightKeyTimes);

        string rightDis = GetDisplacementVector(rightKeyTimes.strikeIndexes, _sampler._rightFootPos);
        string leftDis = GetDisplacementVector(leftKeyTimes.strikeIndexes, _sampler._leftFootPos);

        AnimationData data = new AnimationData
        {
            clipName = _clip.name,
            rightFlightTime = rightFlightTime.ToString(),
            leftFlightTime = leftFlightTime.ToString(),
            rightDisplacement = rightDis,
            leftDisplacement = leftDis,
        };

        return data;*/
    }

    private int[] FindGroundTimes(Vector3[] position, float velocityTh)
    {
        var groundedTimes = new int[_sampleNumber];
        var velocity = new float[_sampleNumber];
        var distances = new float[_sampleNumber];        
        var prevPosition = Vector3.zero;
        var currPosition = Vector3.zero;
        var deltaTime = _sampler._deltaTime;

        for (int i = 0; i < _sampleNumber; i++)
        {
            prevPosition = (i == 0) ? position[_sampleNumber - 1] : position[i - 1];          
            currPosition = position[i];

            distances[i] = Vector3.Distance(prevPosition, currPosition);
            velocity[i] = distances[i] / deltaTime;
            groundedTimes[i] = (velocity[i] <= velocityTh) ? 1 : 0;
        }

        var correctedTimes = new int[_sampleNumber];
        correctedTimes = SmoothGroundTimes(groundedTimes);

        return correctedTimes;
    }

    private int[] SmoothGroundTimes(int[] groundedTimes)
    {
        var correctedTimes = groundedTimes;        

        for (int i = 0; i < groundedTimes.Length; i++)
        {
            var a = groundedTimes[i];
            var b = groundedTimes[(int)AnimationAnalyzer.Mod((i - 1), _sampleNumber)];
            var c = groundedTimes[(int)AnimationAnalyzer.Mod((i + 1), _sampleNumber)];
            if (a != b && a != c)
            {
                correctedTimes[i] = c;
            }
        }

        return correctedTimes;
    }

    public static float Mod(float a, float b)
    {
        float c = a % b;
        if ((c < 0 && b > 0) || (c > 0 && b < 0))
        {
            c += b;
        }
        return c;
    }

    private (int[] peaks, int[] valleys) GetPeaksAndValleys(FootPositionInfo leg)
    {
        List<int> peaks = new List<int>();
        List<int> valleys = new List<int>();
        float[] axisOfMovement = (_clip.averageSpeed.x > _clip.averageSpeed.z) ? leg.x : leg.z;

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

    private (int[] strikeIndexes, int[] liftIndexes) GetKeyTimes(FootPositionInfo leg, float gTh, float tTh)
    {
        List<int> correctPeaks = new List<int>();
        List<int> correctValleys = new List<int>();
        (int[] peaks, int[] valleys) pv;
        pv = GetPeaksAndValleys(leg);

        float groundTh = 0 + gTh;
        float timeTh = (int)Mathf.Floor(_sampleNumber * tTh);

        if (pv.peaks.Length <= 0 || pv.valleys.Length <= 0 ||
           (pv.peaks.Length != pv.valleys.Length))
        {
            Debug.Log(_clip.name);
            Debug.Log(pv.peaks.Length);
            Debug.Log(pv.valleys.Length);

            //return (correctPeaks.ToArray(), correctValleys.ToArray());
        }

        foreach (int peak in pv.peaks)
        {
            var currentPeak = peak;
            for (int i = 0; i <= timeTh; i++)
            {
                if (i == timeTh)
                {
                    correctPeaks.Add(currentPeak);
                    break;
                }
                if (leg.y[currentPeak] <= groundTh)
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
            for (int i = 0; i <= timeTh; i++)
            {
                if (i == timeTh)
                {
                    correctValleys.Add(currentValley);
                    break;
                }
                if (leg.y[currentValley] <= groundTh)
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

    private string GetDisplacementVector(int[] strikeIndexes, FootPositionInfo leg)
    {
        var index = strikeIndexes[0];
        Vector3 displacementVector = new Vector3(leg.x[index], 0, leg.z[index]);

        return displacementVector.ToString("F8").Replace("(", "").Replace(")", "");
    }

    private float GetFlightTime((int[] strikeIndexes, int[] liftIndexes) foot)
    {
        float strikeTime = _timeSample[foot.strikeIndexes[0]];
        float liftTime = _timeSample[foot.liftIndexes[0]];
        float endTime = _clip.length;
        float flightTime;

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

public struct AnimationData
{
    public string clipName;
    public string rightFlightTime;
    public string leftFlightTime;
    public string rightDisplacement;
    public string leftDisplacement;
}


