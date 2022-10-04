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
az account set --subscription "d6b4bc51-75a6-4eb4-8cf2-4114beceec76"


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

##############################
# THE BELOW HAVE BEEN ADDED AS PART OF DAY 2
##############################


# Notice that instead of hardcoding the values every time
# We instead place/assign it to a variable.
# This way - we can reuse the $resourceGroup variable
# Should we want to change the value of the $resourceGroup variable, we only have to do it one place.

$resourceGroup = 'rg_ramtin'
$storageAccountName = 'ramtinstorageacc'
$appServicePlanName = 'ramtinAppServicePlan'
$functionappName = 'ramtinFuncApp111'
$webappName = 'ramtinWebApp111'
$cosmosNamespace = 'ramtinCosmosNameSpace111'

az upgrade

Write-Host "########## Deploying Storage Account"

# Create Storage account
# We've set this command as one of the first ones, since other services (function app) are dependant on this one.
$output = az storage account create `
  --name $storageAccountName `
  --resource-group $resourceGroup `
  --sku 'Standard_LRS' `
  --kind 'StorageV2'

Throw-WhenError -output $output

Write-Host "########## Creating Storage Account Container"

# Create container
# Instead of us having to manually create a container in Azure Portal we prefer automating the process.
# Therefore we have this step here in the script.
$output = az storage container create `
  --name 'images' `
  --account-name $storageAccountName `
  --resource-group $resourceGroup

Throw-WhenError -output $output

Write-Host "########## Deploying App Service Plan"

# Create App Service Plan 
# We've set this command as one of the first ones, since other services (function app) are dependant on this one.
$appServicePlanId = az appservice plan create `
  --name $appServicePlanName `
  --sku B1 `
  --resource-group $resourceGroup `
  --query id
	
Throw-WhenError -output $appServicePlanId

Write-Host "########## Deploying Azure Function App"

# Create function
$output = az functionapp create `
  --name $functionappName `
  --storage $storageAccountName `
  --plan $appServicePlanId `
  --resource-group $resourceGroup `
  --functions-version 3

Throw-WhenError -output $output

# HEEEY!!! Several places we've hardcoded the value. If you have time then you could assign all the hardcoded values to variables (as we did with the resourceGroup).
# HEY again!!!! If you have the time for it, then please go ahead and deploy/create a Web App.
