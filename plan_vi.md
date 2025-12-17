# Kế hoạch triển khai BookingAgent (tiếng Việt)

## Giả định & Ràng buộc
- Nền tảng: Windows Server 2016, SQL Server 2019, .NET 9, Blazor Server.
- Tài liệu chuẩn: `document/RCL Cruise FIT Spec 5.2.pdf` (luồng pricing/booking, trang ~162-205) và bộ bảng tra trong `document/RCL Cruises Ltd - API TABLES/` (tàu, boong, cảng, vùng/phân vùng, hạng/cấu hình cabin, loại giường, danh xưng, gateway).
- Bảo mật: dùng env vars/user-secrets; không commit thông tin nhạy cảm. DB mặc định `BookingAgentDB`, tài khoản admin `admin`/`Admin@2025` lưu an toàn.
- SQL hiện tại: `Server=SHENLUNG\\SQLSERVER2022;Database=BookingAgentDB;User Id=sa;Password=<nạp-khi-chạy>;TrustServerCertificate=True;` (qua env `ConnectionStrings__BookingAgent`).
- Vai trò: khách hàng, agent, supervisor, admin (bắt buộc RBAC cho UI/hành động).
- Tương lai: tích hợp cổng thanh toán; thiết kế biên giao tiếp mở rộng ngay từ đầu.
- API (staging): base `https://stage.services.rccl.com/Reservation_FITWeb/sca/<API>`; CompanyShortName `NOVASTAR`; tài khoản `username=CONNCQN`, `password=qFDmKFM7eTaLk3s` nạp qua env/user-secrets (không commit rõ).

## Tính năng chính (phạm vi)
- Tra cứu chuyến: lọc theo vùng/phân vùng, cảng, tàu, khoảng ngày, số đêm, hạng cabin.
- Pricing: gọi `OTA_CruisePriceBookingRQ/RS` để lấy tổng giá booking, lịch thanh toán, chi tiết giá theo khách, khuyến mãi, chi tiết cabin.
- Booking: tạo/sửa booking, lưu snapshot/audit giá, truy vấn booking và reprice (amend) qua API.
- Dữ liệu tham chiếu: nạp & quản lý lookup (tàu, boong, cảng, vùng, hạng cabin/cấu hình, loại giường, danh xưng, gateway).
- Bảo mật & audit: phân quyền, log hành động (không log secret), chỉ admin được bảo trì dữ liệu.
- Thanh toán (tương lai): tích hợp gateway online, lưu giao dịch/intent, xử lý webhook idempotent.

## Vai trò & Quyền hạn
- Khách hàng: tra cứu, xem giá được phép, khởi tạo booking, xem booking/lịch thanh toán của mình (không thao tác admin/lookup).
- Agent: quyền của khách + xem toàn bộ chi tiết giá, áp dụng khuyến mãi, tạo/sửa booking, xem audit giá.
- Supervisor: quyền agent + phê duyệt/override (nếu thêm), xem báo cáo vận hành.
- Admin: quản lý lookup/refresh, cấu hình hệ thống, quản trị người dùng/role, xem chẩn đoán/health.

## Cập nhật mới (sẵn sàng chạy)
- Đã kiểm tra DB `BookingAgentDB` trên `SHENLUNG\\SQLSERVER2022` có dữ liệu seed (CabinCategories 3447, Ports 1186, Ships 53, SubRegions 366, Regions 21).
- Build sạch: `dotnet build BookingAgent.sln` chạy thành công.
- Cấu hình runtime: đặt env `ConnectionStrings__BookingAgent=Server=SHENLUNG\\SQLSERVER2022;Database=BookingAgentDB;User Id=sa;Password=123456;TrustServerCertificate=True;`; nạp API creds qua env (`RoyalCaribbeanApi__Username=CONNCQN`, `RoyalCaribbeanApi__Password=qFDmKFM7eTaLk3s`, `RoyalCaribbeanApi__UseStub=true` mặc định để tránh gọi thật ngoài ý muốn).
- Ghi chú API: SailingList chạy staging khi `UseStub=false`; Login/BookingPrice vẫn trả CSE0572 (chưa được phép), nên pricing hiện dùng stub. Không commit appsettings trong `bin/` có secret.
- Thử chạy 2025-12-17: khởi động `http://localhost:5234` thất bại vì cổng đã được tiến trình `BookingAgent.App.exe` (PID 41700) sử dụng. Có thể dùng luôn tiến trình đang chạy hoặc khởi động cổng khác, ví dụ `dotnet run --project src/BookingAgent.App/BookingAgent.App.csproj --urls http://localhost:5290`.

