namespace AzureTableArchiver

type TableName = string
/// Lists the storage tables in a storage account.
type ListTables = Async<TableName list>

type FilePath = string

/// Dumps a storage table to a list of files.
type DumpTable = TableName * FilePath -> Async<unit>

/// Creates a backup of the tables, generating a FilePath if one is not provided.
type BackupTables = ListTables -> DumpTable -> FilePath option -> Async<FilePath>

/// Restores tables from a backup.
type RestoreTables = FilePath -> Async<unit>

/// Name of the backup.
type BackupName = string

/// The remote and local location for the backup files.
type BackupLocation =
    { BackupPath : string
      BackupContainer : string
      ArchiveName : string }

/// Synchronization failures in synchronizing local backup to/from storage account.
type SynchronizationFailures =
    | FilesFailedToTransfer of NumberOfFilesFailed:int64

/// Sync the remote and local storage the backup, returning the unique backup name.
type SyncBackupToStorage = BackupLocation -> Async<Result<unit,SynchronizationFailures>>

/// The remote and local restore location.
type RestoreLocation =
    { RestorePath : string
      BackupContainer : string
      ArchiveName : string }

/// Sync the remote and local restore location for the backup name.
type SyncStorageToRestore = RestoreLocation -> Async<Result<unit,SynchronizationFailures>>
