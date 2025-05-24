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
            
            Console.WriteLine("Checking and adding missing columns to AspNetUsers table...");
            
            // Check if IsPhoneVerified column exists
            if (!await ColumnExists(connection, "AspNetUsers", "IsPhoneVerified"))
            {
                Console.WriteLine("Adding IsPhoneVerified column...");
                await ExecuteNonQuery(connection, 
                    "ALTER TABLE \"AspNetUsers\" ADD COLUMN \"IsPhoneVerified\" boolean NOT NULL DEFAULT false;");
                Console.WriteLine("IsPhoneVerified column added successfully.");
            }
            else
            {
                Console.WriteLine("IsPhoneVerified column already exists.");
            }
            
            // Check if PhoneVerificationCode column exists
            if (!await ColumnExists(connection, "AspNetUsers", "PhoneVerificationCode"))
            {
                Console.WriteLine("Adding PhoneVerificationCode column...");
                await ExecuteNonQuery(connection, 
                    "ALTER TABLE \"AspNetUsers\" ADD COLUMN \"PhoneVerificationCode\" text;");
                Console.WriteLine("PhoneVerificationCode column added successfully.");
            }
            else
            {
                Console.WriteLine("PhoneVerificationCode column already exists.");
            }
            
            // Check if PhoneVerificationExpiry column exists
            if (!await ColumnExists(connection, "AspNetUsers", "PhoneVerificationExpiry"))
            {
                Console.WriteLine("Adding PhoneVerificationExpiry column...");
                await ExecuteNonQuery(connection, 
                    "ALTER TABLE \"AspNetUsers\" ADD COLUMN \"PhoneVerificationExpiry\" timestamp with time zone;");
                Console.WriteLine("PhoneVerificationExpiry column added successfully.");
            }
            else
            {
                Console.WriteLine("PhoneVerificationExpiry column already exists.");
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