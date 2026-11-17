#!/usr/bin/env bash
# ==============================================================================
# deploy-app — Zero-Downtime Application Deployment Script for Vivid Imagination
# Compatible with GitHub Actions CI/CD pipeline
# ==============================================================================

set -eo pipefail

DIR="/opt/vivid-imagination"
PROJECT="vivid-imagination"
TAG="latest"
SERVICES=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dir)
      DIR="$2"
      shift 2
      ;;
    --project)
      PROJECT="$2"
      shift 2
      ;;
    --tag)
      TAG="$2"
      shift 2
      ;;
    -h|--help)
      echo "Usage: deploy-app [--dir <deploy_dir>] [--project <project_name>] [--tag <image_tag>] [service1 service2 ...]"
      exit 0
      ;;
    *)
      SERVICES+=("$1")
      shift
      ;;
  esac
done

if [[ ${#SERVICES[@]} -eq 0 ]]; then
  SERVICES=("vividimagination.backend.api")
fi

echo "=========================================================="
echo " Starting Deployment: ${PROJECT} (Tag: ${TAG})"
echo " Target Directory:   ${DIR}"
echo " Services to Deploy: ${SERVICES[*]}"
echo " Time:               $(date -u '+%Y-%m-%d %H:%M:%SZ')"
echo "=========================================================="

if [[ ! -d "$DIR" ]]; then
  echo "[-] Error: Directory $DIR does not exist." >&2
  exit 1
fi

cd "$DIR"

# Docker login to GHCR if token is supplied
GH_TOKEN="${GITHUB_TOKEN:-${GHCR_TOKEN:-}}"
GH_USER="${GHCR_USERNAME:-}"

if [[ -n "$GH_TOKEN" ]]; then
  echo "[+] Authenticating with GitHub Container Registry (ghcr.io)..."
  if [[ -n "$GH_USER" ]]; then
    echo "$GH_TOKEN" | docker login ghcr.io -u "$GH_USER" --password-stdin
  else
    echo "$GH_TOKEN" | docker login ghcr.io -u token --password-stdin
  fi
fi

# Update APP_TAG in .env if present
if [[ -f .env ]]; then
  echo "[+] Updating APP_TAG in .env..."
  if grep -q "^APP_TAG=" .env; then
    sed -i "s/^APP_TAG=.*/APP_TAG=${TAG}/" .env
  else
    echo "APP_TAG=${TAG}" >> .env
  fi
fi

export APP_TAG="${TAG}"

# Pull updated images
for svc in "${SERVICES[@]}"; do
  echo "[+] Pulling latest image for service: ${svc}..."
  docker compose -p "${PROJECT}" -f docker-compose.prod.yml pull "${svc}" || true
done

# Rolling update of target services
echo "[+] Deploying updated service(s): ${SERVICES[*]}..."
docker compose -p "${PROJECT}" -f docker-compose.prod.yml up -d --no-deps "${SERVICES[@]}"

# Verification & Healthcheck wait
echo "[+] Waiting for container health status..."
for svc in "${SERVICES[@]}"; do
  CONTAINER_ID=$(docker compose -p "${PROJECT}" -f docker-compose.prod.yml ps -q "${svc}")
  if [[ -n "$CONTAINER_ID" ]]; then
    echo "[+] Checking health of container ${CONTAINER_ID} (${svc})..."
    ATTEMPTS=0
    MAX_ATTEMPTS=20
    HEALTHY=false
    while [[ $ATTEMPTS -lt $MAX_ATTEMPTS ]]; do
      STATUS=$(docker inspect --format='{{json .State.Health.Status}}' "$CONTAINER_ID" 2>/dev/null || echo '"unknown"')
      STATUS="${STATUS%\"}"
      STATUS="${STATUS#\"}"
      if [[ "$STATUS" == "healthy" ]]; then
        echo "[+] Service ${svc} is HEALTHY!"
        HEALTHY=true
        break
      elif [[ "$STATUS" == "unhealthy" ]]; then
        echo "[-] Service ${svc} reported UNHEALTHY!"
        docker logs --tail 50 "$CONTAINER_ID"
        exit 1
      fi
      echo "    Current status: '${STATUS}'... waiting 3s (attempt $((ATTEMPTS+1))/$MAX_ATTEMPTS)"
      sleep 3
      ATTEMPTS=$((ATTEMPTS+1))
    done

    if [[ "$HEALTHY" != "true" && "$STATUS" != "unknown" ]]; then
      echo "[-] Warning: Healthcheck timed out for service ${svc}."
    fi
  fi
done

# Prune dangling/old images
echo "[+] Pruning unused dangling images..."
docker image prune -f >/dev/null 2>&1 || true

echo "=========================================================="
echo " Deployment Successfully Finished!"
echo "=========================================================="
