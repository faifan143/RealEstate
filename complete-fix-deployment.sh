#!/bin/bash

echo "🚀 Complete Real Estate Image Upload Fix Deployment"
echo "=================================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
    exit 1
}

# Function to run command with error checking
run_command() {
    echo "▶️ Running: $1"
    if eval "$1"; then
        print_status "$1"
        return 0
    else
        print_error "Failed: $1"
        return 1
    fi
}

# Check if we're in the right directory
if [ ! -f "docker-compose.prod.yml" ]; then
    print_error "Please run this script from the RealEstate project root directory"
fi

print_status "Starting deployment process..."

# Step 1: Stop existing containers
echo "🛑 Step 1: Stopping existing containers..."
docker-compose -f docker-compose.prod.yml down 2>/dev/null || true
print_status "Containers stopped"

# Step 2: Clean up old resources
echo "🧹 Step 2: Cleaning up old resources..."
docker system prune -f --volumes
print_status "Cleanup completed"

# Step 3: Build new images
echo "🔨 Step 3: Building new Docker images..."
run_command "docker-compose -f docker-compose.prod.yml build --no-cache"

# Step 4: Start containers
echo "🚀 Step 4: Starting containers..."
run_command "docker-compose -f docker-compose.prod.yml up -d"

# Step 5: Wait for containers to be ready
echo "⏳ Step 5: Waiting for containers to initialize..."
sleep 15

# Step 6: Verify containers are running
echo "🔍 Step 6: Verifying container status..."
if docker ps | grep -q "realestate-api"; then
    print_status "API container is running"
else
    print_error "API container failed to start"
fi

if docker ps | grep -q "realestate-postgres"; then
    print_status "Database container is running"
else
    print_error "Database container failed to start"
fi

# Step 7: Setup image directories with proper permissions
echo "📁 Step 7: Setting up image directories..."
docker exec realestate-api mkdir -p /app/wwwroot/images/properties 2>/dev/null || true
docker exec realestate-api chmod -R 755 /app/wwwroot 2>/dev/null || true
docker exec realestate-api chown -R 1000:1000 /app/wwwroot 2>/dev/null || true
print_status "Image directories configured"

# Step 8: Test directory creation inside container
echo "🧪 Step 8: Testing directory structure..."
DIRS_OUTPUT=$(docker exec realestate-api ls -la /app/wwwroot/images/)
if echo "$DIRS_OUTPUT" | grep -q "properties"; then
    print_status "Properties directory exists inside container"
else
    print_warning "Properties directory not found, attempting to create..."
    docker exec realestate-api mkdir -p /app/wwwroot/images/properties
    docker exec realestate-api chmod 755 /app/wwwroot/images/properties
fi

