namespace auth.Models
{
    public class FakePayViewModel
    {
        public int ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string FakeRef { get; set; } = "";
        public string CurrentStatus { get; set; } = "";
    }
}
