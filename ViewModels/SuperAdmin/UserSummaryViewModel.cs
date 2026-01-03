using System;

namespace auth.ViewModels.SuperAdmin
{
    public class UserSummaryViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string[] Roles { get; set; } = new string[0];
        public bool IsLocked { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
    }
}