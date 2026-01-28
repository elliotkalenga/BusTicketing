
namespace BusTicketing.Models.Common
{
    public enum BusStatus { InService, Maintenance, Inactive }
    public enum TripStatus { Scheduled, Boarding, Departed, Arrived, Canceled }
    public enum BookingStatus { Pending, Confirmed, Canceled, Completed, NoShow }
    public enum PaymentStatus { Initiated, Authorized, Captured, Settled, Refunded, Failed }
    public enum TicketStatus { Issued, CheckedIn, Boarded, Void }
    public enum FareClass { Standard, Premium, VIP }
    public enum FuelType
    {
        Diesel = 1,
        Petrol = 2,
        Electric = 3
    }

}
