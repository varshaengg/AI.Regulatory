---
description: 'Comprehensive development guidelines for Power Platform Custom Connectors using JSON Schema definitions. Covers API definitions (OpenAPI/Swagger 2.0), API properties, and settings configuration with Microsoft extensions.'
applyTo: '**/*.{json,md}'
---

# Power Platform Custom Connectors — Schema Development Guide

**Scope & audience:** This guide is for Microsoft employees who create or review **Power Platform Custom Connectors** using the `paconn` tool. It standardizes how we author **OpenAPI (Swagger) 2.0** definitions, **connector properties**, and **tool settings**, with a focus on Microsoft `x‑ms-*` extensions and schema validation rules.

---

## What’s in this workspace

The repository contains JSON Schemas that validate your connector assets and provide IntelliSense:

- **API Definition** — OpenAPI (Swagger) 2.0 with Microsoft extensions
- **API Properties** — connector metadata, authentication, policies, branding
- **Settings** — environment and deployment configuration for `paconn`

### File layout

#### 1) `apiDefinition.swagger.json`
**Purpose:** OpenAPI 2.0 API specification enriched with Power Platform extensions.

**Highlights**
- Full Swagger 2.0 surface: `info`, `paths`, `definitions`, `parameters`, `responses`, etc.
- Microsoft-specific extensions prefixed with `x-ms-*`
- Extended formats (e.g., `date-no-tz`, `html`)
- Dynamic schema features for runtime flexibility
- Security definitions: `oauth2`, `apiKey`, `basic`

#### 2) `apiProperties.json`
**Purpose:** Connector metadata, authentication, and policy configuration.

**Highlights**
- **Connection parameters** for OAuth, API key, and gateway scenarios
- **Policy template instances** for transformation/routing/pagination/trigger behaviors
- **Branding and metadata** (publisher, capabilities, icon color, stack owner)

#### 3) `settings.json`
**Purpose:** Environment targeting and `paconn` file path/endpoint configuration.

**Highlights**
- Target environment GUID
- File path mappings for assets/configs
- API endpoint URLs (e.g., PROD, TIP1)
- API version alignment with Power Platform services

---

## Development guidance

### A. API definition (`apiDefinition.swagger.json`)

#### A1. General
- **Validate against OpenAPI (Swagger) 2.0.** The schema enforces strict compliance.

#### A2. Microsoft extensions — operations
Use the following `x-ms-*` at **operation** level where applicable:

- `x-ms-summary` — short, user-friendly operation title (use **Title Case**)
- `x-ms-visibility` — parameter visibility: `important` | `advanced` | `internal`
- `x-ms-trigger` — marks triggers: `batch` | `single`
- `x-ms-trigger-hint` — helper text for trigger configuration
- `x-ms-trigger-metadata` — trigger config (e.g., `kind`, `mode`)
- `x-ms-notification` — webhook configuration for real-time notifications
- `x-ms-pageable` — pagination with `nextLinkName`
- `x-ms-safe-operation` — mark idempotent POST without side effects
- `x-ms-no-generic-test` — disable automatic testing
- `x-ms-operation-context` — operation simulation/test context

#### A3. Microsoft extensions — parameters
Apply at **parameter** level:

- `x-ms-dynamic-list` — dynamic dropdown options via API
- `x-ms-dynamic-values` — dynamic value sources for option population
- `x-ms-dynamic-tree` — hierarchical pickers for nested structures
- `x-ms-dynamic-schema` — runtime schema based on user selection
- `x-ms-dynamic-properties` — dynamic property configuration
- `x-ms-enum-values` — enums with friendly display names
- `x-ms-test-value` — **sample values for testing (never secrets)**
- `x-ms-trigger-value` — values for triggers (`value-collection`, `value-path`)
- `x-ms-url-encoding` — URL encoding style: `single` (default) | `double`
- `x-ms-parameter-location` — AutoRest hint (ignored by Power Platform)
- `x-ms-localizeDefaultValue` — localizable default values
- `x-ms-skip-url-encoding` — AutoRest path-encoding hint (ignored by Power Platform)

