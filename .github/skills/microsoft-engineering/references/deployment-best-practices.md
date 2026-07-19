# Microsoft Internal Deployment Best Practices

Comprehensive deployment best practices for Microsoft internal teams, with specific guidance on OneBranch and Ev2 deployment systems.

## OneBranch Deployment System

### Overview
OneBranch is Microsoft's unified build and deployment system providing secure, compliant, and scalable CI/CD pipelines.

### Key Principles
- **Security by Default**: All builds run in secure, isolated environments
- **Compliance First**: Built-in compliance scanning and reporting
- **Reproducible Builds**: Deterministic build processes with full traceability
- **Zero Trust Architecture**: No persistent credentials or secrets in build agents

### OneBranch Pipeline Configuration

#### Basic Pipeline Structure
```yaml
version: https://aka.ms/1ES.PT.v0

extends:
  template: v1/1ES.Official.PipelineTemplate.yml@1ESPipelineTemplates
  parameters:
    pool:
      name: Azure-Pipelines-1ESPT-ExDShared
      image: windows-2022
      os: windows

    stages:
    - stage: Build
      jobs:
      - job: BuildJob
        steps:
        - task: onebranch.pipeline.tsaoptions@1
          displayName: 'Configure TSA Options'
          inputs:
            tsaConfigFilePath: '$(Build.SourcesDirectory)\.config\tsaoptions.json'

        - task: DotNetCoreCLI@2
          displayName: 'Build Application'
          inputs:
            command: 'build'
            projects: '**/*.sln'
            arguments: '--configuration Release'

        - task: onebranch.pipeline.signing@1
          displayName: 'Sign Binaries'
          inputs:
            command: 'sign'
            signing_profile: 'external_distribution'
            files_to_sign: '**/*.exe;**/*.dll'
```

#### Security and Compliance Integration
```yaml
- task: ComponentGovernanceComponentDetection@0
  displayName: 'Component Detection'
  inputs:
    scanType: 'Register'
    verbosity: 'Verbose'
    alertWarningLevel: 'High'

- task: guardian@1
  displayName: 'Run Guardian Security Scan'
  inputs:
    command: 'run'
    configPath: '$(Build.SourcesDirectory)\.config\guardian.yml'
```

## Ev2 (Express V2) Deployment Platform

### Overview
Enterprise deployment platform providing safe, reliable, and scalable deployment capabilities for Azure services.

### Key Features
- **Progressive Deployment**: Gradual rollouts with automatic rollback
- **Health Monitoring**: Real-time health checks and alerting
- **Approval Gates**: Multi-stage approval processes for production
- **Integration**: Deep integration with Azure Resource Manager

### Service Model Definition
```json
{
  "$schema": "https://ev2schema.azure.net/schemas/2020-01-01/ServiceModel.json",
  "contentVersion": "1.0.0.0",
  "serviceMetadata": {
    "serviceGroup": "MyServiceGroup",
    "environment": "Production"
  },
  "serviceResourceGroupDefinitions": [
    {
      "name": "MyApp-RG",
      "serviceResources": [
        {
          "name": "MyAppService",
          "type": "Microsoft.Web/sites",
          "armTemplate": {
            "templatePath": "Templates/webapp.json",
            "parameterPath": "Parameters/webapp.parameters.json"
          }
        }
      ]
    }
  ]
}
```

### Rollout Specification
```json
{
  "$schema": "https://ev2schema.azure.net/schemas/2020-01-01/RolloutSpec.json",
  "orchestratedSteps": [
    {
      "name": "Ring0-Deployment",
      "targetType": "Region",
      "targets": [{"name": "West US 2"}],
      "postDeploymentSteps": [
        {
          "stepType": "WaitStep",
          "waitDurationInMinutes": 30
        },
        {
          "stepType": "HealthCheck",
          "healthCheckDefinition": {
            "healthChecks": [
              {
                "name": "AppHealthCheck",
                "type": "Http",
                "endpoint": "https://myapp-ring0.azurewebsites.net/health",
                "expectedStatusCode": 200
              }
            ]
          }
        }
      ]
    },
    {
      "name": "Ring1-Deployment",
      "dependsOnSteps": ["Ring0-Deployment"],
      "manualApprovalStep": {
        "approvers": ["deployment-team@microsoft.com"],
        "timeoutInHours": 24
      }
    }
  ]
}
```

## Multi-Stage Pipeline Design

### Stage Definitions
1. **Build Stage**: Compile, test, package
2. **Security Gate**: Security scanning and compliance validation
3. **Ring 0 (Canary)**: Limited scope initial validation
4. **Ring 1 (Pilot)**: Broader scope with monitoring
5. **Ring 2 (Production)**: Full production deployment

### Pipeline Integration
```yaml
trigger:
  branches:
    include:
    - main
    - release/*

extends:
  template: v1/1ES.Official.PipelineTemplate.yml@1ESPipelineTemplates
  parameters:
    stages:
    - stage: Build
      jobs:
      - template: templates/onebranch-build.yml

    - stage: SecurityGate
      dependsOn: Build
      jobs:
      - template: templates/security-validation.yml

    - stage: Ring0Deploy
      dependsOn: SecurityGate
      jobs:
      - template: templates/ev2-deploy.yml
        parameters:
          environment: 'Ring0'

    - stage: Ring1Deploy
      dependsOn: Ring0Deploy
      condition: succeeded()
      jobs:
      - template: templates/ev2-deploy.yml
        parameters:
          environment: 'Ring1'
          requiresApproval: true

    - stage: ProductionDeploy
      dependsOn: Ring1Deploy
      condition: succeeded()
      jobs:
      - template: templates/ev2-deploy.yml
        parameters:
          environment: 'Production'
          requiresApproval: true
```

## Environment Promotion Criteria

```yaml
promotionGates:
  ring0ToRing1:
    healthCheckDuration: PT30M
    successRate: 99.5
    errorRate: <0.1%
    latency: <500ms

  ring1ToProduction:
    healthCheckDuration: PT2H
    successRate: 99.9
    errorRate: <0.01%
    latency: <200ms
    manualApproval: true
```

## Automated Rollback

### Rollback Triggers
- Health check failures for 10+ minutes
- Error rate exceeds 1.0% for 5 minutes
- Latency threshold exceeds 1000ms for 3 minutes

### Rollback Strategy
- Type: Previous version rollback
- Max rollback time: 1 hour
- Verification: Health check on rollback endpoint

## Security Best Practices

### Signing
- Always sign all executables and libraries
- Use Microsoft-approved signing certificates
- Implement certificate rotation policies

### Scanning
- Run Guardian security analysis on all builds
- Component governance for OSS dependencies
- Anti-malware scanning on artifacts

### Secrets
- Never store secrets in source code
- Use Azure Key Vault with Managed Identity
- Implement proper RBAC for secret access

## Internal Resources

- [OneBranch Documentation](https://aka.ms/onebranch)
- [Ev2 Platform Guide](https://aka.ms/ev2)
- [Microsoft Engineering Playbook](https://eng.ms/)
- [Security Development Lifecycle](https://www.microsoft.com/securityengineering/sdl/)
