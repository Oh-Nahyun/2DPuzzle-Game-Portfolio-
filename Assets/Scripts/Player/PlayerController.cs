using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// 드래그 중인지 확인하는 변수
    /// </summary>
    bool isDragging = false;

    /// <summary>
    /// 오브젝트의 원래 위치
    /// </summary>
    Vector2 originalPosition;

    /// <summary>
    /// 오브젝트의 원래 각도
    /// </summary>
    Quaternion originalRotation;

    /// <summary>
    /// 오브젝트의 실시간 월드 좌표
    /// </summary>
    Vector2 worldPosition;

    /// <summary>
    /// 선택된 오브젝트와 마우스 사이 거리
    /// </summary>
    public Vector2 offset;

    /// <summary>
    /// 드래그 중인 오브젝트
    /// </summary>
    GameObject draggedObject;

    /// <summary>
    /// 선택한 구멍의 부모의 부모 
    /// (치즈 또는 베이컨)
    /// </summary>
    GameObject cheeseAndBacon;

    /// <summary>
    /// 배치 완료된 오브젝트 배열
    /// </summary>
    public GameObject[] finishedObjectArray;

    /// <summary>
    /// 오브젝트의 원래 레이어
    /// </summary>
    LayerMask originalLayer;

    /// <summary>
    /// 배치 완료 표시용 레이어
    /// (마우스 왼쪽 버튼 입력 무시)
    /// </summary>
    LayerMask ignoreLeftClickLayer;

    /// <summary>
    /// Shift 키가 눌렸는지 안눌렸는지 확인용 변수
    /// </summary>
    bool isShiftPressed = false;

    /// <summary>
    /// 플레이어의 점수
    /// </summary>
    int playerScore = 0;

    /// <summary>
    /// 플레이어의 점수 확인 및 설정용 프로퍼티
    /// </summary>
    public int PlayerScore
    {
        get => playerScore; // 읽기는 public
        private set         // 쓰기는 private
        {
            if (playerScore != value)
            {
                playerScore = Math.Min(value, 999);         // 최대 점수 999
                onPlayerScoreChange?.Invoke(playerScore);   // 이 델리게이트에 함수를 등록한 모든 대상에게 변경된 점수를 알림
            }
        }
    }

    /// <summary>
    /// 플레이어의 점수가 변경되었음을 알리는 델리게이트
    /// (int : 변경된 점수)
    /// </summary>
    public Action<int> onPlayerScoreChange;

    /// <summary>
    /// 꼬치 막대
    /// </summary>
    WoodenSkewer woodenSkewer;

    /// <summary>
    /// 꼬치 재료
    /// </summary>
    Ingredients ingredients;

    /// <summary>
    /// 플레이어 인풋
    /// </summary>
    PlayerInputActions inputActions;

    /// <summary>
    /// 메인 카메라
    /// </summary>
    public Camera mainCamera;

    private void Awake()
    {
        inputActions = new PlayerInputActions();                // 플레이어 인풋 액션
        mainCamera = Camera.main;                               // 메인 카메라 찾기

        woodenSkewer = FindAnyObjectByType<WoodenSkewer>();     // 꼬치 막대 찾기
        ingredients = FindAnyObjectByType<Ingredients>();       // 꼬치 재료 찾기

        finishedObjectArray = new GameObject[10];               // 오브젝트 배열 초기화
    }

    private void Start()
    {
        ignoreLeftClickLayer = LayerMask.NameToLayer("IgnoreLeftClick");    // 무시할 레이어 설정
    }

    private void Update()
    {
        OnDrag();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.LeftClick.performed += OnSelect;
        inputActions.Player.LeftClick.canceled += OnRelease;
        inputActions.Player.RightClick.performed += OnCancel;

        inputActions.Player.HoleMode.performed += OnHoleModeOn;
        inputActions.Player.HoleMode.canceled += OnHoleModeOff;
    }

    private void OnDisable()
    {
        inputActions.Player.HoleMode.canceled -= OnHoleModeOff;
        inputActions.Player.HoleMode.performed -= OnHoleModeOn;

        inputActions.Player.RightClick.performed -= OnCancel;
        inputActions.Player.LeftClick.canceled -= OnRelease;
        inputActions.Player.LeftClick.performed -= OnSelect;

        inputActions.Player.Disable();
    }

    // 입력 관련 함수 -------------------------------------------------------------

    private void OnHoleModeOn(InputAction.CallbackContext context)
    {
        isShiftPressed = true;
    }

    private void OnHoleModeOff(InputAction.CallbackContext context)
    {
        isShiftPressed = false;
    }

    /// <summary>
    /// 선택 처리 함수
    /// </summary>
    private void OnSelect(InputAction.CallbackContext _)
    {
        // 마우스 클릭된 위치
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // 레이 이용
        Ray ray = mainCamera.ScreenPointToRay(mousePosition); // 레이 생성
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray); // 클릭된 오브젝트 감지
        //RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, ~ignoreLeftClickLayer); // 특정 레이어를 무시하는 레이캐스트

        // 충돌 확인
        if (hit.collider != null)
        {
            if (!isShiftPressed && hit.collider.gameObject.layer != ignoreLeftClickLayer) // [Shift 키가 안 눌리고, 입력 무시 레이어가 아닌 경우]
            {
                // 선택한 오브젝트 저장
                GameObject clickedObject = hit.collider.gameObject;

                // 클릭된 오브젝트가 "Clickable" 태그를 가지고 있는지 확인
                if (clickedObject.CompareTag("Clickable"))
                {
                    offset = Vector2.zero;                                      // offset 초기화
                    draggedObject = clickedObject;                              // 드래그 오브젝트 설정
                    originalPosition = OriginalPosition(draggedObject);         // 원래 위치 저장
                    isDragging = true;                                          // 드래그 상태 활성화
                }
                // 클릭된 오브젝트가 "Hole1" 또는 "Hole2" 또는 "Hole3" 태그를 가지고 있는지 확인
                else if (clickedObject.CompareTag("Hole1") || clickedObject.CompareTag("Hole2") || clickedObject.CompareTag("Hole3"))
                {
                    // 선택한 오브젝트의 부모 레이어가 입력 무시 레이어인 경우
                    Transform cheeseAndBaconPrefab = clickedObject.transform.parent;
                    cheeseAndBacon = cheeseAndBaconPrefab.parent.gameObject;
                    if (cheeseAndBacon.layer == ignoreLeftClickLayer)
                        return;                                                 // 뒤쪽 코드 실행 불가

                    // 선택한 오브젝트의 부모 레이어가 입력 무시 레이어가 아닌 경우
                    Transform holeTransform = clickedObject.transform;
                    CircleCollider2D holeCollider = holeTransform.GetComponent<CircleCollider2D>();
                    offset = -holeCollider.offset;                              // offset 설정
                    draggedObject = holeTransform.parent.parent.gameObject;     // 드래그 오브젝트 설정
                    originalPosition = OriginalPosition(draggedObject);         // 원래 위치 저장
                    originalRotation = OriginalRotation(draggedObject);         // 원래 각도 저장
                    isDragging = true;                                          // 드래그 상태 활성화
                }

                // 각 구멍에 대한 추가 행동 처리
                if (hit.collider.CompareTag("Hole1"))
                {
                    FirstHoleAction();
                }
                else if (hit.collider.CompareTag("Hole2"))
                {
                    SecondHoleAction();
                }
                else if (hit.collider.CompareTag("Hole3"))
                {
                    ThirdHoleAction();
                }
            }
            else // [Shift 키가 눌린 경우]
            {
                // 선택한 구멍 저장
                GameObject clickedHole = hit.collider.gameObject;

                // 선택한 구멍의 부모에 따라 프리팹 변경 처리
                if (clickedHole.layer == 6)
                {
                    #region [선택한 구멍의 부모가 [치즈]인 경우]
                    if (!woodenSkewer.canChooseCheeseHole1) // 첫번째 구멍을 배치 완료한 경우
                    {
                        if (hit.collider.CompareTag("Hole1"))
                        {
                            Debug.Log("[Hole1] 선택 불가능");
                        }
                        else if (hit.collider.CompareTag("Hole2"))
                        {
                            ingredients.cheese.CheeseState = Cheese.CheeseMode.NewTwoHoles;             // 스프라이트 변경
                            ingredients.cheeseParent.position -= new Vector3(4.25f, 0.0f, 0.0f);        // 스프라이트 위치 이동
                            ingredients.cheeseParent.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);   // 스프라이트 y축 기준 180도 회전
                            SaveCheeseHole(false, false, true);
                            DeployCheeseHole();
                        }
                        else if (hit.collider.CompareTag("Hole3"))
                        {
                            if (!woodenSkewer.canChooseCheeseHole2)
                            {
                                if (ingredients.cheese.CheeseState == Cheese.CheeseMode.TwoHoles)
                                {
                                    ingredients.cheese.CheeseState = Cheese.CheeseMode.ThreeHoles;                     
                                    SaveCheeseHole(false, false, false);
                                    DeployCheeseHole();
                                }
                                else if (ingredients.cheese.CheeseState == Cheese.CheeseMode.NewTwoHoles)
                                {
                                    ingredients.cheese.CheeseState = Cheese.CheeseMode.NewThreeHoles;                  
                                    SaveCheeseHole(false, false, false);
                                    DeployCheeseHole();
                                }
                            }
                            else
                            {
                                Debug.Log("<<다른 구멍을 클릭해주세요.>>");
                            }
                        }
                    }
                    else if (!woodenSkewer.canChooseCheeseHole2) // 두번째 구멍을 배치 완료한 경우
                    {
                        if (hit.collider.CompareTag("Hole1"))
                        {
                            ingredients.cheese.CheeseState = Cheese.CheeseMode.TwoHoles;
                            ingredients.cheeseParent.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                            SaveCheeseHole(false, false, true);
                            DeployCheeseHole();
                        }
                        else if (hit.collider.CompareTag("Hole2"))
                        {
                            Debug.Log("[Hole2] 선택 불가능");
                        }
                        else if (hit.collider.CompareTag("Hole3"))
                        {
                            ingredients.cheese.CheeseState = Cheese.CheeseMode.TwoHoles;
                            ingredients.cheeseParent.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                            SaveCheeseHole(false, false, true);                                         // 스프라이트가 회전했기 때문에 구멍3을 누르면 구멍1 변수가 false가 된다.
                            DeployCheeseHole();
                        }
                    }
                    else if (!woodenSkewer.canChooseCheeseHole3) // 세번째 구멍을 배치 완료한 경우
                    {
                        if (hit.collider.CompareTag("Hole1"))
                        {
                            Debug.Log("<<다른 구멍을 클릭해주세요.>>");
                        }
                        else if (hit.collider.CompareTag("Hole2"))
                        {
                            ingredients.cheese.CheeseState = Cheese.CheeseMode.NewTwoHoles;                      
                            ingredients.cheeseParent.position += new Vector3(4.25f, 0.0f, 0.0f);                 
                            ingredients.cheeseParent.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);              
                            SaveCheeseHole(false, false, true);
                            DeployCheeseHole();
                        }
                        else if (hit.collider.CompareTag("Hole3"))
                        {
                            Debug.Log("[Hole3] 선택 불가능");
                        }
                    }
                    #endregion
                }
                else if (clickedHole.layer == 7)
                {
                    #region [선택한 구멍의 부모가 [베이컨]인 경우]
                    if (!woodenSkewer.canChooseBaconHole1) // 첫번째 구멍을 배치 완료한 경우
                    {
                        if (hit.collider.CompareTag("Hole1"))
                        {
                            Debug.Log("[Hole1] 선택 불가능");
                        }
                        else if (hit.collider.CompareTag("Hole2"))
                        {
                            ingredients.bacon.BaconState = Bacon.BaconMode.NewTwoHoles;                 // 스프라이트 변경
                            ingredients.baconParent.position -= new Vector3(4.25f, 0.0f, 0.0f);         // 스프라이트 위치 이동
                            ingredients.baconParent.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);    // 스프라이트 y축 기준 180도 회전
                            SaveBaconHole(false, false, true);
                            DeployBaconHole();
                        }
                        else if (hit.collider.CompareTag("Hole3"))
                        {
                            if (!woodenSkewer.canChooseBaconHole2)
                            {
                                if (ingredients.bacon.BaconState == Bacon.BaconMode.TwoHoles)
                                {
                                    ingredients.bacon.BaconState = Bacon.BaconMode.ThreeHoles;
                                    SaveBaconHole(false, false, false);
                                    DeployBaconHole();
                                }
                                else if (ingredients.bacon.BaconState == Bacon.BaconMode.NewTwoHoles)
                                {
                                    ingredients.bacon.BaconState = Bacon.BaconMode.NewThreeHoles;
                                    SaveBaconHole(false, false, false);
                                    DeployBaconHole();
                                }
                            }
                            else
                            {
                                Debug.Log("<<다른 구멍을 클릭해주세요.>>");
                            }
                        }
                    }
                    else if (!woodenSkewer.canChooseBaconHole2) // 두번째 구멍을 배치 완료한 경우
                    {
                        if (hit.collider.CompareTag("Hole1"))
                        {
                            ingredients.bacon.BaconState = Bacon.BaconMode.TwoHoles;
                            ingredients.baconParent.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                            SaveBaconHole(false, false, true);
                            DeployBaconHole();
                        }
                        else if (hit.collider.CompareTag("Hole2"))
                        {
                            Debug.Log("[Hole2] 선택 불가능");
                        }
                        else if (hit.collider.CompareTag("Hole3"))
                        {
                            ingredients.bacon.BaconState = Bacon.BaconMode.TwoHoles;
                            ingredients.baconParent.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                            SaveBaconHole(false, false, true);                                         // 스프라이트가 회전했기 때문에 구멍3을 누르면 구멍1 변수가 false가 된다.
                            DeployBaconHole();
                        }
                    }
                    else if (!woodenSkewer.canChooseBaconHole3) // 세번째 구멍을 배치 완료한 경우
                    {
                        if (hit.collider.CompareTag("Hole1"))
                        {
                            Debug.Log("<<다른 구멍을 클릭해주세요.>>");
                        }
                        else if (hit.collider.CompareTag("Hole2"))
                        {
                            ingredients.bacon.BaconState = Bacon.BaconMode.NewTwoHoles;
                            ingredients.baconParent.position += new Vector3(4.25f, 0.0f, 0.0f);
                            ingredients.baconParent.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                            SaveBaconHole(false, false, true);
                            DeployBaconHole();
                        }
                        else if (hit.collider.CompareTag("Hole3"))
                        {
                            Debug.Log("[Hole3] 선택 불가능");
                        }
                    }
                    #endregion
                }

                // Debug.Log($"[Cheese] : ({woodenSkewer.canChooseCheeseHole1}, {woodenSkewer.canChooseCheeseHole2}, {woodenSkewer.canChooseCheeseHole3})");
                // Debug.Log($"[Bacon] : ({woodenSkewer.canChooseBaconHole1}, {woodenSkewer.canChooseBaconHole2}, {woodenSkewer.canChooseBaconHole3})");
            }
        }
    }

    /// <summary>
    /// 위치에 따른 배치 처리 함수
    /// </summary>
    private void OnRelease(InputAction.CallbackContext _)
    {
        // 오브젝트를 드래그 중인 경우
        if (isDragging && draggedObject != null)
        {
            // 나무 막대 시작 부분에 도달한 후 통과 완료한 경우
            if (woodenSkewer.IsFinished)
            {
                // 오브젝트 위치 설정
                if (draggedObject.CompareTag("Clickable"))
                {
                    draggedObject.transform.position = new Vector2(0.0f, worldPosition.y);
                }
                else ///// if (draggedObject.CompareTag("Hole1") || draggedObject.CompareTag("Hole2") || draggedObject.CompareTag("Hole3"))
                {
                    // Shift 선택이 가능한 상태인지에 대한 변수 설정
                    if (draggedObject.layer == 6)
                    {
                        SetCheeseHole(false, false, false);
                    }
                    else if (draggedObject.layer == 7)
                    {
                        SetBaconHole(false, false, false);
                    }

                    draggedObject.transform.position = new Vector2(offset.x, worldPosition.y);
                }

                // 오브젝트 배치 완료
                OnDeploy(draggedObject);
            }
            else
            {
                CheckLayer(draggedObject, Cheese.CheeseMode.Normal, Bacon.BaconMode.Normal);
                draggedObject.transform.position = originalPosition;                            // 오브젝트를 원래 위치로 되돌림
                draggedObject.transform.rotation = originalRotation;                            // 오브젝트를 원래 각도로 되돌림
                ResetSkewer();                                                                  // 변수 초기화
            }

            draggedObject = null;                                                               // 드래그 중인 오브젝트 해제
            isDragging = false;                                                                 // 드래그 상태 비활성화
        }
    }

    /// <summary>
    /// 배치 완료 처리 함수
    /// </summary>
    /// <param name="draggedObject">처리할 오브젝트</param>
    void OnDeploy(GameObject draggedObject)
    {
        // 예외 처리
        if (finishedObjectArray == null || draggedObject == null)
        {
            Debug.Log("finishedObjectArray 또는 draggedObject가 없습니다.");
            return;
        }

        // 배치 완료 처리
        for (int i = 0; i < finishedObjectArray.Length; i++)                            // 인덱스가 작은 순서대로 확인
        {
            if (finishedObjectArray[i] == null)                                         // 배열 중 null인 곳이 있는 경우
            {
                finishedObjectArray[i] = draggedObject;                                 // 배치 완료된 오브젝트 삽입
                finishedObjectArray[i].layer = ignoreLeftClickLayer;                    // 레이어를 "IgnoreLeftClick"으로 변경
                //Debug.Log($"배치 완료된 인덱스 & 오브젝트 : \n{i}_{finishedObjectArray[i]}"); // 배치 완료된 인덱스 및 오브젝트 출력
                break;
            }
        }

        // 변수 초기화
        ResetSkewer();
    }

    /// <summary>
    /// 배치 취소 처리 함수
    /// </summary>
    private void OnCancel(InputAction.CallbackContext _)
    {
        // 예외 처리
        if (finishedObjectArray == null)
        {
            Debug.Log("finishedObjectArray가 없습니다.");
            return;
        }

        // 배치 취소 처리
        for (int i = finishedObjectArray.Length - 1; i >= 0; i--)                       // 인덱스가 큰 순서대로 확인
        {
            if (finishedObjectArray[i] != null)                                         // 배열 중 null이 아닌 곳이 있는 경우
            {
                originalPosition = OriginalPosition(finishedObjectArray[i]);            // 원래 위치 재설정
                originalRotation = OriginalRotation(finishedObjectArray[i]);            // 원래 각도 재설정

                #region [각 재료마다의 배치 취소 처리 과정]
                if (finishedObjectArray[i].name != "Cheese" && finishedObjectArray[i].name != "Bacon") // [일반 재료의 경우]
                {
                    finishedObjectArray[i].transform.position = originalPosition;   // 오브젝트를 원래 위치로 되돌림
                }
                else // [스페셜 재료의 경우 (치즈 & 베이컨)]
                {
                    if (ingredients.cheese.CheeseState == Cheese.CheeseMode.NewThreeHoles
                    || ingredients.bacon.BaconState == Bacon.BaconMode.NewThreeHoles)
                    {
                        CheckLayer(finishedObjectArray[i], Cheese.CheeseMode.NewTwoHoles, Bacon.BaconMode.NewTwoHoles);
                        SetHoleState(finishedObjectArray[i], false, false, true);
                    }
                    else if (ingredients.cheese.CheeseState == Cheese.CheeseMode.NewTwoHoles
                        || ingredients.bacon.BaconState == Bacon.BaconMode.NewTwoHoles)
                    {
                        CheckLayer(finishedObjectArray[i], Cheese.CheeseMode.OneHole, Bacon.BaconMode.OneHole);
                        if (woodenSkewer.deployFirstHole1)
                        {
                            if (finishedObjectArray[i].name == "Cheese")            // 해당 오브젝트가 [치즈]인 경우
                            {
                                ingredients.cheeseParent.position += new Vector3(4.25f, 0.0f, 0.0f);
                            }
                            else if (finishedObjectArray[i].name == "Bacon")        // 해당 오브젝트가 [베이컨]인 경우
                            {
                                ingredients.baconParent.position += new Vector3(4.25f, 0.0f, 0.0f);
                            }

                            SetHoleState(finishedObjectArray[i], false, true, true);
                        }
                        else if (woodenSkewer.deployFirstHole3)
                        {
                            if (finishedObjectArray[i].name == "Cheese")            // 해당 오브젝트가 [치즈]인 경우
                            {
                                ingredients.cheeseParent.position -= new Vector3(4.25f, 0.0f, 0.0f);
                            }
                            else if (finishedObjectArray[i].name == "Bacon")        // 해당 오브젝트가 [베이컨]인 경우
                            {
                                ingredients.baconParent.position -= new Vector3(4.25f, 0.0f, 0.0f);
                            }

                            SetHoleState(finishedObjectArray[i], true, true, false);
                        }

                    }
                    else if (ingredients.cheese.CheeseState == Cheese.CheeseMode.ThreeHoles
                        || ingredients.bacon.BaconState == Bacon.BaconMode.ThreeHoles)
                    {
                        CheckLayer(finishedObjectArray[i], Cheese.CheeseMode.TwoHoles, Bacon.BaconMode.TwoHoles);
                        SetHoleState(finishedObjectArray[i], false, false, true);
                    }
                    else if (ingredients.cheese.CheeseState == Cheese.CheeseMode.TwoHoles
                        || ingredients.bacon.BaconState == Bacon.BaconMode.TwoHoles)
                    {
                        CheckLayer(finishedObjectArray[i], Cheese.CheeseMode.OneHole, Bacon.BaconMode.OneHole);
                        SetHoleState(finishedObjectArray[i], true, false, true);
                    }
                    else
                    {
                        CheckLayer(finishedObjectArray[i], Cheese.CheeseMode.Normal, Bacon.BaconMode.Normal);
                        finishedObjectArray[i].transform.position = originalPosition;
                        SetHoleState(finishedObjectArray[i], true, true, true);
                        woodenSkewer.deployFirstHole1 = false;
                        woodenSkewer.deployFirstHole3 = false;
                    }
                }
                #endregion

                finishedObjectArray[i].layer = originalLayer;                           // 레이어를 "originalLayer"으로 변경
                finishedObjectArray[i] = null;                                          // 배치 취소된 오브젝트는 배열에서 삭제

                //Debug.Log($"배치 취소된 인덱스 & 오브젝트 : \n{i}_{finishedObjectArray[i]}"); // 배치 취소된 인덱스 및 오브젝트 출력
                break;
            }
        }
    }

    /// <summary>
    /// 드래그 처리 함수
    /// </summary>
    void OnDrag()
    {
        // 오브젝트를 드래그 중인 경우
        if (isDragging && draggedObject != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();     // 마우스 위치
            worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);   // 월드 좌표로 변환
            draggedObject.transform.position = worldPosition + offset;      // 오브젝트 위치 갱신
        }
    }

    // 위치 및 회전 관련 함수 -------------------------------------------------------

    /// <summary>
    /// 꼬치 재료의 원래 위치를 반환해주는 함수
    /// </summary>
    /// <param name="obj">원래 위치를 확인하고 싶은 오브젝트</param>
    /// <returns>오브젝트의 원래 위치</returns>
    Vector2 OriginalPosition(GameObject obj)
    {
        switch (obj.name)
        {
            case "Meat":
                originalPosition = ingredients.meatPosition;
                break;
            case "Pepper":
                originalPosition = ingredients.pepperPosition;
                break;
            case "Shrimp":
                originalPosition = ingredients.shrimpPosition;
                break;
            case "Tomato":
                originalPosition = ingredients.tomatoPosition;
                break;
            case "Cheese":
                originalPosition = ingredients.cheesePosition;
                break;
            case "Bacon":
                originalPosition = ingredients.baconPosition;
                break;
        }

        return originalPosition;
    }

    /// <summary>
    /// 꼬치 재료의 원래 각도를 반환해주는 함수
    /// </summary>
    /// <param name="obj">원래 각도를 확인하고 싶은 오브젝트</param>
    /// <returns>오브젝트의 원래 각도</returns>
    Quaternion OriginalRotation(GameObject obj)
    {
        switch (obj.name)
        {
            case "Cheese":
                originalRotation = ingredients.cheeseRotation;
                break;
            case "Bacon":
                originalRotation = ingredients.baconRotation;
                break;
            default:
                // Debug.Log($"[{obj.name}] 각도 유지");
                break;
        }

        return originalRotation;
    }

    // 설정 관련 함수 -------------------------------------------------------------

    /// <summary>
    /// Shift 선택이 가능한 상태인지에 대한 [치즈] 변수 설정용 함수
    /// </summary>
    public void SetCheeseHole(bool one, bool two, bool three)
    {
        if (woodenSkewer.isHole1)
        {
            woodenSkewer.canChooseCheeseHole1 = one;
        }
        else if (woodenSkewer.isHole2)
        {
            woodenSkewer.canChooseCheeseHole2 = two;
        }
        else if (woodenSkewer.isHole3)
        {
            woodenSkewer.canChooseCheeseHole3 = three;
        }
    }

    /// <summary>
    /// Shift 선택이 가능한 상태인지에 대한 [베이컨] 변수 설정용 함수
    /// </summary>
    public void SetBaconHole(bool one, bool two, bool three)
    {
        if (woodenSkewer.isHole1)
        {
            woodenSkewer.canChooseBaconHole1 = one;
        }
        else if (woodenSkewer.isHole2)
        {
            woodenSkewer.canChooseBaconHole2 = two;
        }
        else if (woodenSkewer.isHole3)
        {
            woodenSkewer.canChooseBaconHole3 = three;
        }
    }

    /// <summary>
    /// 배치 취소될 때, 새로 변수 설정용 함수
    /// </summary>
    /// <param name="obj">설정할 오브젝트</param>
    void SetHoleState(GameObject obj, bool one, bool two, bool three)
    {
        if (obj.name == "Cheese") // 해당 오브젝트가 [치즈]인 경우
        {
            SaveCheeseHole(one, two, three);
        }
        else if (obj.name == "Bacon") // 해당 오브젝트가 [베이컨]인 경우
        {
            SaveCheeseHole(one, two, three);
        }
    }

    // 확인 관련 함수 -------------------------------------------------------------

    /// <summary>
    /// 초기화를 위한 오브젝트 레이어 비교 함수
    /// </summary>
    /// <param name="obj">확인할 오브젝트</param>
    /// <param name="cheeseState">치즈의 상태</param>
    /// <param name="baconState">베이컨의 상태</param>
    void CheckLayer(GameObject obj, Cheese.CheeseMode cheeseState, Bacon.BaconMode baconState)
    {
        if (obj.name == "Cheese") // 해당 오브젝트가 [치즈]인 경우
        {
            originalLayer = LayerMask.NameToLayer("Cheese");    // 해당 레이어 설정
            ingredients.cheese.CheeseState = cheeseState;       // 치즈 모드 초기화
            if (cheeseState == Cheese.CheeseMode.Normal)
            {
                ingredients.cheeseParent.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                ResetCheeseHole();                              // 선택 가능 구멍 초기화
            }
        }
        else if (obj.name == "Bacon") // 해당 오브젝트가 [베이컨]인 경우
        {
            originalLayer = LayerMask.NameToLayer("Bacon");     // 해당 레이어 설정
            ingredients.bacon.BaconState = baconState;          // 베이컨 모드 초기화
            if (baconState == Bacon.BaconMode.Normal)
            {
                ingredients.baconParent.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                ResetBaconHole();                               // 선택 가능 구멍 초기화
            }
        }
        else
        {
            originalLayer = LayerMask.NameToLayer("Default");   // 기본 레이어 설정
        }
    }

    // 저장 관련 함수 -------------------------------------------------------------

    /// <summary>
    /// [치즈] 구멍 선택에 대한 저장용 함수
    /// </summary>
    /// <param name="one">첫번째 구멍 선택 가능 여부</param>
    /// <param name="two">두번째 구멍 선택 가능 여부</param>
    /// <param name="three">세번째 구멍 선택 가능 여부</param>
    void SaveCheeseHole(bool one, bool two, bool three)
    {
        // 구멍 선택 가능 여부 저장
        woodenSkewer.canChooseCheeseHole1 = one;
        woodenSkewer.canChooseCheeseHole2 = two;
        woodenSkewer.canChooseCheeseHole3 = three;
    }

    /// <summary>
    /// [치즈] 구멍 배치 완료 처리용 함수
    /// </summary>
    void DeployCheeseHole()
    {
        // 구멍 중 하나라도 배치 완료된 경우
        if (!woodenSkewer.canChooseCheeseHole1 || !woodenSkewer.canChooseCheeseHole2 || !woodenSkewer.canChooseCheeseHole3)
            OnDeploy(cheeseAndBacon);
    }

    /// <summary>
    /// [베이컨] 구멍 선택에 대한 저장용 함수
    /// </summary>
    /// <param name="one">첫번째 구멍 선택 가능 여부</param>
    /// <param name="two">두번째 구멍 선택 가능 여부</param>
    /// <param name="three">세번째 구멍 선택 가능 여부</param>
    void SaveBaconHole(bool one, bool two, bool three)
    {
        // 구멍 선택 가능 여부 저장
        woodenSkewer.canChooseBaconHole1 = one;
        woodenSkewer.canChooseBaconHole2 = two;
        woodenSkewer.canChooseBaconHole3 = three;
    }

    /// <summary>
    /// [베이컨] 구멍 배치 완료 처리용 함수
    /// </summary>
    void DeployBaconHole()
    {
        // 구멍 중 하나라도 배치 완료된 경우
        if (!woodenSkewer.canChooseBaconHole1 || !woodenSkewer.canChooseBaconHole2 || !woodenSkewer.canChooseBaconHole3)
            OnDeploy(cheeseAndBacon);
    }

    // 초기화 관련 함수 -----------------------------------------------------------

    /// <summary>
    /// woodenSkewer의 변수 초기화용 함수
    /// </summary>
    void ResetSkewer()
    {
        // 통과 가능 여부 초기화
        woodenSkewer.isPassingThrough = false;
        woodenSkewer.isFinished = false;

        // 구멍 선택 여부 초기화
        woodenSkewer.isHole1 = false;
        woodenSkewer.isHole2 = false;
        woodenSkewer.isHole3 = false;
    }

    /// <summary>
    /// [치즈]의 선택 가능 구멍 초기화용 함수
    /// </summary>
    void ResetCheeseHole()
    {
        woodenSkewer.canChooseCheeseHole1 = true;
        woodenSkewer.canChooseCheeseHole2 = true;
        woodenSkewer.canChooseCheeseHole3 = true;
    }

    /// <summary>
    /// [베이컨]의 선택 가능 구멍 초기화용 함수
    /// </summary>
    void ResetBaconHole()
    {
        woodenSkewer.canChooseBaconHole1 = true;
        woodenSkewer.canChooseBaconHole2 = true;
        woodenSkewer.canChooseBaconHole3 = true;
    }

    /// <summary>
    /// [구멍 1]에 대한 행동 처리 함수
    /// </summary>
    void FirstHoleAction()
    {
        woodenSkewer.isHole1 = true;
        woodenSkewer.isHole2 = false;
        woodenSkewer.isHole3 = false;
        // Debug.Log("[Hole 1]을 클릭했습니다.");
    }

    /// <summary>
    /// [구멍 2]에 대한 행동 처리 함수
    /// </summary>
    void SecondHoleAction()
    {
        woodenSkewer.isHole1 = false;
        woodenSkewer.isHole2 = true;
        woodenSkewer.isHole3 = false;
        // Debug.Log("[Hole 2]을 클릭했습니다.");
    }

    /// <summary>
    /// [구멍 3]에 대한 행동 처리 함수
    /// </summary>
    void ThirdHoleAction()
    {
        woodenSkewer.isHole1 = false;
        woodenSkewer.isHole2 = false;
        woodenSkewer.isHole3 = true;
        // Debug.Log("[Hole 3]을 클릭했습니다.");
    }

    // 꼬치 완성 함수 -------------------------------------------------------------

    /// <summary>
    /// [FINISH] 버튼을 누른 경우 처리 함수
    /// </summary>
    public void isTheEnd()
    {
        // 접시까지의 거리 (y좌표)
        float endPosition = 15.0f;
        float specialPosition = endPosition / 3; // 나눗셈 미리 계산

        // 꼬치 막대 위치 이동
        woodenSkewer.gameObject.transform.position += new Vector3(0, endPosition, 0);

        // 배치 완료된 재료들 위치 이동
        for (int i = 0; i < finishedObjectArray.Length; i++)
        {
            if (finishedObjectArray[i] != null)
            {
                if (finishedObjectArray[i].name == "Cheese" || finishedObjectArray[i].name == "Bacon")
                {
                    finishedObjectArray[i].transform.position += new Vector3(0, specialPosition, 0);
                }
                else
                {
                    finishedObjectArray[i].transform.position += new Vector3(0, endPosition, 0);
                }
            }
            else
            {
                // Debug.Log("[FINISH] 모든 재료 배치 완료");
                break;
            }
        }
    }

    /// <summary>
    /// [Next] 버튼을 누른 경우 처리 함수
    /// </summary>
    public void isStartAgain()
    {
        // 꼬치 막대 위치 초기화
        woodenSkewer.gameObject.transform.position = new Vector3(0, -0.7f, 0);

        // 배치 완료된 재료들 위치 초기화
        for (int i = finishedObjectArray.Length - 1; i >= 0; i--)
        {
            // OnCancel 메서드를 직접 호출
            InputAction.CallbackContext context = new InputAction.CallbackContext();
            OnCancel(context);
        }
    }

    // 점수 획득 함수 -------------------------------------------------------------

    /// <summary>
    /// 플레이어의 점수를 추가해주는 함수
    /// </summary>
    /// <param name="getScore">새로 얻은 점수</param>
    public void AddPlayerScore(int getScore)
    {
        PlayerScore += getScore;
    }
}

