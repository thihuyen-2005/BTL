if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Kỳ thi", "");

const tbody = document.querySelector("#tblExams tbody");
const modal = document.getElementById("modal");
const form = document.getElementById("formExam");

async function loadExams() {
    try {
        const status = document.getElementById("filterStatus").value;
        const kw = document.getElementById("search").value.trim().toLowerCase();

        const qs = new URLSearchParams({ page: 1, limit: 100 });
        if (status) qs.set("status", status);

        const data = await apiFetch("/exams?" + qs.toString());
        let items = data.items || data;

        if (kw) {
            items = items.filter(x =>
                x.maKyThi.toLowerCase().includes(kw) ||
                x.tenKyThi.toLowerCase().includes(kw));
        }

        if (items.length === 0) {
            tbody.innerHTML = `
                <tr><td colspan="9">
                    <div class="empty">
                        <div class="icon">📭</div>
                        <div>Chưa có kỳ thi nào</div>
                    </div>
                </td></tr>`;
            return;
        }

        tbody.innerHTML = items.map(k => `
            <tr>
                <td>${k.kyThiId}</td>
                <td><strong>${k.maKyThi}</strong></td>
                <td>${k.tenKyThi}</td>
                <td>${k.loaiChungChi}</td>
                <td>${formatDate(k.thoiGianBatDauDk)}</td>
                <td>${formatDate(k.thoiGianKetThucDk)}</td>
                <td><span class="badge ${k.trangThai}">${trangThaiLabel(k.trangThai)}</span></td>
                <td>${k.soCaThi || 0}</td>
                <td>
                    <div class="actions-cell">
                        <button class="btn-sm btn-edit" onclick="editExam(${k.kyThiId})">✏️ Sửa</button>
                        <button class="btn-sm btn-del" onclick="deleteExam(${k.kyThiId})">🗑️ Xóa</button>
                        <a class="btn-sm btn-view" href="exam-sessions.html?kyThiId=${k.kyThiId}">🕐 Ca thi</a>
                    </div>
                </td>
            </tr>`).join("");
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="9"><div class="empty"><div class="icon">⚠️</div><div>${e.message}</div></div></td></tr>`;
    }
}

function trangThaiLabel(tt) {
    const map = {
        MoiTao: "Mới tạo",
        DangLapLich: "Đang lập lịch",
        DangThi: "Đang thi",
        KetThuc: "Kết thúc",
        Huy: "Đã hủy"
    };
    return map[tt] || tt;
}
function formatDate(iso) {
    if (!iso) return "";
    return new Date(iso).toLocaleDateString("vi-VN");
}

document.getElementById("btnAdd").onclick = () => {
    form.reset();
    document.getElementById("kyThiId").value = "";
    document.getElementById("modalTitle").textContent = "Thêm kỳ thi";
    modal.classList.remove("hidden");
};

document.getElementById("btnCancel").onclick = () => modal.classList.add("hidden");

window.editExam = async function(id) {
    try {
        const k = await apiFetch(`/exams/${id}`);
        document.getElementById("kyThiId").value      = k.kyThiId;
        document.getElementById("maKyThi").value      = k.maKyThi;
        document.getElementById("tenKyThi").value     = k.tenKyThi;
        document.getElementById("loaiChungChi").value = k.loaiChungChi;
        document.getElementById("batDauDk").value     = k.thoiGianBatDauDk.substring(0, 10);
        document.getElementById("ketThucDk").value    = k.thoiGianKetThucDk.substring(0, 10);
        document.getElementById("ghiChu").value       = k.ghiChu || "";
        document.getElementById("modalTitle").textContent = "Sửa kỳ thi";
        modal.classList.remove("hidden");
    } catch (e) {
        toast("Lỗi tải thông tin: " + e.message, "error");
    }
};

window.deleteExam = async function(id) {
    if (!confirm("Bạn chắc chắn muốn xóa kỳ thi này?")) return;
    try {
        await apiFetch(`/exams/${id}`, { method: "DELETE" });
        toast("Đã xóa thành công!", "success");
        loadExams();
    } catch (e) {
        toast("Không thể xóa: " + e.message, "error");
    }
};

form.onsubmit = async (e) => {
    e.preventDefault();
    const id = document.getElementById("kyThiId").value;

    const body = {
        maKyThi: document.getElementById("maKyThi").value,
        tenKyThi: document.getElementById("tenKyThi").value,
        loaiChungChi: document.getElementById("loaiChungChi").value,
        thoiGianBatDauDk: document.getElementById("batDauDk").value + "T00:00:00",
        thoiGianKetThucDk: document.getElementById("ketThucDk").value + "T00:00:00",
        ghiChu: document.getElementById("ghiChu").value
    };

    try {
        if (id) {
            await apiFetch(`/exams/${id}`, {
                method: "PUT",
                body: JSON.stringify({
                    tenKyThi: body.tenKyThi,
                    thoiGianKetThucDk: body.thoiGianKetThucDk,
                    ghiChu: body.ghiChu
                })
            });
            toast("Đã cập nhật kỳ thi", "success");
        } else {
            await apiFetch("/exams", { method: "POST", body: JSON.stringify(body) });
            toast("Đã thêm kỳ thi mới", "success");
        }
        modal.classList.add("hidden");
        loadExams();
    } catch (err) {
        toast("Lỗi: " + err.message, "error");
    }
};

document.getElementById("filterStatus").onchange = loadExams;
document.getElementById("search").oninput = loadExams;

loadExams();