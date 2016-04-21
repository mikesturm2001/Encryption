using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Encryption.ViewModel.Commands
{
    internal class DecryptCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly EncryptionViewModel _encryptionViewModel;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public DecryptCommand(EncryptionViewModel encryptionViewModel)
        {
            _encryptionViewModel = encryptionViewModel;
        }

        public void Execute(object parameter)
        {
            if (_encryptionViewModel != null)
            {
                _encryptionViewModel.DecryptFileCommand();
            }
        }
    }
}
