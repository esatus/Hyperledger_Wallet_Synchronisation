namespace esatusWallet.Services
{
    // Container class for loading cloud configuration
    class CloudConfig
    {
        public enum Provider
        {
            WebDav,
            GoogleDrive,
            OneDrive
            // ....
        }

        //Selected cloud provider
        public Provider provider;
        //Username on cloud
        public string username;
        //Password on cloud
        public string password;
        //Agent context encryption key
        public string encryptionKey;
        //URI to cloud
        public string uri;
        //Path of the backup directory on the cloud
        public string pathToDir;
    }
}