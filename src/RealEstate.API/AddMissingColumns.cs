using System;
using Npgsql;

namespace RealEstate.API
{
    public static class DatabaseUpdater
    {
        public static void AddMissingColumns(string connectionString)
        {
            Console.WriteLine("Database updater disabled for initial deployment.");
            Console.WriteLine("The database schema will be created through EF Core migrations.");
            // Do nothing - let EF Core handle the schema
        }
    }
}