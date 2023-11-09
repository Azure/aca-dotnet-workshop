$file = "./Variables.ps1"
$i = 0

# Create a new or replace any existing file (note how we do not use -Append in the first line).
"# Execute with `"$file`" to restore previously-saved variables." | Out-File -FilePath $file

$vars = @(
    "ACA_ENVIRONMENT_SUBNET_ID",
    "API_APP_PORT",
    "APPINSIGHTS_NAME",
    "APPINSIGHTS_INSTRUMENTATIONKEY",
    "AZURE_CONTAINER_REGISTRY_NAME",
    "AZURE_SUBSCRIPTION_ID",
    "BACKEND_API_EXTERNAL_BASE_URL",
    "BACKEND_API_INTERNAL_BASE_URL",
    "BACKEND_API_NAME",
    "BACKEND_API_PRINCIPAL_ID",
    "BACKEND_SERVICE_APP_PORT",
    "BACKEND_SERVICE_NAME",
    "BACKEND_SERVICE_PRINCIPAL_ID",
    "COSMOS_DB_ACCOUNT",
    "COSMOS_DB_CONTAINER",
    "COSMOS_DB_DBNAME",
    "COSMOS_DB_ENDPOINT",
    "COSMOS_DB_PRIMARY_MASTER_KEY",
    "ENVIRONMENT",
    "FRONTEND_UI_BASE_URL",
    "FRONTEND_UI_BASE_URL_LOCAL",
    "FRONTEND_WEBAPP_NAME",
    "KEYVAULT_NAME",
    "KEYVAULT_SECRETS_OFFICER_ROLE_ID",
    "KEYVAULT_SECRETS_USER_ROLE_ID",
    "LOCATION",
    "RANDOM_STRING",
    "RESOURCE_GROUP",
    "REVISION_NAME",
    "ROLE_ID",
    "SERVICE_BUS_CONNECTION_STRING",
    "SERVICE_BUS_NAMESPACE_NAME",
    "SERVICE_BUS_TOPIC_NAME",
    "SERVICE_BUS_TOPIC_SUBSCRIPTION",
    "SIGNEDIN_USERID",
    "STORAGE_ACCOUNT_NAME",
    "TARGET_PORT",
    "UI_APP_PORT",
    "VNET_NAME",
    "WORKSPACE_ID",
    "WORKSPACE_NAME",
    "WORKSPACE_SECRET"
);

foreach ($var in $vars) { 
    # Ensure the variable exists. If not, don't attempt to get it and don't write out a blank value.

    if (Test-Path variable:$var) {
        $val = Get-Variable -Name $var -ValueOnly
        "Set-Variable -Scope Global -Name $var -Value $val" | Out-File -FilePath $file -Append
        $i++
    }
}

# $TODAY is a special variable that simply, easily captures today's date.
"Set-Variable -Scope Global -Name TODAY -Value (Get-Date -Format 'yyyyMMdd')" | Out-File -FilePath $file -Append
$i++

# When the Variables.ps1 script executes, the following line will inform how many variables were set in the current session.
"Write-Host `"Set $i variable$($i -eq 1 ? '' : 's').`"" | Out-File -FilePath $file -Append

Write-Host "`nWrote $i variable$($i -eq 1 ? '' : 's') to $file.`n"
