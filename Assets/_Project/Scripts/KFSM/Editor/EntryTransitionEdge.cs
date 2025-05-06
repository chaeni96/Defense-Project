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

            // ��� ����/�̵� ��� ��Ȱ��ȭ 
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Movable;
            capabilities &= ~Capabilities.Selectable;

            // Edge ��Ÿ�� ����
            this.edgeControl.edgeWidth = 2;
            this.edgeControl.inputColor = new Color(0.3f, 0.7f, 0.3f);
            this.edgeControl.outputColor = new Color(0.3f, 0.7f, 0.3f);

            // Entry �� �߰�
            // Ư���� Ŭ���� �߰�
            AddToClassList("entry-transition-edge");
            AddToClassList("no-drag");

            // ��� �̺�Ʈ ó���� ����
            RegisterAllCallbacks();
        }

        private void RegisterAllCallbacks()
        {
            // ��� ���콺 �̺�Ʈ ����ä�� (highest priority)
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

            // �巡�� �̺�Ʈ ����ä��
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

        // Edge�� �⺻ ���� ������
        public override bool ContainsPoint(Vector2 localPoint)
        {
            // ���콺 �̺�Ʈ�� ���� �ʵ��� �׻� false ��ȯ
            return false;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            // �����ϴ���, ���� ��� ��Ȱ��ȭ ����
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Movable;

            // ���� ȿ���� ����ϵ�, �ٸ� ������ ����
            this.edgeControl.edgeWidth = 4;
            this.edgeControl.inputColor = new Color(0.4f, 0.8f, 0.4f);
            this.edgeControl.outputColor = new Color(0.4f, 0.8f, 0.4f);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();

            // ���� ��Ÿ�Ϸ� ����
            this.edgeControl.edgeWidth = 2;
            this.edgeControl.inputColor = new Color(0.3f, 0.7f, 0.3f);
            this.edgeControl.outputColor = new Color(0.3f, 0.7f, 0.3f);
        }
    }
}