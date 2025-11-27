using CoreProject.Context;
using CoreProject.Services.IService;
using FaceRecognition.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CoreProject.Services
{
    public class FaceEnrollmentService : IFaceEnrollmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFaceVerificationService _faceService;
        private readonly ILogger<FaceEnrollmentService> _logger;

        public FaceEnrollmentService(
            ApplicationDbContext context,
            IFaceVerificationService faceService,
            ILogger<FaceEnrollmentService> logger)
        {
            _context = context;
            _faceService = faceService;
            _logger = logger;
        }

        public async Task<EnrollmentResult> EnrollUserFaceAsync(int userId, byte[] photoBytes)
        {
            try
            {
                _logger.LogInformation("Starting face enrollment for user {UserId}", userId);

                // Validate input
                if (photoBytes == null || photoBytes.Length == 0)
                {
                    return EnrollmentResult.Fail("No photo data provided");
                }

                // Validate file size (max 5MB)
                if (photoBytes.Length > 5 * 1024 * 1024)
                {
                    return EnrollmentResult.Fail("Photo size exceeds 5MB limit");
                }

                // Extract embedding from photo using FaceRecognition.Core
                var extractResult = await _faceService.ExtractEmbeddingAsync(photoBytes);

                if (!extractResult.Success)
                {
                    _logger.LogWarning("Face extraction failed for user {UserId}: {Error}",
                        userId, extractResult.ErrorMessage);
                    return EnrollmentResult.Fail(extractResult.ErrorMessage ?? "Failed to extract face embedding");
                }

                // Validate exactly one face is detected
                if (extractResult.FaceCount != 1)
                {
                    var message = extractResult.FaceCount == 0
                        ? "No face detected in the photo. Please upload a clear frontal face photo."
                        : $"{extractResult.FaceCount} faces detected. Please upload a photo with exactly one face.";

                    _logger.LogWarning("Invalid face count for user {UserId}: {FaceCount}",
                        userId, extractResult.FaceCount);
                    return EnrollmentResult.Fail(message);
                }

                // Validate embedding
                if (extractResult.Embedding == null || extractResult.Embedding.Length == 0)
                {
                    _logger.LogError("Embedding is null or empty for user {UserId}", userId);
                    return EnrollmentResult.Fail("Failed to generate face embedding");
                }

                // Convert float[] to byte[] for storage (512 floats = 2048 bytes)
                byte[] embeddingBytes = new byte[extractResult.Embedding.Length * sizeof(float)];
                Buffer.BlockCopy(extractResult.Embedding, 0, embeddingBytes, 0, embeddingBytes.Length);

                _logger.LogInformation("Successfully extracted face embedding for user {UserId}. Embedding size: {Size} bytes",
                    userId, embeddingBytes.Length);

                // Find user and update face data
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return EnrollmentResult.Fail("User not found");
                }

                // Store embedding and enrollment timestamp
                user.FaceEmbedding = embeddingBytes;
                user.FaceEnrolledAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully enrolled face for user {UserId} ({UserEmail})",
                    userId, user.Email);

                return EnrollmentResult.SuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling face for user {UserId}", userId);
                return EnrollmentResult.Fail($"An error occurred while processing the face photo: {ex.Message}");
            }
        }

        public async Task<bool> DeleteUserFaceAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Deleting face enrollment for user {UserId}", userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                // Clear face enrollment data
                user.FaceEmbedding = null;
                user.FaceEnrolledAt = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted face enrollment for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting face enrollment for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> HasFaceEnrollmentAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.FaceEmbedding })
                    .FirstOrDefaultAsync();

                return user != null && user.FaceEmbedding != null && user.FaceEmbedding.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking face enrollment for user {UserId}", userId);
                return false;
            }
        }
    }
}
