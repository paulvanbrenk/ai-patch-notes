# Azure Setup Runbook

One-pass setup guide for the PatchNotes Azure infrastructure. Covers custom domain, Function App, Application Insights, and GitHub Actions OIDC — with validation checks after each section.

## Variables

Set these once at the top of your shell session. Every command below references them.

```bash
# Azure
RG="MyPkgUpdate"                          # Resource group
LOCATION="eastus"                         # Region
SUBSCRIPTION_ID="<your-subscription-id>"

# App Service (API)
APP_SERVICE="api-mypkgupdate-com"

# Function App (Sync)
FUNC_APP="fn-patchnotes-sync"
STORAGE_ACCT="stpatchnotessync"

# Application Insights
APPINSIGHTS="ai-patchnotes"

# Domain
DOMAIN="mypkgupdate.com"

# GitHub (for OIDC)
GH_ORG="tinytoolsllc"
GH_REPO="ai-patch-notes"
```

---

## 1. DNS — Custom Domains

### Prerequisites

- Domain registered and DNS management access (Cloudflare, Route 53, etc.)
- Azure Static Web Apps resource already exists for the frontend
- Azure App Service already exists for the API

### 1a. Frontend: `app.mypkgupdate.com` → Static Web Apps

Static Web Apps handles TLS automatically.

```bash
# Add custom domain (Azure validates DNS automatically)
az staticwebapp hostname set \
  --name <static-web-app-name> \
  --resource-group $RG \
  --hostname "app.${DOMAIN}"
```

**DNS record** (at your registrar/DNS provider):

| Type  | Name  | Value                                    |
|-------|-------|------------------------------------------|
| CNAME | `app` | `<static-web-app-default-hostname>.azurestaticapps.net` |

### 1b. API: `api.mypkgupdate.com` → App Service

```bash
# 1. Get the verification ID for your App Service
az webapp show --name $APP_SERVICE --resource-group $RG \
  --query "customDomainVerificationId" -o tsv
```

**DNS records** (add both):

| Type  | Name                        | Value                                        |
|-------|-----------------------------|----------------------------------------------|
| CNAME | `api`                       | `${APP_SERVICE}.azurewebsites.net`           |
| TXT   | `asuid.api`                 | `<customDomainVerificationId from above>`    |

Wait for DNS propagation (check with `dig api.${DOMAIN} CNAME`), then bind the domain:

```bash
# 2. Add the custom domain
az webapp config hostname add \
  --webapp-name $APP_SERVICE \
  --resource-group $RG \
  --hostname "api.${DOMAIN}"

# 3. Create a free managed TLS certificate
az webapp config ssl create \
  --name $APP_SERVICE \
  --resource-group $RG \
  --hostname "api.${DOMAIN}"

# 4. Bind the certificate (enforce HTTPS)
az webapp config ssl bind \
  --name $APP_SERVICE \
  --resource-group $RG \
  --hostname "api.${DOMAIN}" \
  --ssl-type SNI

# 5. Force HTTPS
az webapp update \
  --name $APP_SERVICE \
  --resource-group $RG \
  --set httpsOnly=true
```

### 1c. Update Stytch Redirect URLs

After changing domains, update Stytch to allow the new origins:

1. Go to https://stytch.com/dashboard → **Redirect URLs**
2. Add `https://app.${DOMAIN}/*` if not already present
3. Remove any old domains

### 1d. Update Stripe Webhook URL

If the API moves to a custom domain:

1. Go to https://dashboard.stripe.com/webhooks
2. Update the endpoint URL from `https://${APP_SERVICE}.azurewebsites.net/webhooks/stripe`
   to `https://api.${DOMAIN}/webhooks/stripe`

### 1e. Update Frontend API URL

Update the `VITE_API_URL` GitHub Actions variable to point to the new domain:

```bash
gh variable set VITE_API_URL --body "https://api.${DOMAIN}/api" --repo ${GH_ORG}/${GH_REPO}
```

### Validate DNS

```bash
# Frontend — should resolve to Azure Static Web Apps
dig app.${DOMAIN} CNAME +short

# API — should resolve to App Service
dig api.${DOMAIN} CNAME +short

# TLS — both should show valid certificates
curl -sI https://app.${DOMAIN} | head -5
curl -sI https://api.${DOMAIN}/status | head -5
```

---

## 2. Function App — Sync Pipeline

### Prerequisites

- Resource group exists (`az group show --name $RG`)
- SQL Server database is accessible
- GitHub PAT and Groq API key available

### 2a. Create Storage Account

Azure Functions requires a storage account for internal state (timer lease, logs).

