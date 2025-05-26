#!/bin/bash
# RealEstate VPS Deployment Script
# This script handles the complete deployment process to a VPS

set -e  # Exit on any error

# Text colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="realestate"
COMPOSE_FILE="docker-compose.prod.yml"
BACKUP_DIR="/opt/backups/realestate"
ENV_FILE=".env"

echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}🚀 RealEstate VPS Deployment Script${NC}"
echo -e "${BLUE}========================================${NC}"

# Check if running as root
if [ "$EUID" -ne 0 ]; then
  echo -e "${YELLOW}⚠️  Warning: Not running as root. Some operations may fail.${NC}"
  echo -e "Consider running with sudo: sudo $0 $*"
  sleep 2
fi

# Function to check required tools
check_requirements() {
  echo -e "${BLUE}📋 Checking requirements...${NC}"
  
  local MISSING_TOOLS=()
  
  for tool in docker docker-compose curl git; do
    if ! command -v $tool &> /dev/null; then
      MISSING_TOOLS+=($tool)
    fi
  done
  
  if [ ${#MISSING_TOOLS[@]} -gt 0 ]; then
    echo -e "${RED}❌ Missing required tools: ${MISSING_TOOLS[*]}${NC}"
    echo -e "Please install them before proceeding."
    exit 1
  fi
  
  echo -e "${GREEN}✅ All required tools are installed.${NC}"
}

# Function to create or update .env file
setup_env_file() {
  echo -e "${BLUE}🔧 Setting up environment variables...${NC}"
  
  if [ ! -f "$ENV_FILE" ]; then
    echo -e "${YELLOW}⚠️  No .env file found. Creating one...${NC}"
    
    # Generate a random password if not provided
    DB_PASSWORD=${DB_PASSWORD:-$(openssl rand -base64 12)}
    JWT_SECRET=${JWT_SECRET:-$(openssl rand -base64 32)}
    
    cat > "$ENV_FILE" << EOF
# Database settings
DB_PASSWORD=$DB_PASSWORD

# JWT settings
JWT_SECRET=$JWT_SECRET

# Environment
ASPNETCORE_ENVIRONMENT=Production
EOF
    
    echo -e "${GREEN}✅ Created .env file with secure random passwords.${NC}"
  else
    echo -e "${GREEN}✅ Using existing .env file.${NC}"
    echo -e "${YELLOW}ℹ️  To regenerate passwords, delete the .env file and run again.${NC}"
  fi
  
  # Load environment variables
  export $(grep -v '^#' "$ENV_FILE" | xargs)
}

# Function to create required directories
setup_directories() {
  echo -e "${BLUE}📁 Setting up directories...${NC}"
  
  mkdir -p nginx/conf.d
  mkdir -p db-init
  mkdir -p "$BACKUP_DIR"
  
  # Ensure correct permissions
  chmod -R 755 nginx db-init
  
  echo -e "${GREEN}✅ Directories created.${NC}"
}

# Function to create backup
create_backup() {
  echo -e "${BLUE}📦 Creating database backup...${NC}"
  
  mkdir -p "$BACKUP_DIR"
  BACKUP_FILE="$BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"
  
  if docker ps | grep -q "realestate-postgres"; then
    echo -e "${YELLOW}ℹ️  Database container found, creating backup...${NC}"
    docker exec realestate-postgres pg_dump -U realestateuser realestate > "$BACKUP_FILE"
    echo -e "${GREEN}✅ Backup created: $BACKUP_FILE${NC}"
  else
    echo -e "${YELLOW}⚠️  No running postgres container found, skipping backup${NC}"
  fi
}

# Function to deploy
deploy() {
  echo -e "${BLUE}🔧 Building and deploying containers...${NC}"
  
  # Pull latest images for base images
  echo -e "${YELLOW}ℹ️  Pulling latest base images...${NC}"
  docker-compose -f "$COMPOSE_FILE" pull postgres nginx
  
  # Build the application
  echo -e "${YELLOW}ℹ️  Building API container...${NC}"
  docker-compose -f "$COMPOSE_FILE" build --no-cache api
  
  # Stop existing containers
  echo -e "${YELLOW}ℹ️  Stopping existing containers...${NC}"
  docker-compose -f "$COMPOSE_FILE" down
  
  # Start new containers
  echo -e "${YELLOW}ℹ️  Starting containers...${NC}"
  docker-compose -f "$COMPOSE_FILE" up -d
  
  echo -e "${GREEN}✅ Deployment started.${NC}"
  echo -e "${YELLOW}⏳ Waiting for services to start...${NC}"
  sleep 10
  
  # Check service health
  check_health
}

# Function to check health
check_health() {
  echo -e "${BLUE}🏥 Checking service health...${NC}"
  
  # Check postgres
  if docker ps | grep -q "realestate-postgres.*healthy"; then
    echo -e "${GREEN}✅ PostgreSQL is healthy${NC}"
  else
    echo -e "${YELLOW}⚠️  PostgreSQL is not healthy${NC}"
    docker logs realestate-postgres --tail 20
  fi
  
  # Check API
  if docker ps | grep -q "realestate-api"; then
    echo -e "${GREEN}✅ API container is running${NC}"
    
    # Test API endpoint
    sleep 5
    if curl -s -o /dev/null -w "%{http_code}" http://localhost:4545/health | grep -q "200"; then
      echo -e "${GREEN}✅ API health check passed${NC}"
    else
      echo -e "${YELLOW}⚠️  API health check failed, checking logs...${NC}"
      docker logs realestate-api --tail 20
    fi
  else
    echo -e "${RED}❌ API container is not running${NC}"
  fi
  
  # Check Nginx
  if docker ps | grep -q "realestate-nginx"; then
    echo -e "${GREEN}✅ Nginx is running${NC}"
  else
    echo -e "${RED}❌ Nginx is not running${NC}"
  fi
}

# Function to show logs
show_logs() {
  echo -e "${BLUE}📋 Recent logs:${NC}"
  
  local service=$1
  
  if [ -z "$service" ]; then
    docker-compose -f "$COMPOSE_FILE" logs --tail 50
  else
    if docker ps | grep -q "realestate-$service"; then
      docker-compose -f "$COMPOSE_FILE" logs --tail 50 "$service"
    else
      echo -e "${RED}❌ Service $service not found${NC}"
      echo -e "${YELLOW}Available services: api, postgres, nginx${NC}"
    fi
  fi
}

# Function to show status
show_status() {
  echo -e "${BLUE}📊 Current status:${NC}"
  docker-compose -f "$COMPOSE_FILE" ps
  echo ""
  echo -e "${BLUE}🔗 Container details:${NC}"
  docker ps --filter "name=realestate"
}

# Function to cleanup old images
cleanup() {
  echo -e "${BLUE}🧹 Cleaning up old images...${NC}"
  docker image prune -f
  docker container prune -f
}

# Main deployment flow
main() {
  case "${1:-deploy}" in
    "deploy")
      check_requirements
      setup_env_file
      setup_directories
      create_backup
      deploy
      cleanup
      echo -e "${GREEN}🎉 Deployment completed successfully!${NC}"
      echo -e "${GREEN}🌐 API is available at: http://localhost:4545${NC}"
      ;;
    "status")
      show_status
      ;;
    "logs")
      show_logs "$2"
      ;;
    "health")
      check_health
      ;;
    "backup")
      create_backup
      ;;
    "restart")
      echo -e "${BLUE}🔄 Restarting services...${NC}"
      docker-compose -f "$COMPOSE_FILE" restart
      check_health
      ;;
    "stop")
      echo -e "${BLUE}🛑 Stopping services...${NC}"
      docker-compose -f "$COMPOSE_FILE" down
      ;;
    "start")
      echo -e "${BLUE}▶️  Starting services...${NC}"
      docker-compose -f "$COMPOSE_FILE" up -d
      check_health
      ;;
    *)
      echo -e "${BLUE}Usage: $0 {deploy|status|logs|health|backup|restart|stop|start}${NC}"
      echo ""
      echo -e "${YELLOW}Commands:${NC}"
      echo "  deploy  - Full deployment (backup, build, deploy)"
      echo "  status  - Show current status"
      echo "  logs    - Show recent logs (optional: service name)"
      echo "  health  - Check service health"
      echo "  backup  - Create database backup"
      echo "  restart - Restart services"
      echo "  stop    - Stop all services"
      echo "  start   - Start services"
      exit 1
      ;;
  esac
}

# Run main function with all arguments
main "$@" 