## Tiến độ
- Đã làm: dựng solution (Blazor Server + Domain), model giá, UI shell, pricing mẫu, schema lookup + script import, interface lookup domain, dịch vụ lookup SQL + cache + DI, cấu hình placeholder SHENLUNG\\SQLSERVER2022, trang search dùng lookup (vùng/cảng/tàu/hạng cabin), skeleton SOAP pricing client (binding config, HttpClient, stub phản hồi mẫu), SailingList service + UI bảng kết quả, toggle UseStub.
- Đang làm: hoàn thiện builder/parser SOAP `OTA_CruisePriceBookingRQ/RS`, gắn đầy đủ POS/Sailing/Category/Guest/Promotion; nối UI search -> pricing service (giá hiện từ stub).
- Tiếp theo: thử staging pricing khi vendor cho phép (hiện CSE0572), hoàn thiện luồng booking & phân quyền, chuẩn bị abstraction thanh toán.

## Work log (gần đây)
- Tạo schema SQL + script import; seed lookup từ tài nguyên CSV/XLS.
- Kiểm tra staging API: Login trả cảnh báo CSE0572 (không được ủy quyền); LookupAgency trả thông tin agency (ID 378372/275611); SailingList trả danh sách chuyến (region FAR.E) và đã gắn vào UI; BookingPrice trả SOAP Fault env:Client "Internal Error".
- Thêm cấu hình OperationPath/SoapAction và `UseStub` để kiểm soát gọi thật; giữ credential qua env.
- Kiểm tra build: `dotnet build BookingAgent.sln` xanh.

## Phân công 2 developer
- Dev A (backend): schema, nhập lookup, dịch vụ lookup + cache, SOAP client (pricing/booking), repository, bảo mật/roles, abstraction thanh toán.
- Dev B (frontend): Blazor UI (search, pricing, booking), phân quyền hiển thị, trạng thái lỗi/loading, diagnostic; hỗ trợ test/mocks.
- Cùng làm: mapping SOAP ↔ domain, script triển khai, kiểm thử tích hợp.

## Các pha chi tiết
### Pha 1 — Khám phá & Kiến trúc
- Xác nhận hành trình: search → pricing (`OTA_CruisePriceBookingRQ/RS`) → booking commit/modify → retrieve/amend → (tương lai) payment.
- Liệt kê trường bắt buộc trong SOAP: POS, currency, sailing/ship code, fare code, cabin, promotions, guest qualifiers (theo spec ~162-205).
- Kết quả: sơ đồ kiến trúc, danh sách thành phần, checklist trường API.

### Pha 2 — Dữ liệu & Lookup
- Thiết kế bảng: Ships, Decks, Ports, Regions, SubRegions, CabinCategories, CabinConfig, BedType, Titles, Gateways (có audit).
- Script import từ CSV/XLS, thêm checksum/version để phát hiện thay đổi.
- Dịch vụ lookup có cache, endpoint refresh (admin-only).
- Kết quả: schema + seed; service lookup; test nhanh kiểm tra dữ liệu.

