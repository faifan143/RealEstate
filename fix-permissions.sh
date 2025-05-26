#!/bin/bash

echo "Fixing permissions for RealEstate API container..."

# Enter the container and fix permissions
docker exec -it realestate-api bash -c "chmod -R 777 /app/wwwroot/images && chown -R appuser:appuser /app/wwwroot/images"

echo "Permissions updated. The application should now be able to save images." 