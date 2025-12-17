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
- Thử chạy 2025-12-17: cổng 5234 bị chiếm; đã chạy trên `http://localhost:5290` với `UseStub=false` và env, rồi dừng tiến trình thử nghiệm (PID 8352). Có thể khởi động lại với env trên và `dotnet run --urls http://localhost:5290`.
- Branding: logo Startravel (sao xanh + chữ gradient “Startravel – live your dream”), màu chính #005edc, phụ #212654, hotline 0919 122 127, văn phòng 321 Nam Kỳ Khởi Nghĩa, Phường Xuân Hòa, HCM; layout đã áp dụng màu, header chứa slogan/contact, chừa slot để gắn logo ảnh.

## Danh sách phương thức API (RCL Cruise FIT Spec 5.2)
Base staging: `https://stage.services.rccl.com/Reservation_FITWeb/sca/{MethodName}` (Basic Auth; payload OTA theo namespace từng method). Các interface chính:
- Login: xác thực agency/terminal.
- LookupAgency: lấy thông tin/kiểm tra agency.
- SailingList: danh sách chuyến theo vùng/ngày/số đêm.
- PackageList / PackageDetail: danh sách gói và chi tiết gói.
- TransferList / TransferDetail: danh sách/chi tiết transfer.
- BusList / BusDetail: danh sách/chi tiết bus.
- TourList / TourDetail: danh sách/chi tiết tour bờ.
- CategoryList: danh sách hạng cabin.
- CabinList / CabinDetail: danh sách/chi tiết cabin.
- HoldCabin / ReleaseCabin: giữ hoặc nhả giữ cabin.
- FareList / FareDetail: danh sách/chi tiết fare.
- BookingPrice: tính giá booking (`OTA_CruisePriceBookingRQ/RS`).
- LinkedBooking (validateLinkedBookings): kiểm tra booking liên kết.
- PaymentExtension: lấy tùy chọn gia hạn thanh toán.
- ConfirmBooking: xác nhận booking sau pricing/hold.
- OptionList / OptionDetail: danh sách/chi tiết dịch vụ tùy chọn.
- GuestServiceList: danh sách dịch vụ cho khách.
- FastSell: bán nhanh với dữ liệu định sẵn.
- Payment (makePayment): thanh toán cho booking.
- ItineraryDetail: chi tiết hành trình.
- BookingDocument: lấy chứng từ booking.
- RetrieveBooking / BookingList / BookingHistory / ReleaseBooking: truy vấn booking, lịch sử, hoặc nhả booking.
- AutoAddChargeDetail: lấy thông tin phụ phí tự động.
- DiningList: danh sách lựa chọn dining.

