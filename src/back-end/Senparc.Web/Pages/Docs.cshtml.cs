using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Senparc.Web.Pages
{
    public class Index1Model : PageModel
    {
        public IActionResult OnGet()
        {
            return Redirect("https://doc.ncf.pub");
        }
    }
}
