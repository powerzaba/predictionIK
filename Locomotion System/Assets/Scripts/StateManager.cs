using System.Collections.Generic;

using UnityEngine;

public static class StateManager
{
    public static bool rightFootGround = true;
    public static bool leftFootGround = true;

    public static Vector3 rightFootPosition = Vector3.zero;
    public static Vector3 leftFootPosition = Vector3.zero;

    public static Vector3 currentDirection = Vector3.zero;
    public static Vector3 currentPosition = Vector3.zero;
    public static Vector3 previousPosition = currentPosition;

    public static Dictionary<string, FootData> feetData = new Dictionary<string, FootData>();
    public static float currentFlightTime = 0f;
    public static Vector3 currentRightDis = Vector3.zero;
    public static Vector3 currentLeftDis = Vector3.zero;
    public static Vector3 currentVelocity = Vector3.zero;

    public static void UpdateState(Animator animator, CharacterController ch)
    {
        UpdateFeetStatus(animator);
        UpdateDirection(animator);
        UpdateFeetData(animator);
    }

    public static void UpdateFeetData(Animator animator)
    {
        var flightTime = 0f;
        Vector3 right = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 velocity = Vector3.zero;

        foreach (var info in animator.GetCurrentAnimatorClipInfo(0))
        {
            flightTime += feetData[info.clip.name].flightTime * info.weight;
            right += feetData[info.clip.name].rightDisplacement * info.weight;
            left += feetData[info.clip.name].leftDisplacement * info.weight;
            velocity += info.clip.averageSpeed * info.weight;
        }

        currentFlightTime = flightTime;
        currentRightDis = right;
        currentLeftDis = left;
        currentVelocity = velocity;
    }

    public static void UpdateFeetStatus(Animator animator)
    {
        var rightValue = animator.GetFloat("RightFootCurve");
        var leftValue = animator.GetFloat("LeftFootCurve");

        rightFootPosition = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        leftFootPosition = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

        rightFootGround = (rightValue == 1) ? true : false;
        leftFootGround = (leftValue == 1) ? true : false;
    }

    public static void UpdateDirection(Animator animator)
    {
        currentPosition = animator.bodyPosition;
        //TODO CHECK THIS
        currentPosition.y = 0f;
        Vector3 diff = (currentPosition - previousPosition).normalized;

        currentDirection = diff;
        previousPosition = currentPosition;
    }

    public static void GetDataFromAnimator(Animator animator)
    {
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            AnimationEvent[] evt = clip.events;
            FootData data;
            if (evt.Length != 0)
            {
                var flightTime = evt[0].floatParameter;
                var disVectors = evt[0].stringParameter;
                (Vector3 r, Vector3 l) = GetDisplacementVector(disVectors);
                data = new FootData(r, l, flightTime);
                feetData.Add(clip.name, data);
            }
            else
            {
                //TODO FIX FOR IDLE POSE
                data = new FootData(Vector3.zero, Vector3.zero, 0);
                feetData.Add(clip.name, data);
            }
        }
    }

    private static (Vector3 r, Vector3 l) GetDisplacementVector(string vectors)
    {
        Vector3 r = Vector3.zero;
        Vector3 l = Vector3.zero;

        string[] r_l = vectors.Split('#');
        string[] rV = r_l[0].Split(',');
        string[] lV = r_l[1].Split(',');

        r = new Vector3(float.Parse(rV[0]), float.Parse(rV[1]), float.Parse(rV[2]));
        l = new Vector3(float.Parse(lV[0]), float.Parse(lV[1]), float.Parse(lV[2]));

        return (r, l);
    }
}

public struct FootData
{
    public Vector3 rightDisplacement;
    public Vector3 leftDisplacement;
    public float flightTime;

    public FootData(Vector3 r, Vector3 l, float ft)
    {
        rightDisplacement = r;
        leftDisplacement = l;
        flightTime = ft;
    }
}