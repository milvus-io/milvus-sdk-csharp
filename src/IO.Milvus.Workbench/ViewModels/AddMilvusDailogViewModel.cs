using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IO.Milvus.Workbench.Utils;
using System;
using System.Windows;

namespace IO.Milvus.Workbench.ViewModels
{
    public class AddMilvusDailogViewModel : DialogViewModel
    {
        public string Name { get; set; } = "Test";

        public string Host { get; set; } = "192.168.100.139";

        public int Port { get; set; } = 19530;

        protected override void CancelClick()
        {
            CloseAction?.Invoke(false);
        }

        protected override void AddClick()
        {
            if (!IPValidationUtils.IsHost(Host))
            {
                MessageBox.Show("Host Error");
                return;
            }
            if (!PortValidationUtils.PortInRange(Port))
            {
                MessageBox.Show("Port Error");
                return;
            }

            CloseAction?.Invoke(true);
        }
    }
}
