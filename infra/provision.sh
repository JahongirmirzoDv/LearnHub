#!/usr/bin/env bash
# Provisions LearnHub on Azure without storing a single password:
#   * Linux App Service running .NET 10, HTTPS only.
#   * Azure SQL Database with Microsoft Entra authentication only. The web app signs in with its
#     system-assigned managed identity, which is granted only the database roles it needs.
#   * A user-assigned managed identity that GitHub Actions uses through OpenID Connect to deploy.
#
# Run it in Azure Cloud Shell (Bash) at https://shell.azure.com, which is already signed in and has every tool needed:
#   git clone https://github.com/JahongirmirzoDv/LearnHub.git && cd LearnHub && bash infra/provision.sh
#
# Free tiers are the default (App Service F1 and the Azure SQL Database free offer). Override any setting below with
# an environment variable, for example:  PLAN_SKU=B1 LOCATION=eastasia bash infra/provision.sh
# Re-running the script is safe: existing resources are reused and settings are re-applied.
set -euo pipefail

GITHUB_REPOSITORY="${GITHUB_REPOSITORY:-JahongirmirzoDv/LearnHub}"
LOCATION="${LOCATION:-southeastasia}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-learnhub}"
APP_NAME="${APP_NAME:-}"                    # globally unique; generated (learnhub-xxxxxx) when empty
PLAN_NAME="${PLAN_NAME:-plan-learnhub}"
PLAN_SKU="${PLAN_SKU:-F1}"                  # F1 = free (60 CPU minutes a day); B1 = paid, faster, supports Always On
DATABASE="${DATABASE:-sqlserver}"           # sqlserver = Azure SQL Database; sqlite = file on App Service storage
SQL_TIER="${SQL_TIER:-free}"                # free = Azure SQL Database free offer; basic = paid Basic tier
SQL_DATABASE="${SQL_DATABASE:-LearnHub}"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@learnhub.local}"
DEPLOY_IDENTITY="${DEPLOY_IDENTITY:-id-learnhub-github}"

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }
fail() { printf '\n\033[31mError:\033[0m %s\n' "$1" >&2; exit 1; }

command -v az > /dev/null || fail "The Azure CLI is not installed. Run this script in Azure Cloud Shell."
az account show --output none 2> /dev/null || fail "Sign in first with: az login"

SUBSCRIPTION_ID="$(az account show --query id --output tsv)"
TENANT_ID="$(az account show --query tenantId --output tsv)"
echo "Subscription: $(az account show --query name --output tsv) ($SUBSCRIPTION_ID)"

step "Resource group $RESOURCE_GROUP ($LOCATION)"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --tags project=learnhub --output none

if [ -z "$APP_NAME" ]; then
  APP_NAME="$(az webapp list --resource-group "$RESOURCE_GROUP" --query "[?tags.project=='learnhub'] | [0].name" --output tsv)"
fi
APP_NAME="${APP_NAME:-learnhub-$(openssl rand -hex 3)}"
SQL_SERVER="${SQL_SERVER:-${APP_NAME}-sql}"

step "App Service plan $PLAN_NAME ($PLAN_SKU, Linux)"
az appservice plan create --resource-group "$RESOURCE_GROUP" --name "$PLAN_NAME" --location "$LOCATION" \
  --sku "$PLAN_SKU" --is-linux --tags project=learnhub --output none

step "Web app $APP_NAME (.NET 10)"
if ! az webapp show --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --output none 2> /dev/null; then
  az webapp create --resource-group "$RESOURCE_GROUP" --plan "$PLAN_NAME" --name "$APP_NAME" \
    --runtime "DOTNETCORE:10.0" --tags project=learnhub --output none
fi
az webapp update --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --https-only true --output none
az webapp config set --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" \
  --min-tls-version 1.2 --ftps-state Disabled --http20-enabled true --output none
az webapp identity assign --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --output none
APP_HOST="$(az webapp show --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --query defaultHostName --output tsv)"

