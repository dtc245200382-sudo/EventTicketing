using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var events = new Dictionary<int, EventDto>
{
    [1] = new(1, "Workshop ASP.NET Core", 3),
    [2] = new(2, "Ngày hội công nghệ", 10)
};

var bookings = new ConcurrentDictionary<Guid, BookingDto>();

app.MapGet("/", () => Results.Ok(new
{
    service = "Ticket.Api",
    status = "running"
}));

app.MapGet("/events", () => Results.Ok(events.Values.OrderBy(item => item.Id)));

app.MapGet("/events/{id:int}", (int id) =>
    events.TryGetValue(id, out var ticketEvent)
        ? Results.Ok(ticketEvent)
        : Results.NotFound(new ApiError("EVENT_NOT_FOUND", "Không tìm thấy sự kiện.")));

app.MapPost("/bookings", (CreateBookingRequest request) =>
{
    var userId = request.UserId?.Trim();
    if (request.EventId <= 0 || string.IsNullOrWhiteSpace(userId) || request.Quantity <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_BOOKING", "Cần mã sự kiện, mã người dùng và số lượng vé lớn hơn 0."));
    }

    if (!events.ContainsKey(request.EventId))
    {
        return Results.NotFound(new ApiError("EVENT_NOT_FOUND", "Không tìm thấy sự kiện."));
    }

    var booking = new BookingDto(
        Guid.NewGuid(), request.EventId, userId, request.Quantity,
        "Pending", DateTimeOffset.UtcNow);

    bookings[booking.Id] = booking;
    return Results.Created($"/bookings/{booking.Id}", booking);
});

app.MapGet("/bookings/{id:guid}", (Guid id) =>
    bookings.TryGetValue(id, out var booking)
        ? Results.Ok(booking)
        : Results.NotFound(new ApiError("BOOKING_NOT_FOUND", "Không tìm thấy yêu cầu đặt vé.")));

app.MapGet("/users/{userId}/bookings", (string userId) =>
    Results.Ok(bookings.Values
        .Where(booking => booking.UserId == userId)
        .OrderByDescending(booking => booking.CreatedAt)));

app.Run();

record EventDto(int Id, string Name, int RemainingTickets);
record CreateBookingRequest(int EventId, string? UserId, int Quantity);
record BookingDto(Guid Id, int EventId, string UserId, int Quantity,
    string Status, DateTimeOffset CreatedAt);
record ApiError(string Code, string Message);