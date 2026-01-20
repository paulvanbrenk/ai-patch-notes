# MyPkgUpdate Azure Resources

## Resource Group
- **Name:** MyPkgUpdate
- **Location:** Central US

## Resources

| Resource | Name | Details |
|----------|------|---------|
| App Service Plan | `ASP-MyPkgUpdate` | Free tier (F1) |
| Web App | `api-mypkgupdate-com` | .NET 8, HTTPS-only |
| Managed Identity | `api-mypkgupdate-id` | Federated for GitHub Actions |
| Static Web App | `mypkgupdate` | Custom domain pending |
| Storage Account | `mypkgupdatest` | Standard LRS |
| Function App | `func-mypkgupdate` | .NET 8 Isolated, Consumption plan |
| Application Insights | `func-mypkgupdate` | Monitoring for Function App |

## URLs

- **API:** https://api-mypkgupdate-com.azurewebsites.net
- **Functions:** https://func-mypkgupdate.azurewebsites.net
- **Static Site:** https://polite-mud-08a17f610.4.azurestaticapps.net
- **Custom Domain:** https://mypkgupdate.com (pending DNS validation)

## GitHub Actions Configuration

Add these secrets to your repository:

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | `b72b716a-9eb1-4a12-96a8-eb550561dd5a` |
| `AZURE_TENANT_ID` | `77dd1a7b-29ca-4d8d-8aff-fdfd52debe70` |
| `AZURE_SUBSCRIPTION_ID` | `ba83d8be-06cc-420e-a64e-e2297d620466` |

## DNS Configuration

Add these records to your domain registrar for `mypkgupdate.com`:

| Type | Host | Value |
|------|------|-------|
| TXT | `@` | `_0cu8tluxdxvvhtzyk7k7suo73u0rlx0` |
| CNAME/ALIAS | `@` | `polite-mud-08a17f610.4.azurestaticapps.net` |
