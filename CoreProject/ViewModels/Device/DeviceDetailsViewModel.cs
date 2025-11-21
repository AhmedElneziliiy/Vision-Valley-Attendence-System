namespace CoreProject.ViewModels
{
    public class DeviceDetailsViewModel
    {
        public int Id { get; set; }
        public string? DeviceID { get; set; }
        public char? DeviceType { get; set; }
        public int? CoverageArea { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public bool? IsSignedIn { get; set; }
        public bool? IsSignedOut { get; set; }
        public bool IsPassThrough { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? AccessControlURL { get; set; }
        public int? AccessControlState { get; set; }

        // Computed Properties
        public string DeviceTypeDisplay => DeviceType.HasValue
            ? DeviceType.Value == 'I' ? "In Device (Entry)"
            : DeviceType.Value == 'O' ? "Out Device (Exit)"
            : DeviceType.Value == 'B' ? "Both (Entry/Exit)"
            : "Unknown"
            : "Not Set";

        public string SignInStatusBadge => IsSignedIn == true ? "Enabled" : "Disabled";
        public string SignInStatusClass => IsSignedIn == true ? "success" : "secondary";

        public string SignOutStatusBadge => IsSignedOut == true ? "Enabled" : "Disabled";
        public string SignOutStatusClass => IsSignedOut == true ? "success" : "secondary";

        public string PassThroughBadge => IsPassThrough ? "Enabled" : "Disabled";
        public string PassThroughClass => IsPassThrough ? "info" : "secondary";

        public string AccessControlBadge => AccessControlState switch
        {
            1 => "Active",
            0 => "Inactive",
            _ => "Unknown"
        };

        public string AccessControlClass => AccessControlState switch
        {
            1 => "success",
            0 => "danger",
            _ => "secondary"
        };

        public string IsActiveBadge => IsActive ? "Active" : "Inactive";
        public string IsActiveClass => IsActive ? "success" : "danger";
    }
}
