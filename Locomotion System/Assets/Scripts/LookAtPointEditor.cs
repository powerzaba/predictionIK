

//C# Example (LookAtPointEditor.cs)
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(LookAtPoint))]
[CanEditMultipleObjects]
public class LookAtPointEditor : Editor
{
    SerializedProperty lookAtPoint;
    LookAtPoint script;
    GameObject Char;
    AnimationClip[] myList;
    public float ok;

    private AnimationClip clip;

    void OnEnable()
    {
        script = (LookAtPoint)target;
       
        lookAtPoint = serializedObject.FindProperty("lookAtPoint");
    }

    public void OnGUI()
    {
        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(lookAtPoint);
        serializedObject.ApplyModifiedProperties();
        if (lookAtPoint.vector3Value.y > (target as LookAtPoint).transform.position.y)
        {
            EditorGUILayout.LabelField("(Above this object)");
        }
        if (lookAtPoint.vector3Value.y < (target as LookAtPoint).transform.position.y)
        {
            EditorGUILayout.LabelField("(Below this object)");
        }

        clip = EditorGUILayout.ObjectField("Clip", clip, typeof(AnimationClip), false) as AnimationClip;

   

        EditorGUILayout.LabelField("Curves:");
        if (clip != null)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                
                if (binding.propertyName == "LeftFootT.y")
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    foreach (var val in curve.keys) {
                        EditorGUILayout.LabelField("Current up-down LEFT value: " + val.value);
                    }
                }

                if (binding.propertyName == "RightFootT.y")
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    foreach (var val in curve.keys)
                    {
                        EditorGUILayout.LabelField("Current up-down RIGHT value: " + val.value);
                    }
                }

                if (binding.propertyName == "LeftFootT.y")
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    foreach (var val in curve.keys)
                    {
                        EditorGUILayout.LabelField("Time left value: " + val.time);
                    }
                }

                if (binding.propertyName == "RightFootT.y")
                {
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    foreach (var val in curve.keys)
                    {
                        EditorGUILayout.LabelField("Time Right value: " + val.time);
                    }
                }
                //AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                //EditorGUILayout.LabelField(binding.path + "/" + binding.propertyName + ", Keys: " + curve.keys.Length);

            }

            AnimationEvent[] evt;
            AnimationEvent[] tEvt;
            
            evt = new AnimationEvent[3];
            evt[0] = new AnimationEvent();
            evt[0].intParameter = 45;
            evt[0].time = 0.05f;
            evt[0].functionName = "Test event";

            evt[1] = new AnimationEvent();
            evt[1].intParameter = 12345;
            evt[1].time = 0.2f;
            evt[1].functionName = "Test event";

            evt[2] = new AnimationEvent();
            evt[2].intParameter = 12345;
            evt[2].time = 0.1f;
            evt[2].functionName = "Test OKOKevent";
            AnimationUtility.SetAnimationEvents(clip, evt);

            tEvt = AnimationUtility.GetAnimationEvents(clip);
            EditorGUILayout.LabelField("Events: " + tEvt[0].intParameter);
        }
    }
}
