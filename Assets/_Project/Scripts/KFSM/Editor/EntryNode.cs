using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kylin.FSM
{
    public class EntryNode : Node
    {
        public Port OutputPort { get; private set; }
        public int InitialStateId { get; set; } = 0;
        
        public EntryNode()
        {
            title = "Entry";
            viewDataKey = "EntryNode";
            
            OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputContainer.Add(OutputPort);
            
            
            style.backgroundColor = new StyleColor(new Color(0.3f, 0.5f, 0.3f, 0.8f));
            
            var infoLabel = new Label("Starting Point");
            infoLabel.style.color = new StyleColor(Color.white);
            mainContainer.Add(infoLabel);
            
            RefreshExpandedState();
            RefreshPorts();
            
            //삭제못함
            capabilities &= ~Capabilities.Deletable;
        }
    }
}