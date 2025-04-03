using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIUB.Portal.Services
{
    public interface IAIUBPortalService
    {
        Task<(bool success, string msg, Dictionary<string, object> result, string captchaImageUrl, string captchaId)> LoginAsync(string username, string password);
        Task<(bool success, string msg, Dictionary<string, object> result)> SubmitCaptchaAsync(string username, string password, string captchaCode, string captchaId);
        Task<bool> TryLoadSavedSession();
    }
}
