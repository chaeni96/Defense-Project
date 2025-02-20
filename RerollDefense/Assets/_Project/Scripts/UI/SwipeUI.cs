using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwipeUI : MonoBehaviour
{
    [SerializeField] private Scrollbar scrollBar;

    [SerializeField] private float swipeTime = 0.2f; // 페이지 swipe 되는 시간

    [SerializeField] private float swipeDistance = 50.0f; // 페이지 swipe되기 위해 움직여야하는 최소거리

    [SerializeField] private Transform circlePanel;         // 원형 인디케이터들을 담을 패널
    [SerializeField] private GameObject circleTemplate;     // 복제할 원형 인디케이터 템플릿
    [SerializeField] private float activeCircleScale = 1.4f;// 현재 페이지 원형 크기 배율
    [SerializeField] private Color activeCircleColor = Color.black;    // 현재 페이지 원형 색상
    [SerializeField] private Color inactiveCircleColor = Color.white;  // 비활성 페이지 원형 색상

    private List<GameObject> circleIndicators = new List<GameObject>();  // 생성된 원형 인디케이터들


    private float[] scrollPageValues;           // 각 페이지의 위치 값 [0.0 - 1.0]
    private float valueDistance = 0;            // 각 페이지 사이의 거리
    private int currentPage = 0;            // 현재 페이지
    private int maxPage = 0;                // 최대 페이지
    private float startTouchX;              // 터치 시작 위치
    private float endTouchX;                    // 터치 종료 위치
    private bool isSwipeMode = false;       // 현재 Swipe가 되고 있는지 체크

    public void InitializeSwipe()
    {
        // 스크롤 되는 페이지의 각 value 값을 저장하는 배열 메모리 할당
        scrollPageValues = new float[transform.childCount];

        // 스크롤 되는 페이지 사이의 거리
        valueDistance = 1f / (scrollPageValues.Length - 1f);

        // 스크롤 되는 페이지의 각 value 위치 설정 [0 <= value <= 1]
        for (int i = 0; i < scrollPageValues.Length; ++i)
        {
            scrollPageValues[i] = valueDistance * i;
        }

        // 최대 페이지의 수
        maxPage = transform.childCount;

        // 원형 인디케이터 초기화
        InitializeCircleIndicators();


        // 최초 시작할 때 0번 페이지를 볼 수 있도록 설정
        SetScrollBarValue(0);
    }
    private void InitializeCircleIndicators()
    {
        // 기존 인디케이터 제거
        foreach (var indicator in circleIndicators)
        {
            if (indicator != null && indicator != circleTemplate)
            {
                Destroy(indicator);
            }
        }
        circleIndicators.Clear();

        // 템플릿이 없거나 패널이 없으면 종료
        if (circleTemplate == null || circlePanel == null)
        {
            Debug.LogWarning("Circle template or panel is not assigned!");
            return;
        }

        // 페이지 수에 맞게 인디케이터 생성
        for (int i = 0; i < maxPage; i++)
        {
            GameObject newCircle = Instantiate(circleTemplate, circlePanel);
            newCircle.SetActive(true);
            circleIndicators.Add(newCircle);

            // 클릭 이벤트 추가 (선택 사항)
            int pageIndex = i; // 클로저를 위한 변수 복사
            Button circleButton = newCircle.GetComponent<Button>();
            if (circleButton != null)
            {
                circleButton.onClick.AddListener(() => {
                    StartCoroutine(OnSwipeOneStep(pageIndex));
                });
            }
        }

        // 초기 상태 설정
        UpdateCircleIndicators();
    }

    public void SetScrollBarValue(int index)
    {
        currentPage = index;
        scrollBar.value = scrollPageValues[index];
        UpdateCircleIndicators();
    }

    private void Update()
    {
        UpdateInput();
    }

    private void UpdateInput()
    {
        // 현재 Swipe를 진행중이면 터치 불가
        if (isSwipeMode == true) return;

#if UNITY_EDITOR
        // 마우스 왼쪽 버튼을 눌렀을 때 1회
        if (Input.GetMouseButtonDown(0))
        {
            // 터치 시작 지점 (Swipe 방향 구분)
            startTouchX = Input.mousePosition.x;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // 터치 종료 지점 (Swipe 방향 구분)
            endTouchX = Input.mousePosition.x;

            UpdateSwipe();
        }
#endif

#if UNITY_ANDROID
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // 터치 시작 지점 (Swipe 방향 구분)
                startTouchX = touch.position.x;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                // 터치 종료 지점 (Swipe 방향 구분)
                endTouchX = touch.position.x;

                UpdateSwipe();
            }
        }
#endif
    }

    private void UpdateSwipe()
    {
        // 너무 작은 거리를 움직였을 때는 Swipe X
        if (Mathf.Abs(startTouchX - endTouchX) < swipeDistance)
        {
            // 원래 페이지로 Swipe해서 돌아간다
            StartCoroutine(OnSwipeOneStep(currentPage));
            return;
        }

        // Swipe 방향
        bool isLeft = startTouchX < endTouchX ? true : false;

        // 이동 방향이 왼쪽일 때
        if (isLeft == true)
        {
            // 현재 페이지가 왼쪽 끝이면 종료
            if (currentPage == 0) return;

            // 왼쪽으로 이동을 위해 현재 페이지를 1 감소
            currentPage--;
        }
        // 이동 방향이 오른쪽일 떄
        else
        {
            // 현재 페이지가 오른쪽 끝이면 종료
            if (currentPage == maxPage - 1) return;

            // 오른쪽으로 이동을 위해 현재 페이지를 1 증가
            currentPage++;
        }

        // currentIndex번째 페이지로 Swipe해서 이동
        StartCoroutine(OnSwipeOneStep(currentPage));
    }

    /// <summary>
    /// 페이지를 한 장 옆으로 넘기는 Swipe 효과 재생
    /// </summary>
    private IEnumerator OnSwipeOneStep(int index)
    {
        float start = scrollBar.value;
        float current = 0;
        float percent = 0;

        isSwipeMode = true;

        while (percent < 1)
        {
            current += Time.deltaTime;
            percent = current / swipeTime;

            scrollBar.value = Mathf.Lerp(start, scrollPageValues[index], percent);

            UpdateCircleIndicators();

            yield return null;
        }

        isSwipeMode = false;
    }


    /// <summary>
    /// 현재 페이지에 따라 원형 인디케이터 상태 업데이트
    /// </summary>
    private void UpdateCircleIndicators()
    {
        if (circleIndicators.Count == 0) return;

        for (int i = 0; i < circleIndicators.Count; i++)
        {
            // 스케일 초기화
            circleIndicators[i].transform.localScale = Vector3.one;

            // 색상 초기화
            Image circleImage = circleIndicators[i].GetComponent<Image>();
            if (circleImage != null)
            {
                circleImage.color = inactiveCircleColor;
            }

            // 현재 페이지 강조
            if (i == currentPage)
            {
                circleIndicators[i].transform.localScale = Vector3.one * activeCircleScale;
                if (circleImage != null)
                {
                    circleImage.color = activeCircleColor;
                }
            }
        }
    }
}
