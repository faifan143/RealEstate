#!/bin/bash

# Manual container deployment for RealEstate
# Use this if docker-compose has issues

echo "🔧 Manual container deployment for RealEstate..."

# Stop and remove any existing containers
echo "🛑 Cleaning up existing containers..."
docker stop realestate-api realestate-postgres 2>/dev/null || true
docker rm realestate-api realestate-postgres 2>/dev/null || true

# Create network
echo "🌐 Creating network..."
docker network create realestate-network 2>/dev/null || echo "Network already exists"

# Start PostgreSQL
echo "🐘 Starting PostgreSQL..."
docker run -d \
  --name realestate-postgres \
  --network realestate-network \
  -p 7432:5432 \
  -e POSTGRES_DB=realestate \
  -e POSTGRES_USER=realestateuser \
  -e POSTGRES_PASSWORD=123 \
  -v realestate-postgres-data:/var/lib/postgresql/data \
  --restart unless-stopped \
  postgres:15

# Wait for postgres to start
echo "⏳ Waiting for PostgreSQL to start..."
sleep 15

# Check postgres
if docker ps | grep -q "realestate-postgres"; then
    echo "✅ PostgreSQL is running"
else
    echo "❌ PostgreSQL failed to start"
    docker logs realestate-postgres --tail 10
    exit 1
fi

# Build API image
echo "🔨 Building API image..."
docker build -f src/RealEstate.API/Dockerfile.prod -t realestate_api .

# Start API
echo "🚀 Starting API..."
docker run -d \
  --name realestate-api \
  --network realestate-network \
  -p 7269:5268 \
  -e ConnectionStrings__DefaultConnection="Host=realestate-postgres;Port=5432;Database=realestate;Username=realestateuser;Password=123" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5268 \
  --restart unless-stopped \
  realestate_api

# Wait for API to start
echo "⏳ Waiting for API to start..."
sleep 20

echo "📊 Container status:"
docker ps --filter "name=realestate"

echo ""
echo "🏥 Testing services..."

# Test PostgreSQL
if docker exec realestate-postgres pg_isready -U realestateuser -d realestate; then
    echo "✅ PostgreSQL is healthy"
else
    echo "⚠️  PostgreSQL health check failed"
    docker logs realestate-postgres --tail 10
fi

# Test API
if curl -f http://localhost:7269/health 2>/dev/null; then
    echo "✅ API is healthy!"
else
    echo "⚠️  API health check failed. Checking logs..."
    docker logs realestate-api --tail 20
fi

echo ""
echo "🎉 Manual deployment completed!"
echo ""
echo "📍 Service URLs:"
echo "   🌐 API: http://localhost:7269"
echo "   🐘 PostgreSQL: localhost:7432"
echo ""
echo "📋 To stop services:"
echo "   docker stop realestate-api realestate-postgres"
echo ""
echo "📋 To view logs:"
echo "   docker logs realestate-api"
echo "   docker logs realestate-postgres"
