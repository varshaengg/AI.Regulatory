# GitHub Copilot Instructions

## AI-Native Engineering Standards (NON-NEGOTIABLE)

All work in this repository is governed by the **AI-Native Engineering Non-Negotiable Standards** defined in [instructions/ai-native-standards.instructions.md](./instructions/ai-native-standards.instructions.md). These standards apply to every agent, skill, recipe, and workflow — regardless of team, toolchain, or platform.

**Core mandates** (always in effect):
1. **Specification is the source of truth** — no implementation without a traceable requirement
2. **Architecture decisions are documented** — ADRs for every significant choice
3. **Quality gates block, not warn** — security, compliance, and test gates must pass before advancement
4. **Auditability is non-optional** — every change must be traceable from requirement to production
5. **Humans own outcomes** — AI proposes, humans approve; no auto-shipping past quality gates

To validate compliance, invoke the `ai-native-standards-enforcement` skill in `.github/skills/ai-native-standards-enforcement/SKILL.md`.

---

## Project Overview

<!-- TODO: Replace with your project description -->

**Project Name**: [PROJECT_NAME]
**Project Type**: [POWER_PLATFORM_APP | D365_CUSTOMIZATION | .NET_API | PYTHON_SERVICE | DATA_SCIENCE_MODEL | HYBRID_SOLUTION]
**Tech Stack**: [POWER_PLATFORM | D365 | .NET_CORE | PYTHON | AZURE_ML | FABRIC | OTHER]
**Solution Type**: [LOW_CODE | PRO_CODE | HYBRID]
**Description**: [Brief description of what this project does and its purpose]

## Azure DevOps Configuration

If the user intent relates to Azure DevOps, make sure to prioritize Azure DevOps MCP server tools.
**ADO_DEFAULT_PROJECT**: "[YOUR_ADO_PROJECT_NAME]"
**ADO_DEFAULT_AREA_PATH**: "[YOUR_AREA_PATH]"
**ADO_DEFAULT_REPO**: "[YOUR_REPOSITORY_NAME]"

## Development Environment

### Prerequisites - Low Code Solutions

<!-- TODO: List required tools and environments for Power Platform/D365 -->

- Power Platform Developer Plan or appropriate licensing
- Power Platform CLI version [VERSION]
- Dataverse environment: [ENVIRONMENT_NAME]
- D365 instance: [D365_INSTANCE_URL] (if applicable)
- Power Platform Build Tools for Azure DevOps

### Prerequisites - Pro Code Solutions

<!-- TODO: List required tools, versions, and setup instructions -->

- .NET SDK version [VERSION] (if using .NET)
- Python version [VERSION] (if using Python)
- Visual Studio [VERSION] or VS Code
- Azure CLI version [VERSION]
- Azure ML SDK version [VERSION] (for Data Science projects)
- SQL Server Management Studio (if applicable)

### Setup Instructions

<!-- TODO: Add step-by-step setup instructions -->

1. [Environment setup - Dev/Test/Prod environments]
2. [Service connections configuration in Azure DevOps]
3. [Power Platform environment variables setup]
4. [Database connection strings and secrets configuration]
5. [Additional steps as needed]

## Build and Deploy Commands

### Power Platform Solutions

<!-- TODO: Replace with your Power Platform build commands -->

```powershell
# Pack Power Platform solution
pac solution pack --zipfile [SOLUTION_NAME].zip --folder [SOLUTION_FOLDER] --processCanvasApps

# Import solution to environment
pac solution import --path [SOLUTION_NAME].zip --environment [ENVIRONMENT_URL]

# Export solution from environment
pac solution export --name [SOLUTION_NAME] --path [OUTPUT_PATH] --managed true
```

### .NET Applications

<!-- TODO: Replace with your .NET build commands -->

```powershell
# Restore dependencies
dotnet restore

# Build application
dotnet build --configuration Release

# Publish application
dotnet publish --configuration Release --output ./publish
```

### Python Applications

<!-- TODO: Replace with your Python build commands -->

```powershell
# Install dependencies
pip install -r requirements.txt

# Run application
python [MAIN_APP_FILE].py

# Create package
python setup.py sdist bdist_wheel
```

### Data Science Models

<!-- TODO: Replace with your ML/AI build commands -->

```powershell
# Train model
python train_model.py --config [CONFIG_FILE]

# Deploy model to Azure ML
az ml model deploy --model [MODEL_NAME] --compute-target [COMPUTE_TARGET]
```

### Test Commands

<!-- TODO: Replace with your actual test commands -->

```powershell
# Power Platform - Solution Checker
pac solution check --path [SOLUTION_PATH]

# .NET - Run unit tests
dotnet test

# Python - Run pytest
pytest tests/ --cov=[MODULE_NAME]

# Data Science - Model validation
python validate_model.py --test-data [TEST_DATA_PATH]
```

## Code Standards and Guidelines

### Code Style

<!-- TODO: Specify your code style preferences -->

