using AvalonDock.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IO.Milvus.Utils;
using IO.Milvus.Workbench.Dialogs;
using IO.Milvus.Workbench.DocumentViews;
using IO.Milvus.Workbench.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IO.Milvus.Workbench.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private AsyncRelayCommand _addCmd;
        private MilvusManagerNode _milvusManagerNode;
        private RelayCommand _openPageCmd;
        private RelayCommand _openVectorSearchPageCmd;

        public MainWindowViewModel(LayoutDocumentPane documentPane)
        {
            _milvusManagerNode = new MilvusManagerNode();
            DocumentPane = documentPane;
        }

        #region Properties
        public LayoutDocumentPane DocumentPane { get; }

        public MilvusManagerNode MilvusManagerNode { get => _milvusManagerNode; set => SetProperty(ref _milvusManagerNode, value); }

        public Node SelectedNode { get; private set; }

        public AsyncRelayCommand AddCmd { get => _addCmd ?? (_addCmd = new AsyncRelayCommand(AddClickAsync)); }

        public RelayCommand OpenPageCmd { get => _openPageCmd ?? (_openPageCmd = new RelayCommand(OpenClick)); }

        public RelayCommand OpenVectorSearchPageCmd { get => _openVectorSearchPageCmd ?? (_openVectorSearchPageCmd = new RelayCommand(OpenVectorSearchPageClick, () => MilvusManagerNode.Children.IsNotEmpty())); }
        #endregion

        #region Private Methods
        private void OpenClick()
        {
            SelectedNode = MilvusManagerNode
                .ListAllNode()
                .FirstOrDefault(p => p.IsSelected);
            if (SelectedNode == null)
            {
                return;
            }

            var existDoc = DocumentPane.Children.FirstOrDefault(p => (p.Content as UserControl)?.DataContext == SelectedNode);
            if (existDoc != null)
            {
                existDoc.IsActive = true;
                return;
            }


            if (SelectedNode is CollectionNode collectionNode)
            {
                var newDocPage = new LayoutDocument()
                {
                    Title = collectionNode.Name,
                    Content = new Frame()
                    {
                        Content = new CollectionPage()
                        {
                            DataContext = collectionNode,
                        }
                    },
                };

                DocumentPane.Children.Add(newDocPage);
                newDocPage.IsActive = true;
            }else if (SelectedNode is PartitionNode partitionNode)
            {
                var newDocPage = new LayoutDocument()
                {
                    Title = partitionNode.Name,
                    Content = new Frame()
                    {
                        Content = new ImportDataPage()
                        {
                            DataContext = new ImportDataPageViewModel(
                                partitionNode.Parent.Parent,
                                partitionNode.Parent,
                                partitionNode),
                        }
                    },
                };

                DocumentPane.Children.Add(newDocPage);
                newDocPage.IsActive = true;
            }
        }

        private async Task AddClickAsync()
        {
            var dialog = new AddMilvusDialog();
            if (dialog.ShowDialog() == true)
            {
                var milvus = new MilvusConnectionNode(
                    MilvusManagerNode,
                    dialog.Vm.Name,
                    dialog.Vm.Host,
                    dialog.Vm.Port);
                MilvusManagerNode.Children.Add(milvus);

                await milvus.ConnectAsync();
            }

            OpenVectorSearchPageCmd.NotifyCanExecuteChanged();

            //Save Config
            await MilvusManagerNode.SaveAsync();
        }

        private void OpenVectorSearchPageClick()
        {
            var newDocPage = new LayoutDocument()
            {
                Title = "VectorSearch",
                Content = new Frame()
                {
                    Content = new VectorSearchPage()
                    {
                        DataContext = new VectorSearchViewModel(MilvusManagerNode),
                    }
                },
            };

            DocumentPane.Children.Add(newDocPage);
            newDocPage.IsActive = true;
        }
        #endregion

        #region Internal Methods
        internal async Task LoadMilvusInstanceConfigAsync()
        {
            var configs = await MilvusManagerNode.ReadConfigAsync();

            foreach (var config in configs)
            {
                var milvus = new MilvusConnectionNode(
                    MilvusManagerNode,
                    config.Name,
                    config.Host,
                    config.Port);
                MilvusManagerNode.Children.Add(milvus);

                await milvus.ConnectAsync();
            }

            OpenVectorSearchPageCmd.NotifyCanExecuteChanged();
        }
        #endregion
    }
}
