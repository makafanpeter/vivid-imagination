# Vivid Imagination — AWS EC2 & Cloudflare Deployment Guide

This guide provides step-by-step instructions for deploying the **Vivid Imagination** .NET 10 Modular Monolith stack onto an **Amazon EC2 (`m7i-flex.large`)** instance behind **Cloudflare Tunnel (`cloudflared`)** using **AWS CloudFormation**.

---

## Architecture Overview

```mermaid
flowchart TD
    Client["Client Mobile / Web"] -->|HTTPS (443)| CFEdge["Cloudflare Edge (WAF, CDN, DDoS Protection, SSL)"]
    Admin["CI/CD / Admin"] -->|SSH over Cloudflare Access| CFEdge

    subgraph AWS ["AWS Cloud (VPC: 10.0.0.0/16)"]
        subgraph PublicSubnet ["Public Subnet (10.0.1.0/24)"]
            subgraph EC2 ["EC2 Instance (m7i-flex.large / Amazon Linux 2023)"]
                Cloudflared["cloudflared daemon (Tunnel Systemd Service)"]
                
                subgraph DockerNet ["Docker Network (vivid-network)"]
                    Api["vividimagination.backend.api (:8080)"]
                    Postgres[("PostgreSQL 16 (:5432)")]
                    Mongo[("MongoDB 7.0 (:27017)")]
                    Redis[("Redis 7 (:6379)")]
                    RabbitMQ["RabbitMQ 3.13 (:5672, :15672)"]
                end

                Cloudflared -->|http://localhost:8080| Api
                Api --> Postgres
                Api --> Mongo
                Api --> Redis
                Api --> RabbitMQ
            end
        end

        subgraph AWSServices ["AWS Managed Cloud Services"]
            S3["Amazon S3 Photo Bucket<br/>(AES256, Public Access Blocked)"]
            Rekognition["Amazon Rekognition<br/>(AI Content Moderation)"]
            SSM["AWS Systems Manager<br/>(Session Manager CLI access)"]
        end

        Api -->|IAM Instance Profile: Put/Get/Delete| S3
        Api -->|IAM Instance Profile: DetectModerationLabels| Rekognition
        EC2 -.->|Managed Policy| SSM
    end

    CFEdge <== Outbound Secure Tunnel (Port 443) ==> Cloudflared
```

### Key Security & Reliability Features
- **Zero Open Inbound Ports**: The AWS Security Group contains **no open inbound rules**. All inbound HTTP/HTTPS traffic is multiplexed through an outbound TLS connection from `cloudflared` to Cloudflare edge over port 443. The instance is completely immune to public IP scanning and DDoS attacks.
- **Private GHCR Authentication**: The instance securely logs in to GitHub Container Registry (`ghcr.io`) using a GitHub Personal Access Token (PAT) passed via CloudFormation parameters (masked with `NoEcho: true`).
- **Native IAM Credentials**: The .NET 10 API connects to Amazon S3 and Amazon Rekognition via an attached IAM Instance Profile. No long-lived AWS Access Keys (`AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY`) are stored in code or configuration files.
- **Unified `.env` Configuration**: All ASP.NET Core settings, connection strings, S3 bucket names, Redis/RabbitMQ endpoints, and security keys are consolidated in `/opt/vivid-imagination/.env` and injected via Docker Compose `env_file: - .env`, keeping the production compose file ultra-clean (~60 lines) and declarative (see `infra/.env.example`).
- **`m7i-flex.large` Hardware**: 2 vCPUs and 8 GB memory on 4th/5th Gen Intel Xeon Scalable processors, providing ample headroom for PostgreSQL, MongoDB, Redis, RabbitMQ, and the .NET 10 runtime to run concurrently without memory exhaustion.

---

## Prerequisites

1. **AWS Account**:
   - Permissions to provision VPC, Subnets, Internet Gateways, Route Tables, Security Groups, IAM Roles, S3 Buckets, and EC2 Instances.
   - [AWS CLI v2 installed and configured](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html) (`aws configure`).
2. **Cloudflare Account & Domain**:
   - A domain managed in Cloudflare (with active DNS).
   - Access to **Cloudflare Zero Trust** (`https://one.dash.cloudflare.com/`).
