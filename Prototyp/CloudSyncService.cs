using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Collections.Generic;
using esatusWallet.Models;
using System.Text.Json;
using esatusWallet.Agent;
using esatusWallet.Agent.Interface;

namespace esatusWallet.Services
{
    public class CloudSyncService
    {
        private bool _isActive;

        //Used interfaces
        private ISyncService _syncService;
        private IAgentContextProvider _agentContextProvider;

        //Path to the sqlite.db file
        private string _path = Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "esatus", "sqlite.db");

        //Enumerator for synchronisation type and Initialisation type
        public enum SynchrisationType { ApplicationStart, LocalChanges }
        public enum InitialisationType { NewCloudSync, ExistingCloudSync }

        //Constructor
        public CloudSyncService(IAgentContextProvider agentContextProvider)
        {
            _agentContextProvider = agentContextProvider;

            CloudConfig cc = InitConfig();

            if (_isActive)
            {
                switch (cc.provider)
                {
                    case CloudConfig.Provider.WebDav:
                        _syncService = new WebdavSyncService(cc);
                        break;
                    case CloudConfig.Provider.GoogleDrive:
                        //ToDo
                        break;
                    case CloudConfig.Provider.OneDrive:
                        //ToDo
                        break;
                    //...
                    default:
                        break;
                }
            }       
        }

        //Sets the cloud configuration
        private CloudConfig InitConfig()
        {
            //ToDo: Load saved configuration
            _isActive = true;

            if (_isActive)
            {
                CloudConfig cloudConfig = new CloudConfig();
                cloudConfig.provider = CloudConfig.Provider.WebDav;
                cloudConfig.uri = "https://example.org/webdav/remote.php/";
                cloudConfig.username = "Username";
                cloudConfig.password = "Password";
                cloudConfig.pathToDir = "Path/To/Dir";
                cloudConfig.encryptionKey = "SecretKeyForAgentContext";

                return cloudConfig;
            }

            return null;
        }

        //Initializes the cloud synchronisation depandant on the given type
        public void Initialize(InitialisationType type)
        {
            //Close the wallet
            ((AgentContextProvider)_agentContextProvider).CloseActiveWallet();

            if (type == InitialisationType.NewCloudSync)
            {
                InitializeNewCloudSynchronisation();
            }
            else if (type == InitialisationType.ExistingCloudSync)
            {
                InitializeExistingSynchronisation();
            }
        }
        
        //Initializes a new cloud synchronisation
        private void InitializeNewCloudSynchronisation()
        {
            //Get agent context
            AgentOptions options = _agentContextProvider.GetActiveAgentOptions();

            //Generate hash value and save in list
            string currentHash = GenerateSHA256ForFile();
            List<string> hashList = new List<string>();
            hashList.Add(currentHash);

            bool successful = false;
            if (UpdateCloudDBFile()) //Upload database file
            {              
                if (_syncService.CreateOrUpdateHashList(hashList)) //Upload hash list
                {
                    string optionString = JsonSerializer.Serialize(options, options.GetType());
                    string encOptionString = EncryptionService.Encrypt(optionString, _syncService.CloudConfig.encryptionKey);

                    if (_syncService.CreateOrUpdateEncryptedAgentOptions(encOptionString)) //Upload agent context
                    {
                        successful = false;
                    }
                }
            }

            if (!successful) { /* Error handling */}
        }

        //Initializes an existing synchronisation
        private void InitializeExistingSynchronisation()
        {
            //Create directory for the path, if the database does not exist
            FileInfo fInfo = new FileInfo(_path);
            if (!fInfo.Exists)
            {
                CreateDirectory(fInfo.Directory);
            }

            UpdateLocalDBFile(); // Update local database file

            // Get and save the agent context
            string decEncOptionJson = _syncService.GetEncryptedAgentOptions();
            string optionString = EncryptionService.Decrypt(decEncOptionJson, _syncService.CloudConfig.encryptionKey);

            SaveAgentOptions(optionString);
        }

