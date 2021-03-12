using System;
using System.Collections.Generic;
using System.IO;

namespace esatusWallet.Services
{
    //Synchronisation interface
    internal interface ISyncService
    {
        public CloudConfig CloudConfig { get; set; }

        //Handles hash list
        public List<string> GetHashList();
        public bool CreateOrUpdateHashList(List<string> hashList);
        //Handles sqlite.db file
        public Byte[] GetCloudVersion();
        public bool CreateOrUpdateCloudVersion(Stream stream);
        //Handles agent context
        public string GetEncryptedAgentOptions();
        public bool CreateOrUpdateEncryptedAgentOptions(string optionsJSON);
    }
}