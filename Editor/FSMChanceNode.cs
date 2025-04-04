using Moths.FSM;
using Moths.Graphs.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moths.FSM.Graphs.Editor
{
    [System.Serializable]
    public class FSMChanceNode : BasicNode
    {

        [System.Serializable]
        public class Chance
        {
            public float value;
            public Chance(float value)
            {
                this.value = value;
            }
            // public Modifier modifier;
        }

        //public abstract FSMState GetState();

        //public FSMState state;

        public Port inPort;
        public List<Port> outPorts = new List<Port>();

        public List<Chance> chances;

        public FSMChanceNode(List<Chance> chances)
        {
            //this.state = state;

            this.chances = chances;
            base.title = "Chance";
            var style = Resources.Load<StyleSheet>("styles");
            this.styleSheets.Add(style);
        }

        public override void GeneratePorts()
        {
            UpdatePortsView();
        }

        public override void OnSelected()
        {
            base.OnSelected();

            ////Selection.activeObject = state;
        }

        private void UpdatePortsView()
        {
            outPorts.Clear();

            inputContainer.Clear();
            outputContainer.Clear();

            inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FSMTransition));
            inPort.portName = "In";
            inputContainer.Add(inPort);


            var label = new Label("Out");
            label.AddToClassList("chanceNodeOutLabel");
            outputContainer.Add(label);
            for (int i = 0; i < chances.Count; i++)
            {
                InitChancePort(chances[i], i);
            }


            var addBtn = new Button(() =>
            {
                chances.Add(new Chance(1));
                InitChancePort(chances[chances.Count - 1], chances.Count - 1);
                RefreshPorts();
                RefreshExpandedState();
            })
            {
                text = "+"
            };

            extensionContainer.Add(addBtn);

            RefreshPorts();
            RefreshExpandedState();
        }

        private void InitChancePort(Chance chance, int index)
        {
            VisualElement container = new VisualElement();

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FSMTransition));
            port.portName = "Out";
            port.AddToClassList("chanceNodeOutPort");
            outputContainer.Add(port);
            outPorts.Add(port);

            FloatField valueField = new FloatField();
            valueField.value = chance.value;
            valueField.RegisterValueChangedCallback(ev =>
            {
                chance.value = ev.newValue;
            });

            var removeBtn = new Button(() =>
            {
                chances.Remove(chance);
                outputContainer.Remove(port);
                outPorts.Remove(port);
                inputContainer.Remove(container);
                RefreshPorts();
                RefreshExpandedState();
            })
            {
                text = "-"
            };

            container.AddToClassList("row");

            inputContainer.Add(container);
            container.Add(valueField);
            container.Add(removeBtn);
        }

        public int GetChanceIndexByPort(Port port) 
        {
            return outputContainer.IndexOf(port) - 1;
        }
    }
}