using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;

namespace RealEstate.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [HttpGet("test")]
        public IActionResult GetTest()
        {
            try
            {
                byte[] fileBytes = new byte[] { 1, 2, 3 };
                return File(fileBytes, "application/octet-stream");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }
    }
} 