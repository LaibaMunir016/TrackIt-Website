using System;
using System.Data;
using Dapper;
using System.Collections.Generic;
using Npgsql;
using Microsoft.Extensions.Configuration;
using ProjectWeb.Models;
using ProjectWeb.Interface;
using System.Threading.Tasks;

namespace ProjectWeb.Models.Repository
{
    public class IncomeService : IIncomeService
    {
        private readonly string _connectionString;

        public IncomeService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        
        public void InsertIncome(Income income)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                string insertSql = @"
                INSERT INTO ""Incomes"" 
                (""UserID"", ""DateReceived"", ""Amount"", ""Source"", ""Description"")
                VALUES 
                (@UserID, @DateReceived, @Amount, @Source, @Description)";

                db.Execute(insertSql, income);
            }
        }

        public async Task<decimal> GetCurrentMonthIncomeAsync(string userId)
        {
            const string sql = @"
            SELECT COALESCE(SUM(""Amount""), 0)
            FROM ""Incomes""
            WHERE ""UserID"" = @UserID
            AND EXTRACT(MONTH FROM ""DateReceived"") = EXTRACT(MONTH FROM NOW())
            AND EXTRACT(YEAR FROM ""DateReceived"") = EXTRACT(YEAR FROM NOW());";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<decimal>(sql, new { UserID = userId });
        }

        public void UpsertIncome(Income income)
        {
            using (IDbConnection db = new NpgsqlConnection(_connectionString))
            {
                string checkSql = @"
                SELECT COUNT(1) FROM ""Incomes"" 
                WHERE ""UserID"" = @UserID 
                AND EXTRACT(MONTH FROM ""DateReceived"") = EXTRACT(MONTH FROM @DateReceived::date) 
                AND EXTRACT(YEAR FROM ""DateReceived"") = EXTRACT(YEAR FROM @DateReceived::date)";

                int exists = db.ExecuteScalar<int>(checkSql, new
                {
                income.UserID,
                income.DateReceived
                });

                if (exists > 0)
                {
                    string updateSql = @"
                    UPDATE ""Incomes"" 
                    SET ""Amount"" = @Amount,
                    ""Source"" = @Source,
                    ""Description"" = @Description,
                    ""DateReceived"" = @DateReceived
                    WHERE ""UserID"" = @UserID 
                    AND EXTRACT(MONTH FROM ""DateReceived"") = EXTRACT(MONTH FROM @DateReceived::date) 
                    AND EXTRACT(YEAR FROM ""DateReceived"") = EXTRACT(YEAR FROM @DateReceived::date)";

                    db.Execute(updateSql, new
                    {
                        income.UserID,
                        income.DateReceived,
                        income.Amount,
                        income.Source,
                        income.Description
                    });
                }
                else
                {
                    string insertSql = @"
                    INSERT INTO ""Incomes"" (""UserID"", ""DateReceived"", ""Amount"", ""Source"", ""Description"")
                    VALUES (@UserID, @DateReceived, @Amount, @Source, @Description)";

                    db.Execute(insertSql, new
                    {
                    income.UserID,
                    income.DateReceived,
                    income.Amount,
                    income.Source,
                    income.Description
                    });
                }
            }
        }
    }
}