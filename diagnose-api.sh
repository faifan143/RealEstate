#!/bin/bash

# Comprehensive diagnostic for RealEstate API

echo "🔍 RealEstate API Diagnostics"
echo "============================="

# Check if containers are running
echo "1. 🐳 Container Status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "2. 🔌 Port Connectivity Tests:"

# Test direct API connection
echo "   Testing API on port 7269..."
if curl -s -o /dev/null -w "%{http_code}" http://localhost:7269 | grep -q "200\|404\|500"; then
    echo "   ✅ Port 7269 is responding"
    echo "   Response code: $(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269)"
else
    echo "   ❌ Port 7269 is not responding"
fi

# Test specific endpoints
echo ""
echo "3. 🎯 API Endpoint Tests:"

# Test root endpoint
echo "   Testing root endpoint (/)..."
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/)
echo "   Response: $RESPONSE"

# Test health endpoint
echo "   Testing health endpoint (/health)..."
HEALTH_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/health)
echo "   Response: $HEALTH_RESPONSE"

# Test swagger endpoint
echo "   Testing swagger endpoint (/swagger)..."
SWAGGER_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/swagger)
echo "   Response: $SWAGGER_RESPONSE"

echo ""
echo "4. 📋 Recent API Logs:"
echo "   Last 15 lines from API container:"
docker logs realestate-api --tail 15

echo ""
echo "5. 🌐 Nginx Configuration Test:"
echo "   Checking if nginx can reach the API..."
curl -s -o /dev/null -w "   Nginx → API response: %{http_code}\n" http://localhost:4545/

echo ""
echo "6. 🔍 Network Connectivity:"
echo "   API container network info:"
docker inspect realestate-api | grep -A 10 "NetworkSettings"

echo ""
echo "7. 🧪 Manual curl test commands:"
echo "   Direct API: curl -v http://localhost:7269/"
echo "   Via Nginx:  curl -v http://localhost:4545/"
echo "   Health:     curl -v http://localhost:7269/health"
echo "   Swagger:    curl -v http://localhost:7269/swagger"

echo ""
echo "8. 📊 Summary:"
if docker ps | grep -q "realestate-api.*Up"; then
    echo "   ✅ API container is running"
else
    echo "   ❌ API container is not running properly"
fi

if curl -s http://localhost:7269 > /dev/null 2>&1; then
    echo "   ✅ API is responding on port 7269"
else
    echo "   ❌ API is not responding on port 7269"
fi

if sudo nginx -t > /dev/null 2>&1; then
    echo "   ✅ Nginx configuration is valid"
else
    echo "   ❌ Nginx configuration has issues"
fi
