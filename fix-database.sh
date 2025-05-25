#!/bin/bash

# Quick database fix for RealEstate
# This creates the missing database without restarting containers

echo "🔧 Fixing database configuration..."

# Check if postgres container is running
if ! docker ps | grep -q "realestate-postgres"; then
    echo "❌ PostgreSQL container not running"
    exit 1
fi

echo "✅ PostgreSQL container is running"

# Create the database using the postgres superuser
echo "📦 Creating 'realestate' database..."
docker exec realestate-postgres psql -U postgres -c "CREATE DATABASE realestate;"

# Grant permissions to realestateuser
echo "👤 Creating user and granting permissions..."
docker exec realestate-postgres psql -U postgres -c "CREATE USER realestateuser WITH PASSWORD '123';" 2>/dev/null || echo "User already exists"
docker exec realestate-postgres psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE realestate TO realestateuser;"
docker exec realestate-postgres psql -U postgres -c "ALTER USER realestateuser CREATEDB;"

# Test the connection
echo "🧪 Testing database connection..."
if docker exec realestate-postgres psql -U realestateuser -d realestate -c "SELECT 1;" > /dev/null 2>&1; then
    echo "✅ Database connection successful!"
else
    echo "⚠️  Database connection test failed"
fi

# Restart only the API container to pick up the database
echo "🔄 Restarting API container..."
docker restart realestate-api

# Wait for API to start
echo "⏳ Waiting for API to restart..."
sleep 15

# Test API health
echo "🏥 Testing API health..."
if curl -f http://localhost:7269/health 2>/dev/null; then
    echo "✅ API is healthy!"
else
    echo "⚠️  API health check failed. Checking logs..."
    docker logs realestate-api --tail 15
fi

echo ""
echo "🎉 Database fix completed!"
echo "📍 API URL: http://localhost:7269"
echo "📍 PostgreSQL: localhost:7432"
echo "📍 Database: realestate"
echo "📍 User: realestateuser"

# Show final status
echo ""
echo "📈 Container status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