3. **GitHub Account & Container Registry (GHCR)**:
   - A GitHub Personal Access Token (classic or fine-grained) with `read:packages` scope to pull private container images from `ghcr.io`.

---

## Step 1: Cloudflare Origin Certificate Setup (`cert.pem`)

With `CloudflareCertPem`, you **never need to visit the Cloudflare Zero Trust web dashboard to configure tunnels or define routes**. The EC2 startup script automatically provisions the tunnel and configures DNS CNAME routes for `api.yourdomain.com` and `ssh-api.yourdomain.com` via the `cloudflared` CLI.

1. On your local machine (where you have a web browser), install `cloudflared` if you haven't already:
   - **macOS**: `brew install cloudflared`
   - **Windows**: `winget install --id Cloudflare.cloudflared`
   - **Linux**: Download from [Cloudflare releases](https://github.com/cloudflare/cloudflared/releases)
2. Run the one-time authentication command:
   ```bash
   cloudflared tunnel login
   ```
3. A browser window opens automatically. Log in to your Cloudflare account and select your domain to authorize.
4. Once authorized, `cloudflared` downloads your origin certificate:
   - **Linux/macOS**: `~/.cloudflared/cert.pem`
   - **Windows**: `%USERPROFILE%\.cloudflared\cert.pem`
5. Copy the entire contents of `cert.pem` (typically starting with `-----BEGIN ARGO TUNNEL TOKEN-----` and ending with `-----END ARGO TUNNEL TOKEN-----`, or `-----BEGIN CERTIFICATE-----`). This origin token remains valid indefinitely for your domain.

> [!TIP]
> When pasting `cert.pem` into a JSON parameters file (`parameters.prod.json`), you can escape newlines as `\n` (e.g. `"-----BEGIN ARGO TUNNEL TOKEN-----\n<base64>\n-----END ARGO TUNNEL TOKEN-----\n"`). The EC2 startup script automatically normalizes literal `\n` and `\r\n` characters on boot.

---

## Step 2: Deploy CloudFormation Stack

### Option A: Using AWS CLI (Recommended)

1. Open a terminal in the root of the repository.
2. Copy the example parameters file:
   ```bash
   cp infra/cloudformation/parameters.example.json infra/cloudformation/parameters.prod.json
   ```
3. Edit `infra/cloudformation/parameters.prod.json` with your real secrets:
   - `CloudflareCertPem`: Paste the contents of `cert.pem` from Step 1.
   - `GhcrUsername`: Your GitHub username or organization name.
   - `GhcrToken`: Your GitHub PAT with `read:packages` permission.
   - `GhcrImageRepository`: `ghcr.io/<owner>/vividimagination.backend.api`.
   - `PostgresPassword`: Strong password for PostgreSQL.
   - `JwtSecretKey` & `EncryptionKey`: 32+ character random strings.
   - `AppBaseUrl`: `https://api.yourdomain.com`.
4. Run the CloudFormation deploy command:
   ```bash
   aws cloudformation deploy \
     --template-file infra/cloudformation/vivid-imagination-ec2-cloudflare.yaml \
     --stack-name vivid-imagination-prod \
     --parameter-overrides file://infra/cloudformation/parameters.prod.json \
     --capabilities CAPABILITY_IAM \
     --region eu-west-1
   ```
5. Monitor stack creation in the terminal or AWS Console. The EC2 startup script automatically:
   - Installs `cloudflared`.
   - Provisions the tunnel `production-vivid-tunnel`.
   - Configures Cloudflare DNS CNAME records (`api.yourdomain.com` and `ssh-api.yourdomain.com`) pointing directly to the tunnel.
   - Establishes the outbound TLS tunnel connection.
   - Bootstraps the Docker containers (typically takes ~5–8 minutes).

---

### Option B: Using AWS Management Console

1. Navigate to **AWS CloudFormation** in the AWS Console.
2. Click **Create stack** &rarr; **With new resources (standard)**.
3. Select **Upload a template file** &rarr; choose `infra/cloudformation/vivid-imagination-ec2-cloudflare.yaml` &rarr; click **Next**.
4. Specify Stack Details:
   - **Stack name**: `vivid-imagination-prod`
   - Fill in parameter values (`CloudflareCertPem`, `GhcrUsername`, `GhcrToken`, `PostgresPassword`, etc.).
5. Click **Next**, proceed through default stack options, check **"I acknowledge that AWS CloudFormation might create IAM resources"**, and click **Submit**.

---

## Step 3: Verify the Deployment

### 1. Check Cloudflare Tunnel Status
In the Cloudflare Zero Trust dashboard under **Networks** &rarr; **Tunnels**, verify that your tunnel status changes from *Inactive* to **HEALTHY** with an active connector.

### 2. Connect to EC2 via AWS Systems Manager (No open ports needed)
Run the following AWS CLI command (replace with your Instance ID from stack outputs):
```bash
aws ssm start-session --target i-0123456789abcdef0 --region eu-west-1
```
Once connected, switch to root to inspect container logs and health:
```bash
sudo su -
cd /opt/vivid-imagination

# Check container status
docker compose -f docker-compose.prod.yml ps

# View API logs
docker logs -f vivid-api

# Check cloudflared systemd service status
systemctl status cloudflared
```

### 3. Verify Public API & Swagger UI
Open your browser and navigate to:
```
https://api.yourdomain.com/docs
```
You should see the Swagger UI OpenAPI specification documentation for Vivid Imagination.

### 4. Inspect Cloudflare Tunnel Configuration on Host
`cloudflared` runs directly on the EC2 host (not inside Docker) via systemd and uses an explicit configuration file `/etc/cloudflared/config.yml`:
```bash
# View cloudflared ingress rules
cat /etc/cloudflared/config.yml

# Check cloudflared service logs
journalctl -u cloudflared -f
```
Ingress rules route incoming domain traffic directly to the local API container on port 8080:
```yaml
tunnel: <TUNNEL_UUID>
credentials-file: /etc/cloudflared/<TUNNEL_UUID>.json
ingress:
  - hostname: api.yourdomain.com
    service: http://127.0.0.1:8080
    originRequest:
      connectTimeout: 30s
      noTLSVerify: false
  - hostname: ssh-api.yourdomain.com
    service: ssh://127.0.0.1:22
  - service: http_status:404
```

### 5. Managing and Customizing Environment Variables (`.env`)
- **Automated Deployment**: During CloudFormation provisioning, the EC2 UserData bootstrap script automatically generates `/opt/vivid-imagination/.env` populated with your stack's passwords, S3 bucket name, and AWS region.
- **Reference Template (`infra/.env.example`)**: The repository includes `infra/.env.example` as a full template for operators. You can copy it directly to create `.env`:
  ```bash
  cp infra/.env.example infra/.env
  # Update passwords, JWT secrets, and URLs as needed
  ```

#### Key Configuration Areas in `.env`:
| Category | Variables / Examples | Description |
|---|---|---|
| **Docker Compose** | `API_IMAGE`, `APP_TAG`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | Service credentials and image tags interpolated by Docker Compose. |
| **Logging** | `Logging__LogLevel__Default=Debug`, `Logging__LogLevel__MassTransit=Information` | Log severity levels for runtime components and background consumers. |
| **Host Runtime** | `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_HTTP_PORTS=8080`, `AllowedHosts=*` | Runtime profile and listening port. |
| **PostgreSQL** | `ConnectionStrings__Postgres`, `ConnectionStrings__Identity`, `ConnectionStrings__Social` | Relational database connection strings (`Include Error Detail=true` for debugging). |
| **MongoDB** | `ConnectionStrings__Mongo=mongodb://mongodb:27017`, `Media__ConnectionString`, etc. | Document database endpoints (consistently using container hostname `mongodb`). |
| **Redis Cache** | `ConnectionStrings__Redis=redis:6379`, `ApplicationSettings__Cache__DefaultCacheTime=60` | In-memory distributed caching and item TTL durations in minutes. |
| **RabbitMQ** | `ApplicationSettings__EventBus__RabbitMQ__Host=rabbitmq://rabbitmq` | MassTransit message broker. **Must include `rabbitmq://` scheme** to prevent URI format exceptions. |
| **Quartz Jobs** | `ClearCacheJob=0 0/15 * * * ?`, `Cron__UsePersistentStore=false` | Cron expressions for background cache evictions and scheduled tasks. |
| **Security & JWT** | `ApplicationSettings__SecuritySettings__*`, `ApplicationSettings__Jwt__AccessTokenLifetime=60` | Password complexity, failed login lockouts, and token expiration lifetimes. |
| **OpenID Connect** | `ApplicationSettings__OpenIdConnectSettings__Authority`, `Audiences__0`, `IssuerUri` | Authentication authority and client credentials for OpenIddict. |
| **CORS** | `ApplicationSettings__CORS__AllowAnyOrigin=true`, `AllowedOrigins__*` | Allowed web and mobile origins. |

- **Applying Changes**: Whenever you update `.env`, reload the API container:
  ```bash
  docker compose -f infra/docker-compose.prod.yml up -d vividimagination.backend.api
  docker logs --tail 50 vivid-api
  ```

---

## Step 4: Connecting GitHub Actions CI/CD

Automated zero-downtime deployments and quality checks are orchestrated via `.github/workflows/ci-cd.yml`:

### Pipeline Jobs Overview
1. **`test-backend`**:
   - Triggers on changes to `src/backend/**`, `tests/**`, or workflow files.
   - Restores and compiles `VividImagination.Api` and all test projects on .NET 10.
   - Executes all 74 Unit Tests and 7 NetArchTest architecture boundary tests.
2. **`build-mobile-android`**:
   - Triggers on changes to `src/mobile/**` or workflow files.
   - Configures Java 17 Temurin and installs the `maui-android` workload.
   - Executes `dotnet restore ... -p:TargetFramework=net10.0-android` and compiles release APK and Android App Bundle (AAB).
   - Archives mobile artifacts (`mobile-android-apk`, `mobile-android-aab`) as downloadable pipeline artifacts.
3. **`build`**:
   - Builds and publishes the `vividimagination.backend.api` production container image.
   - Pushes signed container images to GitHub Container Registry (`ghcr.io/<owner>/vividimagination.backend.api:<sha>`).
4. **`deploy`**:
   - Runs conditionally on pushes to `develop` or `main`.
   - Connects securely to the EC2 instance over Cloudflare Access SSH.
   - Executes rolling reload and container health verification.

### Required GitHub Secrets & Setup Procedure

To enable zero-ingress deployment via Cloudflare Access SSH, configure the following secrets in **GitHub** &rarr; **Settings** &rarr; **Secrets and variables** &rarr; **Actions**:

| Secret Name | Example / Format | Source & Purpose |
|---|---|---|
| `SSH_PRIVATE_KEY` | `-----BEGIN OPENSSH PRIVATE KEY-----...` | Private key generated for CI/CD, authorized on EC2 in `/home/ec2-user/.ssh/authorized_keys`. |
| `SANDBOX_SSH_HOST` | `ssh-api.yourdomain.com` | Cloudflare Access hostname routing SSH through the tunnel to `localhost:22`. |
| `CF_ACCESS_CLIENT_ID` | `1234567890abcdef.access` | Cloudflare Access Service Token Client ID. |
| `CF_ACCESS_CLIENT_SECRET` | `64-character secret` | Cloudflare Access Service Token Client Secret. |
| `SANDBOX_DEPLOY_USER` | `ec2-user` | OS user account on Amazon Linux 2023. |
| `GHCR_READ_TOKEN` | `ghp_...` | GitHub Personal Access Token with `read:packages` scope. |

> [!TIP]
> **Subdomain Naming & Cloudflare SSL**:
> Using a hyphenated subdomain such as `ssh-api.yourdomain.com` (1 subdomain level) is strongly recommended over multi-level subdomains like `ssh.api.yourdomain.com`.
> Cloudflare's free Universal SSL certificate covers `yourdomain.com` and `*.yourdomain.com` (1 level only).
> The host `/etc/cloudflared/config.yml` automatically routes `ssh-${APP_HOST}` (e.g. `ssh-api.yourdomain.com`) to the local SSH daemon on port 22.

#### Step-by-Step Setup:

#### 1. Generate & Authorize `SSH_PRIVATE_KEY`
1. On your local machine, generate a dedicated ED25519 keypair with an empty passphrase:
   ```bash
   ssh-keygen -t ed25519 -C "github-actions-deploy" -f ./deploy_key -N ""
   ```
   This generates `deploy_key` (private key) and `deploy_key.pub` (public key).
2. Authorize the public key on EC2 (choose either method):
   - **Method A (CloudFormation Parameter)**: Set `SshPublicKey` in `parameters.prod.json` to the contents of `deploy_key.pub`. The startup script will automatically write it to `/home/ec2-user/.ssh/authorized_keys` with correct permissions.
   - **Method B (Running Instance via AWS SSM)**:
     ```bash
     aws ssm start-session --target <INSTANCE_ID> --region eu-west-1
     ```
     Then append the public key:
     ```bash
     sudo -u ec2-user mkdir -p /home/ec2-user/.ssh
     sudo -u ec2-user chmod 700 /home/ec2-user/.ssh
     echo "<PASTE_CONTENT_OF_deploy_key.pub>" | sudo -u ec2-user tee -a /home/ec2-user/.ssh/authorized_keys
     sudo -u ec2-user chmod 600 /home/ec2-user/.ssh/authorized_keys
     ```
4. Copy the entire contents of the private key file `deploy_key` and save it as the `SSH_PRIVATE_KEY` secret in GitHub.

#### 2. Configure `SANDBOX_SSH_HOST` in Cloudflare Zero Trust
1. **Add Public Hostname to Tunnel**:
   - In [Cloudflare Zero Trust Dashboard](https://one.dash.cloudflare.com/), go to **Networks** &rarr; **Tunnels** &rarr; select `vivid-imagination-prod` &rarr; **Configure**.
   - Under **Public Hostname**, click **Add a public hostname**:
     - **Subdomain**: `ssh-api` (e.g. `ssh-api.yourdomain.com`).
     - **Domain**: Select your registered domain.
     - **Service Type**: `SSH`
     - **URL**: `localhost:22`
   - Click **Save hostname**. Set `SANDBOX_SSH_HOST` in GitHub Secrets to `ssh-api.yourdomain.com`.
2. **Create a Service Token for GitHub Actions**:
   - In Zero Trust, go to **Access** &rarr; **Service Auth** &rarr; **Service Tokens** &rarr; **Create Service Token**.
   - Name: `github-actions-deploy` &rarr; click **Generate token**.
   - Save **Client ID** (`CF_ACCESS_CLIENT_ID`) and **Client Secret** (`CF_ACCESS_CLIENT_SECRET`) to GitHub Secrets.
3. **Protect SSH Hostname with an Access Application**:
   - Go to **Access** &rarr; **Applications** &rarr; **Add an application** &rarr; **Self-hosted**.
   - **Application name**: `EC2 SSH Deploy`.
   - **Application domain**: `ssh-api.yourdomain.com`.
   - Add Policy:
     - **Policy name**: `Service Token Authentication`.
     - **Action**: `Service Auth`.
     - **Include**: Selector `Service Token` &rarr; choose `github-actions-deploy`.
   - Click **Save application**.

When deployment runs, GitHub Actions connects over the Cloudflare TCP Tunnel and executes:
```bash
deploy-app --dir /opt/vivid-imagination --project vivid-imagination --tag <NEW_TAG> vividimagination.backend.api
```
The deployment script authenticates with GHCR, pulls the new image tag, performs a zero-downtime rolling reload of the API container, verifies the container healthcheck, and prunes old images.

---

## Troubleshooting Common Issues

### 1. `vivid-api` Container in Restart Loop (`System.UriFormatException`)
- **Symptom**: `docker compose -f docker-compose.prod.yml ps` displays `vivid-api` in status `Restarting (1)`.
- **Cause**: MassTransit in `RabbitMqHostConfigurator.cs` calls `new Uri(settings.Host)`. If `RabbitMQ__Host` or `ApplicationSettings__EventBus__RabbitMQ__Host` is set to bare `rabbitmq` without a scheme, .NET throws `System.UriFormatException: Invalid URI: The format of the URI could not be determined`.
- **Fix**: Update `.env` to include the `rabbitmq://` scheme:
  ```bash
  sed -i 's|RabbitMQ__Host=rabbitmq$|RabbitMQ__Host=rabbitmq://rabbitmq|' /opt/vivid-imagination/.env
  sed -i 's|ApplicationSettings__EventBus__RabbitMQ__Host=rabbitmq$|ApplicationSettings__EventBus__RabbitMQ__Host=rabbitmq://rabbitmq|' /opt/vivid-imagination/.env
  docker compose -f /opt/vivid-imagination/docker-compose.prod.yml up -d vividimagination.backend.api
  ```

### 2. Amazon Linux 2023 DNF Conflict with `curl-minimal`
- **Symptom**: Package installation fails during EC2 UserData bootstrap with `package curl-minimal-... conflicts with curl provided by curl-...`.
- **Cause**: Amazon Linux 2023 pre-installs `curl-minimal`. Attempting to install `curl` directly creates an RPM transaction conflict.
- **Fix**: CloudFormation uses `dnf install -y --allowerasing docker git jq` to preserve `curl-minimal` while installing required tooling cleanly.

### 3. Cloudflare Tunnel Ingress Connection Refused
- **Symptom**: Cloudflare Edge returns HTTP 502 Bad Gateway.
- **Verification**:
  ```bash
  # Verify cloudflared systemd service is active
  systemctl status cloudflared

  # Check live tunnel logs
  journalctl -u cloudflared -f

  # Verify API is responding locally on port 8080
  curl -I http://127.0.0.1:8080/docs
  ```

---

## Day-2 Operations

### Database Backups

#### PostgreSQL
```bash
# Dump primary database using configured credentials
docker exec vivid-postgres pg_dump -U ${POSTGRES_USER:-postgres} ${POSTGRES_DB:-vivid_imagination} > /opt/vivid-imagination/backup_postgres_$(date +%F).sql
```

#### MongoDB
```bash
# Create MongoDB binary dump
docker exec vivid-mongodb mongodump --out=/data/backup_$(date +%F)
```

### S3 & Rekognition Auditing
- Photo uploads and transcoded WebP variants (`original.webp`, `medium.webp`, `thumbnail.webp`) are stored directly in `vivid-photos-<AccountId>-<Region>`.
- Amazon Rekognition moderation labels are logged automatically in the MongoDB `moderation_reviews` collection.

---

## Stack Clean Up & Teardown

When you want to dismantle the infrastructure and stop all AWS charges, follow these teardown steps.

### 1. (Optional) Empty the S3 Photo Bucket
AWS CloudFormation will refuse to delete an S3 bucket that still contains objects. If any photos were uploaded, empty the bucket prior to deleting the stack:

```bash
# Retrieve the S3 photo bucket name from stack outputs
BUCKET_NAME=$(aws cloudformation describe-stacks \
  --stack-name vivid-imagination-prod \
  --region eu-west-1 \
  --query "Stacks[0].Outputs[?OutputKey=='PhotoBucketName'].OutputValue" \
  --output text)

# Empty all bucket objects and versions
aws s3 rm "s3://${BUCKET_NAME}" --recursive --region eu-west-1
```

### 2. Delete the CloudFormation Stack
Trigger the automated deletion of the EC2 instance, EBS GP3 volume, S3 bucket, IAM roles, Security Group, and VPC resources:

```bash
aws cloudformation delete-stack \
  --stack-name vivid-imagination-prod \
  --region eu-west-1
```

### 3. Wait for Stack Deletion to Complete
Monitor the stack deletion progress until all resources are successfully deleted:

```bash
aws cloudformation wait stack-delete-complete \
  --stack-name vivid-imagination-prod \
  --region eu-west-1
```

### 4. Clean Up Cloudflare Zero Trust Resources
1. In the [Cloudflare Zero Trust Dashboard](https://one.dash.cloudflare.com/):
   - Navigate to **Networks** &rarr; **Tunnels**.
   - Select your tunnel (`vivid-imagination-prod`) and delete the public hostnames (`api.yourdomain.com`, `ssh-api.yourdomain.com`).
   - If no longer needed, click **Delete tunnel**.
2. Navigate to **Access** &rarr; **Applications** and remove the `EC2 SSH Deploy` application.
3. Navigate to **Access** &rarr; **Service Auth** &rarr; **Service Tokens** and revoke/delete the `github-actions-deploy` token.