#### A4. Microsoft extensions — schemas
Apply at **schema/property** level:

- `x-ms-notification-url` — marks property as webhook notification URL
- `x-ms-media-kind` — `image` | `audio` for content media types
- `x-ms-enum` — enhanced enum metadata (AutoRest; ignored by Power Platform)
- **Note:** All parameter extensions above may also be used on schema properties.

#### A5. Microsoft extensions — root & path
- **Root-level**
  - `x-ms-capabilities` — connector-level capabilities (e.g., file picker, testConnection)
  - `x-ms-connector-metadata` — additional connector metadata
  - `x-ms-docs` — documentation configuration and references
  - `x-ms-deployment-version` — deployment version tracking
  - `x-ms-api-annotation` — API-level annotations
- **Path-level**
  - `x-ms-notification-content` — notification content schemas for webhook paths

#### A6. Operation-level capabilities
- `x-ms-capabilities` — operation-specific features, e.g., `chunkTransfer` for large files

#### A7. Security
- Define **`securityDefinitions`** to enforce authentication.
- **Up to two** security definitions allowed per connector (e.g., `oauth2 + apiKey`, `basic + apiKey`).
- **Exception:** If using **None** authentication, **no other** security definitions may appear.
- Guidance:
  - Prefer **`oauth2`** for modern APIs
  - Use **`apiKey`** for token-based access
  - Use **`basic`** only for internal/legacy systems
- Each definition must be exactly **one** type (validated via `oneOf`).

#### A8. Parameters — best practices
- Provide clear, complete `description` text
- Set `x-ms-summary` (Title Case) for a better UX
- Mark **required** parameters correctly
- Use appropriate `format` (including Power Platform extensions)
- Leverage dynamic extensions for validation and usability

#### A9. Extended formats (Power Platform)
- `date-no-tz` — date-time without time zone offset
- `html` — instructs clients to use an HTML editor/viewer
- Standard formats also supported: `int32`, `int64`, `float`, `double`, `byte`, `binary`, `date`, `date-time`, `password`, `email`, `uri`, `uuid`

---

### B. API properties (`apiProperties.json`)

#### B1. Connection parameters
- Choose types appropriately: `string`, `securestring`, `oauthSetting`
- Configure OAuth with the correct identity providers
- Use `allowedValues` to constrain selections where applicable
- Model dependencies for conditional/derived parameters

#### B2. Policy templates
- `routerequesttoendpoint` — route traffic to specific backends
- `setqueryparameter` — set default query parameters
- `updatenextlink` — handle pagination `nextLink`
- `pollingtrigger` — implement polling behavior for triggers

#### B3. Branding & metadata
- **`iconBrandColor` is required** for all connectors
- Declare `capabilities` (e.g., actions, triggers)
- Set meaningful `publisher` and `stackOwner`

---

### C. Settings (`settings.json`)

#### C1. Environment configuration
- Use a valid GUID for `environment` (see pattern below)
- Set `powerAppsUrl` and `flowUrl` for the target environment
- Align API versions with your service requirements

#### C2. File references
- Use the default filenames: `apiProperties.json`, `apiDefinition.swagger.json`
- Prefer **relative** paths for local development
- Ensure icon files exist and are correctly referenced

---

## Schema validation rules

### 1) Required properties
- **API Definition:** `swagger: "2.0"`, `info` (with `title`, `version`), `paths`
- **API Properties:** `properties` including **`iconBrandColor`**
- **Settings:** All properties optional (defaults allowed)

### 2) Pattern validation
- **Vendor extensions (non‑Microsoft):** must match `^x-(?!ms-)`
- **Path items:** must start with `/`
- **Environment GUID:** `^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$`
- **URLs:** valid `uri` format
- **Host pattern:** `^[^{}/ :\\]+(?::\\d+)?$` (no spaces, protocols, or paths)

### 3) Type & enum constraints
- **Security definitions:**
  - At most **two** entries in `securityDefinitions`
  - Each entry is exactly **one** type: `basic` | `apiKey` | `oauth2` (via `oneOf`)
  - **None** auth cannot co-exist with any other definition
