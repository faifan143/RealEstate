#!/bin/bash

# Setup Nginx configuration for RealEstate API

echo "🌐 Setting up Nginx configuration for RealEstate..."

# Create the static files directory
echo "📁 Creating static files directory..."
sudo mkdir -p /opt/RealEstate/wwwroot/images
sudo chown -R www-data:www-data /opt/RealEstate/wwwroot

# Copy the nginx config
echo "📝 Installing nginx configuration..."
sudo cp nginx-realestate.conf /etc/nginx/sites-available/realestate

# Enable the site
echo "🔗 Enabling the site..."
sudo ln -sf /etc/nginx/sites-available/realestate /etc/nginx/sites-enabled/realestate

# Test nginx configuration
echo "🧪 Testing nginx configuration..."
if sudo nginx -t; then
    echo "✅ Nginx configuration is valid"
    
    # Reload nginx
    echo "🔄 Reloading nginx..."
    sudo systemctl reload nginx
    
    echo "✅ Nginx successfully configured and reloaded!"
else
    echo "❌ Nginx configuration test failed"
    exit 1
fi

# Show nginx status
echo ""
echo "📊 Nginx status:"
sudo systemctl status nginx --no-pager -l

echo ""
echo "🎉 Nginx setup completed!"
echo ""
echo "📍 Your RealEstate API is now accessible at:"
echo "   🌐 Main API: http://62.171.153.198:4545"
echo "   📖 Swagger: http://62.171.153.198:4545/swagger"
echo "   ❤️  Health: http://62.171.153.198:4545/health"
echo ""
echo "🔍 To test:"
echo "   curl http://62.171.153.198:4545/health"
