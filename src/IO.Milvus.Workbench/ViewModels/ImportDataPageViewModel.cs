using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IO.Milvus.Workbench.Models;
using System;

namespace IO.Milvus.Workbench.ViewModels
{
    public class ImportDataPageViewModel : ObservableObject
    {
        private CollectionNode _selectedCollection;
        private RelayCommand _browseCsvFileCmd;

        public ImportDataPageViewModel(MilvusConnectionNode connection, CollectionNode collection, PartitionNode partition)
        {
            Connection = connection;
            SelectedCollection = collection;
            SelectedPartition = partition;
        }

        public MilvusConnectionNode Connection { get; set; }

        public CollectionNode SelectedCollection
        {
            get => _selectedCollection; set
            {
                SetProperty(ref _selectedCollection, value);
            }
        }

        public PartitionNode SelectedPartition { get; set; }

        public RelayCommand BrowseCsvFileCmd { get => _browseCsvFileCmd ?? (_browseCsvFileCmd = new RelayCommand(BrowseCsvFileClick)); }

        public string SelectedCsvFile { get; set; }

        private void BrowseCsvFileClick()
        {
            //TODO:Select File
            //var bfDilog = new 
        }
    }
}
