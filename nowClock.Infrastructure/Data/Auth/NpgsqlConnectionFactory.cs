using nowClock.Application.Interfaces.Auth;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace nowClock.Infrastructure.Data.Auth
{
    public class NpgsqlConnectionFactory: IDbConnectionFactory
    {
        private readonly string _connectionString;

        public NpgsqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
    }
}