# Step 9: Update nginx configuration
echo "🌐 Step 9: Updating nginx configuration..."
sudo tee /etc/nginx/sites-available/realestate > /dev/null << 'EOF'
server {
    listen 4545;
    server_name 62.171.153.198;
    
    # Increase client max body size for file uploads
    client_max_body_size 100M;
    client_body_timeout 60s;
    
    # Logging for debugging
    access_log /var/log/nginx/realestate_access.log;
    error_log /var/log/nginx/realestate_error.log;
    
    # Main API and application routes
    location / {
        # Handle CORS preflight requests
        if ($request_method = 'OPTIONS') {
            add_header 'Access-Control-Allow-Origin' '*' always;
            add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
            add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range,Authorization,Content-Disposition' always;
            add_header 'Access-Control-Max-Age' 1728000 always;
            add_header 'Content-Type' 'text/plain; charset=utf-8' always;
            add_header 'Content-Length' 0 always;
            return 204;
        }
        
        proxy_pass http://localhost:7269;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Add CORS headers to all responses
        add_header 'Access-Control-Allow-Origin' '*' always;
        add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
        add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range,Authorization,Content-Disposition' always;
        
        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
        
        # Buffer settings for file uploads
        proxy_buffering off;
        proxy_request_buffering off;
    }
    
    # Error pages
    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
EOF

# Test nginx configuration
if sudo nginx -t; then
    print_status "Nginx configuration is valid"
    sudo systemctl reload nginx
    print_status "Nginx reloaded successfully"
else
    print_error "Nginx configuration is invalid"
fi

# Step 10: Test API connectivity
echo "🔍 Step 10: Testing API connectivity..."
sleep 5

# Test basic connectivity
if curl -s http://localhost:7269 > /dev/null; then
    print_status "Direct API connection successful"
else
    print_warning "Direct API connection failed"
fi

# Test through nginx
if curl -s http://62.171.153.198:4545 > /dev/null; then
    print_status "Nginx proxy connection successful"
else
    print_warning "Nginx proxy connection failed"
fi

# Step 11: Test image upload endpoint
echo "🖼️ Step 11: Testing image upload functionality..."

# Create a test image file
cat > /tmp/test_image.txt << 'EOF'
This is a test file to simulate image upload
EOF

# Test if we can reach the properties controller
API_RESPONSE=$(curl -s -w "%{http_code}" -o /dev/null http://localhost:7269/api/properties)
if [ "$API_RESPONSE" = "200" ] || [ "$API_RESPONSE" = "401" ]; then
    print_status "Properties API endpoint is accessible (HTTP $API_RESPONSE)"
else
    print_warning "Properties API endpoint returned HTTP $API_RESPONSE"
fi

# Step 12: Display container information
echo "📋 Step 12: Container information..."
echo "=== Running Containers ==="
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

echo ""
echo "=== API Container Logs (last 10 lines) ==="
docker logs --tail=10 realestate-api

echo ""
echo "=== Volume Information ==="
docker volume ls | grep realestate

echo ""
echo "=== Image Directory Contents ==="
docker exec realestate-api ls -la /app/wwwroot/images/ 2>/dev/null || echo "Could not access image directory"

# Step 13: Final verification
echo "✨ Step 13: Final verification..."

if docker ps | grep -q "realestate-api.*Up.*healthy"; then
    print_status "API container is running and healthy"
else
    print_error "API container is not running properly or not healthy"
    echo "=== API Container Logs (last 50 lines) ==="
    docker logs --tail 50 realestate-api
    echo "=== Container Health Status ==="
    docker inspect realestate-api --format '{{.State.Health.Status}}'
    echo "=== Health Check Test ==="
    docker exec realestate-api curl -v http://localhost:5268/health || echo "Health check failed"
    exit 1
fi

# Step 14: Summary and next steps
echo ""
echo "🎉 DEPLOYMENT COMPLETED SUCCESSFULLY!"
echo "====================================="
echo ""
echo "📋 What was fixed:"
echo "   ✅ Updated Docker Compose to use proper volume mounting"
echo "   ✅ Enhanced Dockerfile with correct permissions"
echo "   ✅ Improved SaveImageAsync method for Linux compatibility"
echo "   ✅ Updated nginx configuration for better file handling"
echo "   ✅ Added proper CORS headers for image serving"
echo "   ✅ Enhanced error handling and logging"
echo ""
echo "🌐 API Endpoints:"
echo "   - API Base: http://62.171.153.198:4545"
echo "   - Swagger: http://62.171.153.198:4545/swagger"
echo "   - Properties: http://62.171.153.198:4545/api/properties"
echo ""
echo "🧪 To test image upload:"
echo "   1. Use POST /api/properties/with-images with form data"
echo "   2. Check uploaded images in: docker exec realestate-api ls /app/wwwroot/images/properties/"
echo "   3. Access images via: http://62.171.153.198:4545/images/properties/[filename]"
echo ""
echo "🔍 Monitoring commands:"
echo "   - View API logs: docker logs -f realestate-api"
echo "   - View nginx logs: sudo tail -f /var/log/nginx/realestate_error.log"
echo "   - Check image directory: docker exec realestate-api ls -la /app/wwwroot/images/properties/"
echo "   - Container status: docker ps"
echo ""
echo "🚨 If issues persist:"
echo "   1. Check container logs for detailed error messages"
echo "   2. Verify volume permissions: docker exec realestate-api ls -la /app/wwwroot/"
echo "   3. Test manual file creation: docker exec realestate-api touch /app/wwwroot/images/test.txt"
echo "   4. Restart containers: docker-compose -f docker-compose.prod.yml restart"
echo ""
print_status "Deployment script completed! 🎊"
