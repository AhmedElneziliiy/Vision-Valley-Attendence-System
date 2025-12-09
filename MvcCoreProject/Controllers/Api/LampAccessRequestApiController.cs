using CoreProject.Models;
using CoreProject.Services.IService;
using CoreProject.Utilities.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MvcCoreProject.Controllers.Api
{
    [Route("api/lamp-access")]
    [ApiController]
    [Authorize]
    public class LampAccessRequestApiController : ControllerBase
    {
        private readonly ILampAccessRequestService _lampAccessRequestService;
        private readonly ILogger<LampAccessRequestApiController> _logger;

        public LampAccessRequestApiController(
            ILampAccessRequestService lampAccessRequestService,
            ILogger<LampAccessRequestApiController> logger)
        {
            _lampAccessRequestService = lampAccessRequestService;
            _logger = logger;
        }

        /// <summary>
        /// Employee submits a lamp access request
        /// </summary>
        [HttpPost("request")]
        // [Authorize(Roles = "Employee,Manager,HR,Admin")]
        [Authorize]  // Accepts any authenticated user!

        public async Task<ActionResult<LampAccessResponseDto>> SubmitRequest([FromBody] LampAccessRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new LampAccessResponseDto
                    {
                        Success = false,
                        Message = "User not authenticated."
                    });
                }

                var userId = int.Parse(userIdClaim.Value);

                var (success, message, request) = await _lampAccessRequestService.SubmitRequestAsync(
                    userId, dto.LampID, dto.Reason);

                if (!success)
                {
                    return BadRequest(new LampAccessResponseDto
                    {
                        Success = false,
                        Message = message
                    });
                }

                return Ok(new LampAccessResponseDto
                {
                    Success = true,
                    Message = message,
                    Request = MapToDto(request!)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitRequest endpoint");
                return StatusCode(500, new LampAccessResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing your request."
                });
            }
        }

        /// <summary>
        /// Manager approves or declines a lamp access request
        /// </summary>
        [HttpPost("respond")]
        [Authorize(Roles = "Manager,CEO,TechnicalManager")]
        // [Authorize]  // Accepts any authenticated user!

        public async Task<ActionResult<LampAccessResponseDto>> RespondToRequest([FromBody] LampAccessResponseRequestDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new LampAccessResponseDto
                    {
                        Success = false,
                        Message = "User not authenticated."
                    });
                }

                var userId = int.Parse(userIdClaim.Value);

                (bool success, string message) result;

                if (dto.Action == "Approve")
                {
                    result = await _lampAccessRequestService.ApproveRequestAsync(dto.RequestID, userId, dto.Notes);
                }
                else if (dto.Action == "Decline")
                {
                    result = await _lampAccessRequestService.DeclineRequestAsync(dto.RequestID, userId, dto.Notes);
                }
                else
                {
                    return BadRequest(new LampAccessResponseDto
                    {
                        Success = false,
                        Message = "Invalid action. Must be 'Approve' or 'Decline'."
                    });
                }

                if (!result.success)
                {
                    return BadRequest(new LampAccessResponseDto
                    {
                        Success = false,
                        Message = result.message
                    });
                }

                return Ok(new LampAccessResponseDto
                {
                    Success = true,
                    Message = result.message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RespondToRequest endpoint");
                return StatusCode(500, new LampAccessResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing your response."
                });
            }
        }

        /// <summary>
        /// Get pending lamp access requests
        /// - For managers: returns requests they can approve (in their branch)
        /// - For employees: returns their own pending requests
        /// </summary>
        [HttpGet("pending")]
        public async Task<ActionResult<LampAccessRequestListDto>> GetPendingRequests()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new LampAccessRequestListDto
                    {
                        Success = false,
                        Message = "User not authenticated."
                    });
                }

                var userId = int.Parse(userIdClaim.Value);

                var requests = await _lampAccessRequestService.GetPendingRequestsForUserAsync(userId);

                return Ok(new LampAccessRequestListDto
                {
                    Success = true,
                    Message = $"Found {requests.Count} pending request(s).",
                    Requests = requests.Select(r => MapToDto(r)).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPendingRequests endpoint");
                return StatusCode(500, new LampAccessRequestListDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving pending requests."
                });
            }
        }

        /// <summary>
        /// Get request history for the authenticated user
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<LampAccessRequestListDto>> GetHistory(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new LampAccessRequestListDto
                    {
                        Success = false,
                        Message = "User not authenticated."
                    });
                }

                var userId = int.Parse(userIdClaim.Value);

                var requests = await _lampAccessRequestService.GetRequestHistoryAsync(userId, from, to);

                return Ok(new LampAccessRequestListDto
                {
                    Success = true,
                    Message = $"Found {requests.Count} request(s) in history.",
                    Requests = requests.Select(r => MapToDto(r)).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHistory endpoint");
                return StatusCode(500, new LampAccessRequestListDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving request history."
                });
            }
        }

        /// <summary>
        /// Get details of a specific request by ID
        /// </summary>
        [HttpGet("{requestId}")]
        public async Task<ActionResult<LampAccessResponseDto>> GetRequestById(int requestId)
        {
            try
            {
                var request = await _lampAccessRequestService.GetRequestByIdAsync(requestId);

                if (request == null)
                {
                    return NotFound(new LampAccessResponseDto
                    {
                        Success = false,
                        Message = "Request not found."
                    });
                }

                return Ok(new LampAccessResponseDto
                {
                    Success = true,
                    Message = "Request found.",
                    Request = MapToDto(request)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRequestById endpoint");
                return StatusCode(500, new LampAccessResponseDto
                {
                    Success = false,
                    Message = "An error occurred while retrieving the request."
                });
            }
        }

        // Helper method to map LampAccessRequest entity to DTO
        private LampAccessRequestDetailsDto MapToDto(LampAccessRequest request)
        {
            return new LampAccessRequestDetailsDto
            {
                ID = request.ID,
                LampID = request.LampID,
                LampName = request.Lamp?.Name ?? "Unknown",
                LampDeviceID = request.Lamp?.DeviceID ?? "Unknown",
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                TimeoutAt = request.TimeoutAt,
                Reason = request.Reason,
                ApprovedUntil = request.ApprovedUntil,
                RespondedBy = request.RespondedByUser?.DisplayName,
                RespondedAt = request.RespondedAt,
                ResponseNotes = request.ResponseNotes,
                IsAutoClosed = request.IsAutoClosed,
                AutoClosedAt = request.AutoClosedAt
            };
        }
    }
}
