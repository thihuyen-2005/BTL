if (!getToken()) location.href = "index.html";

renderLayout("Trang chủ", "Tổng quan hệ thống");

const fullName = localStorage.getItem("fullName") || "bạn";
document.getElementById("welcomeName").textContent = `Xin chào, ${fullName} 👋`;

async function loadDashboard() {
    try {
        // Kỳ thi
        const exams = await apiFetch("/exams?page=1&limit=100");
        const examItems = exams.items || exams;
        document.getElementById("statKyThi").textContent = examItems.length;

        // Đếm ca thi toàn hệ thống
        let totalCaThi = 0;
        for (const ex of examItems) {
            totalCaThi += (ex.soCaThi || 0);
        }
        document.getElementById("statCaThi").textContent = totalCaThi;

        // Phòng thi
        document.getElementById("statPhong").textContent = 3;
        // Người dùng
        document.getElementById("statUsers").textContent = 2;

        // Render 5 kỳ thi gần đây
        const recent = examItems.slice(0, 5);
        const container = document.getElementById("recentExams");

        if (recent.length === 0) {
            container.innerHTML = `
                <div class="empty">
                    <div class="icon">📭</div>
                    <div>Chưa có kỳ thi nào</div>
                </div>`;
            return;
        }

        container.innerHTML = `
            <table>
                <thead>
                    <tr>
                        <th>Mã</th><th>Tên</th><th>Trạng thái</th><th>Số ca</th>
                    </tr>
                </thead>
                <tbody>
                    ${recent.map(k => `
                        <tr>
                            <td><strong>${k.maKyThi}</strong></td>
                            <td>${k.tenKyThi}</td>
                            <td><span class="badge ${k.trangThai}">${k.trangThai}</span></td>
                            <td>${k.soCaThi || 0}</td>
                        </tr>
                    `).join("")}
                </tbody>
            </table>`;
    } catch (e) {
        console.error(e);
    }
}

loadDashboard();