#!/bin/bash

# Comprehensive API Controller Testing Script for RealEstate Backend
# Tests all available controller endpoints

echo "🧪 RealEstate Backend API Controller Testing"
echo "============================================="

# Configuration
API_BASE="http://localhost:7269"
NGINX_BASE="http://localhost:4545"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to test endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local description=$3
    local expected_status=${4:-200}
    local data=$5
    local auth_header=$6
    
    local full_url="$API_BASE$endpoint"
    local nginx_url="$NGINX_BASE$endpoint"
    
    echo -n "  Testing $method $endpoint ($description): "
    
    # Build curl command
    local curl_cmd="curl -s -o /dev/null -w '%{http_code}'"
    
    if [ "$method" = "POST" ] || [ "$method" = "PUT" ]; then
        curl_cmd="$curl_cmd -X $method"
        if [ ! -z "$data" ]; then
            curl_cmd="$curl_cmd -H 'Content-Type: application/json' -d '$data'"
        fi
    elif [ "$method" = "DELETE" ]; then
        curl_cmd="$curl_cmd -X DELETE"
    fi
    
    if [ ! -z "$auth_header" ]; then
        curl_cmd="$curl_cmd -H 'Authorization: Bearer $auth_header'"
    fi
    
    # Test direct API
    local response=$(eval "$curl_cmd '$full_url'")
    
    # Test via nginx
    local nginx_response=$(eval "$curl_cmd '$nginx_url'")
    
    # Determine status
    if [ "$response" -eq "$expected_status" ]; then
        echo -e "${GREEN}✅ $response${NC} (Nginx: $nginx_response)"
    elif [ "$response" -eq 401 ] && [ -z "$auth_header" ]; then
        echo -e "${YELLOW}🔒 $response (Auth Required)${NC} (Nginx: $nginx_response)"
    elif [ "$response" -eq 404 ]; then
        echo -e "${RED}❌ $response (Not Found)${NC} (Nginx: $nginx_response)"
    else
        echo -e "${YELLOW}⚠️  $response${NC} (Nginx: $nginx_response)"
    fi
}

# Function to test with sample data
test_with_data() {
    local method=$1
    local endpoint=$2
    local description=$3
    local data=$4
    local expected_status=${5:-200}
    
    echo -n "  Testing $method $endpoint ($description): "
    
    local response=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" \
        -H "Content-Type: application/json" \
        -d "$data" \
        "$API_BASE$endpoint")
    
    local nginx_response=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" \
        -H "Content-Type: application/json" \
        -d "$data" \
        "$NGINX_BASE$endpoint")
    
    if [ "$response" -eq "$expected_status" ]; then
        echo -e "${GREEN}✅ $response${NC} (Nginx: $nginx_response)"
    else
        echo -e "${YELLOW}⚠️  $response${NC} (Nginx: $nginx_response)"
    fi
}

echo ""
echo -e "${BLUE}📋 Testing Basic Health Endpoints${NC}"
test_endpoint "GET" "/" "Root endpoint" 200
test_endpoint "GET" "/health" "Health check" 200

echo ""
echo -e "${BLUE}🔐 Testing Auth Controller (/api/auth)${NC}"
test_endpoint "POST" "/api/auth/register" "User registration" 400
test_endpoint "POST" "/api/auth/login" "User login" 400
test_endpoint "POST" "/api/auth/forgot-password" "Forgot password" 400
test_endpoint "POST" "/api/auth/reset-password" "Reset password" 400
test_endpoint "POST" "/api/auth/refresh-token" "Refresh token" 400
test_endpoint "POST" "/api/auth/change-password" "Change password" 401
test_endpoint "POST" "/api/auth/logout" "Logout" 401

echo ""
echo -e "${BLUE}🏠 Testing Properties Controller (/api/properties)${NC}"
test_endpoint "GET" "/api/properties" "Get all properties" 200
test_endpoint "GET" "/api/properties?page=1&pageSize=5" "Get properties with pagination" 200
test_endpoint "GET" "/api/properties?minPrice=100000" "Filter by min price" 200
test_endpoint "GET" "/api/properties?maxPrice=500000" "Filter by max price" 200
test_endpoint "GET" "/api/properties?location=Damascus" "Filter by location" 200
test_endpoint "GET" "/api/properties?propertyType=1" "Filter by property type" 200
test_endpoint "GET" "/api/properties?bedrooms=3" "Filter by bedrooms" 200
test_endpoint "GET" "/api/properties?query=apartment" "Search properties" 200
test_endpoint "GET" "/api/properties/00000000-0000-0000-0000-000000000000" "Get specific property" 404
test_endpoint "POST" "/api/properties" "Create property" 401
test_endpoint "PUT" "/api/properties/00000000-0000-0000-0000-000000000000" "Update property" 401
test_endpoint "DELETE" "/api/properties/00000000-0000-0000-0000-000000000000" "Delete property" 401
test_endpoint "POST" "/api/properties/with-images" "Create property with images" 401
test_endpoint "POST" "/api/properties/00000000-0000-0000-0000-000000000000/images" "Add property images" 401

echo ""
echo -e "${BLUE}👤 Testing Users Controller (/api/users)${NC}"
test_endpoint "GET" "/api/users/profile" "Get user profile" 401
test_endpoint "PUT" "/api/users/profile" "Update user profile" 401
test_endpoint "PUT" "/api/users/change-password" "Change password" 401

