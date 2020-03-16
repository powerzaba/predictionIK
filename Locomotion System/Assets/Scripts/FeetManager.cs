using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeetManager : MonoBehaviour
{
    private Animator _animator;
    private GameObject _model;
    private CharacterController _characterController;
    private LocomotionController _locomotionScript;
    private FeetController _feetController;
    private FeetPredictor _predictor;

    void Start()
    {
        _animator = GetComponent<Animator>();        
        _locomotionScript = GetComponent<LocomotionController>();
        _model = this.gameObject;
        _feetController = new FeetController(_animator);
        _predictor = new FeetPredictor(_animator);
        _characterController = GetComponent<CharacterController>();

        StateManager.GetDataFromAnimator(_animator);
    }

    void Update()
    {
          
    }

    private void OnAnimatorIK(int layerIndex)
    {
        StateManager.UpdateState(_animator, _characterController);
        _predictor.UpdateState();
        _predictor.PredictFeetPosition();
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        if (StateManager.rightFootGround)               
            //Gizmos.DrawSphere(StateManager.rightFootPosition, 0.1f);

        Gizmos.color = Color.blue;
        if (StateManager.leftFootGround)
            //Gizmos.DrawSphere(StateManager.leftFootPosition, 0.1f);

        Gizmos.color = Color.green;
        var current = StateManager.currentPosition;
        var direction = StateManager.currentDirection;
        if (current != Vector3.zero)
        {           
            Gizmos.DrawLine(current, (current + direction * 1.1f));
        }

        Gizmos.color = Color.cyan;
        //Gizmos.DrawSphere(StateManager.currentPosition, 0.1f);

        Gizmos.color = Color.magenta;
        //Gizmos.DrawSphere(_predictor.predictefRootPositionRight, 0.1f);

        Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(_predictor.predictedRootPositionLeft, 0.1f);
        Gizmos.DrawSphere(_predictor.rightShadowPosition, 0.1f);

        

        Gizmos.color = Color.green;
        Vector3 test = _predictor.predictedRightFootPosition;
        Gizmos.DrawSphere(test, 0.1f);


        Gizmos.color = Color.green;
        Vector3 ok = _predictor.predictedLeftFootPosition;
        Gizmos.DrawSphere(ok, 0.1f);

        Color lightRed = new Color(244, 141, 112);
        Color lightBlue = new Color(104, 172, 221);

        Gizmos.color = lightRed;
        Vector3 prevleft = _predictor.previousFootprintLeft;
        Gizmos.DrawSphere(prevleft, 0.1f);

        Gizmos.color = lightBlue;
        Vector3 prevRight = _predictor.previousFootprintRight;
        Gizmos.DrawSphere(prevRight, 0.1f);
    }

    void OnGUI()
    {        
        GUI.Label(new Rect(0, 60, 200, 20), "Current Flight Time :" + StateManager.currentFlightTime);
        GUI.Label(new Rect(0, 90, 200, 20), "Next Foot Time :" + _predictor.nextRightFootTime);
        GUI.Label(new Rect(0, 120, 200, 20), "Time :" + Time.time);
        GUI.Label(new Rect(0, 150, 200, 20), "Current Velocity :" + StateManager.currentVelocity);
        GUI.Label(new Rect(0, 180, 200, 20), "Current Duration :" + _predictor.rightFlightDuration);
    }
}
