if (!getToken()) location.href = "index.html";

renderLayout("Trang chủ", "Tổng quan hệ thống");

const fullName = localStorage.getItem("fullName") || "bạn";
document.getElementById("welcomeName").textContent = `Xin chào, ${fullName} 👋`;
const upcomingSessionsBody = document.querySelector("#tblUpcomingSessions tbody");
const previousSessionsButton = document.getElementById("previousSessions");
let upcomingWindowIndex = 0;
let upcomingRequestId = 0;

async function loadDashboard() {
    const upcomingRequest = loadUpcomingSessions();
    try {
        const exams = await apiFetch("/exams?page=1&limit=100");
        const examItems = exams.items || exams;
        document.getElementById("statKyThi").textContent = examItems.length;

        let totalCaThi = 0;
        for (const ex of examItems) {
            totalCaThi += (ex.soCaThi || 0);
        }
        document.getElementById("statCaThi").textContent = totalCaThi;

        document.getElementById("statPhong").textContent = 3;
        renderRecentExams(examItems.slice(0, 5));
    } catch (e) {
        console.error(e);
    }
    await upcomingRequest;
}

function renderRecentExams(recent) {
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
                    <th>Mã</th><th>Tên</th><th>Bắt đầu đăng ký</th><th>Kết thúc đăng ký</th><th>Trạng thái</th><th>Số ca</th>
                </tr>
            </thead>
            <tbody>
                ${recent.map(k => `
                    <tr>
                        <td><strong>${k.maKyThi}</strong></td>
                        <td>${k.tenKyThi}</td>
                        <td>${formatDate(k.thoiGianBatDauDk)}</td>
                        <td>${formatDate(k.thoiGianKetThucDk)}</td>
                        <td><span class="badge ${k.trangThai}">${trangThaiKyThiLabel(k.trangThai)}</span></td>
                        <td>${k.soCaThi || 0}</td>
                    </tr>
                `).join("")}
            </tbody>
        </table>`;
}

function trangThaiKyThiLabel(status) {
    return {
        MoiTao: "Mới tạo",
        DangLapLich: "Đang lập lịch",
        DangThi: "Đang thi",
        KetThuc: "Kết thúc",
        Huy: "Đã hủy"
    }[status] || status;
}

async function loadUpcomingSessions() {
    const requestId = ++upcomingRequestId;
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    start.setDate(start.getDate() + upcomingWindowIndex * 14);
    const end = new Date(start);
    end.setDate(end.getDate() + 14);
    const queryStart = upcomingWindowIndex === 0 && new Date() > start ? new Date() : start;
    const displayEnd = new Date(end);
    displayEnd.setDate(displayEnd.getDate() - 1);

    previousSessionsButton.disabled = upcomingWindowIndex === 0;
    document.getElementById("upcomingSessionsRange").textContent =
        `${start.toLocaleDateString("vi-VN")} – ${displayEnd.toLocaleDateString("vi-VN")}`;
    upcomingSessionsBody.innerHTML = `<tr><td colspan="6"><div class="empty"><div class="icon">⏳</div><div>Đang tải ca thi...</div></div></td></tr>`;

    const query = new URLSearchParams({ from: toLocalDateTime(queryStart), to: toLocalDateTime(end) });
    try {
        const overview = await apiFetch(`/dashboard?${query.toString()}`);
        if (requestId !== upcomingRequestId) return;

        document.getElementById("statUsers").textContent = overview.userCount;
        renderUpcomingSessions(overview.sessions);
    } catch (error) {
        if (requestId !== upcomingRequestId) return;
        upcomingSessionsBody.innerHTML = `<tr><td colspan="6"><div class="empty"><div class="icon">⚠️</div><div>${escapeHtml(error.message)}</div></div></td></tr>`;
    }
}

function renderUpcomingSessions(sessions) {
    if (!sessions.length) {
        upcomingSessionsBody.innerHTML = `<tr><td colspan="6"><div class="empty"><div class="icon">📭</div><div>Không có ca thi trong khoảng thời gian này</div></div></td></tr>`;
        return;
    }

    upcomingSessionsBody.innerHTML = sessions.map(session => `
        <tr>
            <td><a href="exam-sessions.html?kyThiId=${session.kyThiId}">${escapeHtml(session.maKyThi)} · ${escapeHtml(session.tenKyThi)}</a></td>
            <td>${escapeHtml(session.maPhong)}</td>
            <td>${formatDateTime(session.thoiGianBatDau)}</td>
            <td>${formatDateTime(session.thoiGianKetThuc)}</td>
            <td>${session.sucChua}</td>
            <td><span class="badge ${session.trangThai}">${sessionStatusLabel(session.trangThai)}</span></td>
        </tr>`).join("");
}

function toLocalDateTime(date) {
    const pad = value => String(value).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

function formatDateTime(value) {
    return new Date(value).toLocaleString("vi-VN", {
        day: "2-digit", month: "2-digit", year: "numeric",
        hour: "2-digit", minute: "2-digit"
    });
}

function formatDate(value) {
    return value ? new Date(value).toLocaleDateString("vi-VN") : "—";
}

function sessionStatusLabel(status) {
    return { Du_kien: "Dự kiến", Cho_xep: "Dự kiến", Da_xep: "Đã xếp", Dong: "Đã đóng", Huy: "Đã hủy" }[status] || status;
}

function escapeHtml(value) {
    return String(value).replace(/[&<>"']/g, char => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[char]));
}

document.getElementById("previousSessions").onclick = () => {
    if (upcomingWindowIndex === 0) return;
    upcomingWindowIndex--;
    loadUpcomingSessions();
};
document.getElementById("nextSessions").onclick = () => {
    upcomingWindowIndex++;
    loadUpcomingSessions();
};

loadDashboard();