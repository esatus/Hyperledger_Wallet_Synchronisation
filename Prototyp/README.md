# Cloud synchronization for wallets

- Author: Jonas Schneider, esatus AG - j.schneider@esatus.com
- Date: 2021-03-12
- Status: in process

This folder contains a best practice implementaion of the concept.

## Table of Contents <!-- omit in toc -->

- [File description](#File-description)
- [Integration](#Integration)

## File description

The best practice implementaion contains a set of classes and interfaces which are needed for the synchronization. The following part discribes the files.

**CloudSyncService.cs** - The CloudSyncService.cs class is containing the algorithm for the synchronization. For initilize or start the synchronization, there two functions which can be called (see [Integration](#Integration)).

**Interfaces/ISyncService.cs** - Because of the diffrent APIs for all cloud providers, we need an extra implementation for all of them. The ISyncService.cs file contains an interface with all functions which are needed from the CloudSyncService class for the synchronization. The API implementations extend from the interface and have to contain the functions. An implementation of the WebDav API is shown in the Classes/WebdavSyncService.cs file.

**Classes/WebdavSyncService.cs** - This file contains a WebDav API implementation. This can be used to integrate cloud solutions like OwnCloud and Nextcloud.

**Classes/CloudConfig.cs** - Container for the cloud settings.

**Classes/EncryptionService.cs** - Service for string encryption and decryption. Used for encrypting the agent context.

## Integration

For integrating the synchronisation in the wallet implementation, you can use the CloudSyncService class. There are two important functions which can be used.

**Initialize(InitialisationType type)** - This function can be called for initialize the syncronization. The parameter InitialisationType decides wether an other application allready initilized the cloud files or not. An exaple integration of the function is shown in the following code block.

```cs
void SetCloudService(){
  //Gets the instance of the service and start initialization
  CloudSyncService cloudSyncService = Container.Resolve<CloudSyncService>();
  cloudSyncService.Initialize(CloudSyncService.InitialisationType.ExistingCloudSync);
}
```

**Synchronize(SynchrisationType type)** - This function starts a synchronization process. As described in the paper, their are two types of synchronization, at start and when make changes. The parameter can be used to decide which type will be executed. The following to code blocks shows examples of both synchronization types.

```cs
//Synchronisation at application start
public partial class App : Application
{
  // [...]
  public App()
  {
    InitLanguage();
    InitializeComponent();
    RegisterContainer();

    // [...]

    //Gets the instance of the service and starts synchronize process
    CloudSyncService cloudSyncService = Container.Resolve<CloudSyncService>();
    cloudSyncService.Synchronize(CloudSyncService.SynchrizationType.ApplicationStart);

    // [...]
  }
}
```

```cs
//Synchronisation at make changes
public class CustomWalletRecordService : DefaultWalletRecordService, ICusomWalletRecordService
{
   // [...]
  public new virtual async Task AddAsync<T>(Wallet wallet, T record) where T : RecordBase, new()
  {
      //Gets the instance of the service
      CloudSyncService cloudSyncService = App.Container.Resolve<CloudSyncService>();
      //Generates and saves hash value
      cloudSyncService.GenerateTempHash();

      await base.AddAsync<T>(wallet, record);
      
      //Starts synchronize process
      cloudSyncService.Synchronize(CloudSyncService.SynchrizationType.LocalChanges);
  }
  // [...]  
}
```
