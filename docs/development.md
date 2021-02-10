Development Guide
========

The AzureTableArchiver is broken into several modules.

### Domain.fs
The Domain module contains general type definitions for both structure and behavior.
These provide a general API surface for testing and help define the purpose of the
implementations.

### EntitySerialization.fs
THe EntitySerialization module handles the logic of processing table records generically
as DynamicTableEntity objects. These are converted to and from JSON for persisting to
the local file system and blob storage.

_Currently, etags are not stored. This may be a useful enhancement to prevent restoring
over newer data._

### Backup.fs
The Backup module queries table records, converts results to JSON, and stores them in
the local file system.

### ContainerSync.fs
The ContainerSync module synchronizes the local directory to and from blob storage. It
relies on the Data Movement library for efficient and reliable transfer.

### Restore.fs
The Restore module deserializes JSON records from the file system and uploads them to
tables in the target storage account. It uses InsertOrReplace to create or replace 
records in the table.

Building
--------

Prerequisites - dotnet 5.0.

If you have `make` installed, common build and test scenarios are covered in the file.

* `make build` - restores dependencies and builds
* `make test` - restore dependencies, builds, and runs tests
* `make check` - typically run after `make build`, this will run tests without building again
* `make clean` - removes any existing restore or build artifacts
* `make all` - runs `build` and then `check`

Alternatively, use the standard `dotnet build` and `dotnet test` commands at the 
solution level.

Testing
-------

Testing uses `Expecto` for F# friendly assertions, parallel execution, and native
`async` support. The Azure client library calls are mocked using `Moq` so application
logic can be tested without requiring interaction with a storage account. This is not 
always easy, as some Azure SDK's have sealed classes with internal constructors and 
require reflection to fully mock.
