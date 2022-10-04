###########################################################################
#
# Use this script to test locally
#
###########################################################################

# Use the 'Throw-WhenError' function to get a quick feedback on what went wrong.
function Throw-WhenError {
  param (
    [string]
    $msg
  )

  if ($LastExitCode -gt 0) {
    Write-Error $msg
    throw
  }
}

Write-Host "Initialize local deployment" -ForegroundColor Blue

# Login and set the sub to the one we want to use from Azure Portal
#az logout
#az login #--allow-no-subscriptions

# I'm pointint to the right subscription. In this event this might be redundant, but when you have
# many subscriptions then this is needed or else az won't know which one to choose.
az account set --subscription "d6b4bc51-75a6-4eb4-8cf2-4114beceec76"  

# This will output the account we are on.
az account show

# From now on we define and create our ressources

# The resource group is already created by Delegate, 
# but if we wanted to create it from scratch, we would do something like this

#### Example of how to create a resource group via az cli:

# Write-Host "Deploying resource group" -ForegroundColor Yellow

# $output = az group create `
# --name 'rg_ramtin' `
# --location 'westeurope
# Throw-WhenError -output $output

# Note: use backticks ---> ` <---- to change line

## In your first az cli script, you need to create the following:
# - Function App
# - Storage Account
# - App Service Plan
# Please note that the order matters as the commands are run sequentially...

# You can find the documentation for the az cli commands here:
# https://learn.microsoft.com/en-us/cli/azure/reference-index?view=azure-cli-latest

# Prerequisite
# AZ CLI is installed
# When deploying the script with Powershell you need to enter the following command in order to be able to run the script: 'Set-ExecutionPolicy -Scope Process -ExecutionPolicy  ByPass'
# When the script has run, then go to the portal and check inside your resource group, that the deployment has been successful.
$resourceGroup = 'rg_ramtin'
$storageAccountName = 'ramtinstorageacc'
$appServicePlanName = 'ramtinAppServicePlan'
$functionappName = 'ramtinFuncApp111'
$webappName = 'ramtinWebApp111'
$cosmosNamespace = 'ramtincosmosnameSpace'

# If you don't have the latest az cli, then az functionapp step might fail.
# If so, then please remove the # from the next line.
# When you deploy this step, it will upgrade, and that might take 3-5 minutes...
#az upgrade

Write-Host "###### Creating Storage Account..." -ForegroundColor Blue

# Create Storage account
$output = az storage account create `
  --name $storageAccountName `
  --resource-group $resourceGroup `
  --sku 'Standard_LRS' `
  --kind 'StorageV2'

Throw-WhenError -output $output

# Create container
$output = az storage container create `
  --name 'images' `
  --account-name $storageAccountName `
  --resource-group $resourceGroup

Throw-WhenError -output $output

Write-Host "###### Creating App Service Plan..." -ForegroundColor Blue

# Create App Service Plan 
$appServicePlanId = az appservice plan create `
  --name $appServicePlanName `
  --sku B1 `
  --resource-group $resourceGroup `
  --query id
	
Throw-WhenError -output $appServicePlanId

Write-Host "###### Creating Function App..." -ForegroundColor Blue

# Create function
$output = az functionapp create `
  --name $functionappName `
  --storage $storageAccountName `
  --plan $appServicePlanId `
  --resource-group $resourceGroup `
  --functions-version 3

Throw-WhenError -output $output

Write-Host "###### Creating Web App..." -ForegroundColor Blue

# Create WebApp
$output = az webapp create `
  --name $webappName `
  --resource-group $resourceGroup `
  --plan $appServicePlanId

Throw-WhenError -output $output

# Create Cosmos Namespace
$cosmosAccountResult = az cosmosdb check-name-exists --name $cosmosNamespace

Write-Host "###### Checking if CosmosDB namespace exists..." -ForegroundColor Blue

if($cosmosAccountResult -ne 'true')
{
    Write-Host "###### No CosmosDB namespace exists. Creating a new one..." -ForegroundColor Blue
    az cosmosdb create --name $cosmosNamespace --resource-group $resourceGroup
}

Write-Host "###### CosmosDB namespace successfully created" -ForegroundColor Blue

Write-Host "###### Checking if CosmosDB SQL database exists..." -ForegroundColor Blue

$dbResult = az cosmosdb sql database exists --account-name $cosmosNamespace --name 'MetaData' --resource-group $resourceGroup
if($dbResult -ne 'true')
{
    Write-Host "###### No CosmosDB SQL database exists. Creating a new one..." -ForegroundColor Blue
    az cosmosdb sql database create --account-name $cosmosNamespace --name 'MetaData' --resource-group $resourceGroup
}

Write-Host "###### CosmosDB SQL database successfully created" -ForegroundColor Blue

# Get cosmos connectionString
$cosmosDbConnectionString=$(az cosmosdb keys list `
-n $cosmosNamespace `
--resource-group $resourceGroup `
--type connection-strings `
--query connectionStrings[0].connectionString `
--output tsv)

az webapp config appsettings set --resource-group $resourceGroup -n $webappName --settings "CosmosConnection=$cosmosDbConnectionString"

$saconnectionstring = az storage account keys list --resource-group $resourceGroup -n $storageAccountName

az webapp config appsettings set --resource-group $resourceGroup -n $webappName --settings "AzureWebJobsStorage=$saconnectionstring"

