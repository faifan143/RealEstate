#!/bin/bash

echo "Fixing volume permissions for RealEstate API..."

# Stop containers first
echo "Stopping containers..."
docker-compose -f docker-compose.prod.yml down

# Remove the existing volume
echo "Removing the images volume..."
docker volume rm realestate-images

# Recreate the volume
echo "Recreating the images volume..."
docker volume create realestate-images

# Create a temporary container to set permissions
echo "Setting up permissions on volume..."
docker run --rm \
  -v realestate-images:/data \
  alpine:latest sh -c "mkdir -p /data/properties && chmod -R 777 /data"

# Start containers again
echo "Starting containers..."
docker-compose -f docker-compose.prod.yml up -d

echo "Volume permissions fixed. The application should now be able to save images." 