using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIUB.Portal.Services
{
    public interface IAIUBPortalService
    {
        Task<(bool success, string msg, Dictionary<string, object> result)> LoginAsync(string username, string password);
    }
}
