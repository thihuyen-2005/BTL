if (!getToken()) location.href = "index.html";

const kyThiId = Number(new URLSearchParams(location.search).get("kyThiId"));
if (!Number.isInteger(kyThiId) || kyThiId <= 0) {
    location.href = "exams.html";
}

renderLayout("Quản lý Ca thi", "");

const tbody = document.querySelector("#tblSessions tbody");
const modal = document.getElementById("modal");
const form = document.getElementById("formSession");
const selectAll = document.getElementById("selectAllRows");
const deleteSelectedBtn = document.getElementById("btnDeleteSelected");
let searchTimer;
let editingSessionId = null;
let selectedSessionIds = new Set();

function syncSelectedRows() {
    selectedSessionIds = new Set([...document.querySelectorAll('.session-row-select:checked')].map(item => Number(item.dataset.id)));
    const rows = [...document.querySelectorAll('.session-row-select')];
    const allSelected = rows.length > 0 && rows.every(item => item.checked);
    selectAll.checked = allSelected;
    deleteSelectedBtn.disabled = selectedSessionIds.size === 0;
}

document.getElementById("btnSchedule").onclick = async () => {
    if (!confirm("Xếp tự động các thí sinh chưa có ca thi? Lịch đã xếp sẽ được giữ nguyên.")) return;
    try {
        const result = await apiFetch(`/thisinh/ky-thi/${kyThiId}/xep-lich`, { method: "POST" });
        toast(`Đã xếp ${result.daXep}/${result.tongSoDangKy} thí sinh`, "success");
        loadSessions();
    } catch (e) {
        toast("Không thể xếp lịch: " + e.message, "error");
    }
};

