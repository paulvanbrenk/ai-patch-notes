# Data Model

## Packages

| Column | Type | Nullable |
|---|---|---|
| Id | string(21) | No |
| Name | text | No |
| Url | text | No |
| NpmName | nvarchar(256) | Yes |
| GithubOwner | nvarchar(128) | No |
| GithubRepo | nvarchar(128) | No |
| LastFetchedAt | datetime2 | Yes |
| CreatedAt | datetime2 | No |

## Releases

| Column | Type | Nullable |
|---|---|---|
| Id | string(21) | No |
| PackageId | string(21) | No |
| Version | nvarchar(128) | No |
| Title | text | Yes |
| Body | text | Yes |
| PublishedAt | datetime2 | No |
| FetchedAt | datetime2 | No |
| Major | int | No |
| Minor | int | No |
| IsPrerelease | bit | No |

## Summaries

| Column | Type | Nullable |
|---|---|---|
| Id | string(21) | No |
| PackageId | string(21) | No |
| VersionGroup | nvarchar(64) | No |
| Period | int (enum: Week=0, Month=1) | No |
| PeriodStart | datetime2 | No |
| Content | text | No |
| GeneratedAt | datetime2 | No |

## Users

| Column | Type | Nullable |
|---|---|---|
| Id | string(21) | No |
| StytchUserId | nvarchar(128) | No |
| Email | nvarchar(256) | Yes |
| Name | nvarchar(256) | Yes |
| CreatedAt | datetime2 | No |
| UpdatedAt | datetime2 | No |
| LastLoginAt | datetime2 | Yes |

## Watchlists

| Column | Type | Nullable |
|---|---|---|
| Id | string(21) | No |
| UserId | string(21) | No |
| PackageId | string(21) | No |
| CreatedAt | datetime2 | No |

**Indexes:** Unique on `(UserId, PackageId)`
**Foreign Keys:** `UserId` → Users.Id, `PackageId` → Packages.Id