### Pha 3 — Domain Model
- Hoàn thiện model: SailingInfo, SelectedCategory/Cabin, Promotions, BookingPayment (BookingPrices, PaymentSchedule), GuestPrices/PriceInfos, lỗi/cảnh báo.
- Mapping OTA: price type codes, non-refundable, auto-added, promo codes, POS/currency.
- Kết quả: model + hướng dẫn mapping (tham chiếu trang spec); unit test từ fixture.

### Pha 4 — Tích hợp SOAP (Pricing trước)
- Client wrapper: endpoint cấu hình, timeout, retry, correlation ID, log không lộ secret.
- Request builder `OTA_CruisePriceBookingRQ` (price/reprice) dùng lookup; hỗ trợ promo/guest qualifiers.
- Parser `OTA_CruisePriceBookingRS` sang domain, xử lý cảnh báo/lỗi, auto-add charges.
- Health/diagnostic + mock provider cho offline dev.
- Kết quả: gọi pricing staging khi được phép; test builder/parser; mock.

### Pha 5 — UI Tra cứu & Pricing
- Form search: lọc region/port/ship/date/duration/category, validate input, loading/error state.
- Trang pricing: hiển thị booking totals, lịch thanh toán, guest breakdown, promos, cabin; hiển thị cảnh báo từ RS.
- Phân quyền hiển thị: agent/supervisor xem đầy đủ, customer xem giới hạn.
- Kết quả: UI gắn với pricing service và lookup.

### Pha 6 — Luồng Booking
- Tạo booking: form khách, cabin, promo; gọi API booking; hiển thị xác nhận/lịch thanh toán.
- Lưu snapshot booking + audit giá (booking & guest line items) có version/timestamp.
- Retrieve/amend: gọi repricing với `TransactionActionCode=RetrievePrice`, hiển thị chênh lệch.
- Kết quả: luồng booking end-to-end (chưa thanh toán), lưu DB, amend/retrieve.

### Pha 7 — Bảo mật & Phân quyền
- Auth (Identity/SSO placeholder) + policy: customer/agent/supervisor/admin.
- Bảo vệ API và endpoint admin/refresh; log hành động kèm correlation ID (không lộ secret).
- Kết quả: endpoint/UI có policy; audit log.

### Pha 8 — Sẵn sàng Thanh toán
- Thiết kế abstraction payment (intent/authorize/capture), idempotency, webhook.
- Mở rộng DB: bảng payment_attempts (ref nhà cung cấp, trạng thái, số tiền, audit).
- UI placeholder sau confirm booking để khởi tạo thanh toán; bật/tắt bằng feature flag.
- Kết quả: interface/schema sẵn sàng, UI có nhánh thanh toán khi có gateway.

### Pha 9 — Kiểm thử & Chất lượng
- Unit test: builder, parser, lookup, tính toán giá (nếu có).
- Integration: SOAP client với staging/fixture; repo test trên SQL 2019.
- Hiệu năng: đo độ trễ pricing, hiệu quả cache; phân trang kết quả search.
- Kết quả: bộ test + hướng dẫn chạy; note hiệu năng.

### Pha 10 — Triển khai & Vận hành
- Hướng dẫn deploy Windows Server 2016 (IIS/Kestrel), cấu hình qua env/user-secrets, HTTPS.
- Script migration và backup/restore; job seed lookup.
- Giám sát/cảnh báo: lỗi API, lỗi booking, health check; kế hoạch lưu log.
- Kết quả: checklist triển khai, runbook vận hành.

## Tiêu chí chấp nhận
- Pricing: trả tổng booking và chi tiết khách theo spec, render được trên UI.
- Lookups: dữ liệu tham chiếu hiển thị trên bộ lọc; admin refresh hoạt động.
- Booking: lưu booking với audit giá; retrieve/amend repricing được.
- Bảo mật: policy role enforced; không log/commit secret.
- Thanh toán: abstraction/schema sẵn sàng; UI có đường dẫn sang bước thanh toán khi tích hợp.
