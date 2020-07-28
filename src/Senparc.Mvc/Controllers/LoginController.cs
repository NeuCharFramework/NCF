using Senparc.Service;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Service;

namespace Senparc.Mvc.Controllers
{
    public class LoginController : BaseController
    {
        private AccountService _accountService;
        private IQrCodeLoginCache _qrCodeLoginCache;
        public LoginController(IQrCodeLoginCache qrCodeLoginCache, AccountService accountService)
        {
            _qrCodeLoginCache = qrCodeLoginCache;
            this._accountService = accountService;
        }
    }
}
