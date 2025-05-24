using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;

namespace RealEstate.API
{
    public static class DatabaseUpdater
    {
        public static async Task AddMissingColumns(string connectionString)
        {
            try
            {
                Console.WriteLine("Adding missing columns to Properties table...");
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Read SQL script
                string sqlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AddMissingColumns.sql");
                string sqlScript = File.ReadAllText(sqlPath);
                
                // Execute SQL
                using var cmd = new NpgsqlCommand(sqlScript, connection);
                await cmd.ExecuteNonQueryAsync();
                
                Console.WriteLine("Database update completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating database: {ex.Message}");
                throw;
            }
        }
    }
} 