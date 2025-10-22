using System.Data;

namespace FormsBackend.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}