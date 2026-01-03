using System;

namespace auth.ViewModels
{
    public class PaymentReceiptViewModel
    {
        public int ReservationId { get; set; }
        public string UserEmail { get; set; }
        public string? ParkingName { get; set; }
        public string? SpotCode { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int DurationHours { get; set; }
        public decimal TotalPrice { get; set; }
        public string? PaymentRef { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
