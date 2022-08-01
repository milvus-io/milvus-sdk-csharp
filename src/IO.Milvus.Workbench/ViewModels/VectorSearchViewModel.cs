using CommunityToolkit.Mvvm.ComponentModel;
using IO.Milvus.Workbench.Models;

namespace IO.Milvus.Workbench.ViewModels
{
    public class VectorSearchViewModel : ObservableObject
    {
        private MilvusConnectionNode _selectedMilvusNode;
        private CollectionNode _selectedCollectionNode;

        public VectorSearchViewModel(MilvusManagerNode milvusManagerNode)
        {

            MilvusManagerNode = milvusManagerNode;
        }

        public MilvusManagerNode MilvusManagerNode { get; }

        public MilvusConnectionNode SelectedMilvusNode { get => _selectedMilvusNode; set => SetProperty(ref _selectedMilvusNode, value); }

        public CollectionNode SelectedCollectionNode { get => _selectedCollectionNode; set => SetProperty(ref _selectedCollectionNode, value); }

        public FieldModel SelectedField{ get; set; }

        public int Nprobe { get; set; }

        public int RoundDecimals { get; set; }
    }
}
