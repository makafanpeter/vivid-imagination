#!/usr/bin/env python3
"""
Vivid Imagination - Deployment Assets S3 Uploader
Syncs docker-compose.prod.yml, deploy-app.sh, .env.template, and bootstrap-secrets.py
to the S3 deployment artifacts bucket.
"""

import os
import subprocess
import sys

def main():
    region = os.environ.get("AWS_REGION", "eu-west-1")
    bucket = os.environ.get("ARTIFACT_BUCKET", "")

    if not bucket:
        # Determine account id
        try:
            account_id = subprocess.check_output(
                ["aws", "sts", "get-caller-identity", "--query", "Account", "--output", "text"],
                text=True
            ).strip()
            bucket = f"vivid-artifacts-{account_id}-{region}"
        except Exception as e:
            print(f"[-] Error getting AWS account ID: {e}")
            sys.exit(1)

    print(f"[+] Target S3 Bucket: s3://{bucket}/")

    # Ensure bucket exists
    check_cmd = ["aws", "s3", "ls", f"s3://{bucket}", "--region", region]
    res = subprocess.run(check_cmd, capture_output=True)
    if res.returncode != 0:
        print(f"[+] Bucket does not exist. Creating s3://{bucket}...")
        subprocess.run(["aws", "s3", "mb", f"s3://{bucket}", "--region", region], check=True)
        subprocess.run([
            "aws", "s3api", "put-public-access-block",
            "--bucket", bucket,
            "--public-access-block-configuration",
            "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true",
            "--region", region
        ], check=True)

    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

    files_to_upload = [
        (os.path.join(repo_root, "infra", "docker-compose.prod.yml"), "docker-compose.prod.yml"),
        (os.path.join(repo_root, "infra", "scripts", "deploy-app.sh"), "deploy-app.sh"),
        (os.path.join(repo_root, "infra", ".env.example"), ".env.template"),
        (os.path.join(repo_root, "infra", "scripts", "bootstrap-secrets.py"), "bootstrap-secrets.py"),
    ]

    for local_path, s3_key in files_to_upload:
        if not os.path.exists(local_path):
            print(f"[-] Error: Local file not found: {local_path}")
            sys.exit(1)
        target = f"s3://{bucket}/{s3_key}"
        print(f"[+] Uploading {os.path.basename(local_path)} -> {target}...")
        subprocess.run(["aws", "s3", "cp", local_path, target, "--region", region], check=True)

    print("[+] All deployment assets successfully uploaded to S3!")

if __name__ == "__main__":
    main()
