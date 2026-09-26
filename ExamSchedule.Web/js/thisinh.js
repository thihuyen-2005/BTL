if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Thí sinh", "");

const tbody = document.querySelector("#tblCandidates tbody");
const manualModal = document.getElementById("manualModal");
const importModal = document.getElementById("importModal");
const selectAll = document.getElementById("selectAllRows");
const deleteSelectedBtn = document.getElementById("btnDeleteSelected");
const validationMessage = document.getElementById("manualValidationMessage");
const khoaOptions = [
    "Khoa Giáo dục thể chất - Quốc phòng và An ninh",
    "Khoa Khoa học Xã hội và Nhân văn",
    "Khoa Kỹ thuật và Môi trường",
    "Khoa Kinh tế và Du lịch",
    "Khoa Ngoại ngữ",
    "Khoa Quản lý và Đô thị",
    "Khoa Sư phạm",
    "Khoa Toán - Công nghệ thông tin",
    "Viện Hà Nội học và Đào tạo quốc tế"
];
const nganhTheoKhoa = {
    "Khoa Giáo dục thể chất - Quốc phòng và An ninh": ["Giáo dục thể chất (ĐH)"],
    "Khoa Khoa học Xã hội và Nhân văn": ["Chính trị học (ĐH)", "Công tác xã hội (ĐH)", "Luật (ĐH)", "Quản lý Giáo dục (ĐH)", "Tâm lý học (ĐH)", "Văn học (ĐH)"],
    "Khoa Kỹ thuật và Môi trường": ["Công nghệ - Kỹ thuật môi trường (ĐH)"],
    "Khoa Kinh tế và Du lịch": ["Quản lý kinh tế (ĐH)", "Quản trị dịch vụ du lịch và lữ hành (ĐH)", "Quản trị khách sạn (ĐH)", "Quản trị Kinh doanh (ĐH)", "Tài chính - Ngân hàng (ĐH)"],
    "Khoa Ngoại ngữ": ["Ngôn ngữ Anh (ĐH)", "Ngôn ngữ Trung Quốc (ĐH)", "Sư phạm Tiếng Anh (ĐH)"],
    "Khoa Quản lý và Đô thị": ["Logistics và quản lý chuỗi ứng (ĐH)", "Quản lý công (ĐH)"],
    "Khoa Sư phạm": ["Giáo dục Công dân (ĐH)", "Giáo dục đặc biệt (ĐH)", "Giáo dục Mầm non - Giáo dục hòa nhập (ĐH)", "Giáo dục Mầm non (ĐH)", "Giáo dục Tiểu học - Giáo dục hòa nhập (ĐH)", "Giáo dục Tiểu học (ĐH)", "Giáo dục Tiểu học tiên tiến (ĐH)", "SP Lịch sử (ĐH)", "SP Ngữ văn (ĐH)", "SP Toán học (ĐH)", "SP Vật lý (ĐH)"],
    "Khoa Toán - Công nghệ thông tin": ["Công nghệ Thông tin (ĐH)", "Sư phạm Tin học (ĐH)", "Toán ứng dụng (ĐH)"],
    "Viện Hà Nội học và Đào tạo quốc tế": ["Văn hóa học (ĐH)", "Việt Nam học (ĐH)"]
};
let candidates = [];
let selectedIds = new Set();

