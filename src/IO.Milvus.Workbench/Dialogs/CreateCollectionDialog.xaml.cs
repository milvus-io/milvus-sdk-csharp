using IO.Milvus.Workbench.Utils;
using IO.Milvus.Workbench.ViewModels;
using System.Windows;

namespace IO.Milvus.Workbench.Dialogs
{
    /// <summary>
    /// CreateCollectionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CreateCollectionDialog : Window
    {
        public CreateCollectionDialog(string connectionName)
        {
            InitializeComponent();
            DataContext = Vm = new CreateCollectionDialogViewModel(connectionName);
            Vm.CloseAction = (result) =>
            {
                DialogResult = result;
            };

            this.SetOwnerWindow();
        }

        public CreateCollectionDialogViewModel Vm { get; }
    }
}
