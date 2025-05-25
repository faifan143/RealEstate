#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting RealEstate API deployment...${NC}"

# Make sure Docker and Docker Compose are installed
command -v docker >/dev/null 2>&1 || { echo -e "${RED}Docker is required but not installed. Aborting.${NC}" >&2; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo -e "${RED}Docker Compose is required but not installed. Aborting.${NC}" >&2; exit 1; }

# Stop existing containers if they exist
echo -e "${YELLOW}Stopping existing containers...${NC}"
docker-compose down || true

# Build and start the containers in detached mode
echo -e "${YELLOW}Building and starting containers...${NC}"
docker-compose up -d --build

# Wait for PostgreSQL to be ready
echo -e "${YELLOW}Waiting for PostgreSQL to be ready...${NC}"
timeout=60
elapsed=0
while [ $elapsed -lt $timeout ]; do
    if docker-compose exec -T postgres pg_isready -U postgres >/dev/null 2>&1; then
        echo -e "${GREEN}PostgreSQL is ready!${NC}"
        break
    fi
    echo "Waiting for PostgreSQL... ($elapsed/${timeout}s)"
    sleep 5
    elapsed=$((elapsed + 5))
done

if [ $elapsed -ge $timeout ]; then
    echo -e "${RED}PostgreSQL failed to start within ${timeout} seconds${NC}"
    docker-compose logs postgres
    exit 1
fi

# Wait a bit more for API to fully start
echo -e "${YELLOW}Waiting for API to start...${NC}"
sleep 15

# Check container status
echo -e "${YELLOW}Container status:${NC}"
docker-compose ps

# Show logs from containers
echo -e "${YELLOW}API container logs:${NC}"
docker-compose logs --tail=50 api

echo -e "${YELLOW}PostgreSQL container logs:${NC}"
docker-compose logs --tail=20 postgres

# Get the actual IP address
HOST_IP=$(hostname -I | awk '{print $1}')

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${GREEN}The API is available at: http://${HOST_IP}:5269${NC}"
echo -e "${GREEN}Swagger UI: http://${HOST_IP}:5269/swagger${NC}"
echo -e "${GREEN}Database is available at: ${HOST_IP}:5432${NC}"
echo -e "${GREEN}========================================${NC}"