### Chi tiết payload/trường chính (RQ/RS)
- POS chung: `POS/Source@ISOCurrency`, `@TerminalID`, `RequestorID@Type=5/@ID`, `BookingChannel@Type=7/CompanyName@CompanyShortName`.
- SailingList (`getSailingList` → OTA_CruiseSailAvailRQ): MaxResponses/MoreIndicator, SequenceNmbr, TimeStamp, TransactionIdentifier, Version; `GuestCounts/GuestCount@Quantity`; `SailingDateRange@Start/@MinDuration/@MaxDuration`; `CruiseLinePref@VendorCode`; `RegionPref@RegionCode/@SubRegionCode`; tùy chọn ship (ListOfSailingDescriptionCode) và port. RS: `SailingOption/SelectedSailing` (Start, Duration, ShipCode, Region/SubRegion, Departure/ArrivalPort) + `InclusivePackageOption@CruisePackageCode`; kiểm tra MoreIndicator để phân trang.
- BookingPrice (`getBookingPrice` → OTA_CruisePriceBookingRQ/RS): POS; `SailingInfo/SelectedSailing` (Start, Duration, StartLocationCode, EndLocationCode, ShipCode/ListOfSailingDescriptionCode, InclusivePackageOption@CruisePackageCode); `SelectedCategory/FareCode`; `SelectedCabin@CabinNumber/@DeckNumber/@BerthCount/@Status`; `GuestDetails/GuestDetail` (GuestRefNumber, Age, BirthDate, Citizenship, Residency, Loyalty IDs nếu có); Currency; Promotions/PromotionCode; `ReservationInfo` (ReservationID@ID/@StatusCode, TransactionActionCode: Start/RetrievePrice/Commit); PaymentInfo (tùy chọn). RS: BookingPayment→BookingPrices (PriceTypeCode, Amount, Currency), PaymentSchedule/Payment (PaymentNumber, Amount, DueDate), GuestPrices→GuestPrice→PriceInfo, SelectedSailing/SelectedCategory/SelectedCabin, Promotions, Warnings/Errors.
- HoldCabin (`holdCabin` → OTA_CruiseCabinHoldRQ): POS; SelectedSailing; SelectedCategory; SelectedCabin (CabinNumber, DeckNumber, MaxOccupancy/BerthCount); GuestCounts; OptionExpiry. RS: Hold/Option ID, hạn giữ, trạng thái cabin.
- ReleaseCabin (`releaseCabin` → OTA_CruiseCabinUnholdRQ): POS + Hold/ReservationID; RS xác nhận nhả.
- CategoryList (`getCategoryList`): POS + ngữ cảnh sailing; RS: CategoryCode, Description, Min/MaxOccupancy, Deck info.
- CabinList (`getCabinList`): POS + sailing/category; RS: CabinNumber, DeckNumber, Status (available/held/booked), MaxOccupancy/BerthConfig, vị trí/tầm nhìn.
- FareList/FareDetail: POS + sailing/category; qualifiers (residency/promo). RS: FareCode, Description, Combinability, Currency, Effective/Expiry.
- ConfirmBooking (`confirmBooking`): POS + bối cảnh đã price (Sailing/Category/Cabin/Fare); GuestDetails (tên, ngày sinh, quốc tịch, liên hệ), PaymentInfo, Promotions, SpecialServices. RS: ReservationID, PaymentSchedule, BookingPrices, GuestPrices.
- RetrieveBooking/BookingList/BookingHistory: POS + ReservationID hoặc search qualifiers. RS: thông tin booking, khách, snapshot giá, lịch sử (timestamp + action).
- PaymentExtension: POS + ReservationID; yêu cầu loại/giá trị gia hạn. RS: tùy chọn/duyệt gia hạn.
- Payment (makePayment): POS; ReservationID; PaymentAmount, Currency, PaymentRef/Method. RS: trạng thái + tham chiếu giao dịch.
- OptionList/OptionDetail, GuestServiceList: POS; sailing/booking. RS: mã dịch vụ, mô tả, giá, điều kiện.
- Package/Transfer/Bus/Tour List/Detail: POS; port/ship/ngày; MaxResponses/MoreIndicator để phân trang. RS: mã, mô tả, giá, availability, thời lượng/lộ trình.
- ItineraryDetail: POS; sailing/package. RS: lịch trình theo ngày (PortCode, Arrival/Departure).
- AutoAddChargeDetail: POS; booking/sailing. RS: danh sách phụ phí auto-add (code/amount).
- DiningList: POS; ship/date/meal. RS: lựa chọn dining, timeslot, availability.

### Đào sâu (trường OTA quan trọng)
- GuestDetails: GuestRefNumber, GuestName (Prefix/Given/Surname), BirthDate/Age, Gender, Nationality/Citizenship, ResidentCountryCode, Document (Type/Number/Expiration), LoyaltyInfo (ProgramID/Level/MembershipID), ContactInfo (Email/Phone), ProfileRef (nếu có).
- Promotions: PromotionCode, GroupCode, AgencyGroupCode; hỗ trợ nhiều promotion; tuân thủ combinability từ RS.
- Giá: BookingPrice/GuestPrice PriceTypeCode (FARE/TAX/NCF/GRAT...), Amount, Currency, AutoAddIndicator; PaymentSchedule (PaymentNumber/Amount/DueDate/Type).
- Cabin: CabinNumber, DeckNumber, MaxOccupancy, Status, CategoryLocation, BedConfiguration, ConnectingCabinIndicator, ObstructedViewIndicator.
- Định danh sailing: giữ cả ListOfSailingDescriptionCode và ShipCode để dùng cho hold/price/confirm.
- Phân trang: dùng MaxResponses/MoreIndicator (hoặc StartIndex/NextToken nếu có) cho mọi danh sách; lặp đến khi hết MoreIndicator.
- Lỗi/Cảnh báo: Warnings/Errors có code/text; hiển thị thân thiện, log mã (ẩn PII).
- Tiền tệ/thời gian: dùng CurrencyCode từ RS; ngày/giờ ISO, thời gian cảng có thể theo local.
- Payment: PaymentAmount/Currency/PaymentRef/Authorization/PaymentType; lưu transaction ref cho audit.
- ItineraryDetail: Region, PortCall (PortCode, Arrival/Departure, DayNumber, Duration).

