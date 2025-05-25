#!/bin/bash

# Clean restart deployment for RealEstate
# This completely removes old containers and starts fresh

echo "🧹 Clean restart deployment for RealEstate..."

# Stop all related containers
echo "🛑 Stopping all realestate containers..."
docker stop realestate-api realestate-postgres 2>/dev/null || true

# Remove all related containers
echo "🗑️  Removing all realestate containers..."
docker rm realestate-api realestate-postgres 2>/dev/null || true

# Remove the API image to force rebuild
echo "🗑️  Removing API image..."
docker rmi realestate_api 2>/dev/null || true

# Remove the network if it exists
echo "🌐 Cleaning up network..."
docker network rm realestate-network 2>/dev/null || true

# Important: Remove the volume to avoid configuration conflicts
echo "⚠️  Removing PostgreSQL volume to avoid config conflicts..."
echo "   (This will remove existing database data)"
docker volume rm realestate-postgres-data 2>/dev/null || true

# Clean up any dangling resources
echo "🧹 Cleaning up dangling resources..."
docker system prune -f

echo ""
echo "🔨 Starting fresh with new configuration..."
echo "   - API: localhost:7269 (internal: 5268)"
echo "   - PostgreSQL: localhost:7432 (internal: 5432)"
echo "   - Database: realestate"
echo "   - User: realestateuser"
echo ""

# Start with clean slate
docker-compose -f docker-compose.prod.yml up -d --build

# Wait for services
echo "⏳ Waiting for services to initialize..."
sleep 30

echo "📊 Container status:"
docker ps --filter "name=realestate"

echo ""
echo "🏥 Checking services..."

# Check postgres first
if docker ps | grep -q "realestate-postgres"; then
    echo "✅ PostgreSQL container is running"
    sleep 5
    if docker ps | grep -q "realestate-postgres.*healthy"; then
        echo "✅ PostgreSQL is healthy on port 7432"
    else
        echo "⏳ PostgreSQL still starting up..."
        docker logs realestate-postgres --tail 10
    fi
else
    echo "❌ PostgreSQL container not found"
    docker logs realestate-postgres --tail 10 2>/dev/null || echo "No logs available"
fi

sleep 10

# Check API
if docker ps | grep -q "realestate-api"; then
    echo "✅ API container is running"
    sleep 5
    if curl -f http://localhost:7269/health 2>/dev/null; then
        echo "✅ API is healthy on port 7269!"
    else
        echo "⚠️  API health check failed. Checking logs..."
        echo "📋 API logs:"
        docker logs realestate-api --tail 20
    fi
else
    echo "❌ API container not found"
    docker logs realestate-api --tail 10 2>/dev/null || echo "No logs available"
fi

echo ""
echo "🎉 Clean deployment completed!"
echo ""
echo "📍 Service URLs:"
echo "   🌐 API: http://62.171.153.198:7269"
echo "   🐘 PostgreSQL: localhost:7432"
echo ""
echo "📋 Next steps:"
echo "   1. Update nginx config to proxy to port 7269"
echo "   2. Test the API endpoints"
echo ""

# Show final status
echo "📈 Final status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
