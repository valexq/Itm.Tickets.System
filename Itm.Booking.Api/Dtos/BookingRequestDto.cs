namespace Itm.Booking.Api.Dtos;

public record BookingRequestDto(
    int EventId,
    int Tickets,
    string DiscountCode
);