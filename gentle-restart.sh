#!/bin/bash

# Gentle restart deployment for RealEstate
# This updates the configuration without destroying data

echo "🔄 Gentle restart deployment for RealEstate..."

# Check if containers are running
if docker ps | grep -q "realestate-api\|realestate-postgres"; then
    echo "📦 Found running containers, stopping them gracefully..."
    docker-compose -f docker-compose.prod.yml stop
else
    echo "📦 No running containers found"
fi

# Remove only the API container (keep postgres data)
echo "🗑️  Removing old API container..."
docker rm realestate-api 2>/dev/null || true

# Remove only the API image to force rebuild
echo "🗑️  Removing old API image..."
docker rmi realestate_api 2>/dev/null || true

# Start with updated configuration
echo "🔨 Starting with new configuration..."
echo "   - API: localhost:7269 (internal: 5268)"
echo "   - PostgreSQL: localhost:7432 (internal: 5432)"
echo "   - Database: realestate"
echo "   - User: realestateuser"

docker-compose -f docker-compose.prod.yml up -d --build

# Wait for services
echo "⏳ Waiting for services to initialize..."
sleep 25

echo "📊 Container status:"
docker ps --filter "name=realestate"

echo ""
echo "🏥 Checking services..."

# Check postgres first
if docker ps | grep -q "realestate-postgres.*healthy"; then
    echo "✅ PostgreSQL is healthy on port 7432"
else
    echo "⚠️  PostgreSQL health check..."
    docker logs realestate-postgres --tail 10
fi

sleep 5

# Check API
if curl -f http://localhost:7269/health 2>/dev/null; then
    echo "✅ API is healthy on port 7269!"
else
    echo "⚠️  API health check failed. Checking logs..."
    echo "📋 API logs:"
    docker logs realestate-api --tail 20
fi

echo ""
echo "🎉 Gentle deployment completed!"
echo ""
echo "📍 Service URLs:"
echo "   🌐 API: http://62.171.153.198:7269"
echo "   🐘 PostgreSQL: localhost:7432"
echo ""
echo "📋 To update nginx config:"
echo "   Update proxy_pass to: http://localhost:7269/"

# Show final status
echo ""
echo "📈 Final status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
