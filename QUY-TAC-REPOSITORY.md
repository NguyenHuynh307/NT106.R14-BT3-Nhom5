# Quy tắc làm việc với Repository

## 1. Nguyên tắc

- Không push trực tiếp lên `main`; mọi thay đổi vào `main` qua Pull Request (PR).
- Mỗi người làm trên nhánh riêng, không sửa phần việc của người khác khi chưa trao đổi.
- Không `git push --force`.
- Mỗi thành viên có ít nhất một commit.

## 2. Nhánh

Tên chữ thường, không dấu, nối bằng dấu gạch ngang:

```text
feature/<ten-chuc-nang>    docs/<ten-tai-lieu>    fix/<ten-loi>
```

Bắt đầu một việc:

```cmd
git switch main
git pull --ff-only origin main
git switch -c feature/<ten-chuc-nang>
```

## 3. Commit

Commit nhỏ, bắt đầu bằng tiền tố `feat:`, `fix:`, `refactor:`, `docs:`, `test:` hoặc `chore:`.

```cmd
git commit -m "feat: thêm kết nối TCP bất đồng bộ cho Client"
git push -u origin HEAD
```

Không đưa lên Git: mật khẩu, token, chuỗi kết nối thật, file database, `bin/`, `obj/`.

## 4. Pull Request

- Tiêu đề ngắn, dùng cùng tiền tố commit.
- Mô tả: làm gì, cách chạy, kết quả kiểm thử, phần chưa xong.
- Phải có người khác review trước khi merge; không tự merge PR của mình.
- Có conflict mà chưa hiểu thì dừng lại, hỏi người sở hữu phần code đó.

## 5. Quy tắc code C#/.NET

- Đặt tên có nghĩa, không để `Form1`, `button1`, `textBox1`.
- Chỉ Server nối SQL Server; mọi câu lệnh SQL dùng tham số.
- Thao tác mạng dùng `async`/`await`.
- Danh sách client dùng `ConcurrentDictionary`, không dùng `List<T>` thường.
- Đổi giao thức thì cập nhật `docs/PROTOCOL.md`.

## 6. Trước khi gửi PR

```cmd
git diff --check
dotnet build
```