- **Language**: [PRIMARY_LANGUAGE]
- **Style Guide**: [STYLE_GUIDE_REFERENCE]
- **Linting**: [LINTER_TOOL] with [CONFIG_FILE]
- **Formatting**: [FORMATTER_TOOL] with [CONFIG_FILE]

### Naming Conventions

<!-- TODO: Define your naming conventions -->

- **Files**: [FILE_NAMING_CONVENTION]
- **Functions**: [FUNCTION_NAMING_CONVENTION]
- **Variables**: [VARIABLE_NAMING_CONVENTION]
- **Classes**: [CLASS_NAMING_CONVENTION]

### Architecture Patterns

<!-- TODO: Describe your architectural patterns -->

- **Design Pattern**: [PATTERN_NAME]
- **Folder Structure**: [DESCRIBE_FOLDER_STRUCTURE]
- **Component Organization**: [DESCRIBE_ORGANIZATION]

## Testing Strategy

### Test Types

<!-- TODO: Define your testing approach -->

- **Power Platform Tests**: Solution Checker, Canvas App Tests, Flow Tests
- **Unit Tests**: [UNIT_TEST_FRAMEWORK] - [DESCRIPTION]
  - For C# unit tests, see [csharp-test-best-practices.instructions.md](./instructions/csharp-test-best-practices.instructions.md) for conventions and best practices.
- **Integration Tests**: [INTEGRATION_TEST_FRAMEWORK] - [DESCRIPTION]
- **Data Validation Tests**: Model accuracy, data quality checks
- **User Acceptance Tests**: Power Platform app testing with business users

### Test Coverage

<!-- TODO: Set your coverage requirements -->

- **Minimum Coverage**: [PERCENTAGE]%
- **Coverage Tool**: [COVERAGE_TOOL]

## Security Considerations

<!-- TODO: Add security guidelines specific to your project -->

- Power Platform: Data Loss Prevention (DLP) policies compliance
- D365: Security roles and field-level security configuration
- .NET: OWASP guidelines implementation
- Python: Secure coding practices and dependency scanning
- Data Science: Data privacy, PII handling, and model security
- Azure: Key Vault integration for secrets management
- To automate enforcement of Microsoft's S360 KPIs, the `compliance-enforcement` skill in `.github/skills/compliance-enforcement/` provides guidance on implementing left-shift enforcement across privacy, security, and compliance domains
- **PII Handling**: All agents and recipes must follow the `pii-scrubbing` skill in `.github/skills/pii-scrubbing/SKILL.md` — scan inputs/outputs for PII, apply appropriate scrubbing (redaction, masking, pseudonymization), and never include real PII in generated artifacts, code samples, or test data

## Deployment

### Environments

<!-- TODO: Define your deployment environments -->

- **Development**: [DEV_ENVIRONMENT_DETAILS]
- **Staging**: [STAGING_ENVIRONMENT_DETAILS]
- **Production**: [PROD_ENVIRONMENT_DETAILS]

### Deployment Commands

<!-- TODO: Add deployment instructions -->

## Team-Specific Guidelines

### Azure DevOps Workflow

<!-- TODO: Define your Azure DevOps workflow -->

- **Branching Strategy**: [AZURE_DEVOPS_FLOW | FEATURE_BRANCH | CUSTOM]
- **Branch Naming**: [BRANCH_NAMING_CONVENTION]
- **Work Item Integration**: [WORK_ITEM_LINKING_STRATEGY]
- **Build Pipeline**: [BUILD_PIPELINE_STRATEGY]

### Pull Request Guidelines

<!-- TODO: Define PR requirements -->

- **Required Reviewers**: [NUMBER_OF_REVIEWERS]
- **Required Checks**: [BUILD_VALIDATION | SECURITY_SCAN | SOLUTION_CHECKER]
- **PR Template**: [LINK_TO_PR_TEMPLATE]
- **Work Item Association**: [WORK_ITEM_ASSOCIATION_POLICY]

### Code Review Checklist

<!-- TODO: Add code review criteria -->

- [ ] Power Platform solution follows best practices and naming conventions
- [ ] .NET code follows SOLID principles and coding standards
- [ ] Python code follows PEP 8 and includes proper documentation
- [ ] Data Science models include validation and testing
- [ ] Security requirements are met for the technology stack
- [ ] Performance considerations are addressed

## Documentation

<!-- TODO: Specify documentation requirements -->

- **API Documentation**: [TOOL_OR_LOCATION]
- **Architecture Docs**: [LOCATION]
- **User Guides**: [LOCATION]

## Troubleshooting

<!-- TODO: Add common issues and solutions -->

### Common Issues

1. **Issue**: [PROBLEM_DESCRIPTION]
   **Solution**: [SOLUTION_DESCRIPTION]

2. **Issue**: [PROBLEM_DESCRIPTION]
   **Solution**: [SOLUTION_DESCRIPTION]

## Contact Information

<!-- TODO: Add team contact details -->

- **Team**: [NAME] - [EMAIL]
- **Teams Channel**: [CHANNEL_NAME]
- **Team Wiki**: [WIKI_LINK]