### Tham chiếu trường theo mô hình (RS)
- SailingOption (RS SailingList): ShipCode, VendorCode, RegionCode, SubRegionCode, DeparturePort@LocationCode, ArrivalPort@LocationCode, Start (date), Duration (ISO period), ListOfSailingDescriptionCode, CruisePackageCode (InclusivePackageOption).
- SailingInfo (BookingPrice RS): SelectedSailing (Start, Duration, Start/EndLocationCode, ShipCode, ListOfSailingDescriptionCode, Region/SubRegion), SelectedCategory (FareCode, CategoryLocation, Status), SelectedCabin (CabinNumber, DeckNumber, MaxOccupancy, Status, BedConfiguration, Connecting/Obstructed), InclusivePackageOption@CruisePackageCode, mô tả Sailing/Itinerary (nếu có).
- Pricing totals (BookingPayment RS): BookingPrices[] { PriceTypeCode, Amount, CurrencyCode, AutoAddIndicator }, PaymentSchedule/Payments[] { PaymentNumber, Amount, CurrencyCode, DueDate, Type (Deposit/Final) }, GuestPrices[] { GuestRefNumber, PriceInfos[] { PriceTypeCode, Amount, CurrencyCode, AutoAddIndicator } }.
- Promotions (RS): PromotionCode, Description/Text, Status, Combinability, AutoAddIndicator; có thể ở mức booking hoặc guest.
- Booking identifiers (RS): ReservationID (ID, StatusCode), Option/Hold IDs (hold/release), Transaction identifiers.
- Cabin/Hold (HoldCabin RS): OptionID, ExpirationDateTime, CabinNumber, DeckNumber, Status, BerthCount/MaxOccupancy.
- Fare (FareList/Detail RS): FareCode, Description, CurrencyCode, Effective/ExpiryDate, CombinabilityRules, Qualifiers (Residency, Loyalty, Promo), Min/MaxGuests, ApplicableCategories.
- Category (CategoryList RS): CategoryCode, Description, Min/MaxOccupancy, DeckRange, LocationAttributes.
- Cabin list (CabinList RS): CabinNumber, DeckNumber, Status (Available/Held/Booked), MaxOccupancy, BerthConfig, Location (Forward/Aft/Midship, Port/Starboard), ObstructedViewIndicator, ConnectingIndicator.
- Add-ons (Package/Transfer/Bus/Tour List/Detail RS): Code, Description, Price (Amount/Currency), AvailabilityStatus, Duration/Segments, Port/Ship/Date scope, Capacity limits.
- Options/GuestServices (OptionList/Detail, GuestServiceList RS): ServiceCode, Description, Price, Eligibility (guest/booking), Timing/Slot nếu có.
- DiningList RS: DiningOptionCode, MealType, Timeslot, Availability, Location.
- Payment RS (makePayment): StatusCode, PaymentRef/AuthorizationCode, Amount, Currency, PaymentDate, Error/Warning nếu thất bại.
- Booking retrieval (RetrieveBooking/BookingList/History): ReservationID, SailingInfo, Pricing snapshot (BookingPayment), Promotions, GuestDetails, Cabin/Category/Fare, PaymentSchedule, History entries (ActionCode, TimeStamp, Agent/Channel).

### Tham chiếu mã PriceTypeCode & Promotion (từ PDF)
- PriceTypeCode bắt gặp: 1, 3, 6, 7, 8, 18, 34, 42, 46, 49, 58, 60, 73, 80, 81, 90, 98, 100, 101, 102, 103, 104, 107, 127, 156, 161, 162, 163, 164, 166. Cần map theo bảng spec (FARE/NCF/TAX/GRAT/FEES/INSURANCE...) khi định nghĩa enum.
- PromotionClass bắt gặp: 1, 12, 14, 18. PromotionType bắt gặp: 1, 24, 44, 50, 58, 68. Lưu dưới dạng code theo spec; hiển thị `PromotionDescription` nếu có.

