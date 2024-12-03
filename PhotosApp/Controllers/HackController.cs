using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotosApp.Services;

namespace PhotosApp.Controllers
{
    [Route("hack")]
    public class HackController : Controller
    {
        [HttpGet("super_secret_qwe123")]
        public IActionResult GenerateToken()
        {
            var encodedJwt = TemporaryTokens.GenerateEncoded();

            Response.Cookies.Append(TemporaryTokens.CookieName, encodedJwt,
                new CookieOptions { HttpOnly = true });
            return Content(encodedJwt);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("decode")]
        public IActionResult Decode()
        {
            return View();
        }
    }
}