```bash
az storage account create \
  --name $STORAGE_ACCT \
  --resource-group $RG \
  --location $LOCATION \
  --sku Standard_LRS
```

### 2b. Create Function App

```bash
az functionapp create \
  --name $FUNC_APP \
  --resource-group $RG \
  --storage-account $STORAGE_ACCT \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 10 \
  --functions-version 4 \
  --os-type Linux
```

### 2c. Configure App Settings

```bash
az functionapp config appsettings set \
  --name $FUNC_APP \
  --resource-group $RG \
  --settings \
    'ConnectionStrings__PatchNotes=<sql-server-connection-string>' \
    'GitHub__Token=<github-pat>' \
    'AI__ApiKey=<groq-api-key>' \
    'AI__BaseUrl=https://api.groq.com/openai/v1' \
    'AI__Model=llama-3.3-70b-versatile'
```

> **Note:** Single-quote the connection string in bash if it contains `!` or `;` characters.

### Validate Function App

```bash
# Exists and running
az functionapp show \
  --name $FUNC_APP \
  --resource-group $RG \
  --query "{state:state, runtime:siteConfig.linuxFxVersion, url:defaultHostName}" \
  -o table

# App settings are configured (keys only, no secrets printed)
az functionapp config appsettings list \
  --name $FUNC_APP \
  --resource-group $RG \
  --query "[].name" -o tsv | sort

# Expected settings (minimum):
#   AI__ApiKey
#   AI__BaseUrl
#   AI__Model
#   AzureWebJobsStorage
#   ConnectionStrings__PatchNotes
#   FUNCTIONS_WORKER_RUNTIME
#   GitHub__Token
```

---

## 3. Application Insights

### Prerequisites

- Resource group exists
- App Service and Function App exist

### 3a. Create Application Insights Resource

Both the API and Function App share a single workspace.

```bash
az monitor app-insights component create \
  --app $APPINSIGHTS \
  --location $LOCATION \
  --resource-group $RG \
  --kind web
```

### 3b. Get the Connection String

```bash
APPINSIGHTS_CONN=$(az monitor app-insights component show \
  --app $APPINSIGHTS \
  --resource-group $RG \
  --query connectionString -o tsv)

echo "$APPINSIGHTS_CONN"
```

### 3c. Wire Up to App Service (API)

```bash
az webapp config appsettings set \
  --name $APP_SERVICE \
  --resource-group $RG \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=${APPINSIGHTS_CONN}"
```

### 3d. Wire Up to Function App

```bash
az functionapp config appsettings set \
  --name $FUNC_APP \
  --resource-group $RG \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=${APPINSIGHTS_CONN}"
```

### Validate Application Insights

```bash
# Resource exists
az monitor app-insights component show \
  --app $APPINSIGHTS \
  --resource-group $RG \
  --query "{name:name, connectionString:connectionString, provisioningState:provisioningState}" \
  -o table

# API has the connection string set
az webapp config appsettings list \
  --name $APP_SERVICE \
  --resource-group $RG \
  --query "[?name=='APPLICATIONINSIGHTS_CONNECTION_STRING'].value" -o tsv | \
  grep -q "InstrumentationKey" && echo "API: OK" || echo "API: MISSING"

# Function App has the connection string set
az functionapp config appsettings list \
  --name $FUNC_APP \
  --resource-group $RG \
  --query "[?name=='APPLICATIONINSIGHTS_CONNECTION_STRING'].value" -o tsv | \
  grep -q "InstrumentationKey" && echo "Function: OK" || echo "Function: MISSING"
```

After deploying and sending a few requests, verify telemetry is flowing:

```bash
# Check recent telemetry (last 5 minutes)
az monitor app-insights events show \
  --app $APPINSIGHTS \
  --resource-group $RG \
  --type requests \
  --offset 5m \
  --query "value[].{time:timestamp, name:request.name, status:request.resultCode, duration:request.duration}" \
  -o table
```

---

## 4. GitHub Actions OIDC (Federated Credentials)

### Prerequisites

- Azure AD App Registration or Service Principal for deployments
- GitHub repo: `${GH_ORG}/${GH_REPO}`

### 4a. Create Service Principal (if needed)

```bash
az ad sp create-for-rbac \
  --name "github-deploy-patchnotes" \
  --role contributor \
  --scopes "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RG}" \
  --query "{clientId:appId, tenantId:tenant}" -o table
```

Save the `clientId` and `tenantId` from the output.

### 4b. Add Federated Credential for GitHub Actions

