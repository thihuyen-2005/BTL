if (!getToken()) location.href = "index.html";

const kyThiId = Number(new URLSearchParams(location.search).get("kyThiId"));
if (!Number.isInteger(kyThiId) || kyThiId <= 0) {
    location.href = "exams.html";
}

renderLayout("Quản lý Ca thi", "");

const tbody = document.querySelector("#tblSessions tbody");
const modal = document.getElementById("modal");
const form = document.getElementById("formSession");

async function loadSessions() {
    try {
        const status = document.getElementById("filterStatus").value;
        const qs = new URLSearchParams({ kyThiId });
        if (status) qs.set("trangThai", status);

        const list = await apiFetch("/exam-sessions?" + qs.toString());

        if (list.length > 0) {
            document.getElementById("kyThiTitle").textContent =
                `${list[0].tenKyThi} (${list[0].maKyThi})`;
        }

        if (list.length === 0) {
            tbody.innerHTML = `
                <tr><td colspan="7">
                    <div class="empty">
                        <div class="icon">🕐</div>
                        <div>Chưa có ca thi nào</div>
                    </div>
                </td></tr>`;
            return;
        }

        tbody.innerHTML = list.map(c => `
            <tr>
                <td>${c.caThiId}</td>
                <td><strong>${c.maPhong}</strong></td>
                <td>${formatDateTime(c.thoiGianBatDau)}</td>
                <td>${formatDateTime(c.thoiGianKetThuc)}</td>
                <td>${c.sucChua}</td>
                <td><span class="badge ${c.trangThai}">${trangThaiCaThiLabel(c.trangThai)}</span></td>
                <td>
                    <div class="actions-cell">
                        <a class="btn-sm btn-view" href="proctors.html?caThiId=${c.caThiId}">👤 Phân công</a>
                        <button class="btn-sm btn-del" onclick="cancelSession(${c.caThiId})">🗑️ Hủy</button>
                    </div>
                </td>
            </tr>`).join("");
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="7"><div class="empty"><div class="icon">⚠️</div><div>${e.message}</div></div></td></tr>`;
    }
}

function formatDateTime(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    const pad = n => String(n).padStart(2, "0");
    return `${pad(d.getDate())}/${pad(d.getMonth()+1)}/${pad(d.getFullYear())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
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
    form.reset();
    document.getElementById("sucChua").value = 30;
    modal.classList.remove("hidden");
};

document.getElementById("btnCancel").onclick = () => modal.classList.add("hidden");

form.onsubmit = async (e) => {
    e.preventDefault();

    const body = {
        kyThiId: kyThiId,
        phongThiId: parseInt(document.getElementById("phongThi").value),
        thoiGianBatDau: document.getElementById("batDau").value + ":00",
        thoiGianKetThuc: document.getElementById("ketThuc").value + ":00",
        sucChua: parseInt(document.getElementById("sucChua").value),
        ghiChu: document.getElementById("ghiChu").value
    };

    try {
        await apiFetch("/exam-sessions", {
            method: "POST",
            body: JSON.stringify(body)
        });
        toast("Đã tạo ca thi!", "success");
        modal.classList.add("hidden");
        loadSessions();
    } catch (err) {
        toast("Không thể tạo ca thi: " + err.message, "error");
    }
};

window.cancelSession = async function(id) {
    if (!confirm("Bạn chắc chắn muốn hủy ca thi này?")) return;
    try {
        await apiFetch(`/exam-sessions/${id}/cancel`, { method: "PUT" });
        toast("Đã hủy ca thi!", "success");
        loadSessions();
    } catch (e) {
        toast("Lỗi: " + e.message, "error");
    }
};

document.getElementById("filterStatus").onchange = loadSessions;

loadSessions();