using CommunityToolkit.Mvvm.Input;
using IO.Milvus.Client;
using IO.Milvus.Param;
using IO.Milvus.Param.Collection;
using IO.Milvus.Utils;
using IO.Milvus.Workbench.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IO.Milvus.Workbench.Models
{
    public class MilvusConnectionNode : Node<CollectionNode>
    {
        private AsyncRelayCommand _deleteCmd;
        private RelayCommand _disconnectCmd;
        private AsyncRelayCommand _connectCmd;
        private AsyncRelayCommand _createCollectionCmd;

        public MilvusConnectionNode(MilvusManagerNode parent, string name, string host, int port)
        {
            Name = name;
            Parent = parent;
            Host = host;
            Port = port;
        }

        public MilvusManagerNode Parent { get; }

        public string Host { get; set; }

        public int Port { get; set; }

        public MilvusServiceClient ServiceClient { get; private set; }

        public string DisplayName => string.IsNullOrEmpty(Name) ? Url : $"{Name}({Url})";

        public string Url => $"{Host}:{Port}";

        public AsyncRelayCommand DeleteCmd { get => _deleteCmd ?? (_deleteCmd = new AsyncRelayCommand(DeleteClickAsync)); }

        public RelayCommand DisconnectCmd { get => _disconnectCmd ?? (_disconnectCmd = new RelayCommand(DisconnectClick, () => State == NodeState.Success)); }

        public AsyncRelayCommand ConnectCmd { get => _connectCmd ?? (_connectCmd = new AsyncRelayCommand(RefreshAsync, () => State.CanConnect())); }

        public AsyncRelayCommand CreateCollectionCmd { get => _createCollectionCmd ?? (_createCollectionCmd = new AsyncRelayCommand(CreateCollectionClickAsync, () => State == NodeState.Success)); }

        private async Task CreateCollectionClickAsync()
        {
            //Input collection name
            var dialog = new CreateCollectionDialog(DisplayName);
            if (dialog.ShowDialog() == true)
            {
                //Check if collection existed
                bool has = Children.Any(p => p.Name == dialog.Vm.Name);
                if (has)
                {
                    MessageBox.Show($"{dialog.Vm.Name} Exist");
                    return;
                }

                try
                {
                    var param = CreateCollectionParam.Create(
                        dialog.Vm.Name,
                        2,
                        dialog.Vm.Fields.Select(p => p.ToFieldType()),
                        dialog.Vm.Description);

                    var r = ServiceClient.CreateCollection(param);

                    if (r.Status != Status.Success)
                    {
                        MessageBox.Show(r.Exception.Message);
                    }

                    await RefreshAsync();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public async Task RefreshAsync()
        {
            DisconnectClick();

            await ConnectAsync();
        }

        private void DisconnectClick()
        {
            Children.Clear();
            if (State == NodeState.Success)
            {
                ServiceClient?.Close();
            }
            ServiceClient = null;

            State = NodeState.Closed;
        }

        protected override void OnStateChanged()
        {
            DisconnectCmd.NotifyCanExecuteChanged();
            ConnectCmd.NotifyCanExecuteChanged();
            CreateCollectionCmd.NotifyCanExecuteChanged();
        }

        private async Task DeleteClickAsync()
        {
            Children.Clear();
            ServiceClient?.Close();

            Parent.Children.Remove(this);

            await Parent.SaveAsync();
        }

        public async Task ConnectAsync()
        {
            State = NodeState.Connecting;

            try
            {
                if (ServiceClient != null)
                {
                    ServiceClient.Close();
                    ServiceClient = null;
                }

                var connect = ConnectParam.Create(Host, Port);
                ServiceClient = new MilvusServiceClient(connect);

                if (!ServiceClient.ClientIsReady())
                {
                    State = NodeState.Error;
                    Msg = $"Client is Not Ready";
                    return;
                }

                var r = await Task.Run(() =>
                {
                    return ServiceClient.ShowCollections(ShowCollectionsParam.Create(null, Grpc.ShowType.All));
                });

                if (r.Status != Status.Success)
                {
                    State = NodeState.Error;
                    Msg = $"{r.Status}: {r.Exception.Message}";
                    return;
                }

                if (r.Data.CollectionNames.IsEmpty())
                {
                    return;
                }

                for (int i = 0; i < r.Data.CollectionNames.Count; i++)
                {
                    var collectionNode = new CollectionNode(
                        this,
                        r.Data.CollectionNames[i],
                        r.Data.CollectionIds[i],
                        r.Data.CreatedTimestamps[i],
                        r.Data.CreatedUtcTimestamps[i]);

                    Children.Add(collectionNode);

                    await collectionNode.ConnectAsync();
                }

                State = NodeState.Success;
            }
            catch (System.Exception ex)
            {
                State = NodeState.Error;
                Msg = ex.Message;
            }
        }

        public override string ToString()
        {
            return State == NodeState.Success ? DisplayName : Msg;
        }
    }
}
