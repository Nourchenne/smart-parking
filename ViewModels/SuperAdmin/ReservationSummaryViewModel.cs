using System;

namespace auth.ViewModels.SuperAdmin
{
    public class ReservationSummaryViewModel
    {
        public int Id { get; set; }
        public string ParkingName { get; set; }
        public string SpotCode { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int DurationHours { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }
}