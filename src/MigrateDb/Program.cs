using System;
using System.Threading.Tasks;
using Npgsql;

namespace MigrateDb
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            string connectionString = "Host=localhost;Port=5432;Database=RealEstateDb;Username=postgres;Password=123";
            
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            Console.WriteLine("Checking and adding missing columns to Properties table...");
            
            // Check if IsForRent column exists
            if (!await ColumnExists(connection, "Properties", "IsForRent"))
            {
                Console.WriteLine("Adding IsForRent column...");
                await ExecuteNonQuery(connection, 
                    "ALTER TABLE \"Properties\" ADD COLUMN \"IsForRent\" boolean NOT NULL DEFAULT false;");
                Console.WriteLine("IsForRent column added successfully.");
            }
            else
            {
                Console.WriteLine("IsForRent column already exists.");
            }
            
            // Check if IsForSale column exists
            if (!await ColumnExists(connection, "Properties", "IsForSale"))
            {
                Console.WriteLine("Adding IsForSale column...");
                await ExecuteNonQuery(connection, 
                    "ALTER TABLE \"Properties\" ADD COLUMN \"IsForSale\" boolean NOT NULL DEFAULT true;");
                Console.WriteLine("IsForSale column added successfully.");
            }
            else
            {
                Console.WriteLine("IsForSale column already exists.");
            }
            
            Console.WriteLine("All necessary columns have been checked and added if needed.");
        }
        
        private static async Task<bool> ColumnExists(NpgsqlConnection connection, string tableName, string columnName)
        {
            string sql = @"
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = @tableName
                AND column_name = @columnName";
            
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("tableName", tableName);
            cmd.Parameters.AddWithValue("columnName", columnName);
            
            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
        
        private static async Task ExecuteNonQuery(NpgsqlConnection connection, string sql)
        {
            using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }
} 