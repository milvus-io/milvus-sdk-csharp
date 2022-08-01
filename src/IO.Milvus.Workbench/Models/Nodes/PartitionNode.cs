using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace IO.Milvus.Workbench.Models
{
    public class PartitionNode : Node
    {
        private LoadedState _loadedState;

        public PartitionNode(
            CollectionNode parent,
            string name, long id)
        {
            Parent = parent;
            Name = name;
            ID = id;
        }

        public long ID { get; }

        public LoadedState LoadedState { get => _loadedState; set => SetProperty(ref _loadedState, value); }

        public CollectionNode Parent { get; private set; }

        public override IEnumerable<Node> GetChildren()
        {
            yield break;
        }

        public override string ToString()
        {
            return $"{nameof(Name)}:{Name}\n{nameof(ID)}:{ID}";
        }
    }
}
