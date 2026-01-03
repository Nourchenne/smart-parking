using System;

namespace auth.ViewModels.Manager
{
    public class ManagerReservationViewModel
    {
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string? SpotCode { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int DurationHours { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }
}