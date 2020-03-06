//C# Example (LookAtPoint.cs)
using UnityEngine;
public class LookAtPoint : MonoBehaviour
{
    public Vector3 lookAtPoint = Vector3.zero;
    public GameObject Char;
    private AnimationClip clip;

    private void Start()
    {
    }

    void Update()
    {
        transform.LookAt(lookAtPoint);
        
    }
}