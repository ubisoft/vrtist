using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightRay : MonoBehaviour
{
    private LineRenderer line = null;
    private Transform endPoint = null;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        if (line == null)
        {
            Debug.LogWarning("Cannot find the LineRenderer component in the StraightRay");
        }

        endPoint = transform.Find("RayEnd");
        if (endPoint == null)
        {
            Debug.LogWarning("Cannot find the RayEnd object under the StraightRay");
        }
    }

    public void SetStartPosition(Vector3 start)
    {
        if (line != null)
        {
            line.SetPosition(0, start);
        }
    }

    public void SetEndPosition(Vector3 end)
    {
        if (line != null)
        {
            line.SetPosition(1, end);
        }

        if (endPoint != null)
        {
            endPoint.position = end;
        }
    }

    public void SetPositions(Vector3 begin, Vector3 end)
    {
        if (line != null)
        {
            line.SetPosition(0, begin);
            line.SetPosition(1, end);
        }

        if (endPoint != null)
        {
            endPoint.position = end;
        }
    }
}
