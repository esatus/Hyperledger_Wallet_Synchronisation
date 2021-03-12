using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WebDav;

namespace esatusWallet.Services
{
    //Implementation of the Webdav protocol, extends the ISyncService interface
    class WebdavSyncService : ISyncService
    {
        //Saves the configurations
        public CloudConfig _cloudConfig;

        public CloudConfig CloudConfig { get { return _cloudConfig; } set { _cloudConfig = value; } }


        //Constructor
        public WebdavSyncService(CloudConfig cloudConfig)
        {
            _cloudConfig = cloudConfig;
        }

        //Initializes the web client
        private WebDavClient GetWebClient()
        {
            var clientParams = new WebDavClientParams
            {
                BaseAddress = new Uri(_cloudConfig.uri),
                Credentials = new NetworkCredential(_cloudConfig.username, _cloudConfig.password)
            };
            return new WebDavClient(clientParams);
        }

        //Gets the hash list from cloud
        public List<string> GetHashList()
        {
            WebDavClient client = GetWebClient();

            //Requests the hash list file from the cloud 
            Task<WebDavStreamResponse> t = client.GetRawFile(_cloudConfig.pathToDir + "Hashlist.txt");
            t.Wait();
            WebDavStreamResponse result = t.Result;

            if (result.IsSuccessful)
            {
                using (StreamReader reader = new StreamReader(result.Stream))
                {
                    //Serializes the response string to a list
                    string text = reader.ReadToEnd();
                    return new List<string>(text.Split("\n")).Where(s => !String.IsNullOrEmpty(s)).ToList(); ;
                }
            }
            else
            {
                // Error handling
                return null;
            }
        }

        public bool CreateOrUpdateHashList(List<string> hashList)
        {
            WebDavClient client = GetWebClient();

            // Generation string from given hash list seperated by new line
            string str = "";
            foreach (string hash in hashList)
            {
                str += hash + "\n";
            }

            //Uploads the string
            Stream stream = GenerateStreamFromString(str);
            Task<WebDavResponse> t = client.PutFile(_cloudConfig.pathToDir + "Hashlist.txt", stream);
            t.Wait();

            WebDavResponse result = t.Result;

            if (result.IsSuccessful)
            {
                return true;
            }
            else
            {
                // handle Errors
                return false;
            }
        }

        //Generates a stream for the given string
        private static Stream GenerateStreamFromString(string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        //Upload sqlite.db file
        public bool CreateOrUpdateCloudVersion(Stream stream)
        {
            WebDavClient client = GetWebClient();

            //Put the file on the cloud
            Task<WebDavResponse> t = client.PutFile(_cloudConfig.pathToDir + "sqlite.db", stream);
            t.Wait();
            WebDavResponse result = t.Result;

            if (result.IsSuccessful)
            {
                return true;
            }
            else
            {
                // handle Errors
                return false;
            }
        }

        //Gets the database file from cloud
        public Byte[] GetCloudVersion()
        {
            WebDavClient client = GetWebClient();

            //Requests the database file from the cloud 
            Task<WebDavStreamResponse> t = client.GetRawFile(_cloudConfig.pathToDir + "sqlite.db");
            t.Wait();
            WebDavStreamResponse result = t.Result;

            if (result.IsSuccessful)
            {
                using (var memoryStream = new MemoryStream())
                {
                    //Serializes the response stream to byte array
                    result.Stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            else
            {
                // Error handling
                return null;
            }
        }

        //Upload agent context file
        public bool CreateOrUpdateEncryptedAgentOptions(String optionsJSON)
        {
            WebDavClient client = GetWebClient();

            //Put the json on the cloud
            Stream stream = GenerateStreamFromString(optionsJSON);
            Task<WebDavResponse> t = client.PutFile(_cloudConfig.pathToDir + "AgentOptions.json", stream);
            t.Wait();
            WebDavResponse result = t.Result;

            if (result.IsSuccessful)
            {
                return true;
            }
            else
            {
                // handle errors
                return false;
            }
        }

        //Gets the agent context from cloud
        public string GetEncryptedAgentOptions()
        {
            WebDavClient client = GetWebClient();

            //Requests the agent context from the cloud 
            Task<WebDavStreamResponse> t = client.GetRawFile(_cloudConfig.pathToDir + "AgentOptions.json");
            t.Wait();
            WebDavStreamResponse result = t.Result;

            if (result.IsSuccessful)
            {
                using (StreamReader reader = new StreamReader(result.Stream))
                {
                    //Serializes the response stream to string
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
            else
            {
                // Error handling
                return null;
            }
        }
    }
}