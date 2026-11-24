using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

var apiUrl = new Uri(args.Length > 0 ? args[0] : "http://localhost:5007");

using var http = new HttpClient { BaseAddress = apiUrl };

await using var connection = new HubConnectionBuilder()
    .WithUrl(new Uri(apiUrl, "/hubs/bookings"))
    .WithAutomaticReconnect()
    .Build();

var confirmedNotification = new TaskCompletionSource(
    TaskCreationOptions.RunContinuationsAsynchronously);

using var subscription = connection.On<BookingStatusMessage>(
    "BookingStatusChanged", message =>
    {
        Console.WriteLine(
            $"Thông báo SignalR: {message.BookingId} → {message.Status}");

        if (message.Status == "Confirmed")
        {
            confirmedNotification.TrySetResult();
        }
    });

await connection.StartAsync();
Console.WriteLine("Đã kết nối SignalR.");

var events = await http.GetFromJsonAsync<List<EventDto>>("/events");
var selectedEvent = events?.FirstOrDefault();

if (selectedEvent is null)
{
    Console.WriteLine("Không có sự kiện để đặt vé.");
    return;
}

Console.WriteLine($"Chọn sự kiện: {selectedEvent.Name}");

var request = new CreateBookingRequest(selectedEvent.Id, "SV001", 2);
using var response = await http.PostAsJsonAsync("/bookings", request);

if (response.StatusCode != HttpStatusCode.Created)
{
    Console.Error.WriteLine(await response.Content.ReadAsStringAsync());
    return;
}

var booking = await response.Content.ReadFromJsonAsync<BookingDto>();
if (booking is null)
{
    Console.Error.WriteLine("Không đọc được booking vừa tạo.");
    return;
}

Console.WriteLine($"Đã tạo booking: {booking.Id}");
Console.WriteLine($"Trạng thái REST ban đầu: {booking.Status}");

await connection.InvokeAsync("WatchBooking", booking.Id);
Console.WriteLine("Đang chờ thông báo trạng thái từ API...");

var completed = await Task.WhenAny(
    confirmedNotification.Task,
    Task.Delay(TimeSpan.FromSeconds(10)));

if (completed != confirmedNotification.Task)
{
    Console.WriteLine("Hết thời gian chờ thông báo Confirmed.");
}

var latestBooking = await http.GetFromJsonAsync<BookingDto>(
    $"/bookings/{booking.Id}");

Console.WriteLine($"Trạng thái đọc lại bằng REST: {latestBooking?.Status}");

record EventDto(int Id, string Name, int RemainingTickets);
record CreateBookingRequest(int EventId, string UserId, int Quantity);
record BookingDto(
    Guid Id, int EventId, string UserId, int Quantity,
    string Status, DateTimeOffset CreatedAt);
record BookingStatusMessage(
    Guid BookingId, string Status, DateTimeOffset UpdatedAt);