#!/bin/bash

# Quick fix deployment for RealEstate
# This fixes the .NET version issue and redeploys

echo "🚀 Quick fix deployment for RealEstate..."

# Stop all containers first
echo "🛑 Stopping all containers..."
docker-compose -f docker-compose.prod.yml down

# Remove the failing API image
echo "🗑️  Removing old API image..."
docker rmi realestate_api 2>/dev/null || true

# Clean up any dangling images
echo "🧹 Cleaning up..."
docker image prune -f

# Build and start with the fixed Dockerfile
echo "🔨 Building with .NET 8.0 runtime..."
docker-compose -f docker-compose.prod.yml up -d --build

# Wait for services
echo "⏳ Waiting for services to initialize..."
sleep 20

echo "📊 Container status:"
docker ps --filter "name=realestate"

echo ""
echo "🏥 Checking API health..."
sleep 10

if curl -f http://localhost:7269/health 2>/dev/null; then
    echo "✅ API is healthy!"
else
    echo "⚠️  API health check failed. Checking logs..."
    echo "📋 API logs:"
    docker logs realestate-api --tail 30
    echo ""
    echo "📋 Postgres logs:"
    docker logs realestate-postgres --tail 10
fi

echo ""
echo "🎉 Deployment completed!"
echo "🌐 API should be available at: http://62.171.153.198:7269"

# Show final status
echo ""
echo "📈 Final status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
