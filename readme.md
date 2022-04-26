# Azure Communication Services Static Web App with Token API Template

This repo shows how you can develop and deploy an [Azure Communication Services](https://azure.microsoft.com/services/communication-services) app with the help of [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps/overview).

## Access Tokens

To work with chat and calling in Azure Communication Services you need to
obtain [user access tokens](https://docs.microsoft.comazure/communication-services/concepts/client-and-server-architecture#user-access-management).
This repo comes with a pre-configured `/api/token` endpoint that you can call
from your web app and get access to calling and chat functionality right away.

## Prerequisites

- [Create an Azure Communication Services resource](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp)

## Local Development

To set up local development follow the steps from the Static Web Apps
[how-to guide](https://docs.microsoft.com/azure/static-web-apps/local-development):

1. Install the CLI.

    ```bash
    npm install -g @azure/static-web-apps-cli azure-functions-core-tools
    ```

1. Get your Azure Communication Services [connection string](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp#access-your-connection-strings-and-service-endpoints)
and add enter it under `COMMUNICATION_SERVICES_CONNECTION_STRING` key in your
[`local.settings.json`](https://docs.microsoft.com/azure/static-web-apps/application-settings) file:

    ```json
    {
        "IsEncrypted": false,
        "Values": {
            "AzureWebJobsStorage": "",
            "FUNCTIONS_WORKER_RUNTIME": "dotnet",
            "COMMUNICATION_SERVICES_CONNECTION_STRING": "<YOUR CONNECTION STRING HERE>"
        }
    }
    ```

1. From the root of your repo start the Azure Static Web Apps CLI.

    ```bash
    swa start src --api-location api
    ```

## Deploying to Azure Static Web Apps

1. Follow the Static Web Apps [quickstart](https://docs.microsoft.com/azure/static-web-apps/get-started-portal?tabs=vanilla-javascript)
to configure and deploy your web app via Azure Portal.

1. Once the app is deployed, use the
[Azure Portal to configure app settings](https://docs.microsoft.com/azure/static-web-apps/application-settings#use-the-azure-portal):
add a new app setting with name = `COMMUNICATION_SERVICES_CONNECTION_STRING`
and value = [your Azure Communication Services connection string](https://docs.microsoft.com/azure/communication-services/quickstarts/create-communication-resource?tabs=windows&pivots=platform-azp#access-your-connection-strings-and-service-endpoints).