### TODO: Bảng mã & ý nghĩa (trích từ spec)
| Loại mã | Code | Ý nghĩa (theo spec) | Ghi chú / Trang |
|---------|------|----------------------|-----------------|
| PriceTypeCode | 1,3,6,7,8,18,34,42,46,49,58,60,73,80,81,90,98,100,101,102,103,104,107,127,156,161,162,163,164,166 | TBD từ bảng spec (FARE/NCF/TAX/PORT/GRAT/FEES/INSURANCE/ONBOARD CHARGE, v.v.) | Bảng PriceTypeCode trong phần pricing (~trang 170-180); cần xác nhận |
| PromotionClass | 1,12,14,18 | TBD từ spec | Lấy từ phần Promotions Enhancement |
| PromotionType | 1,24,44,50,58,68 | TBD từ spec | Lấy từ phần promotions |
| PricedComponentType | (giá trị kèm PriceTypeCode, ví dụ “I”) | TBD từ spec | Thu thập khi parse pricing component |
| PricingLevel | ví dụ 24 (sample) | TBD | Xác định ý nghĩa (booking/guest/cabin) |

Hành động: trích mô tả chính xác từ `RCL Cruise FIT Spec 5.2.pdf` (vùng trang ~170-180) và khai báo enum kèm mô tả/tooltips trên UI.

### Kế hoạch trích thủ công (mã và ý nghĩa)
- Xác định bảng pricing trong PDF (khoảng trang 170–180): PriceTypeCode, PricedComponentType, PricingLevel.
- Xác định phần promotions: bảng PromotionClass/PromotionType và mô tả.
- Chép code → ý nghĩa nguyên văn; lưu bảng trong `plan.md`/`plan_vi.md` và tạo enum trong code kèm mô tả.
- So khớp với payload mẫu để kiểm tra; bổ sung tooltip UI với nghĩa mã.
- Bản docx: có `document/RCL Cruise FIT Spec 5.2.docx`; đã cài `python-docx`, đã xuất bảng liên quan vào `document/docx_tables_extract.txt` (59 bảng chứa thuật ngữ). Cần đọc thủ công để lấy bảng code→meaning (extract hiện chủ yếu là mẫu payload, có thể bảng gốc ở dạng ảnh/đối tượng nhúng).

### Thứ tự gọi method (happy path theo spec)
1) Login → (tùy chọn) LookupAgency để xác thực agency.
2) Tra cứu/lọc: PackageList/FareList/CategoryList/CabinList (nếu cần) và SailingList để lấy chuyến (dùng MaxResponses/MoreIndicator để phân trang).
3) Chọn sailing/category/cabin:
   - SailingList → chọn SailingOption (Ship/Start/PackageCode).
   - CategoryList → chọn hạng; CabinList → chọn cabin (nếu cần).
4) Pricing: BookingPrice (OTA_CruisePriceBookingRQ) với POS + sailing/category/cabin + guest + promotion.
5) Hold (tùy chọn): HoldCabin để giữ cabin (nhận Option/Hold ID, hạn giữ).
6) ConfirmBooking: ConfirmBooking với bối cảnh đã price, thông tin khách, thanh toán, promotion; nhận ReservationID, PaymentSchedule, giá cuối.
7) Payment: makePayment hoặc PaymentExtension (nếu cho phép) với ReservationID.
8) Sau đó: RetrieveBooking/BookingHistory/BookingDocument để hiển thị; OptionList/GuestServiceList/Package/Transfer/Bus/Tour để upsell; ReleaseCabin hoặc ReleaseBooking nếu hủy.

Ghi chú:
- Có thể repricing trước khi ConfirmBooking với TransactionActionCode=RetrievePrice.
- MoreIndicator yêu cầu lặp cho các endpoint dạng list (SailingList, Package/Transfer/Bus/Tour, FareList, BookingList).
- Có thể loại bỏ promotion không hoàn tiền qua cờ trong fare/promo (theo spec).

