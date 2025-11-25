using CoreProject.Services.IService;
using CoreProject.Utilities.DTOs;
using FaceRecognition.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MvcCoreProject.Controllers.Api
{
    /// <summary>
    /// API controller for mobile app attendance operations (check-in/check-out)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]  // ADD THIS LINE
    public class FaceApiController : ControllerBase
    {
        private readonly ILogger<FaceApiController> _logger;
        private readonly IFaceVerificationService _faceService;
        private readonly IAttendanceApiService _attendanceApiService;  // ADD THIS FIELD

        public FaceApiController(
            IFaceVerificationService faceService,
            IAttendanceApiService attendanceApiService,
            ILogger<FaceApiController> logger)
        {
            _faceService = faceService;
            _attendanceApiService = attendanceApiService;  // ADD THIS LINE
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Verify if two faces belong to the same person
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> Verify(IFormFile file1, IFormFile file2, CancellationToken cancellationToken)
        {
            if (file1 == null || file1.Length == 0 || file2 == null || file2.Length == 0)
                return BadRequest(new { success = false, error = "Two files are required" });

            try
            {
                using var ms1 = new MemoryStream();
                using var ms2 = new MemoryStream();

                await file1.CopyToAsync(ms1, cancellationToken);
                await file2.CopyToAsync(ms2, cancellationToken);

                var result = await _faceService.VerifyAsync(ms1.ToArray(), ms2.ToArray(), cancellationToken);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Verify endpoint");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }
    }
}