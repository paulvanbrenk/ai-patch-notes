using github actions

seeing this in the logs

Run azure/login@v2
Running Azure CLI Login.
/usr/bin/az cloud set -n azurecloud
Done setting cloud: "azurecloud"
Federated token details:
 issuer - https://token.actions.githubusercontent.com
 subject claim - repo:paulvanbrenk/ai-patch-notes:environment:production
 audience - api://AzureADTokenExchange
 job_workflow_ref - paulvanbrenk/ai-patch-notes/.github/workflows/deploy.yml@refs/heads/main
Attempting Azure CLI login by using OIDC...
Error: AADSTS700213: No matching federated identity record found for presented assertion subject 'repo:paulvanbrenk/ai-patch-notes:environment:production'. Check your federated identity credential Subject, Audience and Issuer against the presented assertion. https://learn.microsoft.com/entra/workload-id/workload-identity-federation Trace ID: 02dd1dd0-22b6-43d8-88d3-72090d2d1600 Correlation ID: ffb50372-fbd3-45ed-99b5-87b117e690fb Timestamp: 2026-01-20 04:48:07Z

Error: Run the command below to authenticate interactively; additional arguments may be added as needed:
az logout
az login

Error: Login failed with Error: The process '/usr/bin/az' failed with exit code 1. Double check if the 'auth-type' is correct. Refer to https://github.com/Azure/login#readme for more information.
Thought for 1m 21s

That error is not about TenantID/ClientID/SubscriptionID being missing — it’s saying Azure AD (Entra) can’t find a federated identity credential whose Issuer + Audience + Subject exactly match the OIDC token GitHub just presented. Entra does strict matching.

From your log, GitHub is presenting:

Issuer: https://token.actions.githubusercontent.com

Audience: api://AzureADTokenExchange

Subject (sub claim): repo:paulvanbrenk/ai-patch-notes:environment:production

So in Azure, you must have a Federated Credential whose Subject identifier is exactly repo:paulvanbrenk/ai-patch-notes:environment:production (plus the same issuer/audience).

Fix (most common): add the matching Federated Credential in Entra ID

Azure Portal → Microsoft Entra ID

App registrations → select the app whose Application (client) ID you’re using in GitHub (AZURE_CLIENT_ID)

Go to Certificates & secrets → Federated credentials → Add credential

Choose GitHub Actions (or similar guided option), and set:

Organization: paulvanbrenk

Repository: ai-patch-notes

Entity type: Environment

Environment: production

This should create a record matching your token’s subject (...:environment:production).

Common mismatch that causes exactly your error

If you previously created the federated credential for Branch (subject like repo:paulvanbrenk/ai-patch-notes:ref:refs/heads/main) but your workflow job has environment: production, GitHub will emit the environment subject (what you’re seeing), and the branch-based credential won’t match.

Fix (alternative): change your workflow to match the credential you already created

If you want the subject to be branch-based instead of environment-based, remove (or change) the job environment: so GitHub issues a token with a ref: subject instead of environment:.

Quick workflow checklist (OIDC essentials)

Make sure your job includes the OIDC permission and uses the three IDs:

permissions:
  id-token: write
  contents: read

steps:
  - uses: azure/login@v2
    with:
      client-id: ${{ secrets.AZURE_CLIENT_ID }}
      tenant-id: ${{ secrets.AZURE_TENANT_ID }}
      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}


Microsoft’s guidance calls out id-token: write and also notes the default audience is api://AzureADTokenExchange (which matches your log).

One more thing to verify

Ensure the federated credential is on the same Entra App Registration as the client-id you’re using (people sometimes add things to the wrong app/service principal).

If you paste your deploy.yml (just the permissions: + jobs.<jobname>: section) I can tell you whether GitHub will emit an environment: or ref: subject and what the federated credential subject must be.

## Required GitHub Secrets

The following secrets must be configured in your GitHub repository (Settings → Secrets and variables → Actions):

| Secret | Description |
|--------|-------------|
| `AZURE_CLIENT_ID` | Application (client) ID from your Entra App Registration |
| `AZURE_TENANT_ID` | Directory (tenant) ID from Azure |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token for Azure Static Web Apps |
| `DATABASE_CONNECTION_STRING` | Production database connection string for EF Core migrations |

The `DATABASE_CONNECTION_STRING` is used by the `migrate-database` job to apply EF Core migrations to the production database before deploying the API.
