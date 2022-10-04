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
#az account set --subscription "7ca35eb7-159a-480c-afe7-a2e4464eaee9"#"d6b4bc51-75a6-4eb4-8cf2-4114beceec76"

#$output = az vm create `
#--name "ramtinstestvm" `
#--resource-group "rg_ramtin" `
#--admin-password "P@sssssw8rd27!!!" `
#--admin-username "adminazure" `
#--image "UbuntuLTS"
#Throw-WhenError -output $output


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
$resourceGroup = 'az-kursus-test'
$storageAccountName = 'chris1test'
$appServicePlanName = 'cbrNewAppPlan'
$functionappName = 'chris1test'
$webappName = 'cbrWebapp'
$cosmosNamespace = 'cbrcosmostest'

az upgrade

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

# Create App Service Plan 
$appServicePlanId = az appservice plan create `
  --name $appServicePlanName `
  --sku B1 `
  --resource-group $resourceGroup `
  --query id
	
Throw-WhenError -output $appServicePlanId

# Create function
$output = az functionapp create `
  --name $functionappName `
  --storage $storageAccountName `
  --plan $appServicePlanId `
  --resource-group $resourceGroup `
  --functions-version 3

Throw-WhenError -output $output

# Create WebApp
$output = az webapp create `
  --name $webappName `
  --resource-group $resourceGroup `
  --plan $appServicePlanId

Throw-WhenError -output $output

# Create Cosmos Namespace
$cosmosAccountResult = az cosmosdb check-name-exists --name $cosmosNamespace
if($cosmosAccountResult -ne 'true')
{
    az cosmosdb create --name $cosmosNamespace --resource-group $resourceGroup
}

$dbResult = az cosmosdb sql database exists --account-name $cosmosNamespace --name 'MetaData' --resource-group $resourceGroup
if($dbResult -ne 'true')
{
    az cosmosdb sql database create --account-name $cosmosNamespace --name 'MetaData' --resource-group $resourceGroup
}

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

