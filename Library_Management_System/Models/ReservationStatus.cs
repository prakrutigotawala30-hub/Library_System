namespace LibraryManagementSystem.Models
{
    public enum ReservationStatus
    {
        Waiting,     // Book not available yet
        Available,   // Book is ready
        Cancelled    // User cancelled reservation
    }
}