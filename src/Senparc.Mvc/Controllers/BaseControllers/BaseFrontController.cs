using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.Core.Extensions;

namespace Senparc.Mvc.Controllers
{
    [UserAuthorize("UserAnonymous")]
    [AllowAnonymous]
    public class BaseFrontController : BaseController
    {

    }
}