```bash
APP_OBJECT_ID=$(az ad app list --display-name "github-deploy-patchnotes" --query "[0].id" -o tsv)

# Allow the main branch to authenticate
az ad app federated-credential create \
  --id $APP_OBJECT_ID \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"${GH_ORG}/${GH_REPO}"':ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Allow the production environment
az ad app federated-credential create \
  --id $APP_OBJECT_ID \
  --parameters '{
    "name": "github-production",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"${GH_ORG}/${GH_REPO}"':environment:production",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### 4c. Set GitHub Secrets

```bash
gh secret set AZURE_CLIENT_ID     --body "<clientId>"         --repo ${GH_ORG}/${GH_REPO}
gh secret set AZURE_TENANT_ID     --body "<tenantId>"         --repo ${GH_ORG}/${GH_REPO}
gh secret set AZURE_SUBSCRIPTION_ID --body "${SUBSCRIPTION_ID}" --repo ${GH_ORG}/${GH_REPO}
```

### Validate OIDC

```bash
# Federated credentials exist
az ad app federated-credential list --id $APP_OBJECT_ID \
  --query "[].{name:name, subject:subject}" -o table

# GitHub secrets are set (gh won't show values, but confirms they exist)
gh secret list --repo ${GH_ORG}/${GH_REPO}

# Expected secrets:
#   AZURE_CLIENT_ID
#   AZURE_SUBSCRIPTION_ID
#   AZURE_TENANT_ID
#   AZURE_STATIC_WEB_APPS_API_TOKEN
#   DATABASE_CONNECTION_STRING
#   VITE_STYTCH_PUBLIC_TOKEN
```

---

## 5. End-to-End Validation

Run these after all sections are complete and the first deploy has finished.

```bash
echo "=== Resource Check ==="

# App Service
az webapp show --name $APP_SERVICE --resource-group $RG \
  --query "{name:name, state:state, https:httpsOnly, hostnames:hostNames}" -o json

# Function App
az functionapp show --name $FUNC_APP --resource-group $RG \
  --query "{name:name, state:state, hostnames:hostNames}" -o json

# App Insights
az monitor app-insights component show --app $APPINSIGHTS --resource-group $RG \
  --query "{name:name, provisioningState:provisioningState}" -o json

echo ""
echo "=== Endpoint Health ==="

# API status page
curl -sf "https://api.${DOMAIN}/status" && echo " API: OK" || echo " API: FAIL"

# API returns data
curl -sf "https://api.${DOMAIN}/api/packages" | head -c 100
echo ""

# Frontend loads
curl -sf "https://app.${DOMAIN}" -o /dev/null && echo " Frontend: OK" || echo " Frontend: FAIL"

echo ""
echo "=== Function App ==="

# List deployed functions
az functionapp function list \
  --name $FUNC_APP \
  --resource-group $RG \
  --query "[].{name:name, isDisabled:isDisabled}" -o table

# Trigger a manual run (optional — fires the sync immediately)
# az functionapp function invoke \
#   --name $FUNC_APP \
#   --resource-group $RG \
#   --function-name SyncReleases

echo ""
echo "=== App Insights Telemetry ==="

# Recent requests (after first deploy + traffic)
az monitor app-insights events show \
  --app $APPINSIGHTS \
  --resource-group $RG \
  --type requests \
  --offset 1h \
  --query "value | length(@)" -o tsv | \
  xargs -I{} echo " Requests in last hour: {}"
```

---

## Quick Reference

### All App Settings

| Service | Setting | Source |
|---------|---------|--------|
| API + Function | `ConnectionStrings__PatchNotes` | SQL Server connection string |
| API + Function | `GitHub__Token` | GitHub PAT |
| API + Function | `AI__ApiKey` | Groq API key |
| API + Function | `AI__BaseUrl` | `https://api.groq.com/openai/v1` |
| API + Function | `AI__Model` | `llama-3.3-70b-versatile` |
| API + Function | `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights |
| API | `Stytch__ProjectId` | Stytch dashboard |
| API | `Stytch__Secret` | Stytch dashboard |
| API | `Stytch__WebhookSecret` | Stytch dashboard |
| API | `Stripe__SecretKey` | Stripe dashboard |
| API | `Stripe__WebhookSecret` | Stripe dashboard |
| Function | `AzureWebJobsStorage` | Storage account (auto-set) |

### DNS Records Summary

| Type  | Name       | Value                                     | Purpose          |
|-------|------------|-------------------------------------------|------------------|
| CNAME | `app`      | `<swa>.azurestaticapps.net`               | Frontend         |
| CNAME | `api`      | `${APP_SERVICE}.azurewebsites.net`        | API              |
| TXT   | `asuid.api`| `<customDomainVerificationId>`            | Domain ownership |
