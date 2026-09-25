if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Thí sinh", "");

const tbody = document.querySelector("#tblCandidates tbody");
const manualModal = document.getElementById("manualModal");
const importModal = document.getElementById("importModal");
const selectAll = document.getElementById("selectAllRows");
const deleteSelectedBtn = document.getElementById("btnDeleteSelected");
const validationMessage = document.getElementById("manualValidationMessage");
let candidates = [];
let selectedIds = new Set();

const REQUIRED_FIELDS = [
    { id: "hoTen", label: "Họ và tên" },
    { id: "ngaySinh", label: "Ngày sinh" },
    { id: "gioiTinh", label: "Giới tính" },
    { id: "soCccdHoChieu", label: "Số CCCD/Hộ chiếu" },
    { id: "soDienThoai", label: "Số điện thoại" },
    { id: "lop", label: "Lớp" },
    { id: "nganhHoc", label: "Ngành học" },
    { id: "khoa", label: "Khoa" },
    { id: "soTien", label: "Tình trạng học phí" }
];

function clearValidationState() {
    validationMessage.classList.add("hidden");
    validationMessage.textContent = "";
    REQUIRED_FIELDS.forEach(field => {
        const el = document.getElementById(field.id);
        if (el) el.classList.remove("input-error");
    });
}

function showValidationState(missingFields) {
    const list = missingFields.map(field => `• ${field}`).join("<br>");
    validationMessage.innerHTML = `Bạn chưa điền đầy đủ các trường bắt buộc:<br>${list}`;
    validationMessage.classList.remove("hidden");

    missingFields.forEach(fieldName => {
        const field = REQUIRED_FIELDS.find(item => item.label === fieldName);
        if (!field) return;
        const el = document.getElementById(field.id);
        if (el) el.classList.add("input-error");
    });
}

function getMissingRequiredFields() {
    const missing = [];
    REQUIRED_FIELDS.forEach(field => {
        const el = document.getElementById(field.id);
        if (!el) return;
        const value = el.value == null ? "" : String(el.value).trim();
        if (!value) missing.push(field.label);
    });
    return missing;
}

function formatDate(value) {
    return value ? new Date(value).toLocaleDateString("vi-VN") : "";
}

function syncSelectedRows() {
    selectedIds = new Set([...document.querySelectorAll('.row-select:checked')].map(item => Number(item.dataset.id)));
    const rows = [...document.querySelectorAll('.row-select')];
    const allSelected = rows.length > 0 && rows.every(item => item.checked);
    selectAll.checked = allSelected;
    deleteSelectedBtn.disabled = selectedIds.size === 0;
}

