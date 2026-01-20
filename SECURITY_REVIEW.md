# Security Review Report: AI-Patch-Notes

**Review Date:** 2026-01-20
**Reviewer:** Automated Security Analysis

## Executive Summary

This codebase has some security considerations to address. The current API key authentication is a known temporary measure until user management with JWT/session cookies is implemented.

**Note:** The client-side API key pattern (`VITE_API_KEY`) is intentionally a placeholder for development. This will be replaced with proper JWT or session-based authentication once user management is set up.

---

## HIGH SEVERITY FINDINGS

### 1. Missing Authentication on AI Summarization Endpoint

**File:** `PatchNotes.Api/Program.cs:384-447`

```csharp
// POST /api/releases/{id}/summarize - No authentication filter applied
app.MapPost("/api/releases/{id:int}/summarize", async (int id, HttpContext httpContext, ...) =>
{
    // Makes calls to external AI APIs (costs money per request)
    var summary = await aiClient.SummarizeReleaseNotesAsync(release.Title, release.Body);
});
```

**Category:** API Security
**Severity:** HIGH

**Description:** The `/api/releases/{id}/summarize` endpoint does not require API key authentication, yet it:
1. Makes calls to external AI APIs (costing money per request)
2. Supports streaming SSE connections that hold server resources
3. Can be abused for denial-of-service or cost inflation attacks

**Exploit Scenario:**
```bash
# Attacker runs infinite loop to drain AI API credits
while true; do
  curl -X POST https://api.mypkgupdate.com/api/releases/1/summarize &
done
```

**Recommended Fix:**
- Add `.AddEndpointFilterFactory(requireApiKey)` to the endpoint
- Implement rate limiting
- Add usage monitoring and alerts

---

## MEDIUM SEVERITY FINDINGS

### 2. Missing Authentication on Notification Endpoints

**File:** `PatchNotes.Api/Program.cs:450-498`

```csharp
// GET /api/notifications - No authentication required
app.MapGet("/api/notifications", async (...) => { ... });

// GET /api/notifications/unread-count - No authentication required
app.MapGet("/api/notifications/unread-count", async (...) => { ... });
```

**Category:** API Security / Data Exposure
**Severity:** MEDIUM

**Description:** Notification endpoints expose user activity and repository watching patterns without authentication. This information could be used for social engineering attacks or identifying security-sensitive projects being watched.

**Recommended Fix:**
- Add authentication to notification read endpoints
- Consider if notifications should be user-scoped

---

### 3. Overly Permissive CORS Configuration

**File:** `PatchNotes.Api/Program.cs:45-53`

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()   // <-- Overly permissive
              .AllowAnyMethod();  // <-- Overly permissive
    });
});
```

**Category:** API Security
**Severity:** MEDIUM

**Description:** While origins are restricted to localhost (development), the policy allows any header and any method. In production, this should be tightened to only allow specific headers and methods needed by the frontend.

**Recommended Fix:**
```csharp
policy.WithOrigins("https://app.mypkgupdate.com")
      .WithHeaders("Content-Type", "X-API-Key", "Accept")
      .WithMethods("GET", "POST", "PATCH", "DELETE");
```

---

### 4. Potential Stored XSS via Markdown Rendering

**File:** `patchnotes-web/src/pages/ReleaseDetail.tsx:233`

```tsx
<div className="prose-release">
  <Markdown>{release.body}</Markdown>
</div>
```

**Category:** Injection & Code Execution (XSS)
**Severity:** MEDIUM

**Description:** Release notes fetched from GitHub are rendered as Markdown. While `react-markdown` is generally safe by default, the risk exists if additional rehype plugins are added that enable HTML, or if GitHub's markdown contains crafted content.

**Recommended Fix:**
- Explicitly configure `react-markdown` to disallow dangerous elements
- Add Content-Security-Policy headers to prevent inline script execution

---

## Positive Security Findings

### SSRF Prevention Implemented

**File:** `PatchNotes.Data/GitHub/GitHubClient.cs:34`

The GitHub client properly escapes user input using `Uri.EscapeDataString()`, preventing path traversal and SSRF attacks.

### SQL Injection Prevention Implemented

The codebase uses Entity Framework Core with parameterized queries throughout. No raw SQL or string concatenation was found in database operations.

---

## Summary Table

| # | Vulnerability | Severity | Category | File |
|---|--------------|----------|----------|------|
| 1 | Unauthenticated AI endpoint | HIGH | API Security | `PatchNotes.Api/Program.cs` |
| 2 | Unauthenticated notification endpoints | MEDIUM | Data Exposure | `PatchNotes.Api/Program.cs` |
| 3 | Overly permissive CORS | MEDIUM | API Security | `PatchNotes.Api/Program.cs` |
| 4 | Potential XSS via Markdown | MEDIUM | XSS | `ReleaseDetail.tsx` |

---

## Recommended Priority Actions

1. **Immediate:** Add authentication to the `/api/releases/{id}/summarize` endpoint (protects AI API costs)
2. **Short-term:** Implement rate limiting middleware
3. **Short-term:** Tighten CORS configuration for production
4. **Medium-term:** Implement JWT or session-based authentication when user management is added