function populateAcademicSelects() {
    const formKhoa = document.getElementById("khoa");
    const filterKhoa = document.getElementById("filterKhoa");

    const renderKhoaOptions = (select, emptyLabel) => {
        const currentValue = select.value;
        select.innerHTML = `<option value="">${emptyLabel}</option>`;
        khoaOptions.forEach(item => {
            const option = document.createElement("option");
            option.value = item;
            option.textContent = item;
            if (item === currentValue) option.selected = true;
            select.appendChild(option);
        });
    };

    renderKhoaOptions(formKhoa, "-- Chọn khoa --");
    renderKhoaOptions(filterKhoa, "Tất cả khoa");

    const filterNganh = document.getElementById("filterNganh");
    const majorSelect = document.getElementById("nganhHoc");
    const allMajors = [...new Set(Object.values(nganhTheoKhoa).flat())];

    const renderMajorOptions = (select, emptyLabel, selectedValue = "") => {
        const currentValue = select.value || selectedValue;
        select.innerHTML = `<option value="">${emptyLabel}</option>`;
        allMajors.forEach(major => {
            const option = document.createElement("option");
            option.value = major;
            option.textContent = major;
            if (major === currentValue) option.selected = true;
            select.appendChild(option);
        });
    };

    renderMajorOptions(majorSelect, "-- Chọn ngành --", majorSelect.value || "");
    renderMajorOptions(filterNganh, "Tất cả ngành", filterNganh.value || "");
}

function refreshMajorOptionsForForm(selectedKhoa) {
    const majorSelect = document.getElementById("nganhHoc");
    const currentValue = majorSelect.value;
    const majors = [...new Set(Object.values(nganhTheoKhoa).flat())];
    if (selectedKhoa && nganhTheoKhoa[selectedKhoa]) {
        majors.unshift(...nganhTheoKhoa[selectedKhoa]);
    }
    majorSelect.innerHTML = '<option value="">-- Chọn ngành --</option>';
    [...new Set(majors)].forEach(major => {
        const option = document.createElement("option");
        option.value = major;
        option.textContent = major;
        if (major === currentValue) option.selected = true;
        majorSelect.appendChild(option);
    });
}

function populateFilterMajorOptions(selectedKhoa) {
    const filterNganh = document.getElementById("filterNganh");
    const currentValue = filterNganh.value;
    const majors = [...new Set(Object.values(nganhTheoKhoa).flat())];
    if (selectedKhoa && nganhTheoKhoa[selectedKhoa]) {
        majors.unshift(...nganhTheoKhoa[selectedKhoa]);
    }
    filterNganh.innerHTML = '<option value="">Tất cả ngành</option>';
    [...new Set(majors)].forEach(major => {
        const option = document.createElement("option");
        option.value = major;
        option.textContent = major;
        if (major === currentValue) option.selected = true;
        filterNganh.appendChild(option);
    });
}

