using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;

public class LoginUser
{
    public int ID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime DateOfBirth  { get; set; }
}

public interface IUserRepository
{
    Task<LoginUser> GetByIDAsync(int id);
    Task<List<LoginUser>> GetByDateOfBirthAsync(DateTime dateOfBirth);
}

public class UserRepository : IUserRepository
{
    private readonly IConfiguration _config;

    public UserRepository(IConfiguration config)
    {
        _config = config;
    }    

    public IDbConnection Connection
    {
        get
        {
            return new SqlConnection(_config.GetConnectionString("AppConnectionString"));
        }
    }

    public async Task<List<LoginUser>> GetByDateOfBirthAsync(DateTime dateOfBirth)
    {
        using (IDbConnection conn = Connection)
        {
            string sQuery = "SELECT ID, FirstName, LastName, DateOfBirth FROM LoginUser WHERE DateOfBirth = @DateOfBirth";
            conn.Open();
            var result = await conn.QueryAsync<LoginUser>(sQuery, new { DateOfBirth = dateOfBirth });
            return result.ToList();
        }
    }

    public async Task<LoginUser> GetByIDAsync(int id)
    {
        using (IDbConnection conn = Connection)
        {
            string sQuery = "SELECT ID, FirstName, LastName, DateOfBirth FROM LoginUser WHERE ID = @ID";
            conn.Open();
            var result = await conn.QueryAsync<LoginUser>(sQuery, new { ID = id });
            Console.WriteLine("get id" + id);
            Console.WriteLine(result.FirstOrDefault());
            return result.FirstOrDefault();
        }
    }
}