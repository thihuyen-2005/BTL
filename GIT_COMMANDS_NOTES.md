# Ghi chú nhanh - Git + chạy project

## 1) Cập nhật code mới nhất từ GitHub của nhóm

```bash
git fetch origin
git pull origin main
```

Nếu branch hiện tại khác `main`:

```bash
git checkout main
git pull origin main
```

Nếu muốn cập nhật branch hiện tại theo remote:

```bash
git pull --rebase origin $(git branch --show-current)
```

---

## 2) Kiểm tra trạng thái repository

```bash
git status
git branch
git remote -v
```

---

## 3) Thêm và commit toàn bộ code của mình

```bash
git add .
git commit -m "update project"
```

Nếu chưa có user config Git:

```bash
git config --global user.name "TenCuaBan"
git config --global user.email "email@gmail.com"
```

---

## 4) Đẩy code lên GitHub

```bash
git push origin HEAD
```

Nếu branch hiện tại là `main`:

```bash
git push origin main
```

---

## 5) Nếu cần tạo branch mới rồi push

```bash
git checkout -b ten-branch
git add .
git commit -m "update project"
git push -u origin ten-branch
```

---

## 6) Nếu có conflict khi pull hoặc push

```bash
git status
git fetch origin
git merge origin/main
```

Hoặc nếu dùng rebase:

```bash
git pull --rebase origin main
```

Sau đó resolve conflict, rồi:

```bash
git add .
git commit -m "resolve conflict"
git push origin HEAD
```

---

## 7) Chạy backend + frontend nhanh (không dùng localhost)

### Chạy backend

```bash
cd /workspaces/BTL
dotnet run --project ExamSchedule.Api/ExamSchedule.Api.csproj --urls http://0.0.0.0:5000
```

### Chạy frontend

```bash
cd /workspaces/BTL/ExamSchedule.Web
PORT=8080 ./serve.sh
```

### Hoặc chạy frontend bằng Python

```bash
cd /workspaces/BTL/ExamSchedule.Web
python3 -m http.server 8080 --bind 0.0.0.0
```

---

## 8) Script đã có sẵn để chạy nhanh

### Chạy cả 2 cùng lúc
```

### Dừng cả 2

```bash
cd /workspaces/BTL
./stop-dev.sh
```

---

## 9) Mẹo nhớ nhanh

### Lần 1: cập nhật code từ nhóm

```bash
git pull origin main
```

### Lần 2: commit code của mình

```bash
git add .
git commit -m "update project"
```

### Lần 3: đẩy lên GitHub

```bash
git push origin HEAD
```

### Lần 4: chạy app

```bash
cd /workspaces/BTL
./run-dev.sh
```

### Lần 5: mở URL từ tab Ports trong VS Code

- Không dùng localhost
- Dùng URL được forward bởi VS Code / Codespaces

---

## 10) Lệnh rút gọn cực nhanh

```bash
git pull origin main && git add . && git commit -m "update project" && git push origin HEAD
```

Lưu ý: chỉ dùng khi bạn chắc chắn đã muốn commit tất cả thay đổi hiện tại.

---

## 11) Nếu muốn xem log, status hoặc check port nhanh

```bash
git status
ss -lntp | grep -E ':5000|:8080'
```

---

## 12) Cách mở app đúng trong môi trường này

- Không mở `localhost`
- Mở URL đã được Forward trong tab `Ports`
- Ví dụ: `https://friendly-cod...app.github.dev`

---

## 13) Thứ tự làm việc nên nhớ

```bash
git pull origin main
./run-dev.sh
# kiểm tra app trên URL forwarded từ Ports
# code xong
git add .
git commit -m "update project"
git push origin HEAD
```

---

## 14) Lệnh backup nhanh nếu quên

```bash
git status
git add .
git commit -m "backup before update"
git push origin HEAD
```

---

## 15) Chú ý quan trọng

- Nếu `git pull` báo conflict: resolve file rồi `git add .` và `git commit -m "resolve conflict"`
- Nếu port 5000 hoặc 8080 đang bị chiếm: dùng script stop rồi chạy lại

```bash
cd /workspaces/BTL
./stop-dev.sh
./run-dev.sh
```