async function loadCandidates() {
    const keyword = document.getElementById("search").value.trim();
    const paidFilter = document.getElementById("filterPaid").value;
    try {
        const params = new URLSearchParams({
            tuKhoa: keyword,
            lop: document.getElementById("filterLop").value.trim(),
            nganhHoc: document.getElementById("filterNganh").value.trim(),
            khoa: document.getElementById("filterKhoa").value.trim()
        });
        if (paidFilter === "paid") params.set("daNop", "true");
        if (paidFilter === "unpaid") params.set("daNop", "false");
        const list = await apiFetch(`/thisinh?${params}`);
        candidates = list;
        tbody.innerHTML = list.length ? list.map(x => `
            <tr>
                <td class="checkbox-col"><input class="row-select" type="checkbox" data-id="${x.thiSinhId}" ${selectedIds.has(x.thiSinhId) ? "checked" : ""}></td>
                <td><strong>${x.maThiSinh}</strong></td><td>${x.hoTen}</td>
                <td>${formatDate(x.ngaySinh)}</td><td>${x.lop || ""}</td>
                <td>${x.nganhHoc || ""}</td><td>${x.soTien == null ? "Chưa nộp" : Number(x.soTien).toLocaleString("vi-VN")}</td>
                <td>${x.emailCaNhan || ""}</td>
                <td><button class="btn-sm btn-edit" onclick="editCandidate(${x.thiSinhId})">✏️ Sửa</button>
                <button class="btn-sm btn-del" onclick="deleteCandidate(${x.thiSinhId})">🗑️ Xóa</button></td>
            </tr>`).join("") :
            `<tr><td colspan="9"><div class="empty">Chưa có thí sinh</div></td></tr>`;
        syncSelectedRows();
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="9"><div class="empty">${error.message}</div></td></tr>`;
    }
}

document.getElementById("btnManual").onclick = () => {
    document.getElementById("candidateForm").reset();
    document.getElementById("candidateId").value = "";
    clearValidationState();
    document.getElementById("manualTitle").textContent = "Thêm thí sinh";
    manualModal.classList.remove("hidden");
};
document.getElementById("btnImport").onclick = () => importModal.classList.remove("hidden");
document.getElementById("btnCancelManual").onclick = () => manualModal.classList.add("hidden");
document.getElementById("btnCancelImport").onclick = () => importModal.classList.add("hidden");
document.getElementById("btnRefreshFilters").onclick = () => {
    document.getElementById("search").value = "";
    document.getElementById("filterLop").value = "";
    document.getElementById("filterNganh").value = "";
    document.getElementById("filterKhoa").value = "";
    document.getElementById("filterPaid").value = "";
    selectedIds.clear();
    loadCandidates();
};
document.getElementById("search").oninput = loadCandidates;
document.getElementById("filterLop").oninput = loadCandidates;
document.getElementById("filterNganh").oninput = loadCandidates;
document.getElementById("filterKhoa").oninput = loadCandidates;
document.getElementById("filterPaid").onchange = loadCandidates;

tbody.addEventListener("change", (event) => {
    if (event.target.matches(".row-select")) {
        syncSelectedRows();
    }
});

selectAll.addEventListener("change", (event) => {
    const isChecked = event.target.checked;
    document.querySelectorAll(".row-select").forEach(item => item.checked = isChecked);
    syncSelectedRows();
});

window.editCandidate = id => {
    const candidate = candidates.find(x => x.thiSinhId === id);
    if (!candidate) return;
    clearValidationState();
    const set = (name, value) => document.getElementById(name).value = value || "";
    set("candidateId", candidate.thiSinhId); set("maThiSinh", candidate.maThiSinh); set("hoTen", candidate.hoTen);
    set("ngaySinh", candidate.ngaySinh?.substring(0, 10)); set("gioiTinh", candidate.gioiTinh);
    set("soCccdHoChieu", candidate.soCccdHoChieu); set("soDienThoai", candidate.soDienThoai);
    set("lop", candidate.lop); set("nganhHoc", candidate.nganhHoc); set("khoa", candidate.khoa);
    set("soTien", candidate.soTien == null ? "0" : String(candidate.soTien)); set("emailCaNhan", candidate.emailCaNhan);
    document.getElementById("manualTitle").textContent = "Sửa thông tin thí sinh";
    manualModal.classList.remove("hidden");
};

window.deleteCandidate = async id => {
    const candidate = candidates.find(x => x.thiSinhId === id);
    const label = candidate ? `${candidate.maThiSinh} - ${candidate.hoTen}` : "thí sinh này";
    if (!confirm(`Bạn có chắc muốn xóa ${label}? Tất cả dữ liệu đăng ký liên quan của thí sinh này sẽ bị xóa khỏi cơ sở dữ liệu. Hành động này không thể hoàn tác.`)) return;
    try { await apiFetch(`/thisinh/${id}`, { method: "DELETE" }); toast("Đã xóa thí sinh", "success"); loadCandidates(); }
    catch (error) { toast("Không thể xóa: " + error.message, "error"); }
};

deleteSelectedBtn.onclick = async () => {
    if (!selectedIds.size) return;
    const selected = candidates.filter(item => selectedIds.has(item.thiSinhId));
    const names = selected.map(item => `${item.maThiSinh} - ${item.hoTen}`).join("\n");
    if (!confirm(`Bạn đang xóa ${selected.length} thí sinh đã chọn:\n${names}\n\nTất cả dữ liệu liên quan sẽ bị xóa khỏi cơ sở dữ liệu. Bạn có chắc chắn muốn tiếp tục?`)) return;
    try {
        for (const id of [...selectedIds]) {
            await apiFetch(`/thisinh/${id}`, { method: "DELETE" });
        }
        selectedIds.clear();
        toast(`Đã xóa ${selected.length} thí sinh`, "success");
        loadCandidates();
    } catch (error) {
        toast("Không thể xóa các thí sinh đã chọn: " + error.message, "error");
    }
};

document.getElementById("candidateForm").onsubmit = async event => {
    event.preventDefault();
    clearValidationState();

    const missing = getMissingRequiredFields();
    if (missing.length > 0) {
        showValidationState(missing);
        return;
    }

    const value = id => document.getElementById(id).value.trim();
    try {
        const id = value("candidateId");
        const payload = {
            maThiSinh: value("maThiSinh") || null, hoTen: value("hoTen"),
            ngaySinh: value("ngaySinh") || null, gioiTinh: value("gioiTinh") || null,
            soCccdHoChieu: value("soCccdHoChieu") || null, soDienThoai: value("soDienThoai") || null,
            lop: value("lop") || null, nganhHoc: value("nganhHoc") || null,
            khoa: value("khoa") || null, soTien: value("soTien") !== "" ? Number(value("soTien")) : null,
            emailCaNhan: value("emailCaNhan") || null
        };
        await apiFetch(id ? `/thisinh/${id}` : "/thisinh", { method: id ? "PUT" : "POST", body: JSON.stringify(payload) });
        manualModal.classList.add("hidden");
        event.target.reset();
        clearValidationState();
        toast("Đã lưu thí sinh", "success");
        loadCandidates();
    } catch (error) { toast("Không thể lưu: " + error.message, "error"); }
};
document.getElementById("btnSaveImport").onclick = async () => {
    try {
        const file = document.getElementById("excelFile").files[0];
        if (!file) throw new Error("Vui lòng chọn file Excel .xlsx.");
        const body = new FormData();
        body.append("file", file);
        const result = await apiFetch("/thisinh/import-excel", { method: "POST", body, headers: {} });
        importModal.classList.add("hidden");
        document.getElementById("excelFile").value = "";
        toast(`Đã nhập ${result.soLuong} thí sinh${result.loi.length ? `, ${result.loi.length} dòng lỗi` : ""}`, result.loi.length ? "info" : "success");
        loadCandidates();
    } catch (error) {
        toast("Không thể nhập: " + error.message, "error");
    }
};

loadCandidates();