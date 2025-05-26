#!/bin/bash
# Script to identify and resolve port conflicts for RealEstate deployment

# Text colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PORT=4545

echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}🔍 RealEstate Port Conflict Resolution${NC}"
echo -e "${BLUE}========================================${NC}"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo -e "${RED}❌ This script must be run as root to manage processes.${NC}"
  echo -e "Please run with sudo: sudo $0"
  exit 1
fi

# Check if port is in use
check_port() {
  echo -e "${BLUE}📋 Checking if port $PORT is in use...${NC}"
  
  if netstat -tuln | grep -q ":$PORT "; then
    echo -e "${YELLOW}⚠️  Port $PORT is already in use.${NC}"
    return 0
  else
    echo -e "${GREEN}✅ Port $PORT is available.${NC}"
    return 1
  fi
}

# Find process using the port
find_process() {
  echo -e "${BLUE}🔍 Finding process using port $PORT...${NC}"
  
  # Try different tools to find the process
  if command -v lsof &> /dev/null; then
    PROCESS_INFO=$(lsof -i :$PORT -t)
  elif command -v fuser &> /dev/null; then
    PROCESS_INFO=$(fuser $PORT/tcp 2>/dev/null)
  elif command -v ss &> /dev/null; then
    PROCESS_INFO=$(ss -lptn "sport = :$PORT" | grep -oP '(?<=pid=)(\d+)' | head -n 1)
  elif command -v netstat &> /dev/null; then
    PROCESS_INFO=$(netstat -tlnp | grep ":$PORT " | awk '{print $7}' | cut -d'/' -f1)
  else
    echo -e "${RED}❌ No tools available to find the process.${NC}"
    return 1
  fi
  
  if [ -z "$PROCESS_INFO" ]; then
    echo -e "${RED}❌ Could not identify the process using port $PORT.${NC}"
    return 1
  fi
  
  PID=$(echo "$PROCESS_INFO" | head -n 1)
  
  if [ -n "$PID" ]; then
    PROCESS_NAME=$(ps -p $PID -o comm=)
    echo -e "${YELLOW}⚠️  Port $PORT is being used by process: $PROCESS_NAME (PID: $PID)${NC}"
    return 0
  else
    echo -e "${RED}❌ Could not identify the process using port $PORT.${NC}"
    return 1
  fi
}

# Stop the process
stop_process() {
  local PID=$1
  local PROCESS_NAME=$(ps -p $PID -o comm=)
  
  echo -e "${BLUE}🛑 Stopping process $PROCESS_NAME (PID: $PID)...${NC}"
  
  # Try to stop gracefully first
  kill -15 $PID
  sleep 2
  
  # Check if process is still running
  if ps -p $PID > /dev/null; then
    echo -e "${YELLOW}⚠️  Process did not stop gracefully. Forcing...${NC}"
    kill -9 $PID
    sleep 1
  fi
  
  # Check again
  if ps -p $PID > /dev/null; then
    echo -e "${RED}❌ Failed to stop process.${NC}"
    return 1
  else
    echo -e "${GREEN}✅ Process stopped successfully.${NC}"
    return 0
  fi
}

# Main function
main() {
  if check_port; then
    echo -e "${YELLOW}⚠️  Port conflict detected.${NC}"
    
    if find_process; then
      echo -e "${BLUE}❓ Do you want to stop the process using port $PORT? [y/N] ${NC}"
      read -r response
      
      if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        if stop_process "$PID"; then
          echo -e "${GREEN}✅ Port $PORT is now available.${NC}"
          echo -e "${BLUE}ℹ️  You can now run the deployment script.${NC}"
        else
          echo -e "${RED}❌ Could not free up port $PORT.${NC}"
          echo -e "${YELLOW}⚠️  Please manually stop the process or use a different port.${NC}"
        fi
      else
        echo -e "${YELLOW}⚠️  Operation cancelled.${NC}"
        echo -e "${BLUE}ℹ️  You can either:${NC}"
        echo -e "  1. Manually stop the process using: sudo kill $PID"
        echo -e "  2. Change the port in docker-compose.prod.yml"
      fi
    else
      echo -e "${YELLOW}⚠️  Could not identify the process. Try manually with:${NC}"
      echo -e "  sudo netstat -tulnp | grep $PORT"
      echo -e "  sudo lsof -i :$PORT"
    fi
  else
    echo -e "${GREEN}✅ No port conflict detected. You can proceed with deployment.${NC}"
  fi
}

# Run main function
main 