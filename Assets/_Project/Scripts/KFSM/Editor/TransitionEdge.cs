using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kylin.FSM
{
    public class TransitionEdge : Edge
    {
        public TransitionEntry transitionData { get; }
        public int FromStateId => transitionData.FromStateId;
        public int ToStateId => transitionData.ToStateId;

        private FSMEditorWindow _editorWindow;

        public TransitionEdge(TransitionEntry transitionData, Port output, Port input)
        {
            this.transitionData = transitionData;
            this.output = output;
            this.input = input;

            // 스타일 설정 옵션
            this.edgeControl.edgeWidth = 2;

            // 전이 정보 표시를 위한 라벨
            var infoLabel = new Label();
            if (transitionData.RequiredTriggers != null && transitionData.RequiredTriggers.Length > 0)
            {
                infoLabel.text = string.Join(", ", transitionData.RequiredTriggers);
            }
            else
            {
                infoLabel.text = "No Triggers";
            }

            // 테두리 강조를 위한 클래스 추가
            this.AddToClassList("transition-edge");

            // 우클릭 메뉴 설정
            this.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                evt.menu.AppendAction("Delete Transition", _ => OnDeleteTransition());
            });

            // 선택 시 시각적 표시 개선을 위한 이벤트 등록
            // Unity 21.3.30에서 OnSelected/OnUnselected 이벤트가 없을 수 있으므로 
            // 직접 선택 상태를 감지하는 방식으로 구현
            this.RegisterCallback<MouseEnterEvent>(evt => {
                if (this.selected)
                {
                    this.edgeControl.edgeWidth = 4;
                    this.edgeControl.inputColor = Color.yellow;
                    this.edgeControl.outputColor = Color.yellow;
                }
                else
                {
                    this.edgeControl.edgeWidth = 3; // 마우스 오버 시 약간 강조
                }
            });

            this.RegisterCallback<MouseLeaveEvent>(evt => {
                if (this.selected)
                {
                    this.edgeControl.edgeWidth = 4;
                    this.edgeControl.inputColor = Color.yellow;
                    this.edgeControl.outputColor = Color.yellow;
                }
                else
                {
                    this.edgeControl.edgeWidth = 2; // 기본 두께로 복원
                    this.edgeControl.inputColor = Color.white;
                    this.edgeControl.outputColor = Color.white;
                }
            });
            _editorWindow = EditorWindow.GetWindow<FSMEditorWindow>();
        }
        public override void OnSelected()
        {
            base.OnSelected();

            Debug.Log($"TransitionEdge 선택됨 (OnSelected): {FromStateId}->{ToStateId}");

            // 시각적 스타일 변경
            this.edgeControl.edgeWidth = 4;
            this.edgeControl.inputColor = Color.yellow;
            this.edgeControl.outputColor = Color.yellow;

            // 인스펙터 업데이트 (lazy 초기화)
            var editorWindow = EditorWindow.GetWindow<FSMEditorWindow>();
            if (editorWindow != null)
            {
                editorWindow.ShowTransitionInspector(this);
            }
        }
        // 선택 해제될 때 호출되는 메서드 오버라이드
        public override void OnUnselected()
        {
            base.OnUnselected();

            Debug.Log($"TransitionEdge 선택 해제됨 (OnUnselected): {FromStateId}->{ToStateId}");

            // 시각적 스타일 복원
            this.edgeControl.edgeWidth = 2;
            this.edgeControl.inputColor = Color.white;
            this.edgeControl.outputColor = Color.white;
        }

        private void OnDeleteTransition()
        {
            if (EditorUtility.DisplayDialog("Delete Transition",
                "Are you sure you want to delete this transition?",
                "Delete", "Cancel"))
            {
                var window = EditorWindow.GetWindow<FSMEditorWindow>();
                window.RemoveTransitionEntry(transitionData);
                window.RefreshGraph(false); // 그래프 부분 갱신
            }
        }
    }
}