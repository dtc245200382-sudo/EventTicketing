using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

sealed class BookingHub(ConcurrentDictionary<Guid, BookingDto> bookings) : Hub
{
    public Task WatchBooking(Guid bookingId)
    {
        if (!bookings.ContainsKey(bookingId))
        {
            throw new HubException("Không tìm thấy yêu cầu đặt vé.");
        }

        return Groups.AddToGroupAsync(
            Context.ConnectionId,
            GroupName(bookingId),
            Context.ConnectionAborted);
    }

    internal static string GroupName(Guid bookingId) => $"booking:{bookingId:D}";
}