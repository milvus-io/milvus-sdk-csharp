using IO.Milvus.Workbench.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IO.Milvus.Workbench.Dialogs
{
    /// <summary>
    /// CreatePartitionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CreatePartitionDialog : Window
    {
        public CreatePartitionDialog()
        {
            InitializeComponent();
            DataContext = VM = new CreatePartitionDialogViewModel()
            {
                CloseAction = (r) => this.DialogResult = r
            };
        }

        public CreatePartitionDialogViewModel VM { get; }
    }
}
