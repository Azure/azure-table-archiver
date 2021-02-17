// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
module BackupTests

open System
open System.Reflection
open Expecto
open Microsoft.Azure.Cosmos.Table
open Microsoft.Azure.Storage.Blob
open Microsoft.Azure.Storage.DataMovement
open Moq
open AzureTableArchiver

// TableResultSegment is sealed with internal setters - unmockable - using reflection to make instances for mocks.
let createTableResultSegment (cloudTables:seq<CloudTable>) =
    typeof<TableResultSegment>
        .GetConstructor(BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| typeof<System.Collections.Generic.List<CloudTable>> |], null)
        .Invoke [| ResizeArray cloudTables |]
        :?> TableResultSegment

let setTableResultContinuation (continuationToken:TableContinuationToken) (tableResultSegment:TableResultSegment) =
    typeof<TableResultSegment>
        .GetProperty("ContinuationToken")
        .SetValue(tableResultSegment, continuationToken)
    tableResultSegment

// TableQuerySegment is sealed with internal setters, have to use reflection to make instances for mocks.
let createTableQuerySegment<'TResult> (results:seq<'TResult>) =
    typeof<TableQuerySegment<'TResult>>
        .GetConstructor(BindingFlags.NonPublic ||| BindingFlags.Instance, null, [| typeof<System.Collections.Generic.List<'TResult>> |], null)
        .Invoke [| ResizeArray results |]
        :?> TableQuerySegment<'TResult>

let setTableQuerySegmentContinuation<'TResult> (continuationToken:TableContinuationToken) (tableQuerySegment:TableQuerySegment<'TResult>) =
    typeof<TableQuerySegment<'TResult>>
        .GetProperty("ContinuationToken")
        .SetValue(tableQuerySegment, continuationToken)
    tableQuerySegment

[<Tests>]
let tests =
    testList "Backup" [
        testAsync "List tables with continuation" {
            let tableResultSegmentWithContinuation =
                [ "foo"; "bar"; "baz" ] |> List.map(fun tableName ->
                    CloudTable(Uri $"https://whatever.com/{tableName}")
                )
                |> createTableResultSegment
                |> setTableResultContinuation (TableContinuationToken())
            let tableResultSegmentNoContinuation =
                [ "fizz"; "buzz" ] |> List.map(fun tableName ->
                    CloudTable(Uri $"https://whatever.com/{tableName}")
                )
                |> createTableResultSegment
            let cloudTableClient = Mock<CloudTableClient>(StorageUri (Uri "https://whatever.com"), StorageCredentials())
            cloudTableClient.Setup(fun client -> client.ListTablesSegmentedAsync(It.IsNotNull<TableContinuationToken>()))
                .ReturnsAsync(tableResultSegmentNoContinuation) |> ignore
            cloudTableClient.Setup(fun client -> client.ListTablesSegmentedAsync(null))
                .ReturnsAsync(tableResultSegmentWithContinuation) |> ignore
            let! tables = Backup.listStorageTables (cloudTableClient.Object)
            Expect.sequenceEqual tables ["foo";"bar";"baz";"fizz";"buzz"] "Incorrect tables returned"
        }
        testAsync "Test all this reflection works since it's not mockable" {
            let cloudTableClient = Mock<CloudTableClient>(StorageUri (Uri "https://whatever.com"), StorageCredentials())
            cloudTableClient.Setup(fun client -> client.GetTableReference(It.IsAny()))
                .Returns<string>(fun name ->
                    let cloudTable = Mock<CloudTable>(Uri $"https://wbatever.com/{name}", TableClientConfiguration())
                    cloudTable.Setup(fun table -> table.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<DynamicTableEntity>>(), null))
                        .ReturnsAsync(createTableQuerySegment [DynamicTableEntity("hello", "world")]) |> ignore
                    cloudTable.Object) |> ignore
            let cloudTable = cloudTableClient.Object.GetTableReference "hi"
            Expect.equal cloudTable.Name "hi" "Returned wrong name for cloud table"
            let! result = cloudTable.ExecuteQuerySegmentedAsync(TableQuery<DynamicTableEntity>(), null) |> Async.AwaitTask
            Expect.isNotNull result "Result was null"
            Expect.hasLength result.Results 1 "Expected one result"
            Expect.isNotNull result.Results.[0] "First result is null"
            Expect.equal result.Results.[0].PartitionKey "hello" "Wrong entity partition key"
            Expect.equal result.Results.[0].RowKey "world" "Wrong entity row key"
        }
        testAsync "Dump table to files" {
            let mockTableResults =
                [ "table1" ] |> List.map(fun tableName ->
                    CloudTable(Uri $"https://whatever.com/{tableName}")
                )
                |> createTableResultSegment
            let cloudTableClient = Mock<CloudTableClient>(StorageUri (Uri "https://whatever.com"), StorageCredentials())
            cloudTableClient.Setup(fun client -> client.GetTableReference("table1"))
                .Returns<string>(fun name ->
                    let cloudTable = Mock<CloudTable>(Uri $"https://wbatever.com/{name}", TableClientConfiguration())
                    cloudTable.Setup(fun table -> table.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<DynamicTableEntity>>(), null))
                        .ReturnsAsync(createTableQuerySegment [
                            DynamicTableEntity("one", "thing", "", ["foo", EntityProperty "bar"] |> dict)
                            DynamicTableEntity("one", "more", "", ["foo", EntityProperty "baz"] |> dict)
                            DynamicTableEntity("another", "item", "", ["foo", EntityProperty 13] |> dict)
                        ]) |> ignore
                    cloudTable.Object) |> ignore
            let temporaryTestFiles = IO.Path.Combine(IO.Path.GetTempPath(), Guid.NewGuid().ToString())
            do! Backup.dumpTableToJsonFiles cloudTableClient.Object ("table1", temporaryTestFiles)
            try
                Expect.isTrue (System.IO.Directory.Exists(IO.Path.Combine(temporaryTestFiles, "table1"))) "table1 directory doesn't exist"
                Expect.isTrue (System.IO.Directory.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "one"))) "table1/one directory doesn't exist"
                Expect.isTrue (System.IO.Directory.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "another"))) "table1/another directory doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "one", "thing.json"))) "table1/one/thing.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "one", "more.json"))) "table1/one/more.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "another", "item.json"))) "table1/another/item.json file doesn't exist"
            finally
                IO.Directory.Delete(temporaryTestFiles, true)
        }
        testAsync "Backup multiple tables to directories and files" {
            let mockTableResults =
                [ "table1"; "table2"; "table3" ] |> List.map(fun tableName ->
                    CloudTable(Uri $"https://whatever.com/{tableName}")
                )
                |> createTableResultSegment
            let cloudTableClient = Mock<CloudTableClient>(StorageUri (Uri "https://whatever.com"), StorageCredentials())
            cloudTableClient.Setup(fun client -> client.ListTablesSegmentedAsync(null)) // not testing continuation here.
                .ReturnsAsync(mockTableResults) |> ignore
            cloudTableClient.Setup(fun client -> client.GetTableReference("table1"))
                .Returns<string>(fun name ->
                    let cloudTable = Mock<CloudTable>(Uri $"https://wbatever.com/{name}", TableClientConfiguration())
                    cloudTable.Setup(fun table -> table.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<DynamicTableEntity>>(), null))
                        .ReturnsAsync(createTableQuerySegment [
                            DynamicTableEntity("one", "thing", "", ["foo", EntityProperty "bar"] |> dict)
                            DynamicTableEntity("one", "more", "", ["foo", EntityProperty "baz"] |> dict)
                            DynamicTableEntity("another", "item", "", ["foo", EntityProperty 13] |> dict)
                        ]) |> ignore
                    cloudTable.Object) |> ignore
            cloudTableClient.Setup(fun client -> client.GetTableReference("table2"))
                .Returns<string>(fun name ->
                    let cloudTable = Mock<CloudTable>(Uri $"https://wbatever.com/{name}", TableClientConfiguration())
                    cloudTable.Setup(fun table -> table.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<DynamicTableEntity>>(), null))
                        .ReturnsAsync(createTableQuerySegment [
                            DynamicTableEntity("two", "number7", "", ["seven", EntityProperty 7] |> dict)
                            DynamicTableEntity("two", "number8", "", ["eight", EntityProperty 8.0] |> dict)
                            DynamicTableEntity("another", "number", "", ["nine", EntityProperty "9"] |> dict)
                        ]) |> ignore
                    cloudTable.Object) |> ignore
            cloudTableClient.Setup(fun client -> client.GetTableReference("table3"))
                .Returns<string>(fun name ->
                    let cloudTable = Mock<CloudTable>(Uri $"https://wbatever.com/{name}", TableClientConfiguration())
                    cloudTable.Setup(fun table -> table.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<DynamicTableEntity>>(), null))
                        .ReturnsAsync(createTableQuerySegment [
                            DynamicTableEntity("three", "things", "", ["foo", EntityProperty "bar"] |> dict)
                            DynamicTableEntity("three", "items", "", ["foo", EntityProperty "baz"] |> dict)
                        ]) |> ignore
                    cloudTable.Object) |> ignore
            let! temporaryTestFiles = Backup.backupTables (Backup.listStorageTables cloudTableClient.Object) (Backup.dumpTableToJsonFiles cloudTableClient.Object) Backup.GeneratedBackupPath
            try
                Expect.isTrue (System.IO.Directory.Exists(IO.Path.Combine(temporaryTestFiles, "table1"))) "table1 directory doesn't exist"
                Expect.isTrue (System.IO.Directory.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "one"))) "table1/one directory doesn't exist"
                Expect.isTrue (System.IO.Directory.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "another"))) "table1/another directory doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "one", "thing.json"))) "table1/one/thing.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "one", "more.json"))) "table1/one/more.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table1", "another", "item.json"))) "table1/another/item.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table2", "two", "number7.json"))) "table2/two/number7.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table2", "two", "number8.json"))) "table2/two/number8.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table2", "another", "number.json"))) "table2/another/number.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table3", "three", "things.json"))) "table3/three/things.json file doesn't exist"
                Expect.isTrue (System.IO.File.Exists(IO.Path.Combine(temporaryTestFiles, "table3", "three", "items.json"))) "table3/three/items.json file doesn't exist"
            finally
                IO.Directory.Delete(temporaryTestFiles, true)
        }
    ]
