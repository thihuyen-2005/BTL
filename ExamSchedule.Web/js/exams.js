if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Kỳ thi", "");

const tbody = document.querySelector("#tblExams tbody");
const modal = document.getElementById("modal");
const form = document.getElementById("formExam");
const selectAll = document.getElementById("selectAllRows");
const deleteSelectedBtn = document.getElementById("btnDeleteSelected");
let searchTimer;
let loadRequestId = 0;
let selectedExamIds = new Set();
let examCache = [];
const examCacheKey = "exam-list-cache";

function syncSelectedRows() {
    selectedExamIds = new Set([...document.querySelectorAll('.exam-row-select:checked')].map(item => Number(item.dataset.id)));
    const rows = [...document.querySelectorAll('.exam-row-select')];
    const allSelected = rows.length > 0 && rows.every(item => item.checked);
    selectAll.checked = allSelected;
    deleteSelectedBtn.disabled = selectedExamIds.size === 0;
}

async function loadExams() {
    const requestId = ++loadRequestId;
    try {
        const data = await apiFetch("/exams?page=1&limit=100");
        if (requestId !== loadRequestId) return;
        examCache = data.items || data;
        renderExams();
        try {
            sessionStorage.setItem(examCacheKey, JSON.stringify(examCache));
        } catch {
            // Bỏ qua nếu trình duyệt không cho phép lưu sessionStorage.
        }
    } catch (e) {
        if (!examCache.length) {
            tbody.innerHTML = `<tr><td colspan="10"><div class="empty"><div class="icon">⚠️</div><div>${e.message}</div></div></td></tr>`;
        }
    }
}

function renderExams() {
    const status = document.getElementById("filterStatus").value;
    const keyword = document.getElementById("search").value.trim().toLowerCase();
    const items = examCache.filter(exam => {
        const matchesStatus = !status || exam.trangThai === status;
        const matchesKeyword = !keyword
            || exam.maKyThi.toLowerCase().includes(keyword)
            || exam.tenKyThi.toLowerCase().includes(keyword);
        return matchesStatus && matchesKeyword;
    });

    if (items.length === 0) {
        tbody.innerHTML = `
            <tr><td colspan="10">
                <div class="empty">
                    <div class="icon">📭</div>
                    <div>Chưa có kỳ thi nào</div>
                </div>
            </td></tr>`;
        return;
    }

    tbody.innerHTML = items.map(k => `
        <tr>
            <td class="checkbox-col"><input class="exam-row-select" type="checkbox" data-id="${k.kyThiId}" ${selectedExamIds.has(k.kyThiId) ? "checked" : ""}></td>
            <td>${k.kyThiId}</td>
            <td><strong>${k.maKyThi}</strong></td>
            <td>${k.tenKyThi}</td>
            <td>${formatDate(k.thoiGianBatDauDk)}</td>
            <td>${formatDate(k.thoiGianKetThucDk)}</td>
            <td><span class="badge ${k.trangThai}">${trangThaiLabel(k.trangThai)}</span></td>
            <td>${k.soCaThi || 0}</td>
            <td class="sticky-action">
                <div class="actions-cell">
                    <button class="btn-sm btn-edit" onclick="editExam(${k.kyThiId})">✏️ Sửa</button>
                    <button class="btn-sm btn-del" onclick="deleteExam(${k.kyThiId})">🗑️ Xóa</button>
                    <a class="btn-sm btn-view" href="exam-sessions.html?kyThiId=${k.kyThiId}">🕐 Ca thi</a>
                </div>
            </td>
        </tr>`).join("");
    syncSelectedRows();
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
    try {
        const exam = await apiFetch(`/exams/${id}`);
        const label = `${exam.maKyThi} - ${exam.tenKyThi}`;
        if (!confirm(`Bạn có chắc muốn xóa ${label}? Mọi ca thi, lịch phân công và dữ liệu liên quan của kỳ thi này sẽ bị xóa khỏi cơ sở dữ liệu.`)) return;
        await apiFetch(`/exams/${id}`, { method: "DELETE" });
        toast("Đã xóa thành công!", "success");
        loadExams();
    } catch (e) {
        toast("Không thể xóa: " + e.message, "error");
    }
};

selectAll.addEventListener("change", (event) => {
    document.querySelectorAll(".exam-row-select").forEach(item => item.checked = event.target.checked);
    syncSelectedRows();
});

tbody.addEventListener("change", (event) => {
    if (event.target.matches(".exam-row-select")) {
        syncSelectedRows();
    }
});

deleteSelectedBtn.onclick = async () => {
    if (!selectedExamIds.size) return;
    const ids = [...selectedExamIds];
    const labels = ids.map(id => `#${id}`).join("\n");
    if (!confirm(`Bạn đang xóa ${ids.length} kỳ thi đã chọn:\n${labels}\n\nTất cả ca thi, thí sinh đăng ký và dữ liệu liên quan sẽ bị xóa khỏi cơ sở dữ liệu. Tiếp tục?`)) return;
    try {
        for (const id of ids) {
            await apiFetch(`/exams/${id}`, { method: "DELETE" });
        }
        selectedExamIds.clear();
        toast(`Đã xóa ${ids.length} kỳ thi`, "success");
        loadExams();
    } catch (error) {
        toast("Không thể xóa các kỳ thi đã chọn: " + error.message, "error");
    }
};

form.onsubmit = async (e) => {
    e.preventDefault();
    const id = document.getElementById("kyThiId").value;

    const body = {
        maKyThi: document.getElementById("maKyThi").value,
        tenKyThi: document.getElementById("tenKyThi").value,
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

document.getElementById("filterStatus").onchange = renderExams;
document.getElementById("search").oninput = renderExams;

try {
    examCache = JSON.parse(sessionStorage.getItem(examCacheKey) || "[]");
    renderExams();
} catch {
    examCache = [];
}
if (!examCache.length) {
    tbody.innerHTML = `<tr><td colspan="10"><div class="empty"><div class="icon">⏳</div><div>Đang tải danh sách kỳ thi...</div></div></td></tr>`;
}
loadExams();