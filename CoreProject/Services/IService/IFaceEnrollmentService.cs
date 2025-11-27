using System.Threading.Tasks;

namespace CoreProject.Services.IService
{
    public interface IFaceEnrollmentService
    {
        /// <summary>
        /// Enrolls a user's face by extracting embedding from photo and storing it in database
        /// </summary>
        /// <param name="userId">User ID to enroll face for</param>
        /// <param name="photoBytes">Photo bytes (JPG, PNG)</param>
        /// <returns>Enrollment result with success status and error message if failed</returns>
        Task<EnrollmentResult> EnrollUserFaceAsync(int userId, byte[] photoBytes);

        /// <summary>
        /// Deletes a user's face enrollment data
        /// </summary>
        /// <param name="userId">User ID to delete face for</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteUserFaceAsync(int userId);

        /// <summary>
        /// Checks if a user has face enrollment data
        /// </summary>
        /// <param name="userId">User ID to check</param>
        /// <returns>True if user has face enrolled, false otherwise</returns>
        Task<bool> HasFaceEnrollmentAsync(int userId);
    }

    /// <summary>
    /// Result of face enrollment operation
    /// </summary>
    public class EnrollmentResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static EnrollmentResult SuccessResult() => new() { Success = true };
        public static EnrollmentResult Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
