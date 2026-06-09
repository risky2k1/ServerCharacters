# Windows Setup Checklist

Mục tiêu của checklist này:

- dựng lại môi trường Windows để build được baseline hiện tại
- xác nhận baseline chạy được trước khi sửa code
- chuẩn bị đủ điều kiện để bắt đầu implement workaround snapshot

Khi bạn về máy Windows, chỉ cần prompt:

`Tiến hành checklist`

và mình sẽ đi theo đúng thứ tự trong file này.

## Phase 1: Xác nhận môi trường

### 1. Kiểm tra toolchain

Cần có tối thiểu:

- Valheim đã cài
- dedicated server hoặc local dedicated server có thể chạy
- BepInEx đúng version cho Valheim hiện tại
- .NET Framework targeting pack phù hợp với project
- Visual Studio Build Tools hoặc Visual Studio
- NuGet restore chạy được

### 2. Kiểm tra path thực tế

Cần xác nhận:

- đường dẫn cài Valheim
- đường dẫn dedicated server nếu tách riêng
- thư mục `BepInEx\core`
- workspace repo hiện tại

### 3. Kiểm tra project references

Xác nhận các dependency project đang trỏ đúng:

- `BepInEx.dll`
- `0Harmony.dll`
- các package trong `packages.config`
- Jotunn / package restore nếu project cần

## Phase 2: Build baseline

### 4. Restore packages

Mục tiêu:

- không còn thiếu package
- project load/build được sạch ở baseline hiện tại

### 5. Build baseline hiện tại

Mục tiêu:

- build được DLL của baseline chưa sửa
- chưa implement workaround mới

Nếu fail:

- đọc lỗi build
- sửa path/reference/environment trước, chưa đụng logic mod

## Phase 3: Smoke test baseline

### 6. Test startup

Xác nhận:

- server khởi động được
- plugin được load
- không có lỗi load assembly cơ bản

### 7. Test join flow

Xác nhận:

- client có mod vào được
- server gửi authoritative profile
- không văng lỗi decode ngay lúc join

### 8. Test save flow

Xác nhận:

- vào server
- thay đổi inventory / progress nhẹ
- save xong không lỗi
- file `.fch` phía server cập nhật

### 9. Test reconnect flow

Xác nhận:

- thoát ra vào lại
- profile vẫn load đúng
- inventory không mất bất thường

## Phase 4: Chuẩn bị cho snapshot workaround

### 10. Kiểm tra hook points hiện có

Các điểm sẽ rà lại trước khi code:

- server save thành công ở đâu
- login path gửi authoritative profile ở đâu
- chỗ nào đã có log/debug sẵn
- chỗ nào có thể chèn snapshot update
- chỗ nào có thể chèn snapshot restore

### 11. Chốt design trước khi sửa

Nhắc lại rule:

- follow [CENTRAL_RULE.md](/home/tuanpm1/Dev/mods/ServerCharacters/CENTRAL_RULE.md:1)
- follow [.claude/skills/servercharacters-debug-tracing/SKILL.md](/home/tuanpm1/Dev/mods/ServerCharacters/.claude/skills/servercharacters-debug-tracing/SKILL.md:1)
- không làm flow profile phức tạp hơn mức cần thiết

## Phase 5: Bắt đầu implementation

### 12. Chỉ sửa sau khi baseline ổn

Khi bắt đầu code, thứ tự ưu tiên là:

1. thêm snapshot folder/helper
2. thêm snapshot update sau server save thành công
3. thêm snapshot restore trước login send path
4. thêm guard rules chống offline snapshot pollution
5. thêm debug logs đầy đủ cho từng nhánh quyết định

## Quick Success Criteria

Checklist được coi là hoàn thành nếu:

- build baseline thành công
- server load mod thành công
- join/save/reconnect của baseline chạy được
- xác định được chính xác hook points để bắt đầu sửa

## Quick Failure Rule

Nếu baseline chưa build hoặc chưa join/save/reconnect ổn:

- không bắt đầu implement snapshot workaround
- chỉ sửa môi trường, dependency, hoặc debug baseline trước
