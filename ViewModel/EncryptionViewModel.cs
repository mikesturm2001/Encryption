using Encryption.ViewModel.Commands;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Encryption.ViewModel
{
    public class EncryptionViewModel : INotifyPropertyChanged
    {
        //Declare CsParmeters and RsaCryptoServicePRovider objects with global scope
        CspParameters cspp = new CspParameters();
        RSACryptoServiceProvider rsa;

        //Path variables for source, encryption and decryption folders. Must end with a backslash.
        const string EncryptionFolder = @"c:\Encrypt\";
        const string DecryptionFolder = @"c:\Decryption\";
        const string SrcFolder = @"c:\doc";

        // Public key File
        const string PubKeyFile = @"c:\Encrypt\rsaPublicKey.txt";

        //Key container name for private/public key value pair
        const string keyName = "Key01";

        //String for label text
        string labelText;

        public string LabelText
        {
            get { return labelText; }
            set
            {
                labelText = value;
                OnPropertyChanged("LabelText");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ICommand decryptCommand { get; private set; }
        public ICommand encryptCommand { get; private set; }
        public ICommand createAsmKeyCommand { get; private set; }
        public ICommand exportPublicKeyCommand { get; private set; }
        public ICommand getPrivateKeyCommand { get; private set; }
        public ICommand importPublicKeyCommand { get; private set; }

        public EncryptionViewModel()
        {
            ShowPopUp = new RelayCommand(() => ShowPopUpExecute(), () => true);

            decryptCommand = new DecryptCommand(this);
            encryptCommand = new EncryptCommand(this);
            createAsmKeyCommand = new CreateAsmKeysCommand(this);
            exportPublicKeyCommand = new ExportPublicKeyCommand(this);
            getPrivateKeyCommand = new GetPrivateKeyCommand(this);
            importPublicKeyCommand = new ImportPublicKeyCommand(this);

            LabelText = "Welcome";
        }

        public ICommand ShowPopUp { get; private set; }

        private void ShowPopUpExecute()
        {
            MessageBox.Show("Hello!");
        }

        //This task creates an asymetric key that encrypts and decrypts the RijndaelManager Key.
        public void CreateAsmKeys()
        {
            //Stores a key pair in the key container.
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)
                LabelText = string.Format("Key: {0} - Public Only", cspp.KeyContainerName);
            else
                LabelText = string.Format("Key: {0} - Full Key Pair", cspp.KeyContainerName);

        }

        //Encrypting a File
        public void EncryptFileCommand()
        {
            if (rsa == null)
                MessageBox.Show("Key not set.");
            else
            {
                //Display a dialog box to select a file to encrypt.
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.InitialDirectory = SrcFolder;
                if (ofd.ShowDialog() == true)
                {

                    string fileName = ofd.FileName;
                    if (fileName != null)
                    {
                        FileInfo fileInfo = new FileInfo(fileName);
                        //Pass the file name without the path.
                        string name = fileInfo.FullName;
                        EncryptFile(name);
                    }
                }
            }
        }

        //Encrypt the actul file
        private void EncryptFile(string inFile)
        {
            //Create instance of Rijndael for symetric encryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;
            ICryptoTransform transform = rjndl.CreateEncryptor();

            //Use RSACryptoServiceProvider to encrypt the Rijndael key.
            byte[] keyEncrypted = rsa.Encrypt(rjndl.Key, false);

            //Create byte arrays to contain th length values of the Key and IV.
            byte[] Lenk = new byte[4];
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            Lenk = BitConverter.GetBytes(lKey);
            int lIV = rjndl.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            //Write the following to the FileStream for the encrypted file (outFs):
            // - length of the key
            // - length of the IV
            // - encrypted key
            // - the IV
            // - the encrypted cipher content

            int startFileName = inFile.LastIndexOf("\\") + 1;
            //Change the file's extension to ".enc"
            string outFile = EncryptionFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".enc";

            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {
                outFs.Write(Lenk, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(keyEncrypted, 0, lKey);
                outFs.Write(rjndl.IV, 0, lIV);

                //now write the cipher text using a cryptostream for encrypting
                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {
                    //By encrypting a chunk at a time you can save memory
                    int count = 0;
                    int offset = 0;

                    //blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    using (FileStream inFs = new FileStream(inFile, FileMode.Open))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outFs.Close();
            }
        }

        //Decypting the file
        public void DecryptFileCommand()
        {
            if (rsa == null)
                MessageBox.Show("Key not set.");
            else
            {
                //Display a dialog box to select the encrypted file.
                Microsoft.Win32.OpenFileDialog ofd2 = new Microsoft.Win32.OpenFileDialog();
                ofd2.InitialDirectory = EncryptionFolder;

                if (ofd2.ShowDialog() == true)
                {
                    string fName = ofd2.FileName;
                    if (fName != null)
                    {
                        FileInfo fi = new FileInfo(fName);
                        string name = fi.Name;
                        DecryptFile(name);
                    }
                }
            }
        }

        private void DecryptFile(string inFile)
        {
            // Create instance of Rijndael for symetric decryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;

            //Create byte arrays to get the length of the encrypted key and IV.
            //These values were stored as 4 bytes each at the beginning of the encrypted package.
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            //Construct the file name for the decrypted file.
            string outFile = DecryptionFolder + inFile.Substring(0, inFile.LastIndexOf(".")) + ".txt";

            // Use FileStream objects to read the encrypted file and save the decryted file
            using (FileStream inFs = new FileStream(EncryptionFolder + inFile, FileMode.Open))
            {
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                //Convert the lengths to integer values.
                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                //Determine the start position of the cipher text and langeth
                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                //create the byte array for the encrypted Rijndael key, IV and cipher text.
                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                //Extract the key and IV starting from index 9 after the length values.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);
                Directory.CreateDirectory(DecryptionFolder);

                //Use RSACryptoServiceProvider to decrypt the Rijnadel key
                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                //Decrypt the key.
                ICryptoTransform transformation = rjndl.CreateDecryptor(KeyDecrypted, IV);

                //Decrypt the cipher text from the FileStream of the encrypted file.
                using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                {
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];


                    // By decrypting a chunk a time you can save time and memory

                    // Start at the beginning of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transformation, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);

                        }
                        while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();
                        outStreamDecrypted.Close();
                    }
                    outFs.Close();
                }
                inFs.Close();
            }

        }

        public void ExportPublicKey()
        {
            //Save the public key created by the RSA to a file. saving the key to disk is a risk
            Directory.CreateDirectory(EncryptionFolder);
            StreamWriter sw = new StreamWriter(PubKeyFile, false);
            sw.Write(rsa.ToXmlString(false));
            sw.Close();
        }

        public void ImportPublicKey()
        {
            StreamReader sr = new StreamReader(PubKeyFile);
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            string keytxt = sr.ReadToEnd();
            rsa.FromXmlString(keytxt);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)
                LabelText = string.Format("Key: {0} - Public Only", cspp.KeyContainerName);
            else
                LabelText = string.Format("Key: {0} - Full Key Pair", cspp.KeyContainerName);
            sr.Close();
        }

        public void GetPrivateKey()
        {
            cspp.KeyContainerName = keyName;

            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;

            if (rsa.PublicOnly == true)
                LabelText = string.Format("Key: {0} - Public Only", cspp.KeyContainerName);
            else
                LabelText = string.Format("Key: {0} - Full Key Pair", cspp.KeyContainerName);

        }
    }
}
