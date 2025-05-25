using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstate.Infrastructure.Data;

namespace RealEstate.API.Controllers
{
    [ApiController]
    [Route("api/database")]
    public class DatabaseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(ApplicationDbContext context, ILogger<DatabaseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("add-missing-columns")]
        // Removed authorization requirement for easy setup
        public async Task<IActionResult> AddMissingColumns()
        {
            try
            {
                // Read the SQL script
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AddMissingColumns.sql");
                if (!System.IO.File.Exists(scriptPath))
                {
                    scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "AddMissingColumns.sql");
                }

                if (!System.IO.File.Exists(scriptPath))
                {
                    return NotFound(new { message = "SQL script not found" });
                }

                var sql = await System.IO.File.ReadAllTextAsync(scriptPath);

                // Execute the SQL script
                await _context.Database.ExecuteSqlRawAsync(sql);

                _logger.LogInformation("Missing columns added successfully");
                return Ok(new { message = "Missing columns added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding missing columns");
                return StatusCode(500, new { message = "Error adding missing columns", error = ex.Message });
            }
        }
    }
} 