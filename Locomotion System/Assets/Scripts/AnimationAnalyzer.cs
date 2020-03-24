
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
        var rightPos = _sampler._rightFootPos.position;
        var leftPos = _sampler._leftFootPos.position;

        var rightGroundTimes = FindGroundTimes(rightPos, velocityTh);
        var leftGroundTimes = FindGroundTimes(leftPos, velocityTh);

        _eventManager.InsertGroundedCurve(rightGroundTimes, leftGroundTimes);

        rightPos = _sampler._rightFootPos.inPlacePosition;
        leftPos = _sampler._leftFootPos.inPlacePosition;

        var rightData = GenerateFlightTimes(rightGroundTimes, rightPos);
        var leftData = GenerateFlightTimes(leftGroundTimes, leftPos);

        _eventManager.InsertFlightTimeCurve(rightData.f, leftData.f);
        _eventManager.InsertDisplacementX(rightData.x, leftData.x);
        _eventManager.InsertDisplacementZ(rightData.z, leftData.z);
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

    private (float[] f, float[] x, float[] z) GenerateFlightTimes(int[] groundedTimes, Vector3[] position)
    {
        float[] flightTimes = new float[_sampleNumber];
        float[] x = new float[_sampleNumber];
        float[] z = new float[_sampleNumber];
        var endTime = _clip.length;
        float flightTime;
        bool isIdle = true;

        for (int i = 1; i < groundedTimes.Length; i++)
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

                    int strikeIndex = (int)AnimationAnalyzer.Mod(j, _sampleNumber);
                    var strikeTime = _timeSample[strikeIndex];

                    flightTime = (liftTime < strikeTime) ? (strikeTime - liftTime) : 
                                                           (strikeTime + endTime - liftTime);

                    Vector3 displacement = position[strikeIndex];                    
                    FillArray(ref flightTimes, flightTime, liftIndex, strikeIndex);
                    FillArray(ref x, displacement.x, liftIndex, strikeIndex);
                    FillArray(ref z, displacement.z, liftIndex, strikeIndex);
                }
            }
        }

        //TODO: Fix this code so that it can handle Idle animations
        /*if (isIdle)
        {
            Vector3 displacement = position[0];            
            FillArray(ref x, displacement.x, 0, 0);
            FillArray(ref z, displacement.z, 0, 0);
        }*/

        return (flightTimes, x, z);
    }

    private void FillArray(ref float[] array, float value, int start, int end)
    {
        float length;
        length = (start < end) ? (end - start) : end + (_sampleNumber - 1) - start;

        for (int i = start; i <= (start + length); i++)
        {
            var index = (int)AnimationAnalyzer.Mod(i, _sampleNumber);
            array[index] = value;
        }
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
}


