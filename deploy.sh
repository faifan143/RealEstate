#!/bin/bash
set -e

# Make sure Docker and Docker Compose are installed
command -v docker >/dev/null 2>&1 || { echo "Docker is required but not installed. Aborting." >&2; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo "Docker Compose is required but not installed. Aborting." >&2; exit 1; }

echo "Starting RealEstate API deployment..."

# Build and start the containers in detached mode
docker-compose up -d --build

echo "Waiting for services to start..."
sleep 10

# Check container status
echo "Container status:"
docker-compose ps

# Show logs from API container to verify it's running correctly
echo "API container logs:"
docker-compose logs api --tail 50

echo "PostgreSQL container logs:"
docker-compose logs postgres --tail 20

echo "========================================"
echo "Deployment completed successfully!"
echo "The API is available at: http://$(hostname -I | awk '{print $1}'):5268"
echo "Swagger UI: http://$(hostname -I | awk '{print $1}'):5268/swagger"
echo "========================================" 