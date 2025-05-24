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

# Server details
SERVER="root@62.171.153.198"
DEPLOY_PATH="/var/www/realestate"

# Compress the published output
echo "Compressing release files..."
rm -f release.zip
cd src/RealEstate.API/bin/Release/net8.0/publish/
zip -r ../../../../../release.zip *
cd ../../../../../

# Upload to server
echo "Uploading to server..."
scp release.zip $SERVER:$DEPLOY_PATH/

# Connect to server and deploy
echo "Deploying on server..."
ssh $SERVER << 'EOF'
    cd /var/www/realestate
    
    # Stop the service
    systemctl stop realestate.service
    
    # Backup current deployment
    if [ -d "publish_backup" ]; then
        rm -rf publish_backup
    fi
    if [ -d "publish" ]; then
        mv publish publish_backup
    fi
    
    # Create a new publish directory
    mkdir -p publish
    
    # Extract the new files
    unzip -o release.zip -d publish
    
    # Set proper permissions
    chown -R www-data:www-data publish
    chmod -R 755 publish
    
    # Restart the service
    systemctl start realestate.service
    
    # Check status
    sleep 3
    systemctl status realestate.service
EOF

echo "Deployment completed!" 