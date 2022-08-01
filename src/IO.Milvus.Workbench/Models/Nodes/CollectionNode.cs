using IO.Milvus.Grpc;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Partition;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using IO.Milvus.Param.Dml;
using IO.Milvus.Workbench.Dialogs;

namespace IO.Milvus.Workbench.Models
{
    public enum LoadedState
    {
        Unknown,
        Loading,
        Loaded,
    }

    public class CollectionNode : Node<PartitionNode>
    {
        private List<FieldModel> _fields;
        private AsyncRelayCommand _queryCmd;
        private RelayCommand _resetCmd;
        private string _queryText;
        private List<FieldData> _queryResultData;
        private LoadedState _loadedState;
        private List<string> _alias;
        private AsyncRelayCommand _loadCollectionCmd;
        private AsyncRelayCommand _releaseCollectionCmd;
        private AsyncRelayCommand _dropCollectionCmd;
        private AsyncRelayCommand _createPartitionCmd;

        public CollectionNode(
                    MilvusConnectionNode parent,
                    string name,
                    long id,
                    ulong createdTimestampsmeStamp,
                    ulong createdUtcTimeStamp)
        {
            Name = name;
            Parent = parent;
            Id = id;
        }

        #region Properties
        public MilvusConnectionNode Parent { get; }

        public long Id { get; set; }

        public bool AutoID { get; set; }

        public int ShardsNum { get; set; }

        public LoadedState LoadedState { get => _loadedState; set => SetProperty(ref _loadedState, value); }

        public List<FieldModel> Fields { get => _fields; set => SetProperty(ref _fields, value); }

        public List<string> Aliases { get => _alias; set => SetProperty(ref _alias, value); }

        public List<FieldData> QueryResultData { get => _queryResultData; set => SetProperty(ref _queryResultData, value); }

        public string QueryText
        {
            get => _queryText; set
            {
                _queryText = value;
                QueryCmd.NotifyCanExecuteChanged();
            }
        }

        public AsyncRelayCommand LoadCollectionCmd { get => _loadCollectionCmd ?? (_loadCollectionCmd = new AsyncRelayCommand(LoadCollectionClickAsync)); }

        public AsyncRelayCommand ReleaseCollectionCmd { get => _releaseCollectionCmd ?? (_releaseCollectionCmd = new AsyncRelayCommand(ReleaseCollectionClickAsync)); }

        public AsyncRelayCommand DropCollectionCmd { get => _dropCollectionCmd ?? (_dropCollectionCmd = new AsyncRelayCommand(DropCollectionClickAsync)); }

        public AsyncRelayCommand CreatePartitionCmd { get => _createPartitionCmd ?? (_createPartitionCmd = new AsyncRelayCommand(CreatePartitionClickAsync));}

        public AsyncRelayCommand QueryCmd { get => _queryCmd ?? (_queryCmd = new AsyncRelayCommand(QueryClickAsync, () => !string.IsNullOrWhiteSpace(QueryText))); }

        public RelayCommand ResetCmd { get => _resetCmd ?? (_resetCmd = new RelayCommand(ResetClick, () => QueryResultData != null)); }

        private void ResetClick()
        {
            QueryResultData = null;
            ResetCmd.NotifyCanExecuteChanged();
        }

        private async Task DropCollectionClickAsync()
        {
            var res = MessageBox.Show($"Are you sure delete:{Name}?","Delete",MessageBoxButton.OKCancel,MessageBoxImage.Warning);
            if (res != MessageBoxResult.OK)
            {
                return;
            }
            try
            {
                var r = Parent.ServiceClient.DropCollection(Name);
                if (r.Status != Param.Status.Success)
                {
                    MessageBox.Show(r.Exception.Message);
                }

               await Parent.RefreshAsync();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }            
        }

        private async Task LoadCollectionClickAsync()
        {
            LoadedState = LoadedState.Loading;
            var r = await Parent.ServiceClient.LoadCollectionAsync(LoadCollectionParam.Create(Name));

            //TODO Query Load State

        }

        private async Task CreatePartitionClickAsync()
        {
            var dialog = new CreatePartitionDialog();
            if (dialog.ShowDialog() == true)
            {
                var r = await Task.Run(() =>
                {
                    return Parent.ServiceClient.CreatePartition(CreatePartitionParam.Create(
                    Name,
                    dialog.VM.PartitionName));
                });

                //TODO:Validate
            }
        }

        private Task ReleaseCollectionClickAsync()
        {
            throw new NotImplementedException();
        }

        private async Task QueryClickAsync()
        {
            if (string.IsNullOrWhiteSpace(QueryText))
            {
                MessageBox.Show("Please Input QueryText First");
                return;
            }

            var r = await Parent.ServiceClient.QueryAsync(QueryParam.Create(Name,
                Children.Select(p => p.Name).ToList(),
                Fields.Select(p => p.Name).ToList(),
                expr: QueryText));

            if (r.Status != Param.Status.Success)
            {
                MessageBox.Show(r.Exception.Message);
                return;
            }

            QueryResultData = r.Data.FieldsData.ToList();
        }
        #endregion

        public async Task ConnectAsync()
        {
            var r = Parent.ServiceClient.DescribeCollection(DescribeCollectionParam.Create(Name));

            if (r.Status != Param.Status.Success)
            {
                State = NodeState.Error;
                return;
            }

            AutoID = r.Data.Schema.AutoID;
            Description = r.Data.Schema.Description;
            ShardsNum = r.Data.ShardsNum;

            Aliases = r.Data.Aliases.ToList();

            //Query Partition
            var allPartitionR = await Task.Run(() => Parent.ServiceClient.ShowPartitions(ShowPartitionsParam.Create(Name, null)));

            if (allPartitionR.Status != Param.Status.Success)
            {
                State = NodeState.Error;
                return;
            }

            for (int i = 0; i < allPartitionR.Data.PartitionNames.Count; i++)
            {
                var partition = new PartitionNode(
                    this,
                    allPartitionR.Data.PartitionNames[i],
                    allPartitionR.Data.PartitionIDs[i]);

                Children.Add(partition);
            }

            var fields = new List<FieldModel>();
            foreach (var field in r.Data.Schema.Fields)
            {
                var fieldNode = new FieldModel(
                    field.IsPrimaryKey,
                    field.Name,
                    field.FieldID,
                    field.DataType,
                    field.Description);

                if (field.DataType == DataType.FloatVector)
                {
                    var dim = field.TypeParams.FirstOrDefault(p => p.Key == "dim")?.Value;
                    if (dim != null)
                    {
                        fieldNode.Dimension = int.Parse(dim);
                    }
                }

                fields.Add(fieldNode);
            }
            Fields = fields;
        }

        public override string ToString()
        {
            return $"ID:{Id}\nAutoID:{AutoID}\nDescription:{Description}";
        }

    }
}
