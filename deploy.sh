#!/bin/bash

# Since we're already on the server, just build and deploy locally
DEPLOY_PATH="/var/www/realestate"

# Stop the service
systemctl stop realestate.service

# Build the release
cd $DEPLOY_PATH
dotnet publish -c Release -o $DEPLOY_PATH/publish

# Set proper permissions
chown -R www-data:www-data publish
chmod -R 755 publish

# Restart the service
systemctl start realestate.service

# Check status
sleep 3
systemctl status realestate.service

echo "Deployment completed!"