# Bootstrapping Admin Access — AI Regulatory Assistant

## Situation

You're testing the deployed app and the **"Add user" button is disabled** on screen **A5 (User Management)** because you don't have Admin permissions.

## Root Cause

The API determines permissions by:
1. **Checking bootstrap OIDs** (Admin:BootstrapOids config) — fastest path
2. **Looking up AppUser record** by your Entra ID Object ID
3. **Reading your assigned personas** from the UserPersona join table

Your account exists in Entra ID but has **no AppUser record** and is **not in bootstrap OIDs**, so the system treats you as unauthorized.

## Solution

Add yourself to the `AppUser` and `UserPersona` tables with Admin persona.

### Prerequisites

- ✅ Access to the deployed SQL Server database
- ✅ Your Azure AD Object ID: `1a403da5-4458-420a-adaf-6ff802800cd8`
- ✅ SQL Management Studio or Azure Portal Query Editor

### Steps

**1. Open SQL Management Studio or Azure Portal → Query Editor**

Connect to your deployed AI Regulatory Assistant database.

**2. Copy and execute the bootstrap script**

File: `data/sql/make-sopan-admin.sql`

```sql
-- The script does the following:
-- a) Inserts a new AppUser row for Sopan (if not exists)
-- b) Assigns the Admin persona to this user
-- c) Verifies the result
```

**3. Verify success**

Run this verification query:

```sql
SELECT 
    u.[Id],
    u.[AadObjectId],
    u.[DisplayName],
    u.[Email],
    p.[Code] AS [Persona],
    u.[IsActive]
FROM [dbo].[AppUser] u
LEFT JOIN [dbo].[UserPersona] up ON u.[Id] = up.[UserId]
LEFT JOIN [dbo].[Persona] p ON up.[PersonaId] = p.[Id]
WHERE u.[AadObjectId] = CAST('1a403da5-4458-420a-adaf-6ff802800cd8' AS UNIQUEIDENTIFIER);
```

Expected result:
```
Id    | AadObjectId                          | DisplayName | Email                                | Persona | IsActive
------|--------------------------------------|-------------|--------------------------------------|---------|----------
<ID>  | 1a403da5-4458-420a-adaf-6ff802800cd8 | Sopan       | sopan@varshassive.onmicrosoft.com   | Admin   | 1
```

**4. Test in the app**

- Refresh the browser (clear cache if needed)
- Navigate to **A5 (User Management)**
- The **"Add user" button** should now be ✅ **ENABLED**

### Testing the Full Flow

Once enabled, you can:

| Action | Screen | Expected Result |
|--------|--------|-----------------|
| Click "Add user" | A5 | Opens people picker dialog |
| Search for a colleague | A5 | Searches live Microsoft Graph (AAD-only) |
| Select and add user | A5 | Creates new AppUser + shows in table |
| Assign personas | A5 | Updates UserPersona join table |
| View permission matrix | A6 | Shows all persona-feature-verb combinations |
| Toggle permissions | A6 | Changes PersonaPermission matrix |

### Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| Button still disabled after script | Browser cached old permissions | Hard refresh (Ctrl+Shift+R) or clear sessionStorage |
| "Persona not found" SQL error | Seed data not deployed | Run dacpac deployment with post-deployment script |
| Can't find user in people picker | People picker is AAD-only, not on-prem AD | Make sure you're searching for @varshassive.onmicrosoft.com users |
| "Add user" dialog closes without saving | API is returning 400/401 | Check browser Network tab, look for error response |

### Next Steps

Once you have Admin access, you can:

1. **Add team members** via A5 (people picker)
2. **Assign personas** (Admin, RaLead, RaAuthor, RaReviewer)
3. **Configure permissions** via A6 (permission matrix)
4. **Test dossier workflows** once permissions are set

---

## Reference: Database Schema

```
AppUser (id, AadObjectId, DisplayName, Email, Upn, IsActive, CreatedUtc, UpdatedUtc)
    ↓ (1:N join)
UserPersona (UserId, PersonaId, AssignedUtc, AssignedBy)
    ↓ (N:1)
Persona (id, Code, Name, Description, IsSystem)
```

When A5/A6 queries happen:
1. **GET /api/v1/me/permissions** → reads `AppUser` by OID → collects personas from `UserPersona`
2. **GET /api/v1/permissions/matrix** → expands `PersonaPermission` for each persona → assembles grant list
3. **React usePermissions hook** → caches grants in context → drives A5 button visibility

---

## SQL Quick Reference

### View all users and their personas

```sql
SELECT 
    u.Id, 
    u.DisplayName, 
    u.Email, 
    p.Code AS Persona
FROM dbo.AppUser u
LEFT JOIN dbo.UserPersona up ON u.Id = up.UserId
LEFT JOIN dbo.Persona p ON up.PersonaId = p.Id
ORDER BY u.DisplayName, p.Code;
```

### Check who has Admin persona

```sql
SELECT u.DisplayName, u.Email
FROM dbo.AppUser u
INNER JOIN dbo.UserPersona up ON u.Id = up.UserId
INNER JOIN dbo.Persona p ON up.PersonaId = p.Id
WHERE p.Code = 'Admin';
```

### Remove a user

```sql
DELETE FROM dbo.AppUser WHERE AadObjectId = '<GUID>';
-- Cascades automatically to UserPersona via FK
```

---

**Author:** Copilot  
**Date:** 2026-07-22  
**Status:** Production-Ready Bootstrap Guide
