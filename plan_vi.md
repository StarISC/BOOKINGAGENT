# Kế hoạch triển khai BookingAgent (tiếng Việt)

## Giả định & Ràng buộc
- Nền tảng: Windows Server 2016, SQL Server 2019, .NET 9, Blazor Server.
- Tài liệu chuẩn: `document/RCL Cruise FIT Spec 5.2.pdf` (luồng pricing/booking, trang ~162-205) và bảng tra cứu trong `document/RCL Cruises Ltd - API TABLES/` (tàu, boong, cảng, vùng/phân vùng, hạng/cấu hình cabin, loại giường, danh xưng, gateway sân bay).
- Bảo mật: dùng env vars/user-secrets; không commit thông tin nhạy cảm. DB mặc định `BookingAgentDB`, tài khoản admin `admin`/`Admin@2025` lưu an toàn.
- Máy chủ SQL hiện tại: `Server=SHENLUNG\\SQLSERVER2022;Database=BookingAgentDB;User Id=sa;Password=<nạp-khi-chạy>;TrustServerCertificate=True;` (thiết lập qua biến môi trường `ConnectionStrings__BookingAgent`).
- Vai trò: khách hàng, agent, supervisor, admin. Bắt buộc phân quyền UI/hành động.
- Tương lai: tích hợp cổng thanh toán; thiết kế biên giao tiếp mở rộng ngay từ đầu.
- API (staging): base `https://stage.services.rccl.com/Reservation_FITWeb/sca/<API>`; CompanyShortName `NOVASTAR`; tài khoản `username=CONNCQN`, `password=qFDmKFM7eTaLk3s` nạp qua env/user-secrets (không commit rõ).

## Tính năng chính (phạm vi)
- Tra cứu chuyến: lọc theo vùng/phân vùng, cảng, tàu, khoảng ngày, số đêm, hạng cabin.
- Pricing: gọi `OTA_CruisePriceBookingRQ/RS` để nhận tổng giá booking, lịch thanh toán, chi tiết giá theo khách, khuyến mãi, chi tiết cabin.
- Booking: tạo/sửa booking, lưu ảnh chụp audit giá, truy vấn booking và reprice (amend) qua API.
- Dữ liệu tham chiếu: nạp và quản lý bảng lookup (tàu, boong, cảng, vùng, hạng cabin/cấu hình, loại giường, danh xưng, gateway).
- Bảo mật & audit: phân quyền, log hành động (không log secret), chỉ admin mới được bảo trì dữ liệu.
- Thanh toán (tương lai): tích hợp gateway thanh toán online, lưu giao dịch/intent, xử lý webhook idempotent.

## Vai trò & Quyền hạn
- Khách hàng: tra cứu chuyến, xem thông tin giá được phép, khởi tạo booking, xem booking/lịch thanh toán của mình (không được thao tác admin/lookup).
- Agent: tất cả quyền của khách + xem toàn bộ chi tiết giá, áp dụng khuyến mãi, tạo/sửa booking, xem audit giá.
- Supervisor: quyền agent + phê duyệt/override (nếu thêm), xem báo cáo vận hành.
- Admin: quản lý lookup/refresh, cấu hình hệ thống, quản trị người dùng/role, xem chẩn đoán/health.

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
- Viết script import từ CSV/XLS, thêm checksum/version để phát hiện thay đổi.
- Dịch vụ lookup có cache, endpoint refresh (admin-only).
- Kết quả: schema + seed script; service lookup; test nhanh kiểm tra dữ liệu.

### Pha 3 — Domain Model
- Hoàn thiện model C#: SailingInfo, SelectedCategory/Cabin, Promotions, BookingPayment (BookingPrices, PaymentSchedule), GuestPrices/PriceInfos, lỗi/cảnh báo.
- Mapping OTA: price type codes, non-refundable, auto-added, promo codes, POS/currency.
- Kết quả: model + hướng dẫn mapping (tham chiếu trang spec); unit test mapping từ fixture.

### Pha 4 — Tích hợp SOAP (Pricing trước)
- Client wrapper: endpoint cấu hình, timeout, retry, correlation ID, log không lộ secret.
- Request builder `OTA_CruisePriceBookingRQ` (price/reprice) dùng lookup; hỗ trợ promo/guest qualifiers.
- Parser `OTA_CruisePriceBookingRS` sang domain, xử lý cảnh báo/lỗi, auto-add charges.
- Health/diagnostic + mock provider cho offline dev.
- Kết quả: gọi pricing staging được; test builder/parser; mock.

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

## Tiến độ
- Đã làm: solution (Blazor Server + Domain), model giá, UI shell, pricing sample, schema lookup + script import, interface lookup domain, dịch vụ lookup SQL + cache + DI, cấu hình placeholder SHENLUNG\\SQLSERVER2022, trang search dùng lookup (vùng/cảng/tàu/hạng cabin) tại `/search`, skeleton SOAP pricing client (binding config, HttpClient, stub phản hồi mẫu).
- Đang làm: xây dựng request/response SOAP thật cho OTA_CruisePriceBookingRQ/RS với endpoint staging; nối trang search để gọi pricing service với bộ lọc đã chọn (đang có builder/parser stub).
- Tiếp theo: hoàn thiện UI search → pricing call; luồng booking & phân quyền; chuẩn bị abstraction thanh toán.






