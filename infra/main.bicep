// PatchNotes Azure Infrastructure
// Deploy with: az deployment group create --resource-group MyPkgUpdate --template-file main.bicep

@description('Location for all resources')
param location string = 'centralus'

@description('GitHub repository in format owner/repo')
param gitHubRepo string = 'tinytoolsllc/ai-patch-notes'

@description('App Service name')
param apiAppName string = 'api-mypkgupdate-com'

@description('Static Web App name')
param staticWebAppName string = 'mypkgupdate'

@description('Storage account name')
param storageAccountName string = 'mypkgupdatest'

// User-assigned Managed Identity for GitHub Actions OIDC
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${apiAppName}-id'
  location: location
}

// Federated credential for GitHub Actions main branch
resource federatedCredentialMain 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: managedIdentity
  name: 'github-actions-main'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${gitHubRepo}:ref:refs/heads/main'
    audiences: ['api://AzureADTokenExchange']
  }
}

// Federated credential for GitHub Actions production environment
resource federatedCredentialProduction 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: managedIdentity
  name: 'github-actions-production'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${gitHubRepo}:environment:production'
    audiences: ['api://AzureADTokenExchange']
  }
}

// App Service Plan (Free tier)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'ASP-MyPkgUpdate'
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
    size: 'F1'
    family: 'F'
    capacity: 0
  }
  kind: 'app'
  properties: {
    reserved: false // Windows
  }
}

// App Service (.NET 10 API)
resource apiApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiAppName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      use32BitWorkerProcess: true
      cors: {
        allowedOrigins: [
          'https://app.mypkgupdate.com'
          'http://localhost:5173'
          'http://localhost:3000'
        ]
        supportCredentials: true
      }
    }
  }
}

// App Settings (secrets should be set separately via CLI or Key Vault)
resource apiAppSettings 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: apiApp
  name: 'appsettings'
  properties: {
    // Stytch settings - values should be set via CLI:
    // az webapp config appsettings set --name api-mypkgupdate-com --resource-group MyPkgUpdate --settings Stytch__ProjectId=xxx Stytch__Secret=xxx Stytch__WebhookSecret=xxx
    WEBSITE_RUN_FROM_PACKAGE: '1'
  }
}

// Static Web App (Frontend)
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: 'https://github.com/${gitHubRepo}'
    branch: 'main'
    stagingEnvironmentPolicy: 'Enabled'
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// Storage Account (for database/files)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

// Role assignment: Contributor on resource group for managed identity
// This allows GitHub Actions to deploy to the App Service
resource contributorRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, managedIdentity.id, 'contributor')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c') // Contributor
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output managedIdentityClientId string = managedIdentity.properties.clientId
output managedIdentityPrincipalId string = managedIdentity.properties.principalId