- **Parameter types:** `string` | `number` | `integer` | `boolean` | `array` | `file`
- **Policy templates:** must satisfy type-specific parameter requirements
- **Format values:** include extended Power Platform formats (see A9)
- **Visibility values:** `important` | `advanced` | `internal`
- **Trigger types:** `batch` | `single`

### 4) Additional validation
- **`$ref` targets:** only `#/definitions/`, `#/parameters/`, or `#/responses/`
- **Path parameters:** must have `required: true`
- **`info` object:** `description` must differ from `title`
- **`contact` object:** `email` is valid email; `url` is a valid URI
- **`license` object:** `name` required; `url` (if present) must be valid URI
- **`externalDocs` object:** requires `url` (valid URI)
- **Tags:** names must be unique
- **Schemes:** `http`, `https`, `ws`, `wss` only
- **MIME types:** valid types in `consumes`/`produces`

---

## Common Patterns and Examples

### API Definition Examples

#### Basic Operation with Microsoft Extensions
```json
{
  "get": {
    "operationId": "GetItems",
    "summary": "Get items",
    "x-ms-summary": "Get Items",
    "x-ms-visibility": "important",
    "description": "Retrieves a list of items from the API",
    "parameters": [
      {
        "name": "category",
        "in": "query",
        "type": "string",
        "x-ms-summary": "Category",
        "x-ms-visibility": "important",
        "x-ms-dynamic-values": {
          "operationId": "GetCategories",
          "value-path": "id",
          "value-title": "name"
        }
      }
    ],
    "responses": {
      "200": {
        "description": "Success",
        "x-ms-summary": "Success",
        "schema": {
          "type": "object",
          "properties": {
            "items": {
              "type": "array",
              "x-ms-summary": "Items",
              "items": {
                "$ref": "#/definitions/Item"
              }
            }
          }
        }
      }
    }
  }
}
```

#### Trigger Operation Configuration
```json
{
  "get": {
    "operationId": "WhenItemCreated",
    "x-ms-summary": "When an Item is Created",
    "x-ms-trigger": "batch",
    "x-ms-trigger-hint": "To see it work now, create an item",
    "x-ms-trigger-metadata": {
      "kind": "query",
      "mode": "polling"
    },
    "x-ms-pageable": {
      "nextLinkName": "@odata.nextLink"
    }
  }
}
```

#### Dynamic Schema Example
```json
{
  "name": "dynamicSchema",
  "in": "body",
  "schema": {
    "x-ms-dynamic-schema": {
      "operationId": "GetSchema",
      "parameters": {
        "table": {
          "parameter": "table"
        }
      },
      "value-path": "schema"
    }
  }
}
```

#### File Picker Capability
```json
{
  "x-ms-capabilities": {
    "file-picker": {
      "open": {
        "operationId": "OneDriveFilePickerOpen",
        "parameters": {
          "dataset": {
            "value-property": "dataset"
          }
        }
      },
      "browse": {
        "operationId": "OneDriveFilePickerBrowse",
        "parameters": {
          "dataset": {
            "value-property": "dataset"
          }
        }
      },
      "value-title": "DisplayName",
      "value-collection": "value",
      "value-folder-property": "IsFolder",
      "value-media-property": "MediaType"
    }
  }
}
```

#### Test Connection Capability (Note: Not Supported for Custom Connectors)
```json
{
  "x-ms-capabilities": {
    "testConnection": {
      "operationId": "TestConnection",
      "parameters": {
        "param1": "literal-value"
      }
    }
  }
}
```

#### Operation Context for Simulation
```json
{
  "x-ms-operation-context": {
    "simulate": {
      "operationId": "SimulateOperation",
      "parameters": {
        "param1": {
          "parameter": "inputParam"
        }
      }
    }
  }
}
```

### Basic OAuth Configuration
```json
{
  "type": "oauthSetting",
  "oAuthSettings": {
    "identityProvider": "oauth2",
    "clientId": "your-client-id",
    "scopes": ["scope1", "scope2"],
    "redirectMode": "Global"
  }
}
```

