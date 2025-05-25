#!/bin/bash

# Quick manual deployment commands for RealEstate
# Use this if the main docker-deploy.sh has issues

echo "🚀 Quick RealEstate deployment..."

# Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose -f docker-compose.prod.yml down

# Remove old API image to force rebuild
echo "🗑️  Removing old API image..."
docker rmi realestate_api 2>/dev/null || true

# Build and start
echo "🔨 Building and starting containers..."
docker-compose -f docker-compose.prod.yml up -d --build

# Wait and check
echo "⏳ Waiting for services..."
sleep 15

echo "📊 Container status:"
docker ps --filter "name=realestate"

echo ""
echo "🏥 Testing API health..."
sleep 5
if curl -f http://localhost:5269/health; then
    echo "✅ API is healthy!"
else
    echo "⚠️  API might still be starting up..."
    echo "📋 API logs:"
    docker logs realestate-api --tail 20
fi

echo ""
echo "🎉 Deployment completed!"
echo "🌐 API should be available at: http://62.171.153.198:4545"
