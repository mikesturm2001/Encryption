﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Encryption.ViewModel.Commands
{
    internal class CreateAsmKeysCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly EncryptionViewModel _encryptionViewModel;

        public CreateAsmKeysCommand(EncryptionViewModel encryptionViewModel)
        {
            _encryptionViewModel = encryptionViewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if(_encryptionViewModel != null)
            {
                _encryptionViewModel.CreateAsmKeys();
            }
        }
    }
}
