AzureTableArchiver
==================

[![Build and Test](https://github.com/Azure/azure-table-archiver/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/Azure/azure-table-archiver/actions/workflows/build-and-test.yml)
[![AzureTableArchiver on Nuget](https://buildstats.info/nuget/AzureTableArchiver)](https://www.nuget.org/packages/AzureTableArchiver/)


The AzureTableArchiver is intended for creating archives of Azure Storage Tables from Storage Accounts or Cosmos DB and storing them in Azure Storage Blob containers. Because multiple archives may be retained, previous copies of table records are available to restore from the various points of time when archives are created.

Archives are not pruned by this library - it's recommended to enable blob expiration to remove old archives automatically.

Existing records in tables are not removed prior to restore, so records that were added to the tables since the backup was taken will be left in the table.

## Contributing

Please read the [Development Guide](docs/development.md) to understand the project structure and the local development and testing process.

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft  trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).

Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
