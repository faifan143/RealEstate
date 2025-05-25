#!/bin/bash

# Check RealEstate API status and health

echo "📊 RealEstate Status Check"
echo "========================="

# Container status
echo "🐳 Container Status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "🏥 Health Checks:"

# Test PostgreSQL
if docker exec realestate-postgres pg_isready -U realestateuser -d realestate > /dev/null 2>&1; then
    echo "✅ PostgreSQL: Healthy"
else
    echo "❌ PostgreSQL: Not responding"
fi

# Test API with more detailed check
echo "🔍 API Status:"
if curl -s http://localhost:7269 > /dev/null 2>&1; then
    echo "✅ API: Responding on port 7269"
    
    # Try health endpoint
    if curl -s http://localhost:7269/health > /dev/null 2>&1; then
        echo "✅ Health endpoint: Working"
    else
        echo "⏳ Health endpoint: Still initializing"
    fi
    
    # Try swagger
    if curl -s http://localhost:7269/swagger > /dev/null 2>&1; then
        echo "✅ Swagger UI: Available"
    else
        echo "⏳ Swagger UI: Loading"
    fi
else
    echo "⏳ API: Still starting up"
fi

echo ""
echo "📋 Recent API Logs (last 10 lines):"
echo "-----------------------------------"
docker logs realestate-api --tail 10

echo ""
echo "🌐 Access URLs:"
echo "  API: http://62.171.153.198:7269"
echo "  Swagger: http://62.171.153.198:7269/swagger"
echo "  Health: http://62.171.153.198:7269/health"
echo ""
echo "📝 Next step: Update nginx to proxy to port 7269"
