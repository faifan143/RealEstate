#!/bin/bash

echo "Initializing Docker volumes with proper permissions..."

# Create Docker volumes if they don't exist
docker volume create realestate-images
docker volume create realestate-logs
docker volume create realestate-dataprotection

# Create a temporary container to set permissions
echo "Setting up permissions on volumes..."
docker run --rm \
  -v realestate-images:/data/images \
  -v realestate-logs:/data/logs \
  -v realestate-dataprotection:/data/keys \
  alpine:latest sh -c "mkdir -p /data/images/properties && chmod -R 777 /data/images /data/logs /data/keys"

echo "Volumes initialized with proper permissions."
echo "You can now start your containers with: docker-compose -f docker-compose.prod.yml up -d" 