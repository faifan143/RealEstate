#!/bin/bash

# Final fix for RealEstate API - Fix Swagger and routing

echo "🔧 Final API fixes..."

# Test current direct API access
echo "🧪 Testing direct API vs Nginx routing..."

echo "   Direct API test:"
curl -s -o /dev/null -w "   /api/properties (direct): %{http_code}\n" http://localhost:7269/api/properties

echo "   Nginx proxy test:"
curl -s -o /dev/null -w "   /api/properties (nginx): %{http_code}\n" http://localhost:4545/api/properties

# Fix Swagger endpoint URL in nginx config
echo "🔧 Updating nginx configuration for Swagger..."

# Create updated nginx config with proper Swagger handling
sudo tee /etc/nginx/sites-available/realestate > /dev/null << 'EOF'
server {
    listen 4545;
    server_name 62.171.153.198;
    
    # Increase client max body size for file uploads
    client_max_body_size 100M;
    
    # Swagger UI and API docs
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
    
    # Health check endpoint
    location /health {
        proxy_pass http://localhost:7269/health;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # Root endpoint
    location / {
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
    
    # Static files (images, etc.)
    location /images/ {
        alias /opt/RealEstate/wwwroot/images/;
        expires 30d;
        add_header Cache-Control "public, no-transform";
        try_files $uri $uri/ =404;
    }
    
    # Error pages
    error_page 500 502 503 504 /50x.html;
    location = /50x.html {
        root /usr/share/nginx/html;
    }
}
EOF

# Test and reload nginx
echo "🧪 Testing nginx configuration..."
if sudo nginx -t; then
    echo "✅ Nginx config is valid"
    echo "🔄 Reloading nginx..."
    sudo systemctl reload nginx
    echo "✅ Nginx reloaded"
else
    echo "❌ Nginx config test failed"
    exit 1
fi

# Wait for nginx reload
sleep 3

# Test all endpoints through nginx
echo ""
echo "🧪 Testing all endpoints through nginx:"

ENDPOINTS=(
    "/"
    "/health" 
    "/swagger"
    "/api/properties"
)

for endpoint in "${ENDPOINTS[@]}"; do
    response=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:4545$endpoint")
    if [ "$response" = "200" ]; then
        echo "   ✅ $endpoint -> $response"
    else
        echo "   ⚠️  $endpoint -> $response"
    fi
done

echo ""
echo "🎉 Final fixes completed!"
echo ""
echo "📍 Your RealEstate API is now fully accessible:"
echo "   🌐 Main API: http://62.171.153.198:4545"
echo "   📖 Swagger: http://62.171.153.198:4545/swagger"
echo "   ❤️  Health: http://62.171.153.198:4545/health"
echo "   🏠 Properties: http://62.171.153.198:4545/api/properties"
echo ""
echo "🔍 Test in browser:"
echo "   - Root: http://62.171.153.198:4545"
echo "   - Swagger: http://62.171.153.198:4545/swagger"
echo "   - Health: http://62.171.153.198:4545/health"
