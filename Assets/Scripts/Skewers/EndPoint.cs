using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndPoint : MonoBehaviour
{
    /// <summary>
    /// 꼬치 막대
    /// </summary>
    WoodenSkewer woodenSkewer;

    private void Awake()
    {
        woodenSkewer = FindAnyObjectByType<WoodenSkewer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 꼬치 막대의 중간 부분에 도달했는지 확인
        if (collision.CompareTag("Clickable") && woodenSkewer.IsPassingThrough)
        {
            woodenSkewer.isFinished = true;
            // Debug.Log("[Skewer's EndPoint Arrived] 꼬치 막대 통과 완료");
        }
        else if (collision.CompareTag("Hole1") && woodenSkewer.IsPassingThrough)
        {
            if (woodenSkewer.isHole1 && !woodenSkewer.isHole2 && !woodenSkewer.isHole3)
            {
                woodenSkewer.deployFirstHole1 = true;
                woodenSkewer.isFinished = true;
                // Debug.Log("Hole1 통과 완료");
            }
        }
        else if (collision.CompareTag("Hole2") && woodenSkewer.IsPassingThrough)
        {
            if (!woodenSkewer.isHole1 && woodenSkewer.isHole2 && !woodenSkewer.isHole3)
            {
                woodenSkewer.isFinished = true;
                // Debug.Log("Hole2 통과 완료");
            }
        }
        else if (collision.CompareTag("Hole3") && woodenSkewer.IsPassingThrough)
        {
            if (!woodenSkewer.isHole1 && !woodenSkewer.isHole2 && woodenSkewer.isHole3)
            {
                woodenSkewer.deployFirstHole3 = true;
                woodenSkewer.isFinished = true;
                // Debug.Log("Hole3 통과 완료");
            }
        }
    }
}