if [ "$DATABASE" = "sqlserver" ]; then
  step "Azure SQL server $SQL_SERVER (Microsoft Entra authentication only)"
  ADMIN_OBJECT_ID="$(az ad signed-in-user show --query id --output tsv)"
  ADMIN_NAME="$(az ad signed-in-user show --query userPrincipalName --output tsv)"
  if ! az sql server show --resource-group "$RESOURCE_GROUP" --name "$SQL_SERVER" --output none 2> /dev/null; then
    az sql server create --resource-group "$RESOURCE_GROUP" --name "$SQL_SERVER" --location "$LOCATION" \
      --enable-ad-only-auth --external-admin-principal-type User \
      --external-admin-name "$ADMIN_NAME" --external-admin-sid "$ADMIN_OBJECT_ID" \
      --minimal-tls-version 1.2 --tags project=learnhub --output none
  fi

  # 0.0.0.0 is Azure's special rule for "services hosted in Azure"; it does not open the server to the internet.
  az sql server firewall-rule create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
    --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none

  step "Database $SQL_DATABASE ($SQL_TIER)"
  if ! az sql db show --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" --name "$SQL_DATABASE" --output none 2> /dev/null; then
    if [ "$SQL_TIER" = "free" ]; then
      az sql db create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" --name "$SQL_DATABASE" \
        --edition GeneralPurpose --family Gen5 --capacity 2 --compute-model Serverless \
        --use-free-limit --free-limit-exhaustion-behavior AutoPause \
        --backup-storage-redundancy Local --tags project=learnhub --output none
    else
      az sql db create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" --name "$SQL_DATABASE" \
        --edition Basic --backup-storage-redundancy Local --tags project=learnhub --output none
    fi
  fi

  step "Database access for the web app's managed identity"
  GRANT_SQL="IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${APP_NAME}') CREATE USER [${APP_NAME}] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [${APP_NAME}];
ALTER ROLE db_datawriter ADD MEMBER [${APP_NAME}];
ALTER ROLE db_ddladmin ADD MEMBER [${APP_NAME}];"

  run_grant() {
    sqlcmd -S "tcp:${SQL_SERVER}.database.windows.net,1433" -d "$SQL_DATABASE" \
      --authentication-method ActiveDirectoryAzCli -l 60 -b -Q "$GRANT_SQL" 2>&1
  }

  granted=false
  if command -v sqlcmd > /dev/null; then
    if output="$(run_grant)"; then
      granted=true
    else
      # Azure SQL names the address it refused; allow exactly that address briefly, retry, then remove the rule.
      client_ip="$(printf '%s' "$output" | grep -oE "IP address '[0-9.]+'" | grep -oE '[0-9.]+' | head -n 1 || true)"
      if [ -n "$client_ip" ]; then
        az sql server firewall-rule create --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
          --name provision-cloud-shell --start-ip-address "$client_ip" --end-ip-address "$client_ip" --output none
        sleep 30
        if output="$(run_grant)"; then granted=true; fi
        az sql server firewall-rule delete --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
          --name provision-cloud-shell --output none
      fi
    fi
  fi

  if [ "$granted" = true ]; then
    echo "Granted db_datareader, db_datawriter and db_ddladmin to $APP_NAME."
  else
    printf '%s\n' "${output:-sqlcmd is not available.}"
    cat << EOF

Could not grant database access automatically. Do it once in the Azure portal:
  SQL databases > $SQL_DATABASE > Query editor > sign in with Microsoft Entra, then run:

$GRANT_SQL

EOF
  fi

  CONNECTION_SETTINGS=(
    "Database__Provider=SqlServer"
    "ConnectionStrings__SqlServer=Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DATABASE};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60"
  )
else
  CONNECTION_SETTINGS=(
    "Database__Provider=Sqlite"
    "ConnectionStrings__Sqlite=Data Source=/home/data/learnhub/learnhub.db"
  )
fi

step "Application settings"
echo "The administrator account is created on the first start with the email $ADMIN_EMAIL."
while true; do
  read -r -s -p "Choose its password (at least 8 characters with upper-case, lower-case and a digit): " ADMIN_PASSWORD
  echo
  if [ "${#ADMIN_PASSWORD}" -ge 8 ] && [[ "$ADMIN_PASSWORD" =~ [a-z] ]] && [[ "$ADMIN_PASSWORD" =~ [A-Z] ]] && [[ "$ADMIN_PASSWORD" =~ [0-9] ]]; then
    break
  fi
  echo "That password does not meet the rules; try again."
done

