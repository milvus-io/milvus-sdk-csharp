using CommunityToolkit.Mvvm.ComponentModel;

namespace IO.Milvus.Workbench.ViewModels
{
    public class CreatePartitionDialogViewModel : DialogViewModel
    {
        public CreatePartitionDialogViewModel()
        {

        }

        public string PartitionName { get; set; }

        protected override void AddClick()
        {

            CloseAction?.Invoke(true);
        }
    }
}
