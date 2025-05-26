#!/bin/bash
# Simple health check script for RealEstate API

# Try to access the health endpoint
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5268/health)

# Check if response is 200 OK
if [ "$RESPONSE" -eq 200 ]; then
    echo "API is healthy"
    exit 0
else
    echo "API health check failed with status: $RESPONSE"
    exit 1
fi 