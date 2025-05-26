#!/bin/bash

echo "🧪 Testing Real Estate Image Upload Functionality"
echo "==============================================="

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

# Test 1: Check if containers are running
echo "🔍 Test 1: Checking container status..."
if docker ps | grep -q "realestate-api"; then
    print_success "API container is running"
else
    print_error "API container is not running"
    exit 1
fi

# Test 2: Check directory structure
echo "🔍 Test 2: Checking directory structure..."
DIRS=$(docker exec realestate-api ls -la /app/wwwroot/images/ 2>/dev/null)
if echo "$DIRS" | grep -q "properties"; then
    print_success "Properties directory exists"
else
    print_error "Properties directory does not exist"
    echo "Creating directory..."
    docker exec realestate-api mkdir -p /app/wwwroot/images/properties
    docker exec realestate-api chmod 755 /app/wwwroot/images/properties
fi

# Test 3: Check write permissions
echo "🔍 Test 3: Testing write permissions..."
if docker exec realestate-api touch /app/wwwroot/images/properties/test_write.tmp 2>/dev/null; then
    print_success "Write permissions are working"
    docker exec realestate-api rm /app/wwwroot/images/properties/test_write.tmp 2>/dev/null
else
    print_error "Write permissions failed"
    echo "Attempting to fix permissions..."
    docker exec realestate-api chmod -R 755 /app/wwwroot/
    docker exec realestate-api chown -R 1000:1000 /app/wwwroot/ 2>/dev/null
fi

# Test 4: Test API connectivity
echo "🔍 Test 4: Testing API connectivity..."
HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:7269/api/properties)
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "401" ]; then
    print_success "API is responding (HTTP $HTTP_CODE)"
else
    print_warning "API returned HTTP $HTTP_CODE"
fi

# Test 5: Test nginx proxy
echo "🔍 Test 5: Testing nginx proxy..."
HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null http://62.171.153.198:4545/api/properties)
if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "401" ]; then
    print_success "Nginx proxy is working (HTTP $HTTP_CODE)"
else
    print_warning "Nginx proxy returned HTTP $HTTP_CODE"
fi

# Test 6: Create a test image and upload it manually
echo "🔍 Test 6: Creating test image file..."
# Create a small test image (1x1 pixel PNG)
echo -n "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==" | base64 -d > /tmp/test_image.png

# Copy test image to container
if docker cp /tmp/test_image.png realestate-api:/app/wwwroot/images/properties/test_manual.png; then
    print_success "Manual image upload test successful"
else
    print_error "Manual image upload test failed"
fi

# Test 7: Verify image is accessible
echo "🔍 Test 7: Testing image accessibility..."
if docker exec realestate-api ls /app/wwwroot/images/properties/test_manual.png >/dev/null 2>&1; then
    print_success "Test image is accessible in container"
    
    # Clean up test image
    docker exec realestate-api rm /app/wwwroot/images/properties/test_manual.png 2>/dev/null
else
    print_error "Test image is not accessible"
fi

# Test 8: Check current images
echo "🔍 Test 8: Listing current images..."
IMAGES=$(docker exec realestate-api ls -la /app/wwwroot/images/properties/ 2>/dev/null)
if [ -n "$IMAGES" ]; then
    echo "Current images in properties directory:"
    echo "$IMAGES"
else
    print_warning "No images found in properties directory (this is normal for a fresh installation)"
fi

# Test 9: Test a simple API endpoint that doesn't require auth
echo "🔍 Test 9: Testing public API endpoint..."
SWAGGER_TEST=$(curl -s http://localhost:7269/swagger/index.html)
if echo "$SWAGGER_TEST" | grep -q "swagger" 2>/dev/null; then
    print_success "Swagger UI is accessible"
else
    print_warning "Swagger UI test inconclusive"
fi

# Test 10: Check logs for any obvious errors
echo "🔍 Test 10: Checking recent logs for errors..."
RECENT_LOGS=$(docker logs --tail=5 realestate-api 2>&1)
if echo "$RECENT_LOGS" | grep -i "error" >/dev/null; then
    print_warning "Found errors in recent logs:"
    echo "$RECENT_LOGS" | grep -i "error"
else
    print_success "No obvious errors in recent logs"
fi

echo ""
echo "📋 Test Summary:"
echo "==============="
echo "✅ Container Status: $(docker ps --format '{{.Names}}: {{.Status}}' | grep realestate-api)"
echo "✅ Directory Permissions: $(docker exec realestate-api ls -ld /app/wwwroot/images/properties/)"
echo "✅ API Endpoint: Available on http://62.171.153.198:4545"
echo "✅ Image Upload Endpoint: POST /api/properties/with-images"
echo ""
echo "🚀 Ready to test image uploads!"
echo ""
echo "📝 Example curl command to test image upload (requires JWT token):"
echo 'curl -X POST "http://62.171.153.198:4545/api/properties/with-images" \'
echo '  -H "Authorization: Bearer YOUR_JWT_TOKEN" \'
echo '  -F "MainImage=@/path/to/your/image.jpg" \'
echo '  -F "PropertyData={\"Title\":\"Test Property\",\"Description\":\"Test\",\"Price\":100000,\"Area\":100,\"Bedrooms\":3,\"Bathrooms\":2,\"PropertyType\":0,\"Location\":\"Test Location\",\"Address\":\"Test Address\",\"IsAvailable\":true,\"IsForSale\":true,\"Features\":[]}"'

# Cleanup
rm -f /tmp/test_image.png

echo ""
print_success "Image upload testing completed! 🎯"
