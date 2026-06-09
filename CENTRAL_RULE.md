# Central Rule

## One Rule To Connect Everything

`Server authoritative state must always beat local state, and any workaround must reduce profile-handling complexity rather than increase it.`

Đây là rule trung tâm để đọc và đánh giá toàn bộ mod này.

Nó có 2 ý bắt buộc:

1. `server state thắng local state`
2. `mọi thay đổi mới phải làm luồng profile đơn giản hơn, không thông minh hơn`

Nếu một ý tưởng vi phạm một trong hai ý này, thì ý tưởng đó không nên đi tiếp.

## Why This Rule Exists

Toàn bộ vấn đề của mod xoay quanh 2 mâu thuẫn:

- nếu local state thắng, người chơi có thể mang đồ / stat từ offline vào server
- nếu logic profile quá phức tạp, mod rất dễ đọc nhầm, ghi nhầm, restore nhầm, hoặc làm mất đồ

Vì vậy rule trung tâm phải khóa cả hai hướng rủi ro đó cùng lúc.

## Design Consequences

Từ rule này, kéo ra các quyết định nền:

- chỉ có một authoritative source trong runtime
- local profile không được phép override server-approved profile
- snapshot chỉ được dùng nếu nó giúp server authority mạnh hơn mà không làm flow phức tạp quá mức
- không thêm logic “thông minh” kiểu chọn profile động nếu không thật sự bắt buộc
- tránh can thiệp sâu vào byte layout của profile nếu có thể tránh

## Operational Rule Set

### Rule 1

Khi player login, trạng thái được dùng để chơi phải là trạng thái server chấp nhận, không phải trạng thái local player mang vào.

### Rule 2

Offline progress không được phép làm bẩn authoritative state.

### Rule 3

Backup/snapshot là công cụ bảo toàn hoặc ép authority, không phải nơi sinh thêm nhiều nhánh đồng bộ mới.

### Rule 4

Nếu có hai hướng:

- hướng A ít tính năng hơn nhưng dễ đoán hơn
- hướng B thông minh hơn nhưng thêm resolve/restore/reconcile

thì ưu tiên hướng A.

### Rule 5

Mọi workaround mới phải trả lời được 2 câu:

1. nó có giúp server state luôn thắng local state không?
2. nó có làm profile flow đơn giản hơn hoặc ít nhất không phức tạp hơn không?

Nếu không trả lời “có” cho cả hai, không dùng.

## How The Existing Docs Connect To This Rule

### [MOD_OVERVIEW.md](/home/tuanpm1/Dev/mods/ServerCharacters/MOD_OVERVIEW.md:1)

Trả lời:

- mod này là gì
- mục tiêu thật sự của baseline là gì

Vai trò:

- cung cấp bức tranh tổng quát để hiểu rule trung tâm đang áp vào hệ thống nào

### [ARCHITECTURE_NOTES.md](/home/tuanpm1/Dev/mods/ServerCharacters/ARCHITECTURE_NOTES.md:1)

Trả lời:

- mod chạy theo flow nào
- join/save/recovery đi qua đâu

Vai trò:

- cho thấy những flow nào cần bị ràng buộc bởi rule trung tâm

### [MODULE_BREAKDOWN.md](/home/tuanpm1/Dev/mods/ServerCharacters/MODULE_BREAKDOWN.md:1)

Trả lời:

- mod có những subsystem nào
- subsystem nào là authority, backup, admin, template, web API

Vai trò:

- giúp phân biệt cái gì là lõi cần bảo vệ, cái gì là phụ có thể cắt bớt

### [CORE_PATHS.md](/home/tuanpm1/Dev/mods/ServerCharacters/CORE_PATHS.md:1)

Trả lời:

- 3 xương sống của mod là gì
- `join`
- `save`
- `disconnect/recovery`

Vai trò:

- đây là nơi áp rule trung tâm mạnh nhất
- nếu một thay đổi làm hỏng 1 trong 3 core path này, thì thay đổi đó sai hướng

### [WORKAROUND_OPTIONS.md](/home/tuanpm1/Dev/mods/ServerCharacters/WORKAROUND_OPTIONS.md:1)

Trả lời:

- có thể workaround theo những kiểu nào
- kiểu nào phù hợp thực tế nhất

Vai trò:

- chuyển rule trung tâm thành lựa chọn thực dụng

### [IMPLEMENTATION_SKETCH.md](/home/tuanpm1/Dev/mods/ServerCharacters/IMPLEMENTATION_SKETCH.md:1)

Trả lời:

- nếu chọn workaround đã chốt, sẽ cài nó vào đâu và bằng policy nào

Vai trò:

- đây là bước biến rule trung tâm thành kế hoạch kỹ thuật

## Recommended Reading Order

Nếu muốn hiểu toàn bộ repo mà không bị rời rạc:

1. đọc file này trước
2. đọc [MOD_OVERVIEW.md](/home/tuanpm1/Dev/mods/ServerCharacters/MOD_OVERVIEW.md:1)
3. đọc [MODULE_BREAKDOWN.md](/home/tuanpm1/Dev/mods/ServerCharacters/MODULE_BREAKDOWN.md:1)
4. đọc [CORE_PATHS.md](/home/tuanpm1/Dev/mods/ServerCharacters/CORE_PATHS.md:1)
5. đọc [WORKAROUND_OPTIONS.md](/home/tuanpm1/Dev/mods/ServerCharacters/WORKAROUND_OPTIONS.md:1)
6. đọc [IMPLEMENTATION_SKETCH.md](/home/tuanpm1/Dev/mods/ServerCharacters/IMPLEMENTATION_SKETCH.md:1)

## Practical Decision Filter

Từ giờ, trước khi thêm hoặc sửa logic nào liên quan character profile, dùng filter này:

### Pass

- server-approved state luôn thắng local state
- không làm tăng số nhánh resolve/restore/reconcile
- dễ giải thích cho host server
- failure mode rõ ràng

### Fail

- local/offline progress có cửa chen vào authoritative path
- thêm nhiều lớp “smart profile resolution”
- cần quá nhiều điều kiện ẩn để đoán file nào là đúng
- khó debug khi mất đồ hoặc lệch state

## Short Version

Nếu cần nhớ đúng một câu:

`Giữ authority ở phía server, và giữ luồng profile càng ngu càng tốt.`
