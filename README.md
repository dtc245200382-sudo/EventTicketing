# Đề tài 03: Đặt vé sự kiện

Bài thực hành Chương 1–2 xây dựng API đặt vé bằng ASP.NET Core Minimal API và một ứng dụng Console gọi API. Phạm vi hiện tại là minh họa HTTP, JSON, kiểm tra dữ liệu và tra cứu yêu cầu đặt vé.

- `src/Ticket.Api`: cung cấp các endpoint HTTP và xử lý dữ liệu đặt vé.
- `src/Ticket.Client`: dùng `HttpClient` gửi yêu cầu và nhận dữ liệu JSON từ API.

## Yêu cầu và chạy chương trình

Cần cài **.NET 10 SDK**. Clone repository của bài, sau đó mở PowerShell tại thư mục gốc `EventTicketing` và build:

```powershell
dotnet build EventTicketing.slnx
```

Trong cửa sổ PowerShell thứ nhất, chạy API:

```powershell
dotnet run --project src/Ticket.Api --launch-profile http
```

API dùng địa chỉ `http://localhost:5007` theo `launchSettings.json`. Giữ cửa sổ này chạy, mở cửa sổ PowerShell thứ hai tại thư mục gốc và chạy client:

```powershell
dotnet run --project src/Ticket.Client -- http://localhost:5007
```

Client nhận địa chỉ API qua đối số dòng lệnh. Client hiển thị danh sách sự kiện, tạo yêu cầu đặt hai vé cho `SV001`, nhận `201 Created`, rồi dùng header `Location` để đọc lại booking theo mã. Cuối cùng, client gửi lại cùng sự kiện và người dùng với số lượng bằng 0; API trả `400 Bad Request` cùng nội dung JSON có mã `INVALID_BOOKING`.

## Các endpoint

| Phương thức và đường dẫn | Chức năng | Mã phản hồi chính |
| --- | --- | --- |
| `GET /` | Kiểm tra API đang chạy | `200 OK` |
| `GET /events` | Xem danh sách sự kiện | `200 OK` |
| `GET /events/{id}` | Xem chi tiết sự kiện theo mã số nguyên | `200 OK`, `404 Not Found` |
| `POST /bookings` | Tạo yêu cầu đặt vé từ JSON gồm `eventId`, `userId`, `quantity` | `201 Created` kèm `Location`, `400 Bad Request`, `404 Not Found` nếu không có sự kiện |
| `GET /bookings/{id}` | Tra cứu yêu cầu theo mã GUID | `200 OK`, `404 Not Found` |
| `GET /users/{userId}/bookings` | Xem các yêu cầu của người dùng | `200 OK` |

## Lưu trữ và phạm vi

Danh sách sự kiện mẫu và các yêu cầu đặt vé được lưu trong bộ nhớ của tiến trình API. **Các booking sẽ mất khi API khởi động lại.** Đây là ví dụ minh họa: số vé còn lại là dữ liệu mẫu và chưa được cập nhật khi tạo booking. Dự án hiện không cấu hình cơ sở dữ liệu, RabbitMQ, Worker hay Docker Compose.
