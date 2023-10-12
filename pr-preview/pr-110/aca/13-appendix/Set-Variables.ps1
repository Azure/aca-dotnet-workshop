$file = "./Variables.ps1"

# Create a new or replace any existing file (note how we do not use -Append in the first line).
"# Execute with `"$file`" to restore previously-saved variables." | Out-File -FilePath $file

$vars = @(
    "API_APP_PORT",
    "APPINSIGHTS_NAME",
    "APPINSIGHTS_INSTRUMENTATIONKEY",
    "AZURE_CONTAINER_REGISTRY_NAME",
    "AZURE_SUBSCRIPTION_ID",
    "BACKEND_API_EXTERNAL_BASE_URL",
    "BACKEND_API_INTERNAL_BASE_URL",
    "BACKEND_API_NAME",
    "BACKEND_SERVICE_APP_PORT",
    "BACKEND_SERVICE_NAME",
    "BACKEND_SERVICE_PRINCIPAL_ID",
    "COSMOS_DB_ACCOUNT",
    "COSMOS_DB_CONTAINER",
    "COSMOS_DB_DBNAME",
    "ENVIRONMENT",
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
    "TODAY",
    "UI_APP_PORT",
    "WORKSPACE_ID",
    "WORKSPACE_NAME",
    "WORKSPACE_SECRET"
);

foreach ($var in $vars) { 
    # Ensure the variable exists. If not, don't attempt to get it and don't write out a blank value.

    if (Test-Path variable:$var) {
        Write-Host "$var exists."
        $val = Get-Variable -Name $var -ValueOnly
        "Set-Variable -Name $var -Value $val -Scope Global" | Out-File -FilePath $file -Append
    }
}
