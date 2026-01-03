using System.Collections.Generic;

namespace auth.ViewModels.SuperAdmin
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalManagers { get; set; }
        public int TotalParkings { get; set; }
        public int TotalReservations { get; set; }

        public List<UserSummaryViewModel> Users { get; set; } = new List<UserSummaryViewModel>();
    }
}