        //Saves the given agent context
        private void SaveAgentOptions(string encOptionJson)
        {
            //ToDo: Decrypt
            AgentOptions options = JsonSerializer.Deserialize<AgentOptions>(encOptionJson);
            AgentOptions currentOptions = _agentContextProvider.GetActiveAgentOptions();

            currentOptions.WalletCredentials.Key = options.WalletCredentials.Key;

            currentOptions.EndpointUri = options.EndpointUri;
            currentOptions.AgentKey = options.AgentKey;

            Task<bool> t = ((AgentContextProvider)_agentContextProvider).UpdateAgentOptionsAsync(options);
            t.Wait();
        }

        //Creates the directories recursive
        private void CreateDirectory(DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.Parent.Exists)
            {
                CreateDirectory(directoryInfo.Parent);
            }
            directoryInfo.Create();
        }

        //Synchronizes the wallet depandant on the given type
        public void Synchronize(SynchrisationType type)
        {
            ((AgentContextProvider)_agentContextProvider).CloseActiveWallet();

            if (type == SynchrisationType.ApplicationStart)
            {
                SynchonizeAtStart();
            }
            else if (type == SynchrisationType.LocalChanges)
            {
                SynchonizeLocalChanges();
            }
        }

        //Synchronizes the wallet at application start
        private void SynchonizeAtStart() 
        { 
            if (_isActive)
            {   
                //Generate hash value
                string currentHash = GenerateSHA256ForFile();
                //Load hash list from cloud
                List<string> hashList = _syncService.GetHashList();

                //Check if there are no unsynchronized local changes
                if (hashList.Contains(currentHash))
                {
                    //Check if there are no unsynchronized changes on cloud
                    if (hashList.Last() == currentHash)  
                    {
                        //No changes
                        return; 
                    }
                    else
                    {
                        // Update local database file
                        UpdateLocalDBFile(); 
                    }
                }
                else 
                {
                    //ToDo: Consolidate local and cloud changes
                    //Upload new database file and add hash value to list
                    if (UpdateCloudDBFile())
                    {
                        hashList.Add(currentHash);
                        _syncService.CreateOrUpdateHashList(hashList);
                    }
                    else { /* Error handling */ }
                }
            }
        }

        //Synchronizes the wallet when changes are made
        private void SynchonizeLocalChanges()
        {
            if (_isActive)
            {
                //Load hash list from cloud
                List<string> hashList = _syncService.GetHashList();

                //Check if there are unsyncronized changes
                if (hashList.Last() != _hashBeforChanges)
                {
                    //ToDo: Consolidate local and cloud changes
                }

                //Generate new hash value
                string currentHash = GenerateSHA256ForFile();

                //Upload new database file and add hash value to list
                if (UpdateCloudDBFile())
                {
                    hashList.Add(currentHash);
                    _syncService.CreateOrUpdateHashList(hashList);
                }
                else { /* Error handling */ }
            }
        }

        //Generates hash value of the database file
        private string GenerateSHA256ForFile()
        {
            FileInfo fInfo = new FileInfo(_path);

            using (Stream fileStream = fInfo.OpenRead())
            {
                fileStream.Position = 0;

                using (SHA256 mySHA256 = SHA256.Create())
                {
                    byte[] hashValue = mySHA256.ComputeHash(fileStream);
                    return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
                }
            }
        }

        //Uploads the local database file using the synchronisation interface
        private bool UpdateCloudDBFile()
        {
            FileInfo fileInfo = new FileInfo(_path);
            using (Stream fileStream = fileInfo.OpenRead())
            {
                fileStream.Position = 0;
                return _syncService.CreateOrUpdateCloudVersion(fileStream);
            }
        }

        //Updates the local database file using the synchronisation interface
        private bool UpdateLocalDBFile()
        {
            FileInfo fileInfo = new FileInfo(_path);
            using (Stream fileStreamWrite = fileInfo.OpenWrite())
            {
                Byte[] bytes = _syncService.GetCloudVersion();
                fileStreamWrite.Write(bytes);
            }
            return true;
        }

        //Generates and saves a hash value befor changes are made
        //_hashBeforChanges are used for SynchonizeLocalChanges()
        private string _hashBeforChanges;
        public void GenerateTempHash()
        {
            if (_isActive)
            {
                _hashBeforChanges = GenerateSHA256ForFile();
            }
        }
    }
}
