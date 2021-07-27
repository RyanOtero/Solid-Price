﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Solidworks_Cutlist_Generator.ViewModels {
    class SourceBrowseCommand : ICommand {

        MainWindowViewModel ViewModel { get; set; }

        public event EventHandler CanExecuteChanged;

        public SourceBrowseCommand(MainWindowViewModel viewModel) {
            ViewModel = viewModel;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            ViewModel.SourceBrowse();
        }
    }
}