using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers.Commons
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase { }
}
