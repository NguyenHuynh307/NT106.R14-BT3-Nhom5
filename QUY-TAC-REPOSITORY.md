# Quy tắc làm việc với Repository

Tài liệu này quy định cách các thành viên làm việc chung trên GitHub cho **NT106.R14-BT3-Nhom5**.

## 1. Nguyên tắc chung

- `main` là nhánh tích hợp ổn định. Không commit hoặc push trực tiếp lên `main`.
- Mỗi thành viên làm việc trên branch riêng, chỉ đưa thay đổi vào `main` thông qua Pull Request (PR).
- Trước khi sửa code, phải đồng bộ `main` mới nhất.
- Không ghi đè công việc của thành viên khác, không force-push lên `main`.
- Mỗi thành viên cần có ít nhất một commit được ghi nhận trong lịch sử bài tập.

## 2. Quy ước đặt tên branch

Dùng chữ thường, không dấu, từ cách nhau bằng dấu gạch ngang:

```text
feature/<github-username>/<short-description>
fix/<github-username>/<short-description>
docs/<github-username>/<short-description>
```

Ví dụ:

```text
feature/nguyenhuynh307/tcp-client
feature/phu529/user-repository
fix/liamz07/login-validation
docs/hoanganh/protocol-specification
```

Không dùng branch tên chung chung như `test`, `new`, `update` hoặc `final`.

## 3. Bắt đầu một công việc

Mở CMD/PowerShell tại thư mục repository:

```cmd
git switch main
git pull --ff-only origin main
git switch -c feature/<github-username>/<short-description>
```

Thay phần trong dấu `<...>` bằng username và nội dung công việc thực tế.

## 4. Commit

Commit nhỏ, tập trung vào một mục đích và dùng tiền tố thống nhất:

| Tiền tố | Sử dụng |
|---|---|
| `feat:` | Thêm chức năng |
| `fix:` | Sửa lỗi |
| `refactor:` | Tái cấu trúc, không đổi hành vi mong muốn |
| `docs:` | Tài liệu |
| `test:` | Kiểm thử |
| `chore:` | Cấu hình, công việc bảo trì |

Ví dụ:

```cmd
git add .
git commit -m "feat: add async TCP client connection"
git push -u origin HEAD
```

Không đưa mật khẩu, token, connection string thật, file database cá nhân, log chứa dữ liệu nhạy cảm hoặc thư mục build lên Git.

## 5. Pull Request

- Tạo PR từ branch công việc vào `main`.
- Tiêu đề PR mô tả đúng phần thay đổi, có thể dùng cùng tiền tố commit.
- Mô tả: mục đích, file/chức năng đã sửa, cách chạy và kết quả kiểm thử.
- Nêu rõ phần chưa hoàn thành hoặc giới hạn đã biết; không ghi “đã xong” nếu chưa kiểm tra.
- Nhóm trưởng hoặc thành viên được phân công review trước khi merge.
- Không tự merge PR của mình khi chưa có review theo thống nhất nhóm.

## 6. Đồng bộ và xử lý xung đột

Trước khi mở PR, đồng bộ `main` mới nhất vào branch công việc. Nếu có conflict:

1. Dừng thao tác merge/push nếu chưa hiểu thay đổi xung đột.
2. Trao đổi với người sở hữu phần code liên quan.
3. Giải quyết trên branch công việc, build/test lại.
4. Không dùng `git push --force` để ghi đè lịch sử của người khác.

## 7. Quy tắc riêng cho bài tập C#/.NET

- Dùng C#/.NET cho Client, Server và Shared; thống nhất Target Framework trước khi tạo project.
- Đặt tên class, method, property và file có ý nghĩa; không để tên mặc định như `Form1`, `button1`, `textBox1` khi đã biết chức năng.
- Không đưa SQL access hoặc connection string vào Client. Chỉ Server được kết nối SQL Server.
- Mọi truy vấn SQL dùng tham số.
- Giữ các thao tác mạng bất đồng bộ bằng `async`/`await`; không chặn UI bằng thao tác mạng đồng bộ.
- Không dùng `List<T>` không đồng bộ giữa các tác vụ để quản lý Client; cần collection thread-safe hoặc khóa phù hợp.
- Khi sửa giao thức, cập nhật `docs/PROTOCOL.md` và bảo đảm Client, Server, Shared thống nhất.

## 8. Trước khi gửi PR

```cmd
git status -sb
git diff --check
```

Tự build/test phần mình thay đổi; kiểm tra không có file thừa, secret, `bin/`, `obj/` hoặc dữ liệu cá nhân. Ghi kết quả thật vào PR.
