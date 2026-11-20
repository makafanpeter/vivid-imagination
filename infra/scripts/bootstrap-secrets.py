#!/usr/bin/env python3
"""
Vivid Imagination - Bootstrap Secrets & Cloudflare Tunnel Initializer
Fetches encrypted parameters from AWS Systems Manager (SSM) Parameter Store,
injects them into /opt/vivid-imagination/.env, provisions the Cloudflare Tunnel
using an ephemeral in-memory certificate in /dev/shm, and immediately shreds the cert.
"""

import json
import os
import re
import subprocess
import sys

def main():
    region = os.environ.get('AWS_REGION', 'eu-west-1')
    env_name = os.environ.get('ENV_NAME', 'production')
    app_host = os.environ.get('APP_HOST', '')
    tunnel_name = os.environ.get('TUNNEL_NAME', f'{env_name}-vivid-tunnel')
    ssm_path = f"/vivid-imagination/{env_name}/"

    print(f"[SSM] Querying configuration from: {ssm_path} ({region})")
    cmd = [
        "aws", "ssm", "get-parameters-by-path",
        "--path", ssm_path,
        "--with-decryption",
        "--region", region,
        "--output", "json"
    ]
    try:
        res = subprocess.check_output(cmd)
        params = json.loads(res).get("Parameters", [])
        secrets = {p["Name"].rstrip("/").split("/")[-1]: p["Value"] for p in params}
        print(f"[SSM] Retrieved {len(secrets)} parameters from SSM.")
    except Exception as e:
        print(f"[SSM] Warning: Failed to query SSM path {ssm_path}: {e}")
        secrets = {}

    # Non-sensitive configuration defaults (can be overridden by SSM if present)
    defaults = {
        "POSTGRES_USER": os.environ.get("PG_USER", "vivid_user"),
        "POSTGRES_DB": os.environ.get("PG_DB", "vivid_imagination"),
        "GHCR_USERNAME": os.environ.get("GHCR_USER", ""),
    }
    for k, v in defaults.items():
        if k not in secrets and v:
            secrets[k] = v

    # Strict validation: all sensitive credentials must exist in SSM Parameter Store
    required_secrets = [
        "CLOUDFLARE_CERT_PEM",
        "POSTGRES_PASSWORD",
        "JWT_SECRET_KEY",
        "ENCRYPTION_KEY",
        "GHCR_TOKEN",
    ]
    missing = [k for k in required_secrets if not secrets.get(k)]
    if missing:
        print(f"[SSM] ERROR: Missing required secrets in SSM under {ssm_path}: {', '.join(missing)}")
        print("[SSM] ERROR: All sensitive credentials must be stored in AWS SSM Parameter Store as SecureString.")
        print("[SSM] Please follow Step 2 in docs/DEPLOYMENT_GUIDE.md to configure them.")
        sys.exit(1)

    # 1. Update /opt/vivid-imagination/.env
    env_file = "/opt/vivid-imagination/.env"
    if os.path.exists(env_file):
        with open(env_file, "r") as f:
            content = f.read()

        key_mapping = {
            "POSTGRES_PASSWORD": "POSTGRES_PASSWORD",
            "POSTGRES_USER": "POSTGRES_USER",
            "POSTGRES_DB": "POSTGRES_DB",
            "GHCR_TOKEN": "GHCR_TOKEN",
            "GHCR_USERNAME": "GHCR_USERNAME",
            "JWT_SECRET_KEY": "ApplicationSettings__General__SecretKey",
            "ENCRYPTION_KEY": "ApplicationSettings__General__EncryptionKey",
        }

        for ssm_key, env_key in key_mapping.items():
            if ssm_key in secrets and secrets[ssm_key]:
                val = secrets[ssm_key]
                content = re.sub(f"^{re.escape(env_key)}=.*", f"{env_key}={val}", content, flags=re.MULTILINE)

        for k, v in secrets.items():
            if k not in ["CLOUDFLARE_CERT_PEM"] and re.search(f"^{re.escape(k)}=.*", content, flags=re.MULTILINE):
                content = re.sub(f"^{re.escape(k)}=.*", f"{k}={v}", content, flags=re.MULTILINE)

        pg_user = secrets.get("POSTGRES_USER", "vivid_user")
        pg_pass = secrets.get("POSTGRES_PASSWORD", "")
        if pg_pass:
            content = content.replace("postgres:mysecretpassword", f"{pg_user}:{pg_pass}")
            content = content.replace("ReplaceWithStrongPostgresPassword123!", pg_pass)

        with open(env_file, "w") as f:
            f.write(content)
        os.chmod(env_file, 0o600)
        print("[Config] /opt/vivid-imagination/.env successfully populated with decrypted secrets.")

    # 2. Ephemeral Cloudflare Tunnel Provisioning
    cert_pem = secrets.get("CLOUDFLARE_CERT_PEM", "")
    if cert_pem and cert_pem.strip():
        cert_path = "/dev/shm/cert.pem"
        with open(cert_path, "w") as f:
            f.write(cert_pem.replace("\\n", "\n").replace("\r", "").strip() + "\n")
        os.chmod(cert_path, 0o600)

        print(f"[Cloudflare] Provisioning tunnel '{tunnel_name}' using in-memory certificate...")
        create_res = subprocess.run(["cloudflared", "tunnel", "--origincert", cert_path, "create", tunnel_name], capture_output=True, text=True)
        out = create_res.stdout + create_res.stderr
        tunnel_id_match = re.search(r'[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}', out)
        if not tunnel_id_match:
            subprocess.run(["cloudflared", "tunnel", "--origincert", cert_path, "delete", "-f", tunnel_name], capture_output=True)
            create_res = subprocess.run(["cloudflared", "tunnel", "--origincert", cert_path, "create", tunnel_name], capture_output=True, text=True)
            out = create_res.stdout + create_res.stderr
            tunnel_id_match = re.search(r'[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}', out)

        if tunnel_id_match:
            tunnel_id = tunnel_id_match.group(0)
            print(f"[Cloudflare] Tunnel provisioned with ID: {tunnel_id}")
            src_json = f"/root/.cloudflared/{tunnel_id}.json"
            dst_json = f"/etc/cloudflared/{tunnel_id}.json"
            if os.path.exists(src_json):
                subprocess.run(["cp", "-f", src_json, dst_json])
                os.chmod(dst_json, 0o600)

            if app_host:
                print(f"[Cloudflare] Configuring DNS routes for {app_host} and ssh-{app_host}...")
                subprocess.run(["cloudflared", "tunnel", "--origincert", cert_path, "route", "dns", "-f", tunnel_name, app_host], capture_output=True)
                subprocess.run(["cloudflared", "tunnel", "--origincert", cert_path, "route", "dns", "-f", tunnel_name, f"ssh-{app_host}"], capture_output=True)

            config_content = f"""tunnel: {tunnel_id}
credentials-file: {dst_json}
ingress:
  - hostname: {app_host}
    service: http://127.0.0.1:8080
    originRequest:
      connectTimeout: 30s
      noTLSVerify: false
  - hostname: ssh-{app_host}
    service: ssh://127.0.0.1:22
  - service: http_status:404
"""
            with open("/etc/cloudflared/config.yml", "w") as f:
                f.write(config_content)
            os.chmod("/etc/cloudflared/config.yml", 0o600)

        # Securely shred and remove cert.pem from RAM
        subprocess.run(["shred", "-u", cert_path], capture_output=True)
        print("[Cloudflare] Origin certificate successfully used and securely shredded from RAM.")


if __name__ == "__main__":
    main()
