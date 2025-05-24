   #!/bin/bash

   # Wait for the database to be ready
   echo "Waiting for the database to be ready..."
   sleep 10

   # Check if tables exist
   HAS_TABLES=$(sudo -u postgres psql -d realestate -t -c "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Properties')")

   if [[ $HAS_TABLES == *"t"* ]]; then
       echo "Tables exist, running SQL migration script..."
       sudo -u postgres psql -d realestate -f /var/www/realestate/publish/AddMissingColumns.sql
   else
       echo "Tables don't exist yet. Please run this script after the application has started and created the tables."
   fi
