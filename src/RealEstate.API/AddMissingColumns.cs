using System;
using Npgsql;

namespace RealEstate.API
{
    public static class DatabaseUpdater
    {
        public static void AddMissingColumns(string connectionString)
        {
            try
            {
                Console.WriteLine("Checking if database tables exist before adding columns...");
                
                using var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                
                // First check if Properties table exists
                bool tableExists = TableExists(connection, "Properties");
                
                if (!tableExists)
                {
                    Console.WriteLine("Properties table does not exist yet. Skipping column additions.");
                    Console.WriteLine("Tables will be created through EF Core migrations first.");
                    return;
                }
                
                Console.WriteLine("Properties table exists. Safe to proceed with column additions if needed.");
                // Additional column logic would go here
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in database updater: {ex.Message}");
                Console.WriteLine("Continuing with application startup...");
            }
        }
        
        private static bool TableExists(NpgsqlConnection connection, string tableName)
        {
            using var cmd = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = @tableName)", 
                connection);
            cmd.Parameters.AddWithValue("tableName", tableName);
            var result = cmd.ExecuteScalar();
            return result != null && (bool)result;
        }
    }
}
