using CommunityToolkit.Mvvm.ComponentModel;
using IO.Milvus.Client;
using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using IO.Milvus.Utils;
using IO.Milvus.Workbench.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IO.Milvus.Workbench.Models
{
    public abstract class Node : ObservableObject
    {
        private string _msg;
        private bool _isExpanded = true;
        private bool _isSelected;

        public string Name { get; set; }

        public string Description { get; set; }

        public string Msg { get => _msg; set => SetProperty(ref _msg, value); }

        public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected , value); }

        public abstract IEnumerable<Node> GetChildren();

        public IEnumerable<Node> ListAllNode()
        {
            foreach (var child in GetChildren())
            {
                yield return child;

                foreach (var childchild in child.ListAllNode())
                {
                    yield return childchild;
                }
            }
        }
    }

    public class Node<TChild> : Node
        where TChild:Node
    {
        private NodeState _state;

        public ObservableCollection<TChild> Children { get; set; } = new ObservableCollection<TChild>();

        public NodeState State
        {
            get => _state; set
            {
                SetProperty(ref _state, value);
                OnStateChanged();
            }
        }

        protected virtual void OnStateChanged() { }

        public override IEnumerable<Node> GetChildren()
        {
            return Children;
        }
    }
}
