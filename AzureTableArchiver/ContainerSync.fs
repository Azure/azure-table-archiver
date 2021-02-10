namespace AzureTableArchiver

open System
open Microsoft.Azure.Storage.Blob
open Microsoft.Azure.Storage.DataMovement

module ContainerSync =

    /// Synchronize a local archive directory with a storage account container directory.
    let syncToContainer (blobClient:CloudBlobClient) (reportProgress:Progress<TransferStatus> option) : SyncBackupToStorage =
        fun backupLocation ->
        async {
            let container = blobClient.GetContainerReference backupLocation.BackupContainer
            let! _ = container.CreateIfNotExistsAsync () |> Async.AwaitTask
            let blobDirectoryName = backupLocation.ArchiveName
            let blobDirectory = container.GetDirectoryReference blobDirectoryName
            let transferCheckpoint = Unchecked.defaultof<TransferCheckpoint>
            let context = DirectoryTransferContext(transferCheckpoint)
            reportProgress |> Option.iter (fun progress -> context.ProgressHandler <- progress)
            let options = UploadDirectoryOptions(Recursive = true)
            let! transferStatus = TransferManager.UploadDirectoryAsync (backupLocation.BackupPath, blobDirectory, options, context) |> Async.AwaitTask
            if transferStatus.NumberOfFilesFailed > 0L then
                return Result.Error (FilesFailedToTransfer transferStatus.NumberOfFilesFailed)
            else
                return Ok ()
        }

    /// Synchronize a storage account container directory with a local directory for restoring an archive.
    let syncFromContainer (blobClient:CloudBlobClient) (reportProgress:Progress<TransferStatus> option) : SyncStorageToRestore =
        fun (restoreLocation:RestoreLocation) ->
            async {
                let container = blobClient.GetContainerReference restoreLocation.BackupContainer
                do! container.CreateIfNotExistsAsync () |> Async.AwaitTask |> Async.Ignore
                let blobDirectory = container.GetDirectoryReference restoreLocation.ArchiveName
                let transferCheckpoint = Unchecked.defaultof<TransferCheckpoint>
                let context = DirectoryTransferContext(transferCheckpoint)
                reportProgress |> Option.iter (fun progress -> context.ProgressHandler <- progress)
                let options = DownloadDirectoryOptions(Recursive = true)
                let! transferStatus = TransferManager.DownloadDirectoryAsync (blobDirectory, restoreLocation.RestorePath, options, context) |> Async.AwaitTask
                if transferStatus.NumberOfFilesFailed > 0L then
                    return Result.Error (FilesFailedToTransfer transferStatus.NumberOfFilesFailed)
                else
                    return Result.Ok ()
            }
