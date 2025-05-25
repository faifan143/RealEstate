#!/bin/bash

# Fix schema permissions for RealEstate database
# This grants proper schema permissions to realestateuser

echo "🔧 Fixing schema permissions..."

# Grant schema permissions
echo "🔐 Granting schema permissions to realestateuser..."
docker exec realestate-postgres psql -U postgres -d realestate -c "GRANT ALL ON SCHEMA public TO realestateuser;"
docker exec realestate-postgres psql -U postgres -d realestate -c "GRANT CREATE ON SCHEMA public TO realestateuser;"
docker exec realestate-postgres psql -U postgres -d realestate -c "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO realestateuser;"
docker exec realestate-postgres psql -U postgres -d realestate -c "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO realestateuser;"

# Make realestateuser owner of the database for full permissions
echo "👑 Making realestateuser owner of the database..."
docker exec realestate-postgres psql -U postgres -c "ALTER DATABASE realestate OWNER TO realestateuser;"

# Test schema permissions
echo "🧪 Testing schema permissions..."
if docker exec realestate-postgres psql -U realestateuser -d realestate -c "CREATE TABLE test_table (id INTEGER); DROP TABLE test_table;" > /dev/null 2>&1; then
    echo "✅ Schema permissions are working!"
else
    echo "⚠️  Schema permissions test failed"
fi

# Restart API container
echo "🔄 Restarting API container..."
docker restart realestate-api

# Wait for API to start
echo "⏳ Waiting for API to restart..."
sleep 20

# Test API health
echo "🏥 Testing API health..."
if curl -f http://localhost:7269/health 2>/dev/null; then
    echo "✅ API is healthy!"
else
    echo "⚠️  API health check failed. Checking logs..."
    docker logs realestate-api --tail 20
fi

echo ""
echo "🎉 Schema permissions fix completed!"
echo "📍 API URL: http://localhost:7269"
echo ""
echo "📈 Container status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
