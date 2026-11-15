using System.Net;
using System.Net.Http.Json;

if (args.Length != 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out var apiUrl))
{
    Console.Error.WriteLine("Hãy truyền địa chỉ HTTP của Ticket API.");
    return;
}

using var http = new HttpClient { BaseAddress = apiUrl };

try
{
    var events = await http.GetFromJsonAsync<EventDto[]>("/events");
    foreach (var item in events ?? Array.Empty<EventDto>())
    {
        Console.WriteLine($"{item.Id}. {item.Name} — còn {item.RemainingTickets} vé");
    }

    var request = new CreateBookingRequest(1, "SV001", 2);
    using var response = await http.PostAsJsonAsync("/bookings", request);
    Console.WriteLine($"Tạo yêu cầu: HTTP {(int)response.StatusCode} {response.StatusCode}");

    if (response.StatusCode != HttpStatusCode.Created)
    {
        Console.Error.WriteLine($"HTTP {(int)response.StatusCode} {response.StatusCode}");
        Console.Error.WriteLine(await response.Content.ReadAsStringAsync());
        return;
    }

    var location = response.Headers.Location;
    if (location is null)
    {
        Console.Error.WriteLine("Phản hồi thiếu đường dẫn tra cứu yêu cầu.");
        return;
    }

    var booking = await http.GetFromJsonAsync<BookingDto>(location);
    if (booking is null)
    {
        Console.Error.WriteLine("Không đọc được nội dung yêu cầu đặt vé.");
        return;
    }

    Console.WriteLine($"Tra cứu {location}: mã đặt vé {booking.Id} — trạng thái {booking.Status}");

    using var invalidResponse = await http.PostAsJsonAsync(
        "/bookings", request with { Quantity = 0 });
    Console.WriteLine($"Đặt 0 vé: HTTP {(int)invalidResponse.StatusCode} {invalidResponse.StatusCode}");
    Console.WriteLine(await invalidResponse.Content.ReadAsStringAsync());
    if (invalidResponse.StatusCode != HttpStatusCode.BadRequest)
    {
        Console.Error.WriteLine("Kết quả không như mong đợi: API phải trả HTTP 400.");
        Environment.ExitCode = 1;
    }
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Lỗi khi gọi Ticket API. {ex.Message}");
}

record EventDto(int Id, string Name, int RemainingTickets);
record CreateBookingRequest(int EventId, string UserId, int Quantity);
record BookingDto(Guid Id, int EventId, string UserId, int Quantity,
    string Status, DateTimeOffset CreatedAt);
