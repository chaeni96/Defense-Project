using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kylin.FSM
{
    public class EntryTransitionEdge : Edge
    {
        public EntryTransitionEdge(Port output, Port input)
        {
            this.output = output;
            this.input = input;

            // 모든 삭제/이동 기능 비활성화 
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Selectable;

            // Edge 스타일 설정
            this.edgeControl.edgeWidth = 2;
            this.edgeControl.inputColor = new Color(0.3f, 0.7f, 0.3f);
            this.edgeControl.outputColor = new Color(0.3f, 0.7f, 0.3f);

            // Entry 라벨 추가
            // 특별한 클래스 추가
            AddToClassList("entry-transition-edge");
            AddToClassList("no-drag");

            // 모든 이벤트 처리기 연결
            RegisterAllCallbacks();
        }

        private void RegisterAllCallbacks()
        {
            // 모든 마우스 이벤트 가로채기 (highest priority)
            RegisterCallback<MouseDownEvent>(evt => {
                evt.StopPropagation();
                evt.PreventDefault();
            });

            RegisterCallback<MouseUpEvent>(evt => {
                evt.StopPropagation();
                evt.PreventDefault();
            });

            RegisterCallback<MouseMoveEvent>(evt => {
                evt.StopPropagation();
                evt.PreventDefault();
            });

            // 드래그 이벤트 가로채기
            RegisterCallback<DragUpdatedEvent>(evt => {
                evt.StopPropagation();
                evt.PreventDefault();
            });

            RegisterCallback<DragPerformEvent>(evt => {
                evt.StopPropagation();
                evt.PreventDefault();
            });

            RegisterCallback<DragExitedEvent>(evt => {
                evt.StopPropagation();
                evt.PreventDefault();
            });
        }

        // Edge의 기본 동작 재정의
        public override bool ContainsPoint(Vector2 localPoint)
        {
            // 마우스 이벤트를 받지 않도록 항상 false 반환
            return false;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            // 선택하더라도, 삭제 기능 비활성화 유지
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Movable;

            // 선택 효과는 허용하되, 다른 조작은 방지
            this.edgeControl.edgeWidth = 4;
            this.edgeControl.inputColor = new Color(0.4f, 0.8f, 0.4f);
            this.edgeControl.outputColor = new Color(0.4f, 0.8f, 0.4f);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();

            // 원래 스타일로 복원
            this.edgeControl.edgeWidth = 2;
            this.edgeControl.inputColor = new Color(0.3f, 0.7f, 0.3f);
            this.edgeControl.outputColor = new Color(0.3f, 0.7f, 0.3f);
        }
    }
}