using UnityEngine;

public class PatrolPoints : MonoBehaviour
{
    [Header(" - Waypoint Setiings - ")]
    [SerializeField]
    private bool bLoop;

    [SerializeField]
    private bool bReverse;

    [SerializeField]
    private int toIndex;


    [Header(" - Draw Setiings - ")]
    [SerializeField]
    private float drawHeight = 0.1f;

    [SerializeField]
    private Color drawSphereColor = Color.green;

    [SerializeField]
    private Color drawLineColor = Color.magenta;


    public Vector3 GetMoveToPosition()
    {
        Debug.Assert(toIndex >= 0 && toIndex < transform.childCount, $"{toIndex}");

        return transform.GetChild(toIndex).position;
    }


    public  void  UpdateNextIndex()
    {
        int count = transform.childCount;

        if(bReverse)
        {
            if(toIndex > 0)
            {
                toIndex--;

                return; 
            }

            if(bLoop)
            {
                toIndex = count - 1;

                return; 
            }

            bReverse = false;
            toIndex = 1;

            return;
        }


        // if 와 return으로 처리하는게 간편하다
        if (toIndex < count - 1)
        {
            toIndex++;
            
            return;
        }

        if(bLoop)
        {
            toIndex = 0;

            return; 
        }

        bReverse = true;
        toIndex = count - 2;

    }

    private void OnDrawGizmos()
    {
        int count = transform.childCount;

        for (int i = 0; i < count; i++)
        {
            DrawSphere(i);

            if (i < count - 1)
                DrawLine(i, i + 1);
        }

        if (bLoop)
            DrawLine(count - 1, 0);

    }

    private void DrawSphere(int index)
    {
        Vector3 position = transform.GetChild(index).position + new Vector3(0, drawHeight, 0);

        Gizmos.color = drawSphereColor;
        Gizmos.DrawSphere(position, 0.15f);
    }

    private void DrawLine(int startIndex, int endIndex)
    {
        Transform start = transform.GetChild(startIndex);
        Transform end = transform.GetChild(endIndex);

        Vector3 startPosition = start.position + new Vector3(0, drawHeight, 0);
        Vector3 endPosition = end.position + new Vector3(0, drawHeight, 0);

        Gizmos.color = drawLineColor;
        Gizmos.DrawLine(startPosition, endPosition);
    }

}

