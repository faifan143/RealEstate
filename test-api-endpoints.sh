#!/bin/bash

# Quick fix for RealEstate API routing and health check

echo "🔧 Applying API fixes..."

# Test if API controllers are accessible
echo "🧪 Testing API controller endpoints..."

# Test some common API endpoints that should exist
ENDPOINTS=(
    "/api/auth"
    "/api/properties" 
    "/api/users"
    "/auth"
    "/properties"
    "/users"
)

echo "   Testing common API endpoints:"
for endpoint in "${ENDPOINTS[@]}"; do
    response=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:7269$endpoint")
    if [ "$response" != "404" ]; then
        echo "   ✅ $endpoint -> $response"
    else
        echo "   ❌ $endpoint -> $response"
    fi
done

# Check what controllers are available by looking at the DLL
echo ""
echo "🔍 Checking available controllers in the API..."
docker exec realestate-api find /app -name "*.dll" -exec strings {} \; | grep -i "controller" | head -10

echo ""
echo "📋 Recent detailed API logs:"
docker logs realestate-api --tail 25

echo ""
echo "🌐 Testing direct container access:"
echo "   Container internal IP test..."
CONTAINER_IP=$(docker inspect realestate-api | grep '"IPAddress"' | tail -1 | sed 's/.*"IPAddress": "\([^"]*\)".*/\1/')
echo "   Container IP: $CONTAINER_IP"

if [ ! -z "$CONTAINER_IP" ]; then
    echo "   Testing direct container access..."
    curl -s -o /dev/null -w "   Direct IP response: %{http_code}\n" "http://$CONTAINER_IP:5268/"
fi

echo ""
echo "💡 Suggested fixes:"
echo "   1. Enable Swagger in Production mode"
echo "   2. Add health check endpoint"
echo "   3. Verify controller routing"
echo "   4. Check if migrations are blocking startup"

echo ""
echo "🔄 Would you like me to create a temporary fix? (y/n)"
