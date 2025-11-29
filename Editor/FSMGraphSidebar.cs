using System;
using UnityEngine.UIElements;

namespace Moths.FSM.Graphs.Editor
{
    public class FSMGraphSidebar : VisualElement
    {
        private Label _titleLabel;

        public string Title { get => _titleLabel.text; set => _titleLabel.text = value; }

        public VisualElement Content { get; private set; }

        public FSMGraphSidebar()
        {
            _titleLabel = new Label();
            _titleLabel.AddToClassList("title");
            this.Add(_titleLabel);

            Content = new();
            this.Add(Content);
        }

        public void AddItem(string name, Action callback)
        {
            Button btn = new Button(callback) { text = name };
            btn.AddToClassList("button");
            Content.Add(btn);
        }

        public new void Clear()
        {
            Content.Clear();
        }
    }
}