const REQUIRED_FIELDS = [
    { id: "hoTen", label: "Họ và tên" },
    { id: "ngaySinh", label: "Ngày sinh" },
    { id: "gioiTinh", label: "Giới tính" },
    { id: "soDienThoai", label: "Số điện thoại" },
    { id: "lop", label: "Lớp" },
    { id: "nganhHoc", label: "Ngành học" },
    { id: "khoa", label: "Khoa" }
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

function normalizeVietnameseName(value = "") {
    return value
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/g, "d").replace(/Đ/g, "D")
        .toLowerCase();
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
        candidates = [...list].sort((a, b) => normalizeVietnameseName(a.hoTen).localeCompare(normalizeVietnameseName(b.hoTen)));
        tbody.innerHTML = candidates.length ? candidates.map(x => {
            const scheduleStatus = getSchedulingEligibilityStatus(x);
            const scheduleHook = x.trangThaiXepLich === "Xem lịch" && x.kyThiId
                ? `<button class="btn-link" type="button" onclick="viewStudentSchedule(${x.kyThiId}, ${x.caThiId ?? 0})">${scheduleStatus.label}</button>`
                : `<span class="schedule-status ${scheduleStatus.className}">${scheduleStatus.label}</span>`;
            return `
            <tr>
                <td class="checkbox-col"><input class="row-select" type="checkbox" data-id="${x.thiSinhId}" ${selectedIds.has(x.thiSinhId) ? "checked" : ""}></td>
                <td><strong>${x.maThiSinh || ""}</strong></td>
                <td>${x.hoTen || ""}</td>
                <td>${formatDate(x.ngaySinh)}</td>
                <td>${x.gioiTinh || ""}</td>
                <td>${x.danToc || ""}</td>
                <td>${x.noiSinh || ""}</td>
                <td>${x.quocTich || ""}</td>
                <td>${x.soCccdHoChieu || ""}</td>
                <td>${x.soDienThoai || ""}</td>
                <td>${x.lop || ""}</td>
                <td>${x.khoa || ""}</td>
                <td>${x.nganhHoc || ""}</td>
                <td>${x.soTien == null ? "Chưa nộp" : Number(x.soTien).toLocaleString("vi-VN")}</td>
                <td>${scheduleHook}</td>
                <td>${x.emailCaNhan || ""}</td>
                <td class="sticky-action">
                    <div class="actions-cell">
                        <button class="btn-sm btn-view" onclick="goToManualScheduleForStudent(${x.thiSinhId})">Xếp lịch</button>
                        <button class="btn-sm btn-edit" onclick="editCandidate(${x.thiSinhId})">Sửa</button>
                        <button class="btn-sm btn-del" onclick="deleteCandidate(${x.thiSinhId})">Xóa</button>
                    </div>
                </td>
            </tr>`;
        }).join("") :
            `<tr><td colspan="17"><div class="empty">Chưa có thí sinh</div></td></tr>`;
        syncSelectedRows();
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="16"><div class="empty">${error.message}</div></td></tr>`;
    }
}

function getSchedulingEligibilityStatus(student) {
    const amount = Number(student.soTien ?? 0);
    if (student.trangThaiXepLich === "Xem lịch") {
        return { label: "Xem lịch", className: "status-success" };
    }
    if (!student.soTien || Number.isNaN(amount) || amount < 800) {
        return { label: "Chưa đủ điều kiện xếp lịch", className: "status-warning" };
    }
    return { label: "Chờ xếp lịch", className: "status-info" };
}

function isEligibleForScheduling(student) {
    const amount = Number(student.soTien ?? 0);
    return Number.isFinite(amount) && amount >= 800;
}

function openManualScheduleFromSelection() {
    window.location.href = "manual-schedule.html?fromThiSinh=1";
}

function goToManualScheduleForStudent(studentId) {
    window.location.href = `manual-schedule.html?fromThiSinh=1`;
}

function viewStudentSchedule(kyThiId, caThiId) {
    if (!kyThiId) return;
    const params = new URLSearchParams({ kyThiId: String(kyThiId) });
    if (caThiId) params.set("caThiId", String(caThiId));
    window.location.href = `manual-schedule.html?${params.toString()}`;
}

document.getElementById("btnManualSchedule").onclick = openManualScheduleFromSelection;

document.getElementById("btnManual").onclick = () => {
    document.getElementById("candidateForm").reset();
    document.getElementById("candidateId").value = "";
    populateAcademicSelects();
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
    populateAcademicSelects();
    selectedIds.clear();
    loadCandidates();
};
let searchDebounceTimer = null;
document.getElementById("search").addEventListener("input", () => {
    clearTimeout(searchDebounceTimer);
    searchDebounceTimer = setTimeout(() => loadCandidates(), 300);
});
document.getElementById("filterLop").oninput = loadCandidates;
document.getElementById("filterNganh").onchange = loadCandidates;
document.getElementById("filterKhoa").onchange = () => {
    populateFilterMajorOptions(document.getElementById("filterKhoa").value);
    loadCandidates();
};
document.getElementById("khoa").addEventListener("change", event => {
    refreshMajorOptionsForForm(event.target.value);
});
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
    set("lop", candidate.lop); set("khoa", candidate.khoa || "");

    const khoaSelect = document.getElementById("khoa");
    const majorSelect = document.getElementById("nganhHoc");
    const selectedKhoa = khoaSelect.value || "";
    refreshMajorOptionsForForm(selectedKhoa);
    majorSelect.value = candidate.nganhHoc || "";
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
        populateAcademicSelects();
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

populateAcademicSelects();
loadCandidates();