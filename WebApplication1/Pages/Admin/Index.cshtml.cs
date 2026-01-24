using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Revia.Pages.Admin
{
    [Authorize(Roles = "Admin")] // Doar Adminul intră aici
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
