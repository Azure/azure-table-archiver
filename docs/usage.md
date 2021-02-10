Usage
=========

Backup and restore of tables and interaction with blob containers leverages the 
`CloudTableClient` and `CloudBlobClient` instances created by the application
hosting the library. The responsibility of authentication with the table and blob
services is handled by the hosting application.

Once the application creates clients for the table and blob accounts, a backup of
the tables can be created on the local file system where the backup to a directory
which can then by transferred to the blob storage for archival.

### Backup and Archive Tables
This snippet creates a backup of tables to a local directory and then copies that
directory to a blob container for archival.

```fsharp
let tableClient = CloudStorageAccount.Parse(sourceConnString).CreateCloudTableClient()
let archiveBlobClient = Microsoft.Azure.Storage.CloudStorageAccount.Parse(archiveBlobsConnString).CreateCloudBlobClient()

// Createa a backup to a local temporary directory
let! backupPath = Backup.BackupTables tableClient

// Archive the backup to a blob storage container

// First give it a name based on your own conventions. A timestamp is a good choice.
let archiveName = DateTimeOffset.UtcNow.ToString "O"
// Specify the name of the container for storing the backups, the local path to the backup,
// and the name of the archive, which becomes the directory in the container.
let backupLocation = {
    BackupContainer="tablebackups"
    BackupPath=backupPath
    ArchiveName=archiveName }

// You may also want to report progress of the transfer.
let reportProgress = Some (Progress<TransferStatus>(fun progress -> Console.WriteLine ($"Transferred: {progress.BytesTransferred}")))

// Finally begin the sync and transfer the table backup to the blob for archival.
match! ContainerSync.syncToContainer archiveBlobClient reportProgress backupLocation with
| Error err ->
    Console.Error.WriteLine $"Some files failed to transfer {err}"
| Ok backupName ->
    Console.WriteLine $"Syncronized {backupName} to archive {archiveName}"
    System.IO.Directory.Delete(backupPath, true)
```

### Transfer Table Data to Another Account
This snippet downloads table data from one account and uploads it to another. This can 
be used to move a set of tables between two accounts or from a Storage Account to
CosmosDB.

```fsharp
let sourceTableClient = CloudStorageAccount.Parse(sourceConnString).CreateCloudTableClient()
let targetTableClient = CloudStorageAccount.Parse(targetConnString).CreateCloudTableClient()

// Backup to a local temporary directory,
let! backupPath = Backup.BackupTables sourceTableClient

// Restore to the target tables account.
do! Restore.restoreTables targetTableClient backupPath
Console.WriteLine "Tables restored."
```
