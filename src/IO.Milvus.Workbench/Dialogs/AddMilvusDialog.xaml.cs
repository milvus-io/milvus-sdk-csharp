using IO.Milvus.Workbench.Utils;
using IO.Milvus.Workbench.ViewModels;
using System.Windows;

namespace IO.Milvus.Workbench.Dialogs
{
    /// <summary>
    /// AddMilvusDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddMilvusDialog : Window
    {
        public AddMilvusDialog()
        {
            InitializeComponent();
            DataContext = Vm = new AddMilvusDailogViewModel();
            Vm.CloseAction = (result) =>
            {
                DialogResult = result;
            };

            this.SetOwnerWindow();
        }

        public AddMilvusDailogViewModel Vm { get; }

    }
}
