#!/bin/bash

# Quick fix for RealEstate API - Enable Swagger and add health endpoints

echo "🔧 Quick API Fix - Enable Swagger in Production"

# Create a patched Program.cs
echo "📝 Creating patched Program.cs..."

# Read the current Program.cs and apply fixes
cp src/RealEstate.API/Program.cs src/RealEstate.API/Program.cs.backup

# Create a sed script to patch Program.cs
cat > patch_program.sed << 'EOF'
# Replace the development-only swagger block
/^if (app\.Environment\.IsDevelopment())/,/^}$/{
    # Replace the entire if block
    c\
// Enable Swagger in all environments for debugging\
app.UseSwagger();\
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RealEstate API v1"));
}

# Add health check endpoint after UseAuthorization
/^app\.UseAuthorization();$/a\
\
// Add health check endpoint\
app.MapGet("/health", () => "Healthy").WithName("HealthCheck");\
app.MapGet("/", () => "RealEstate API is running").WithName("Root");
EOF

# Apply the patch
sed -f patch_program.sed src/RealEstate.API/Program.cs.backup > src/RealEstate.API/Program.cs

echo "✅ Program.cs patched"

# Clean up
rm patch_program.sed

# Rebuild only the API container
echo "🔨 Rebuilding API container with fixes..."
docker stop realestate-api
docker rm realestate-api

# Build new image
docker build -f src/RealEstate.API/Dockerfile.prod -t realestate_api .

# Start the API container
echo "🚀 Starting fixed API container..."
docker run -d \
  --name realestate-api \
  --network realestate-network \
  -p 7269:5268 \
  -e ConnectionStrings__DefaultConnection="Host=realestate-postgres;Port=5432;Database=realestate;Username=realestateuser;Password=123" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:5268 \
  --restart unless-stopped \
  realestate_api

# Wait for startup
echo "⏳ Waiting for API to start..."
sleep 20

# Test the fixes
echo "🧪 Testing fixes..."

echo "   Testing health endpoint..."
HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/health)
echo "   Health endpoint: $HEALTH"

echo "   Testing root endpoint..."
ROOT=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/)
echo "   Root endpoint: $ROOT"

echo "   Testing swagger..."
SWAGGER=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/swagger)
echo "   Swagger endpoint: $SWAGGER"

echo "   Testing properties (should still work)..."
PROPERTIES=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:7269/api/properties)
echo "   Properties endpoint: $PROPERTIES"

echo ""
echo "🎉 API Fix completed!"
echo ""
echo "📍 Updated URLs:"
echo "   🌐 Main API: http://62.171.153.198:4545"
echo "   📖 Swagger: http://62.171.153.198:4545/swagger"
echo "   ❤️  Health: http://62.171.153.198:4545/health"
echo "   🏠 Properties: http://62.171.153.198:4545/api/properties"

echo ""
echo "📊 Container status:"
docker ps --filter "name=realestate" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
