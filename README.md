# 🦗 JirJirak

**A lightweight .NET worker that keeps your HTTP endpoints alive and running on schedule.**

جیرجیرک یک برنامه سبک و مستقل مبتنی بر .NET است که درخواست‌های HTTP را به‌صورت زمان‌بندی‌شده به Endpointهای مشخص ارسال می‌کند.

## ✨ قابلیت‌ها

- **اجرای زمان‌بندی‌شده:** تعیین فاصله اجرای مستقل برای هر Endpoint.
- **چندین Endpoint:** مدیریت هم‌زمان چند مقصد مختلف.
- **بارگذاری مجدد تنظیمات:** خواندن مجدد `appsettings.json` در حین اجرا، بدون نیاز به Restart.
- **فعال‌سازی و غیرفعال‌سازی:** کنترل اجرای هر Endpoint از طریق تنظیمات.
- **احراز هویت اختیاری:** پشتیبانی از Token اختصاصی برای هر درخواست.
- **مدیریت خطا:** ثبت خطاهای ارتباطی و تلاش مجدد در نوبت بعدی اجرا.
- **پشتیبانی از HTTP و HTTPS:** امکان ارسال درخواست به هر دو پروتکل.

## ⚙️ Configuration

تنظیمات برنامه از طریق فایل `appsettings.json` انجام می‌شود.

نمونه:

```json
{
  "targets": [
    {
      "url": "https://example.com/api/worker",
      "duration": 10,
      "enabled": true,
      "token": "your-secret-token"
    },
    {
      "url": "https://example.com/api/another-task",
      "duration": 30,
      "enabled": true,
      "token": null
    }
  ]
}
```

### Configuration properties

| Property | Description |
|---|---|
| `url` | آدرس Endpoint موردنظر |
| `duration` | فاصله اجرای درخواست بر حسب دقیقه |
| `enabled` | فعال یا غیرفعال بودن Endpoint |
| `token` | Token اختیاری برای احراز هویت |

اگر `duration` مشخص نشده باشد، مقدار پیش‌فرض ۱۰ دقیقه در نظر گرفته می‌شود.

## 🚀 Getting Started

1. مخزن را Clone کنید.
2. فایل `appsettings.json` را در کنار فایل اجرایی قرار دهید.
3. Endpointهای موردنظر را در تنظیمات تعریف کنید.
4. برنامه را اجرا کنید.

```bash
dotnet run
```

## 🔐 Security

- Tokenها را در مخزن عمومی GitHub قرار ندهید.
- برای Endpointهای غیرمحلی که از Token استفاده می‌کنند، HTTPS الزامی است.
- از Tokenهای امن و اختصاصی استفاده کنید.

## 🛠️ Technologies

- C#
- .NET
- HttpClient
- System.Text.Json
- Async / Await

---

**JirJirak 🦗**

*Small cricket. Big responsibilities.*

