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

            capabilities &= ~Capabilities.Deletable;


            var infoLabel = new Label("Start");
            infoLabel.style.color = new StyleColor(new Color(0.3f, 0.7f, 0.3f));


            // Ư�� Ŭ���� �߰�
            this.AddToClassList("entry-transition-edge");
        }
        public override void OnSelected()
        {
            base.OnSelected();
            capabilities &= ~Capabilities.Deletable;
        }
    }
}