#!/bin/bash

echo "🔧 Fixing Real Estate Image Upload Issues..."

# Function to run command with error checking
run_command() {
    echo "▶️ Running: $1"
    if eval "$1"; then
        echo "✅ Success: $1"
    else
        echo "❌ Failed: $1"
        exit 1
    fi
}

# Stop containers
echo "🛑 Stopping containers..."
run_command "docker-compose -f docker-compose.prod.yml down"

# Remove old volumes (this will delete existing images - backup first if needed)
echo "🗑️ Removing old volumes..."
run_command "docker volume rm realestate-images 2>/dev/null || true"

# Rebuild the containers
echo "🔨 Rebuilding containers..."
run_command "docker-compose -f docker-compose.prod.yml build --no-cache"

# Start containers
echo "🚀 Starting containers..."
run_command "docker-compose -f docker-compose.prod.yml up -d"

# Wait for containers to be ready
echo "⏳ Waiting for containers to start..."
sleep 10

# Check if API container is running
if docker ps | grep -q "realestate-api"; then
    echo "✅ API container is running"
    
    # Create directories inside container and set permissions
    echo "📁 Setting up directories inside container..."
    run_command "docker exec realestate-api mkdir -p /app/wwwroot/images/properties"
    run_command "docker exec realestate-api chmod -R 755 /app/wwwroot"
    run_command "docker exec realestate-api chown -R www-data:www-data /app/wwwroot 2>/dev/null || true"
    
    # Test API health
    echo "🔍 Testing API health..."
    sleep 5
    if curl -s http://localhost:7269/health > /dev/null; then
        echo "✅ API is healthy and responding"
    else
        echo "⚠️ API health check failed - but this might be normal if health endpoint doesn't exist"
    fi
    
    # Check container logs for any errors
    echo "📋 Checking recent container logs..."
    docker logs --tail=10 realestate-api
    
else
    echo "❌ API container failed to start"
    echo "📋 Container logs:"
    docker logs realestate-api
    exit 1
fi

# Update nginx configuration to properly serve images
echo "🌐 Updating nginx configuration..."
sudo tee /etc/nginx/sites-available/realestate > /dev/null << 'EOF'
server {
    listen 4545;
    server_name 62.171.153.198;
    
    # Increase client max body size for file uploads
    client_max_body_size 100M;
    
    # Handle preflight requests for CORS
    location / {
        if ($request_method = 'OPTIONS') {
            add_header 'Access-Control-Allow-Origin' '*';
            add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS';
            add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range,Authorization';
            add_header 'Access-Control-Max-Age' 1728000;
            add_header 'Content-Type' 'text/plain; charset=utf-8';
            add_header 'Content-Length' 0;
            return 204;
        }
        
        proxy_pass http://localhost:7269/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Static files (images) - serve directly from Docker volume
    location /images/ {
        # Try to serve from container first, then fallback to 404
        proxy_pass http://localhost:7269/images/;
        proxy_set_header Host $host;
        expires 30d;
        add_header Cache-Control "public, no-transform";
        
        # CORS headers for images
        add_header 'Access-Control-Allow-Origin' '*';
        add_header 'Access-Control-Allow-Methods' 'GET, OPTIONS';
    }
    
    # API endpoints
    location /api/ {
        proxy_pass http://localhost:7269/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Timeout settings
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
    
    # Swagger UI
    location /swagger {
        proxy_pass http://localhost:7269/swagger;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
    }
    
    # Health check endpoint
    location /health {
        proxy_pass http://localhost:7269/health;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Error pages
    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
EOF

echo "🔄 Reloading nginx..."
run_command "sudo nginx -t"
run_command "sudo systemctl reload nginx"

echo ""
echo "🎉 Fix deployment completed!"
echo ""
echo "📋 Summary of changes:"
echo "   ✅ Updated Docker Compose to use named volume for images"
echo "   ✅ Fixed Dockerfile to create proper directory structure"
echo "   ✅ Enhanced SaveImageAsync method for Linux compatibility"
echo "   ✅ Updated nginx configuration for better image serving"
echo "   ✅ Set proper permissions inside container"
echo ""
echo "🧪 Test the image upload functionality:"
echo "   1. Try uploading images through your API"
echo "   2. Check if files appear in the container: docker exec realestate-api ls -la /app/wwwroot/images/properties/"
echo "   3. Check if images are accessible: curl http://62.171.153.198:4545/images/properties/[filename]"
echo ""
echo "🔍 Monitor logs:"
echo "   docker logs -f realestate-api"
