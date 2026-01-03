using System;

namespace auth.ViewModels.SuperAdmin
{
    public class ParkingSummaryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string OwnerId { get; set; }
        public string OwnerEmail { get; set; }
    }
}