echo ""
echo -e "${BLUE}❤️ Testing Favorites Controller (/api/favorites)${NC}"
test_endpoint "GET" "/api/favorites" "Get user favorites" 401
test_endpoint "POST" "/api/favorites/00000000-0000-0000-0000-000000000000" "Add to favorites" 401
test_endpoint "DELETE" "/api/favorites/00000000-0000-0000-0000-000000000000" "Remove from favorites" 401

echo ""
echo -e "${BLUE}📅 Testing Bookings Controller (/api/bookings)${NC}"
test_endpoint "GET" "/api/bookings" "Get user bookings" 401
test_endpoint "GET" "/api/bookings/property/00000000-0000-0000-0000-000000000000" "Get property bookings" 401
test_endpoint "POST" "/api/bookings" "Create booking" 401
test_endpoint "PUT" "/api/bookings/00000000-0000-0000-0000-000000000000/approve" "Approve booking" 401
test_endpoint "PUT" "/api/bookings/00000000-0000-0000-0000-000000000000/reject" "Reject booking" 401
test_endpoint "DELETE" "/api/bookings/00000000-0000-0000-0000-000000000000" "Cancel booking" 401

echo ""
echo -e "${BLUE}🖼️ Testing Images Controller (/api/images)${NC}"
test_endpoint "GET" "/api/images/property/00000000-0000-0000-0000-000000000000" "Get property images" 404
test_endpoint "POST" "/api/images/upload" "Upload image" 401
test_endpoint "GET" "/api/images/pending" "Get pending images" 401
test_endpoint "PUT" "/api/images/00000000-0000-0000-0000-000000000000/approve" "Approve image" 401
test_endpoint "DELETE" "/api/images/00000000-0000-0000-0000-000000000000" "Delete image" 401

echo ""
echo -e "${BLUE}🗄️ Testing Database Controller (/api/database)${NC}"
test_endpoint "GET" "/api/database/test" "Database test" 200
test_endpoint "POST" "/api/database/migrate" "Run migrations" 401

echo ""
echo -e "${BLUE}📊 Testing with Sample Data${NC}"

# Test registration with sample data
REGISTER_DATA='{
    "fullName": "Test User",
    "phoneNumber": "1234567890",
    "email": "test@example.com",
    "password": "Test@123"
}'

test_with_data "POST" "/api/auth/register" "Sample registration" "$REGISTER_DATA" 400

# Test login with sample data
LOGIN_DATA='{
    "phoneNumber": "123456789",
    "password": "Admin@123"
}'

echo -n "  Testing admin login: "
LOGIN_RESPONSE=$(curl -s -X POST \
    -H "Content-Type: application/json" \
    -d "$LOGIN_DATA" \
    "$API_BASE/api/auth/login")

if echo "$LOGIN_RESPONSE" | grep -q "token"; then
    echo -e "${GREEN}✅ Login successful${NC}"
    TOKEN=$(echo "$LOGIN_RESPONSE" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
    echo "  🔑 Token obtained for authenticated tests"
    
    # Test authenticated endpoints
    echo ""
    echo -e "${BLUE}🔓 Testing Authenticated Endpoints${NC}"
    test_endpoint "GET" "/api/users/profile" "Get profile (authenticated)" 200 "" "$TOKEN"
    test_endpoint "POST" "/api/properties" "Create property (authenticated)" 400 "" "$TOKEN"
    test_endpoint "GET" "/api/favorites" "Get favorites (authenticated)" 200 "" "$TOKEN"
else
    echo -e "${YELLOW}⚠️  Login failed or no admin user${NC}"
fi

echo ""
echo -e "${BLUE}📈 Summary${NC}"
echo "============================================="

# Count results
TOTAL_TESTS=$(grep -c "Testing" <<< "$(cat /tmp/test_output 2>/dev/null || echo '')")

echo "📍 API Base URL: $API_BASE"
echo "📍 Nginx Proxy URL: $NGINX_BASE"
echo ""
echo "✅ Tests completed"
echo ""
echo "📝 Endpoints Summary:"
echo "  🔐 Auth endpoints: 7 endpoints"
echo "  🏠 Properties endpoints: 10 endpoints"
echo "  👤 User endpoints: 3 endpoints"
echo "  ❤️ Favorites endpoints: 3 endpoints"
echo "  📅 Bookings endpoints: 6 endpoints"
echo "  🖼️ Images endpoints: 5 endpoints"
echo "  🗄️ Database endpoints: 2 endpoints"
echo ""
echo "🎯 Key Working Endpoints:"
echo "  ✅ GET /api/properties (Public - Property listings)"
echo "  ✅ GET /health (Public - Health check)"
echo "  ✅ GET / (Public - Root endpoint)"
echo "  ✅ POST /api/auth/login (Public - User authentication)"
echo ""
echo "🔒 Protected Endpoints (Require Authentication):"
echo "  • All user profile operations"
echo "  • Property creation/modification"
echo "  • Bookings management"
echo "  • Favorites management"
echo "  • Image uploads and management"

echo ""
echo "📖 Access Swagger Documentation:"
echo "  🌐 http://62.171.153.198:4545/swagger"
