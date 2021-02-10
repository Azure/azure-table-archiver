namespace AzureTableArchiver

open System
open System.IO
open Microsoft.Azure.Cosmos.Table
open EntitySerialization

module Backup =

    /// Use a generated filepath.
    let GeneratedBackupPath = Option<FilePath>.None

    let backupTables : BackupTables =
        fun listTables dumpTable backupPath ->
            async {
                let! tables = listTables
                let backupPath = backupPath |> Option.defaultValue (Path.Combine (Path.GetTempPath (), Guid.NewGuid().ToString()))
                do! tables
                    |> Seq.map (fun table -> (table, backupPath) |> dumpTable)
                    |> Async.Parallel // Download and write backups of all tables in parallel
                    |> Async.Ignore
                return backupPath
            }

    let listStorageTables (cloudTableClient:CloudTableClient) : ListTables =
        async {
            let rec getTables (continuationToken) (acc:CloudTable list) =
                async {
                    let! tablesResult = cloudTableClient.ListTablesSegmentedAsync(continuationToken) |> Async.AwaitTask
                    if isNull tablesResult.ContinuationToken then
                        return acc @ (List.ofSeq tablesResult.Results)
                    else
                        return! getTables tablesResult.ContinuationToken (acc @ List.ofSeq tablesResult.Results)
                }
            let! allTables = getTables null []
            return allTables |> List.map (fun t -> t.Name)
        }

    let dumpTableToJsonFiles (cloudTableClient:CloudTableClient) : DumpTable =
        fun (tableName, filePath) ->
            async {
                let cloudTable = cloudTableClient.GetTableReference tableName
                let rec queryAndWrite (continuationToken:TableContinuationToken) =
                    async {
                        let! queryResult = cloudTable.ExecuteQuerySegmentedAsync(TableQuery<DynamicTableEntity>(), continuationToken) |> Async.AwaitTask
                        for entity in queryResult.Results do
                            let filename = System.IO.Path.Combine(filePath, tableName, entity.PartitionKey, $"{entity.RowKey}.json")
                            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(filePath, tableName, entity.PartitionKey)) |> ignore
                            do! System.IO.File.WriteAllTextAsync(filename, entity.ToJson()) |> Async.AwaitTask
                        if not <| isNull queryResult.ContinuationToken then
                            do! queryAndWrite queryResult.ContinuationToken
                    }
                do! queryAndWrite null
            }

    /// Backs tables up to a generated directory in the temporary folder.
    let BackupTables (tableClient:CloudTableClient) =
        backupTables (listStorageTables tableClient) (dumpTableToJsonFiles tableClient) GeneratedBackupPath

