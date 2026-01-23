# Infrastructure

Azure infrastructure for PatchNotes, defined as Bicep templates.

## Resources

| Resource | Type | SKU |
|----------|------|-----|
| `api-mypkgupdate-com` | App Service | Free (F1) |
| `ASP-MyPkgUpdate` | App Service Plan | Free (F1) |
| `api-mypkgupdate-id` | Managed Identity | - |
| `mypkgupdate` | Static Web App | Free |
| `mypkgupdatest` | Storage Account | Standard LRS |

## Deploy

```bash
# Create resource group (if needed)
az group create --name MyPkgUpdate --location centralus

# Deploy infrastructure
az deployment group create \
  --resource-group MyPkgUpdate \
  --template-file main.bicep \
  --parameters main.bicepparam
```

## Post-Deployment

After deploying, configure secrets via CLI:

```bash
# Set Stytch configuration
az webapp config appsettings set \
  --name api-mypkgupdate-com \
  --resource-group MyPkgUpdate \
  --settings \
    Stytch__ProjectId=<project-id> \
    Stytch__Secret=<secret> \
    Stytch__WebhookSecret=<webhook-secret>

# Set database connection string
az webapp config appsettings set \
  --name api-mypkgupdate-com \
  --resource-group MyPkgUpdate \
  --settings \
    ConnectionStrings__PatchNotes=<connection-string>
```

## GitHub Actions Setup

The managed identity `api-mypkgupdate-id` has federated credentials for GitHub Actions OIDC. Add these secrets to your GitHub repository:

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | Managed identity client ID (output from deployment) |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

## Custom Domains

Static Web App custom domains:
- `app.mypkgupdate.com`
- `www.mypkgupdate.com`

Configure DNS with CNAME records pointing to the Static Web App default hostname.
