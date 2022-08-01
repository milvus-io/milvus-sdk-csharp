using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IO.Milvus.Workbench.Utils;
using IO.Milvus.Workbench.ViewModels;
using System;
using System.Windows;

namespace IO.Milvus.Workbench.ViewModels
{
    public abstract class DialogViewModel : ObservableObject
    {
        private RelayCommand _addCmd;
        private RelayCommand _canacelCmd;

        public RelayCommand AddCmd { get => _addCmd = (_addCmd = new RelayCommand(AddClick)); }

        protected abstract void AddClick();

        public RelayCommand CanacelCmd { get => _canacelCmd = (_canacelCmd = new RelayCommand(CancelClick)); }

        public Action<bool> CloseAction { get;internal set; }

        protected virtual void CancelClick()
        {
            CloseAction?.Invoke(false);
        }
    }
}
