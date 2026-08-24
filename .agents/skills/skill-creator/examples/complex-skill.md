---
name: cloud-deployment
description: |
  Deploy applications to AWS, GCP, or Azure with AI Agent using provider-specific patterns.
  Use when: (1) deploying to cloud platforms, (2) generating infrastructure-as-code,
  (3) validating cloud configurations, (4) managing multi-cloud deployments,
  (5) implementing CI/CD for cloud deployment
---

# Cloud Deployment Assistant

Help AI Agent generate and validate infrastructure deployments across AWS, GCP, and Azure using provider-specific patterns and deterministic validation scripts.

## Cloud Platform Selection

When deploying with AI Agent, select your cloud provider:

### AWS Deployment

AI Agent can help with:
- CloudFormation templates (JSON/YAML)
- Terraform configurations
- SAM (Serverless Application Model) templates
- CDK (Cloud Development Kit) code

**Validation**:
```bash
python scripts/validate_aws.py cloudformation.yaml
```

**Examples**: See [aws-deployment-patterns.md](../references/aws-deployment-patterns.md)

### GCP Deployment

AI Agent can help with:
- Google Cloud Deployment Manager templates
- Terraform for GCP
- Infrastructure as Code (bicep alternative)
- Cloud Run configurations

**Validation**:
```bash
python scripts/validate_gcp.py deployment.yaml
```

**Examples**: See [gcp-deployment-patterns.md](../references/gcp-deployment-patterns.md)

### Azure Deployment

AI Agent can help with:
- Bicep templates (recommended)
- ARM (Azure Resource Manager) templates
- Terraform for Azure
- Azure CLI scripts
- Azure Resource MCP integration

**Validation**:
```bash
python scripts/validate_azure.py template.bicep
```

**Examples**: See [azure-deployment-patterns.md](../references/azure-deployment-patterns.md)

---

## Deployment Workflow

### Step 1: Choose Provider & Service

AI Agent asks: "What are you deploying? (App, Database, Function, Static Site, etc.)"

Supported services:
- **Compute**: App Service, Lambda, Cloud Run, VMs
- **Databases**: RDS, Cloud SQL, Azure Database
- **Serverless**: Lambda, Cloud Functions, Azure Functions
- **Storage**: S3, Cloud Storage, Azure Blob Storage
- **Networking**: VPC, VPN, Load Balancers

### Step 2: Generate Infrastructure Code

AI Agent generates provider-specific code:

**AWS Example**:
```yaml
AWSTemplateFormatVersion: '2010-09-09'
Description: 'App Service with ALB'
Resources:
  ApplicationRole:
    Type: AWS::IAM::Role
    Properties:
      AssumeRolePolicyDocument:
        Version: '2012-10-17'
        Statement:
          - Effect: Allow
            Principal:
              Service: ec2.amazonaws.com
            Action: sts:AssumeRole
```

**GCP Example**:
```yaml
resources:
  - name: app-deployment
    type: compute.v1.instance
    properties:
      zone: us-central1-a
      machineType: zones/us-central1-a/machineTypes/n1-standard-1
```

**Azure Example (Bicep)**:
```bicep
param location string = 'eastus'
param appServicePlanName string = 'myplan'

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: appServicePlanName
  location: location
  properties: {
    reserved: true
  }
}
```

### Step 3: Validate Configuration

Run provider-specific validation:

```bash
# AWS
python scripts/validate_aws.py template.yaml

# GCP
python scripts/validate_gcp.py deployment.yaml

# Azure
python scripts/validate_azure.py template.bicep
```

### Step 4: Deploy

AI Agent provides deployment commands:

**AWS**:
```bash
aws cloudformation deploy --template-file template.yaml --stack-name mystack
```

**GCP**:
```bash
gcloud deployment-manager deployments create my-deployment --config deployment.yaml
```

**Azure**:
```bash
az deployment group create --resource-group mygroup --template-file template.bicep
```

---

## Multi-Cloud Decision Tree

```
Are you using one cloud provider or multiple?
├─ Single provider: Use provider-specific patterns
└─ Multiple providers: See [multi-cloud-guide.md](../references/multi-cloud-guide.md)

Do you need infrastructure-as-code or CLI scripts?
├─ Infrastructure-as-code: Use templates (CloudFormation, Bicep, Terraform)
└─ CLI scripts: Generate provider-specific commands

What's your comfort level with infrastructure?
├─ Beginner: Use managed services (App Service, Cloud Run, Lambda)
└─ Advanced: Use lower-level resources (VMs, networking)
```

---

## Least-Privilege Security

Always apply least-privilege principles:

**AWS**: Attach minimal IAM policies
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::mybucket/data/*"
    }
  ]
}
```

**GCP**: Use custom roles with minimal permissions
```yaml
title: "Custom App Role"
includedPermissions:
- storage.buckets.get
- storage.objects.get
```

**Azure**: Use Azure Roles with specific scope
```bicep
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2020-08-01-preview' = {
  scope: storageAccount
  name: guid(storageAccount.id, principal.id, 'Storage Blob Data Reader')
  properties: {
    roleDefinitionId: '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
  }
}
```

---

## Troubleshooting

**Q: How do I validate templates before deploying?**
A: Use provider validation scripts before deployment:
```bash
python scripts/validate_aws.py template.yaml
```

**Q: Can AI Agent help with Terraform?**
A: Yes. Terraform works across providers; see [terraform-patterns.md](../references/terraform-patterns.md)

**Q: How do I manage secrets in deployment templates?**
A: Use parameter stores/secrets managers:
- AWS: AWS Secrets Manager
- GCP: Cloud Secret Manager
- Azure: Azure Key Vault

Never embed credentials in templates.

**Q: What about GitOps and automation?**
A: See [cicd-deployment.md](../references/cicd-deployment.md) for GitHub Actions, GitLab CI, and cloud-native CI/CD.

---

## Further Resources

- [AWS Patterns](../references/aws-deployment-patterns.md)
- [GCP Patterns](../references/gcp-deployment-patterns.md)
- [Azure Patterns](../references/azure-deployment-patterns.md)
- [Terraform Guide](../references/terraform-patterns.md)
- [Multi-Cloud Strategy](../references/multi-cloud-guide.md)
- [CI/CD Integration](../references/cicd-deployment.md)
- [Security Best Practices](../references/cloud-security.md)

---

This skill demonstrates how AI Agent handles **complex multi-domain scenarios** by:
1. Organizing domain-specific guidance in separate sections
2. Using validation scripts for each provider
3. Providing decision trees for AI Agent decision-making
4. Referencing detailed guides for advanced topics
