using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore.Api.Controllers
{
    //[Route("api/userlist")]
    [ApiController]
    //[Authorize(Roles ="Admin")]
    public class UserListController : ControllerBase
    {
        [HttpGet]
        [Route("api/userlistOnlyAdmin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get()
        {
            var uerlist= await Task.FromResult(new string[] { "Virat", "Messi", "Ozil", "Lara", "MS Dhoni" });
            return Ok(uerlist);
        }



        [Route("api/userlistAdminAndUser")]
        [HttpGet, Authorize(Roles = "Admin,User")]
        public async Task<string[]> sdasdasd()
        {
            var uerlist = await Task.FromResult(new string[] { "Virat", "Messi", "Ozil", "Lara", "MS Dhoni" });

            return uerlist;
        }
    }

   
}