# Settings go through a private temporary file so the password never appears in a command line or the shell history.
settings_file="$(mktemp)"
chmod 600 "$settings_file"
trap 'rm -f "$settings_file"' EXIT
ADMIN_PASSWORD="$ADMIN_PASSWORD" python3 - "$settings_file" "$ADMIN_EMAIL" "${CONNECTION_SETTINGS[@]}" << 'PY'
import json, os, sys
path, admin_email, *pairs = sys.argv[1:]
settings = {
    "ASPNETCORE_ENVIRONMENT": "Production",
    # TLS ends at App Service's front end; the app must trust X-Forwarded-Proto to know requests are HTTPS.
    "ASPNETCORE_FORWARDEDHEADERS_ENABLED": "true",
    # Outside wwwroot, so deployments never remove uploaded files.
    "Storage__RootPath": "/home/data/learnhub/storage",
    "Seed__DemoData": "true",
    "Seed__AdminEmail": admin_email,
    "Seed__AdminPassword": os.environ["ADMIN_PASSWORD"],
}
for pair in pairs:
    name, value = pair.split("=", 1)
    settings[name] = value
with open(path, "w", encoding="utf-8") as handle:
    json.dump([{"name": k, "value": v, "slotSetting": False} for k, v in settings.items()], handle)
PY
unset ADMIN_PASSWORD
az webapp config appsettings set --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" \
  --settings "@$settings_file" --output none
rm -f "$settings_file"

step "Deployment identity for GitHub Actions ($DEPLOY_IDENTITY)"
az identity create --resource-group "$RESOURCE_GROUP" --name "$DEPLOY_IDENTITY" --location "$LOCATION" \
  --tags project=learnhub --output none
CLIENT_ID="$(az identity show --resource-group "$RESOURCE_GROUP" --name "$DEPLOY_IDENTITY" --query clientId --output tsv)"
PRINCIPAL_ID="$(az identity show --resource-group "$RESOURCE_GROUP" --name "$DEPLOY_IDENTITY" --query principalId --output tsv)"

if ! az identity federated-credential show --resource-group "$RESOURCE_GROUP" --identity-name "$DEPLOY_IDENTITY" \
  --name github-production --output none 2> /dev/null; then
  az identity federated-credential create --resource-group "$RESOURCE_GROUP" --identity-name "$DEPLOY_IDENTITY" \
    --name github-production --issuer "https://token.actions.githubusercontent.com" \
    --subject "repo:${GITHUB_REPOSITORY}:environment:production" --audiences "api://AzureADTokenExchange" --output none
fi

WEBAPP_ID="$(az webapp show --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --query id --output tsv)"
# A new identity can take a minute to replicate, so the role assignment is retried.
for attempt in 1 2 3 4 5 6; do
  if az role assignment create --assignee-object-id "$PRINCIPAL_ID" --assignee-principal-type ServicePrincipal \
    --role "Website Contributor" --scope "$WEBAPP_ID" --output none 2> /dev/null; then
    break
  fi
  [ "$attempt" = 6 ] && fail "Could not assign the Website Contributor role to $DEPLOY_IDENTITY."
  sleep 20
done

cat << EOF

LearnHub infrastructure is ready.
  Web app:   https://$APP_HOST
  Database:  $DATABASE

Next, add these repository variables to GitHub. None of them is a secret.
Either use Settings > Secrets and variables > Actions > Variables, or run these commands wherever the GitHub CLI
is signed in ('gh auth login' also works here in Cloud Shell):

  gh variable set AZURE_WEBAPP_NAME     --repo $GITHUB_REPOSITORY --body "$APP_NAME"
  gh variable set AZURE_WEBAPP_URL      --repo $GITHUB_REPOSITORY --body "https://$APP_HOST"
  gh variable set AZURE_CLIENT_ID       --repo $GITHUB_REPOSITORY --body "$CLIENT_ID"
  gh variable set AZURE_TENANT_ID       --repo $GITHUB_REPOSITORY --body "$TENANT_ID"
  gh variable set AZURE_SUBSCRIPTION_ID --repo $GITHUB_REPOSITORY --body "$SUBSCRIPTION_ID"

Then run the "Deploy to Azure" workflow from the Actions tab (or push to main).
After you have signed in as the administrator once, remove the seed password from the app settings:

  az webapp config appsettings delete --resource-group $RESOURCE_GROUP --name $APP_NAME --setting-names Seed__AdminPassword
EOF
