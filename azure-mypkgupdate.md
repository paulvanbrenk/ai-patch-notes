# MyPkgUpdate Azure Resources

## Resource Group
- **Name:** MyPkgUpdate
- **Location:** Central US

## Resources

| Resource | Name | Details |
|----------|------|---------|
| App Service Plan | `ASP-MyPkgUpdate` | Free tier (F1) |
| Web App | `api-mypkgupdate-com` | .NET 10, HTTPS-only |
| Managed Identity | `api-mypkgupdate-id` | Federated for GitHub Actions |
| Static Web App | `mypkgupdate` | Custom domain pending |
| Storage Account | `mypkgupdatest` | Standard LRS |
| Function App | `func-mypkgupdate` | .NET 10 Isolated, Consumption plan |
| Application Insights | `func-mypkgupdate` | Monitoring for Function App |

## URLs

- **API:** https://api-mypkgupdate-com.azurewebsites.net
- **Functions:** https://func-mypkgupdate.azurewebsites.net
- **Static Site:** https://polite-mud-08a17f610.4.azurestaticapps.net
- **Custom Domain:** https://app.mypkgupdate.com

## GitHub Actions Configuration

Required secrets (already configured in repo):

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

## DNS Configuration

`app.mypkgupdate.com` CNAME points to the static web app.
