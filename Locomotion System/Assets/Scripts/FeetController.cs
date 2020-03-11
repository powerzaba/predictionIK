using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeetController
{
    private Animator _animator;
    public Vector3 rightFootPosition { get; private set; }
    public Vector3 leftFootPosition { get; private set; }
    public bool rightFootGround = true;
    public bool leftFootGround = true;

    public FeetController(Animator animator)
    {
        _animator = animator;
    }

    public void UpdateFeetState()
    {
        var rightValue = _animator.GetFloat("RightFootCurve");
        var leftValue = _animator.GetFloat("LeftFootCurve");

        rightFootPosition = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        leftFootPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;

        rightFootGround = (rightValue == 1) ? true : false;
        leftFootGround = (leftValue == 1) ? true : false;       
    }
    

}
