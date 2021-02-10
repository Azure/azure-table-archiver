namespace AzureTableArchiver

open Microsoft.Azure.Cosmos.Table
open EntitySerialization

module Restore =

    let entityFromJsonFile (partition:string) (rowKey:string) (filePath:string) : Async<DynamicTableEntity> =
        async {
            let! json = System.IO.File.ReadAllTextAsync filePath |> Async.AwaitTask
            return DynamicTableEntity(partition,rowKey).LoadJson json
        }

    let restoreTables (cloudTableClient:CloudTableClient) : RestoreTables =
        fun (filePath:FilePath) ->
            async {
                let tableDirectories = filePath |> System.IO.Directory.GetDirectories
                do! tableDirectories |> Seq.map (fun tableDir ->
                    async {
                        let tableName = System.IO.DirectoryInfo(tableDir).Name
                        let cloudTable = cloudTableClient.GetTableReference tableName
                        do! cloudTable.CreateIfNotExistsAsync () |> Async.AwaitTask |> Async.Ignore
                        let partitionDirectories = tableDir |> System.IO.Directory.GetDirectories
                        for partitionDir in partitionDirectories do
                            let partition = System.IO.DirectoryInfo(partitionDir).Name
                            let rowFiles = partitionDir |> System.IO.Directory.GetFiles
                            do!
                                rowFiles |> Seq.map (fun rowFile ->
                                    async {
                                        let rowKey = System.IO.FileInfo(System.IO.Path.GetFileNameWithoutExtension rowFile).Name
                                        let! entity = entityFromJsonFile partition rowKey rowFile
                                        do! cloudTable.ExecuteAsync(TableOperation.InsertOrReplace entity) |> Async.AwaitTask |> Async.Ignore
                                    }
                                ) |> Async.Parallel |> Async.Ignore // rows in parallel
                    }) |> Async.Parallel |> Async.Ignore // and tables in parallel
            }
