using System.Windows;
using AvalonDock.Layout;
using IO.Milvus.Workbench.ViewModels;

namespace IO.Milvus.Workbench
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel VM { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = (VM = new MainWindowViewModel(documentPane));
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;
            
            await VM.LoadMilvusInstanceConfigAsync();
        }
    }
}
