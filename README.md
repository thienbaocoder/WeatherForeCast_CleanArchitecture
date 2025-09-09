# WeatherWeb – Ứng dụng dự báo thời tiết (Blazor Server, .NET 8)

Ứng dụng **Blazor Server** dùng **Bootstrap 5** hiển thị thời tiết **hiện tại**, **theo giờ** và **7 ngày**. Dữ liệu lấy từ **Open-Meteo** (miễn phí, không cần API key). Dự án tổ chức theo **Clean Architecture** (Domain / Application / Infrastructure / UI) và hỗ trợ **GPS** của trình duyệt qua **JS interop**.

---

## ✨ Tính năng

- Tìm kiếm thời tiết theo **tên thành phố** (geocoding).
- **Thời tiết hiện tại**: nhiệt độ, gió, trạng thái mây, thời điểm quan trắc.
- **Dự báo theo giờ** (một vài giờ sắp tới).
- **Dự báo 7 ngày** (nhiệt độ cao/thấp, biểu tượng thời tiết).
- **Thông tin bổ sung**: cảm giác như, mây che phủ, áp suất, tầm nhìn.
- **Dùng GPS** để lấy thời tiết tại vị trí hiện tại (reverse geocoding tên địa điểm).
- Giao diện **Bootstrap 5**, icon **Font Awesome**.

---

## 🧱 Kiến trúc & cấu trúc thư mục

WeatherWeb.sln
├─ WeatherWeb/ # UI (Blazor Server)
│ ├─ Pages/
│ │ ├─ Weather.razor # Trang chính
│ │ └─ _Host.cshtml # (chứa script geolocation nếu dùng window.weather)
│ ├─ wwwroot/ # CSS/JS tĩnh
│ └─ Program.cs / Startup.cs # Khởi tạo DI
│
├─ WeatherWeb.Domain/ # Domain (thuần C#)
│ ├─ Entities/ (Location, WeatherSnapshot, …)
│ └─ ValueObjects/ (Coordinates, Temperature, WindSpeed, …)
│
├─ WeatherWeb.Application/ # Application
│ ├─ Abstractions/ (IWeatherService)
│ └─ Features/Weather_DTOs/WeatherViewModel.cs (Current + Hourly + Daily)
│
└─ WeatherWeb.Infrastructure/ # Infrastructure
├─ Weather/
│ ├─ OpenMeteoModels.cs # Model JSON (current + hourly + daily)
│ └─ OpenMeteoService.cs # Implement IWeatherService (HttpClientFactory)
└─ Configuration/DependencyInjection.cs


**Luồng dữ liệu:** UI → `IWeatherService` → `OpenMeteoService` → Open-Meteo API → map về `WeatherViewModel` → bind vào UI.

---

## 🛠 Công nghệ

- **.NET 8**, **Blazor Server**
- **Bootstrap 5**, **Font Awesome**
- **HttpClientFactory**, **Dependency Injection**
- **Open-Meteo APIs** (không cần key)

---

## ▶️ Cài đặt & chạy

### Yêu cầu
- .NET SDK **8.x**
- Trình duyệt hỗ trợ Geolocation (Chrome/Edge/Firefox)

### Chạy dev
```bash
# (lần đầu) tạo/trust dev cert để dùng https://localhost:5001
dotnet dev-certs https --trust

# build & chạy
dotnet build
dotnet watch run 
Mở: https://localhost:5001/weather

Khi bấm Dùng GPS, trình duyệt sẽ hỏi quyền vị trí → chọn Allow.

