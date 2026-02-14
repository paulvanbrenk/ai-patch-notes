# patchnotes-email

Azure Functions project for email delivery in the PatchNotes application.

## Overview

A TypeScript Azure Functions app that handles email notifications via [Resend](https://resend.com/). Provides welcome emails, release notifications, and weekly digest emails.

## Functions

| Function | Trigger | Description |
|----------|---------|-------------|
| `sendWelcome` | HTTP POST | Sends welcome email to new users |
| `sendRelease` | HTTP POST | Sends release notification email |
| `sendDigest` | Timer | Sends weekly digest of release activity |

## Running

```bash
pnpm install
pnpm start
```

This builds the TypeScript and starts the Azure Functions runtime.

## Scripts

| Command | Description |
|---------|-------------|
| `pnpm build` | Compile TypeScript |
| `pnpm watch` | Watch mode compilation |
| `pnpm clean` | Remove dist/ |
| `pnpm start` | Clean, build, and start function host |

## Configuration

Create a `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "RESEND_API_KEY": "<your-resend-api-key>"
  }
}
```

## Directory Structure

```
patchnotes-email/
├── src/
│   ├── functions/
│   │   ├── sendWelcome.ts    # Welcome email (HTTP trigger)
│   │   ├── sendRelease.ts    # Release notification (HTTP trigger)
│   │   └── sendDigest.ts     # Weekly digest (timer trigger)
│   └── lib/
│       └── resend.ts         # Resend client setup
├── host.json                 # Function host configuration
├── package.json
└── tsconfig.json
```

## Dependencies

- **@azure/functions** - Azure Functions SDK
- **resend** - Email delivery API