#### Multiple Security Definitions Example
```json
{
  "securityDefinitions": {
    "oauth2": {
      "type": "oauth2",
      "flow": "accessCode",
      "authorizationUrl": "https://api.example.com/oauth/authorize",
      "tokenUrl": "https://api.example.com/oauth/token",
      "scopes": {
        "read": "Read access",
        "write": "Write access"
      }
    },
    "apiKey": {
      "type": "apiKey",
      "name": "X-API-Key",
      "in": "header"
    }
  }
}
```

**Note**: Maximum of two security definitions can coexist, but "None" authentication cannot be combined with other methods.

### Dynamic Parameter Setup
```json
{
  "x-ms-dynamic-values": {
    "operationId": "GetItems",
    "value-path": "id",
    "value-title": "name"
  }
}
```

### Policy Template for Routing
```json
{
  "templateId": "routerequesttoendpoint",
  "title": "Route to backend",
  "parameters": {
    "x-ms-apimTemplate-operationName": ["GetData"],
    "x-ms-apimTemplateParameter.newPath": "/api/v2/data"
  }
}
```

## Best Practices

1. **Use IntelliSense**: These schemas provide rich autocomplete and validation capabilities that help during development.
2. **Follow Naming Conventions**: Use descriptive names for operations and parameters to improve code readability.
3. **Implement Error Handling**: Define appropriate response schemas and error codes to handle failure scenarios properly.
4. **Test Thoroughly**: Validate schemas before deployment to catch issues early in the development process.
5. **Document Extensions**: Comment Microsoft-specific extensions for team understanding and future maintenance.
6. **Version Management**: Use semantic versioning in API info to track changes and compatibility.
7. **Security First**: Always implement appropriate authentication mechanisms to protect your API endpoints.

## Troubleshooting

### Common Schema Violations
- **Missing required properties**: `swagger: "2.0"`, `info.title`, `info.version`, `paths`
- **Invalid pattern formats**:
  - GUIDs must match exact format `^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$`
  - URLs must be valid URIs with proper scheme
  - Paths must start with `/`
  - Host must not include protocol, paths, or spaces
- **Incorrect vendor extension naming**: Use `x-ms-*` for Microsoft extensions, `^x-(?!ms-)` for others
- **Mismatched security definition types**: Each security definition must be exactly one type
- **Invalid enum values**: Check allowed values for `x-ms-visibility`, `x-ms-trigger`, parameter types
- **$ref pointing to invalid locations**: Must point to `#/definitions/`, `#/parameters/`, or `#/responses/`
- **Path parameters not marked as required**: All path parameters must have `required: true`
- **Type 'file' in wrong context**: Only allowed in `formData` parameters, not in schemas

### API Definition Specific Issues
- **Dynamic schema conflicts**: Can't use `x-ms-dynamic-schema` with fixed schema properties
- **Trigger configuration errors**: `x-ms-trigger-metadata` requires both `kind` and `mode`
- **Pagination setup**: `x-ms-pageable` requires `nextLinkName` property
- **File picker misconfiguration**: Must include both `open` operation and required properties
- **Capability conflicts**: Some capabilities may conflict with certain parameter types
- **Test value security**: Never include secrets or PII in `x-ms-test-value`
- **Operation context setup**: `x-ms-operation-context` requires a `simulate` object with `operationId`
- **Notification content schema**: Path-level `x-ms-notification-content` must define proper schema structure
- **Media kind restrictions**: `x-ms-media-kind` only supports `image` or `audio` values
- **Trigger value configuration**: `x-ms-trigger-value` must have at least one property (`value-collection` or `value-path`)

### Validation Tools
- Use JSON Schema validators to check your schema definitions for compliance.
- Leverage VS Code's built-in schema validation to catch errors during development.
- Test with paconn CLI before deployment using: `paconn validate --api-def apiDefinition.swagger.json`
- Validate against Power Platform connector requirements to ensure compatibility.
- Use the Power Platform Connector portal for validation and testing in the target environment.
- Check that operation responses match expected schemas to prevent runtime errors.

Remember: These schemas ensure your Power Platform connectors are properly formatted and will work correctly in the Power Platform ecosystem.
