using System.Data;
using Microsoft.Data.SqlClient;

namespace MebelOrg;

public static class Db
{
    public const string Title = "ООО «МебельОрг»";
    public const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=MebelOrgDB;Trusted_Connection=True;TrustServerCertificate=True";

    public static DataTable Query(string sql, params SqlParameter[] ps)
    {
        using var cn = new SqlConnection(ConnectionString);
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(ps);
        var dt = new DataTable();
        new SqlDataAdapter(cmd).Fill(dt);
        return dt;
    }

    public static int Exec(string sql, params SqlParameter[] ps)
    {
        using var cn = new SqlConnection(ConnectionString);
        cn.Open();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(ps);
        return cmd.ExecuteNonQuery();
    }

    public static object Scalar(string sql, params SqlParameter[] ps)
    {
        using var cn = new SqlConnection(ConnectionString);
        cn.Open();
        using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(ps);
        return cmd.ExecuteScalar();
    }
}