## Tình trạng API & phác thảo DTO (staging)
- Login (`login`): đã test → HTTP 200 nhưng cảnh báo CSE0572 “ACCESS NOT AUTHORIZED FOR THIS AGENCY”; chưa được cấp quyền. DTO: `LoginResponse { WarningCode, WarningText }`.
- LookupAgency (`lookupAgency`): đã test → 200 OK, trả thông tin agency (ID 378372/275611, địa chỉ/điện thoại). DTO: `AgencyInfo { AgencyIds[], Name, Address, Phone }`.
- SailingList (`getSailingList`): đã test → 200 OK, trả danh sách chuyến (region FAR.E, tàu OV/SC, packages). DTO: `SailingOption { ShipCode, PackageCode, StartDate, Duration, DeparturePort, ArrivalPort, Region, SubRegion }`.
- BookingPrice (`getBookingPrice`): đã test → 500 env:Client “Internal Error”; bị chặn tới khi vendor mở quyền. DTO: `BookingPriceResponse { BookingPayment (BookingPrices, PaymentSchedule, GuestPrices), SailingInfo, Promotions, Warnings/Errors }`.
- HoldCabin/ReleaseCabin: chưa test; DTO kỳ vọng: `HoldCabinResponse { OptionId, Expiration, Cabin }`, `ReleaseCabinResponse { Success, Message }`.
- CategoryList/CabinList: chưa test; DTO: `Category { Code, Description, Min/MaxOccupancy }`, `Cabin { CabinNumber, DeckNumber, Status, MaxOccupancy, LocationFlags }`.
- FareList/FareDetail: chưa test; DTO: `Fare { FareCode, Description, Currency, Qualifiers, Combinability }`.
- ConfirmBooking: chưa test; DTO: `ConfirmBookingResponse { ReservationId, PaymentSchedule, BookingPrices, GuestPrices }`.
- Payment/PaymentExtension: chưa test; DTO: `PaymentResponse { Status, PaymentRef, Amount }`, `PaymentExtensionResponse { Options[], Approved }`.
- OptionList/GuestServiceList/Package/Transfer/Bus/Tour: chưa test; DTO: `AddOn { Code, Description, Price, Availability, Scope }`.
- ItineraryDetail: chưa test; DTO: `ItineraryDetail { PortCalls[], Region }`.
- BookingDocument/RetrieveBooking/BookingHistory/BookingList/ReleaseBooking: chưa test; DTO: `BookingSnapshot { ReservationId, SailingInfo, Pricing, Promotions, Guests, History[] }`.
## Chức năng & menu theo vai trò (nháp)
- Khách hàng:
  - Menu:
    - Overview/Dashboard: chào mừng, trạng thái booking hiện tại, liên kết nhanh.
    - Tra cứu chuyến: bộ lọc (vùng/cảng/tàu/ngày/số đêm/hạng cabin), danh sách kết quả, thông tin cơ bản.
    - Pricing (giới hạn): xem bảng giá được phép cho sailing/cabin đã chọn.
    - Booking của tôi: danh sách + chi tiết (hành trình, lịch thanh toán), bắt đầu thanh toán (khi mở).
    - Hỗ trợ/Liên hệ: hotline, địa chỉ văn phòng.
  - Hành động: tra cứu, xem giá được phép, khởi tạo booking, xem booking của mình, bắt đầu thanh toán (nếu mở).
- Agent:
  - Menu:
    - Menu của Khách (được xem đầy đủ giá).
    - Tạo/Sửa booking: nhập khách, chọn cabin/promo, gửi pricing/booking.
    - Giữ/Nhả cabin: tạo hold, giải phóng hold.
    - Booking List/Retrieve: tìm theo ReservationID/ngày/agency.
    - Thanh toán/Gia hạn: kích hoạt thanh toán, yêu cầu gia hạn.
    - Báo cáo cơ bản: danh sách booking/xuất file.
  - Hành động: xem chi tiết giá, áp dụng promotion, giữ/nhả cabin, tạo/sửa booking, yêu cầu gia hạn thanh toán, xử lý thanh toán, xem audit.
