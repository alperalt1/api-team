using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace nowClock.Application.Interfaces.Auth
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
