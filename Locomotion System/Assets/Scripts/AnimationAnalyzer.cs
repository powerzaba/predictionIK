
using System.Collections.Generic;
using UnityEngine;

public class AnimationAnalyzer
{
    private AnimationClip _clip;
    private readonly int _sampleNumber;
    private readonly float[] _timeSample;
    private readonly Sampler _sampler;
    private readonly EventManager _eventManager;

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
        var rightPos = _sampler._rightFootPos.position;
        var leftPos = _sampler._leftFootPos.position;

        var rightGroundTimes = FindGroundTimes(rightPos, velocityTh);
        var leftGroundTimes = FindGroundTimes(leftPos, velocityTh);

        _eventManager.InsertGroundedCurve(rightGroundTimes, leftGroundTimes);

        var rightPosInPlace = _sampler._rightFootPos.inPlacePosition;
        var leftPosInPlace = _sampler._leftFootPos.inPlacePosition;

        if (_clip.name == "PIK_Walk")
        {
            Debug.Log("Right Foot now");
        }
        
        var rightData = GenerateFlightTimes(rightGroundTimes, rightPosInPlace, rightPos);

        if (_clip.name == "PIK_Walk")
        {
            Debug.Log("Left Foot now");
        }
        
        var leftData = GenerateFlightTimes(leftGroundTimes, leftPosInPlace, leftPos);

        _eventManager.InsertFlightTimeCurve(rightData.f, leftData.f);
        _eventManager.InsertDisplacementX(rightData.x, leftData.x);
        _eventManager.InsertDisplacementZ(rightData.z, leftData.z);
        _eventManager.InsertStepLength(rightData.s, leftData.s);
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

        var correctedTimes = SmoothGroundTimes(groundedTimes);
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

    private (float[] f, float[] x, float[] z, float[] s) GenerateFlightTimes(int[] groundedTimes, Vector3[] inPlacePos, Vector3[] position)
    {
        var flightTimes = new float[_sampleNumber];
        float[] x = new float[_sampleNumber];
        float[] z = new float[_sampleNumber];
        
        //TODO: refactor this method
        //stride test
        float[] s = new float[_sampleNumber];
        
        //test
        List<int> strikeList = new List<int>();
        List<float> flightList = new List<float>();
        List<float> xList = new List<float>();
        List<float> zList = new List<float>();
        List<float> strideList = new List<float>();

        var endTime = _clip.length;
        float flightTime;
        bool isIdle = true;

        for (var i = 1; i < groundedTimes.Length; i++)
        {
            if (groundedTimes[i] != groundedTimes[i - 1])
            {
                isIdle = false;
                if (groundedTimes[i] == 0)
                {
                    var liftTime = _timeSample[i];
                    int liftIndex = i;
                    var j = i;

                    while (groundedTimes[(int)AnimationAnalyzer.Mod(j, _sampleNumber)] == 0)
                    {
                        j++;
                    }

                    int strikeIndex = (int)AnimationAnalyzer.Mod(j + 1, _sampleNumber);
                    var strikeTime = _timeSample[strikeIndex];

                    flightTime = (liftTime < strikeTime) ? (strikeTime - liftTime) :
                                                           (strikeTime + endTime - liftTime);

                    Vector3 startPos = position[liftIndex];
                    Vector3 endPos = position[strikeIndex];
                    Vector3 displacement = inPlacePos[strikeIndex];

                    //stridelength test
                    float strideLength;
                    if (strikeTime < liftTime)
                    {
                        Vector3 finalPos = position[_sampleNumber - 1];
                        Vector3 initialPos = position[0];
                        float d1 = Vector3.Distance(finalPos, startPos);
                        float d2 = Vector3.Distance(initialPos, endPos);
                        strideLength = d1 + d2;
                    }
                    else
                    {
                        strideLength = Vector3.Distance(endPos, startPos);
                    }
                    
                    strikeList.Add(strikeIndex);
                    flightList.Add(flightTime);
                    xList.Add(displacement.x);
                    zList.Add(displacement.z);
                    strideList.Add(strideLength);

                    if (_clip.name == "PIK_Walk")
                    {
                        Debug.Log(displacement.z);
                    }
                    
                }
            }
        }

        for (var i = 0; i < strikeList.Count; i++)
        {
            var currentFlight = flightList[i];
            var prevStrike = strikeList[(int)AnimationAnalyzer.Mod((i - 1), strikeList.Count)];
            var currentStrike = strikeList[i];
            var currentX = xList[i];
            var currentZ = zList[i];
            var strideLength = strideList[i];
            FillArray(ref x, currentX, prevStrike, currentStrike);
            FillArray(ref z, currentZ, prevStrike, currentStrike);
            FillArray(ref flightTimes, currentFlight, prevStrike, currentStrike);
            FillArray(ref s, strideLength, prevStrike, currentStrike);
        }
        
        if (isIdle)
        {
            Vector3 displacement = inPlacePos[0];
            FillArray(ref x, displacement.x, 0, _sampleNumber - 1);
            FillArray(ref z, displacement.z, 0, _sampleNumber - 1);
            FillArray(ref s, 0, 0, _sampleNumber - 1);
        }

        return (flightTimes, x, z, s);
    }

    private void FillArray(ref float[] array, float value, int startIndex, int endIndex)
    {
        float length = (startIndex < endIndex) ? (endIndex - startIndex) : endIndex + (_sampleNumber - 1) - startIndex;

        for (var i = startIndex; i <= (startIndex + length); i++)
        {
            var index = (int)AnimationAnalyzer.Mod(i, _sampleNumber);
            array[index] = value;
        }
    }

    public static float Mod(float a, float b)
    {
        var c = a % b;
        if ((c < 0 && b > 0) || (c > 0 && b < 0))
        {
            c += b;
        }
        return c;
    }
}


