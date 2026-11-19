#!/bin/bash

file="./variables.sh"
i=0
existingVars=0

vars=(
    "ACA_ENVIRONMENT_SUBNET_ID"
    "API_APP_PORT"
    "APPINSIGHTS_NAME"
    "APPINSIGHTS_INSTRUMENTATIONKEY"
    "AZURE_CONTAINER_REGISTRY_NAME"
    "AZURE_SUBSCRIPTION_ID"
    "BACKEND_API_EXTERNAL_BASE_URL"
    "BACKEND_API_INTERNAL_BASE_URL"
    "BACKEND_API_NAME"
    "BACKEND_API_PRINCIPAL_ID"
    "BACKEND_API_REVISION_NAME"
    "BACKEND_SERVICE_APP_PORT"
    "BACKEND_SERVICE_NAME"
    "BACKEND_SERVICE_PRINCIPAL_ID"
    "BACKEND_SERVICE_REVISION_NAME"
    "COSMOS_DB_ACCOUNT"
    "COSMOS_DB_CONTAINER"
    "COSMOS_DB_DBNAME"
    "COSMOS_DB_ENDPOINT"
    "COSMOS_DB_PRIMARY_MASTER_KEY"
    "ENVIRONMENT"
    "FRONTEND_UI_BASE_URL"
    "FRONTEND_UI_BASE_URL_LOCAL"
    "FRONTEND_WEBAPP_NAME"
    "KEYVAULT_NAME"
    "KEYVAULT_SECRETS_OFFICER_ROLE_ID"
    "KEYVAULT_SECRETS_USER_ROLE_ID"
    "LOCATION"
    "RANDOM_STRING"
    "RESOURCE_GROUP"
    "ROLE_ID"
    "SERVICE_BUS_CONNECTION_STRING"
    "SERVICE_BUS_NAMESPACE_NAME"
    "SERVICE_BUS_TOPIC_NAME"
    "SERVICE_BUS_TOPIC_SUBSCRIPTION"
    "SIGNEDIN_USERID"
    "STORAGE_ACCOUNT_NAME"
    "STORAGE_ACCOUNT_KEY"
    "TARGET_PORT"
    "UI_APP_PORT"
    "VNET_NAME"
    "WORKSPACE_ID"
    "WORKSPACE_NAME"
    "WORKSPACE_SECRET"
    "PROJECT_ROOT"
)

# Ensure that variables exist in the terminal session. If none exist, we need to prevent accidental wiping of the Variables.sh file.
for var in "${vars[@]}"; do
    if [[ -v $var ]]; then
        ((existingVars++))
    fi
done

if [[ $existingVars -eq 0 ]]; then
    echo -e "\nNo variables were found in the current session. Exiting script to prevent accidental overwrite of Variables.sh file.\n"
    exit
fi

# Now that we know variables exist in the current terminal session, we proceed with writing them to the Variables.sh file. We replace
# any existing file with the same name. Note how we intentionally do not use >>, so that a fresh file gets created here.
echo "# Execute with \". $file\" to restore previously-saved variables." > $file

for var in "${vars[@]}"; do
    # Ensure the variable exists. If not, don't attempt to get it and don't write out a blank value.
    if [[ -v $var ]]; then
        val=${!var}

        # Bash requires a bit more type safety by setting quotes around strings.
        if [[ $val =~ ^[0-9]+$ ]]; then
            echo "export $var=$val" >> $file
        else
            echo "export $var=\"$val\"" >> $file
        fi
        ((i++))
    fi
done

# $TODAY is a special variable that simply, easily captures today's date.
echo "export TODAY=$(date +%Y%m%d)" >> $file
((i++))

# When the Variables.sh script executes, the following line will inform how many variables were set in the current session.
echo "# Set $i variable(s)." >> $file
echo "Wrote $i variable(s) to $file."