- Supervisor:
  - Menu:
    - Menu của Agent.
    - Phê duyệt/Override: hàng chờ yêu cầu (discount/override).
    - Báo cáo vận hành: hiệu năng/số lượng booking.
    - Lịch sử booking: audit chi tiết.
    - Health/Diagnostics (chỉ xem): trạng thái dịch vụ/API.
  - Hành động: phê duyệt override/discount, giám sát booking, xem lịch sử/audit, xử lý sự cố.
- Admin:
  - Menu:
    - Admin Console: refresh/import lookup, cờ cấu hình (UseStub, endpoint, feature toggle), quản trị user/role.
    - Diagnostics/Health: kết nối API/DB, job nền.
    - Logs (ẩn secret): log vận hành.
    - Dữ liệu tham chiếu: xem bảng lookup, kích hoạt reseed.
  - Hành động: quản lý user/role, bật/tắt UseStub/config, refresh/import lookup, xem health, quản lý feature flag (payment, live API), chạy backup/bảo trì.

### Kho auth/role (chưa có trong DB)
- Schema hiện tại chỉ có lookup; chưa có bảng user/role/permission.
- Đề xuất (tương lai): `Users` (Id, Username, PasswordHash, Email, Phone, IsActive, CreatedAt/UpdatedAt), `Roles` (Id, Name, Description), `UserRoles` (UserId, RoleId). Tùy chọn: `Permissions`/`RolePermissions` nếu cần chi tiết hơn.
- Cân nhắc Identity/SSO về sau; tạm thời local auth dùng salted hash, không lưu plaintext mật khẩu.
- Đã thêm script: `db/auth_schema.sql` tạo `Users`, `Roles`, `UserRoles` (có thêm trường hồ sơ, liên hệ, lockout, timestamps) và seed roles (Customer, Agent, Supervisor, Admin); bảng permissions để mở rộng được comment sẵn.
- Admin seed: script tạo user `admin` với hash/salt rỗng (0x); phải thiết lập hash/salt thật qua bước triển khai an toàn trước khi dùng production.
- Đã cập nhật mật khẩu admin (tạm staging): user `admin` đã được set PBKDF2-SHA256 với mật khẩu `123456` trên BookingAgentDB (có thể đổi lại khi deploy prod).
- Đã tích hợp auth trong app: cookie auth, trang login, AuthService PBKDF2, nav login/logout, menu theo role (placeholder), `UseAuthentication/UseAuthorization` với login path `/login`.

## Tiến độ
- Đã làm: dựng solution (Blazor Server + Domain), model giá, UI shell, pricing mẫu, schema lookup + script import, interface lookup domain, dịch vụ lookup SQL + cache + DI, cấu hình placeholder SHENLUNG\\SQLSERVER2022, trang search dùng lookup (vùng/cảng/tàu/hạng cabin), skeleton SOAP pricing client (binding config, HttpClient, stub phản hồi mẫu), SailingList service + UI bảng kết quả, toggle UseStub.
- Đang làm: hoàn thiện builder/parser SOAP `OTA_CruisePriceBookingRQ/RS`, gắn đủ POS/Sailing/Category/Guest/Promotion; nối UI search -> pricing service (giá hiện từ stub).
- Tiếp theo: thử staging pricing khi vendor cho phép (hiện CSE0572), hoàn thiện luồng booking & phân quyền, chuẩn bị abstraction thanh toán.

## Work log (gần đây)
- Tạo schema SQL + script import; seed lookup từ CSV/XLS.
- Kiểm tra staging API: Login trả cảnh báo CSE0572; LookupAgency trả thông tin agency (ID 378372/275611); SailingList trả danh sách chuyến (region FAR.E) và đã gắn vào UI; BookingPrice trả SOAP Fault env:Client "Internal Error".
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
- Pricing: trả tổng booking và chi tiết khách theo spec, hiển thị trên UI.
- Lookups: dữ liệu tham chiếu hiển thị trên bộ lọc; admin refresh hoạt động.
- Booking: lưu booking với audit giá; retrieve/amend repricing được.
- Bảo mật: policy role enforced; không log/commit secret.
- Thanh toán: abstraction/schema sẵn sàng; UI có đường dẫn sang bước thanh toán khi tích hợp.
