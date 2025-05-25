#!/bin/bash

# Simple Docker deployment script for RealEstate on VPS
# This script handles the complete Docker workflow

set -e  # Exit on any error

echo "🚀 Starting RealEstate Docker deployment..."

# Configuration
PROJECT_NAME="realestate"
COMPOSE_FILE="docker-compose.prod.yml"
BACKUP_DIR="/opt/backups/realestate"

# Function to create backup
create_backup() {
    echo "📦 Creating database backup..."
    mkdir -p $BACKUP_DIR
    BACKUP_FILE="$BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"
    
    if docker ps | grep -q "realestate-postgres"; then
        docker exec realestate-postgres pg_dump -U postgres RealEstateDb > $BACKUP_FILE
        echo "✅ Backup created: $BACKUP_FILE"
    else
        echo "⚠️  No running postgres container found, skipping backup"
    fi
}

# Function to deploy
deploy() {
    echo "🔧 Building and deploying containers..."
    
    # Pull latest images for base images
    docker-compose -f $COMPOSE_FILE pull postgres
    
    # Build the application
    docker-compose -f $COMPOSE_FILE build --no-cache api
    
    # Stop existing containers
    docker-compose -f $COMPOSE_FILE down
    
    # Start new containers
    docker-compose -f $COMPOSE_FILE up -d
    
    echo "⏳ Waiting for services to start..."
    sleep 10
    
    # Check service health
    check_health
}

# Function to check health
check_health() {
    echo "🏥 Checking service health..."
    
    # Check postgres
    if docker ps | grep -q "realestate-postgres.*healthy"; then
        echo "✅ PostgreSQL is healthy"
    else
        echo "❌ PostgreSQL is not healthy"
        docker logs realestate-postgres --tail 20
    fi
    
    # Check API
    if docker ps | grep -q "realestate-api"; then
        echo "✅ API container is running"
        
        # Test API endpoint
        sleep 5
        if curl -f http://localhost:5269/health > /dev/null 2>&1; then
            echo "✅ API health check passed"
        else
            echo "⚠️  API health check failed, checking logs..."
            docker logs realestate-api --tail 20
        fi
    else
        echo "❌ API container is not running"
    fi
}

# Function to show logs
show_logs() {
    echo "📋 Recent logs:"
    docker-compose -f $COMPOSE_FILE logs --tail 50
}

# Function to show status
show_status() {
    echo "📊 Current status:"
    docker-compose -f $COMPOSE_FILE ps
    echo ""
    echo "🔗 Container details:"
    docker ps --filter "name=realestate"
}

# Function to cleanup old images
cleanup() {
    echo "🧹 Cleaning up old images..."
    docker image prune -f
    docker container prune -f
}

# Main deployment flow
main() {
    case "${1:-deploy}" in
        "deploy")
            create_backup
            deploy
            cleanup
            echo "🎉 Deployment completed successfully!"
            echo "🌐 API is available at: http://62.171.153.198:4545"
            ;;
        "status")
            show_status
            ;;
        "logs")
            show_logs
            ;;
        "health")
            check_health
            ;;
        "backup")
            create_backup
            ;;
        "restart")
            echo "🔄 Restarting services..."
            docker-compose -f $COMPOSE_FILE restart
            check_health
            ;;
        "stop")
            echo "🛑 Stopping services..."
            docker-compose -f $COMPOSE_FILE down
            ;;
        "start")
            echo "▶️  Starting services..."
            docker-compose -f $COMPOSE_FILE up -d
            check_health
            ;;
        *)
            echo "Usage: $0 {deploy|status|logs|health|backup|restart|stop|start}"
            echo ""
            echo "Commands:"
            echo "  deploy  - Full deployment (backup, build, deploy)"
            echo "  status  - Show current status"
            echo "  logs    - Show recent logs"
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