async function loadSessions() {
    try {
        const status = document.getElementById("filterStatus").value;
        const keyword = document.getElementById("search").value.trim();
        const qs = new URLSearchParams({ kyThiId });
        if (status) qs.set("trangThai", status);
        if (keyword) qs.set("keyword", keyword);

        const list = await apiFetch("/exam-sessions?" + qs.toString());

        if (list.length > 0) {
            document.getElementById("kyThiTitle").textContent =
                `${list[0].tenKyThi} (${list[0].maKyThi})`;
        }

        if (list.length === 0) {
            tbody.innerHTML = `
                <tr><td colspan="9">
                    <div class="empty">
                        <div class="icon">🕐</div>
                        <div>Chưa có ca thi nào</div>
                    </div>
                </td></tr>`;
            return;
        }

        tbody.innerHTML = list.map(c => `
            <tr>
                <td class="checkbox-col"><input class="session-row-select" type="checkbox" data-id="${c.caThiId}" ${selectedSessionIds.has(c.caThiId) ? "checked" : ""}></td>
                <td>${c.caThiId}</td>
                <td><strong>${c.maPhong}</strong></td>
                <td>${formatDateTime(c.thoiGianBatDau)}</td>
                <td>${formatDateTime(c.thoiGianKetThuc)}</td>
                <td>${c.sucChua}</td>
                <td><span class="badge ${c.trangThai}">${trangThaiCaThiLabel(c.trangThai)}</span></td>
                <td>
                    <div class="actions-cell">
                        <a class="btn-sm btn-view" href="proctors.html?kyThiId=${kyThiId}&caThiId=${c.caThiId}">👤 Phân công</a>
                        <button class="btn-sm btn-edit" onclick="editSession(${c.caThiId})">✏️ Sửa</button>
                        <button class="btn-sm btn-del" onclick="deleteSession(${c.caThiId})">🗑️ Xóa</button>
                        <button class="btn-sm btn-view" onclick="cancelSession(${c.caThiId})">⛔ Hủy</button>
                    </div>
                </td>
            </tr>`).join("");
        syncSelectedRows();
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="9"><div class="empty"><div class="icon">⚠️</div><div>${e.message}</div></div></td></tr>`;
    }
}

function formatDateTime(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    const pad = n => String(n).padStart(2, "0");
    return `${pad(d.getDate())}/${pad(d.getMonth()+1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function trangThaiCaThiLabel(tt) {
    const map = {
        Du_kien: "Dự kiến",
        Cho_xep: "Chờ xếp",
        Da_xep:  "Đã xếp",
        Dong:    "Đã đóng",
        Huy:     "Đã hủy"
    };
    return map[tt] || tt;
}

document.getElementById("btnAdd").onclick = () => {
    editingSessionId = null;
    form.reset();
    document.getElementById("modalTitle").textContent = "Thêm ca thi";
    document.getElementById("sucChua").value = 30;
    modal.classList.remove("hidden");
};

document.getElementById("btnCancel").onclick = () => modal.classList.add("hidden");

form.onsubmit = async (e) => {
    e.preventDefault();

    const body = {
        kyThiId,
        phongThiId: parseInt(document.getElementById("phongThi").value),
        thoiGianBatDau: document.getElementById("batDau").value + ":00",
        sucChua: parseInt(document.getElementById("sucChua").value),
        ghiChu: document.getElementById("ghiChu").value
    };

    try {
        await apiFetch(editingSessionId ? `/exam-sessions/${editingSessionId}` : "/exam-sessions", {
            method: editingSessionId ? "PUT" : "POST",
            body: JSON.stringify(body)
        });
        toast(editingSessionId ? "Đã cập nhật ca thi!" : "Đã tạo ca thi!", "success");
        modal.classList.add("hidden");
        editingSessionId = null;
        loadSessions();
    } catch (err) {
        toast("Không thể tạo ca thi: " + err.message, "error");
    }
};

function toDateTimeLocal(iso) {
    const date = new Date(iso);
    const pad = value => String(value).padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

window.editSession = async function (id) {
    try {
        const session = (await apiFetch(`/exam-sessions?kyThiId=${kyThiId}`))
            .find(item => item.caThiId === id);
        if (!session) throw new Error("Không tìm thấy ca thi.");
        editingSessionId = id;
        document.getElementById("modalTitle").textContent = "Sửa ca thi";
        document.getElementById("phongThi").value = session.phongThiId;
        document.getElementById("batDau").value = toDateTimeLocal(session.thoiGianBatDau);
        document.getElementById("sucChua").value = session.sucChua;
        document.getElementById("ghiChu").value = session.ghiChu || "";
        modal.classList.remove("hidden");
    } catch (error) {
        toast("Không thể tải ca thi: " + error.message, "error");
    }
};

window.cancelSession = async function(id) {
    if (!confirm("Bạn chắc chắn muốn hủy ca thi này? Lịch hủy này chỉ thay đổi trạng thái của ca thi, không xóa dữ liệu trong cơ sở dữ liệu.")) return;
    try {
        await apiFetch(`/exam-sessions/${id}/cancel`, { method: "PUT" });
        toast("Đã hủy ca thi!", "success");
        loadSessions();
    } catch (e) {
        toast("Lỗi: " + e.message, "error");
    }
};

window.deleteSession = async function(id) {
    const detail = (await apiFetch(`/exam-sessions?kyThiId=${kyThiId}`)).find(item => item.caThiId === id);
    const label = detail ? `${detail.maPhong} (${formatDateTime(detail.thoiGianBatDau)} - ${formatDateTime(detail.thoiGianKetThuc)})` : `ca thi #${id}`;
    if (!confirm(`Bạn có chắc muốn xóa ${label}? Mọi dữ liệu phân công và đăng ký trong ca thi này sẽ bị xóa khỏi cơ sở dữ liệu.`)) return;
    try {
        await apiFetch(`/exam-sessions/${id}`, { method: "DELETE" });
        toast("Đã xóa ca thi", "success");
        loadSessions();
    } catch (e) {
        toast("Không thể xóa ca thi: " + e.message, "error");
    }
};

selectAll.addEventListener("change", (event) => {
    document.querySelectorAll(".session-row-select").forEach(item => item.checked = event.target.checked);
    syncSelectedRows();
});

tbody.addEventListener("change", (event) => {
    if (event.target.matches(".session-row-select")) {
        syncSelectedRows();
    }
});

deleteSelectedBtn.onclick = async () => {
    if (!selectedSessionIds.size) return;
    const ids = [...selectedSessionIds];
    if (!confirm(`Bạn đang xóa ${ids.length} ca thi đã chọn. Tất cả dữ liệu phân công và đăng ký của các ca thi này sẽ bị xóa khỏi cơ sở dữ liệu. Tiếp tục?`)) return;
    try {
        for (const id of ids) {
            await apiFetch(`/exam-sessions/${id}`, { method: "DELETE" });
        }
        selectedSessionIds.clear();
        toast(`Đã xóa ${ids.length} ca thi`, "success");
        loadSessions();
    } catch (error) {
        toast("Không thể xóa các ca thi đã chọn: " + error.message, "error");
    }
};

document.getElementById("filterStatus").onchange = loadSessions;
document.getElementById("search").oninput = () => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(loadSessions, 200);
};

loadSessions();