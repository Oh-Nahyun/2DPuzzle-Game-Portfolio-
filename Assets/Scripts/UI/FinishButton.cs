using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinishButton : MonoBehaviour
{
    /// <summary>
    /// [FINISH] 버튼
    /// </summary>
    Button finishButton;

    /// <summary>
    /// 카드와 일치하지 않음을 알리는 안내 패널 오브젝트
    /// </summary>
    public GameObject infoPanel;

    /// <summary>
    /// 카드 매니저
    /// </summary>
    CardManager cardManager;

    /// <summary>
    /// 플레이어 텍스트
    /// </summary>
    public PlayerText playerText;

    private void Awake()
    {
        finishButton = GetComponent<Button>();
        cardManager = FindAnyObjectByType<CardManager>();

        // [FINISH] 버튼이 눌려지면 AddListener로 등록한 함수 실행
        finishButton.onClick.AddListener(() =>
        {
            CheckCard();
        });
    }

    void CheckCard()
    {
        if (!cardManager.IsSameCard())
        {
            StopAllCoroutines();
            StartCoroutine(ShowInfoPanel());
            Debug.Log("<< 카드 불일치 >>");
        }
        else
        {
            Debug.Log("<< 카드 일치 >>");

            if (!playerText.IsSameText())
            {
                Debug.Log("<< 텍스트 불일치 >>");
            }
            else
            {
                Debug.Log("<< 텍스트 일치 >>");
                Debug.Log("<< FINISH >>");
                ///////////////////////////////////////////////////////////////////////////////
            }
        }
    }

    /// <summary>
    /// 안내 패널을 일정 시간동안 보여주는 코루틴
    /// </summary>
    /// <returns></returns>
    IEnumerator ShowInfoPanel()
    {
        infoPanel.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        infoPanel.SetActive(false);
    }
}
