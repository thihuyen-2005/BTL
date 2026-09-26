if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Giám thị", "Danh sách cán bộ coi thi");

const tbody = document.querySelector("#tblProctors tbody");
const search = document.getElementById("search");
const filterStatus = document.getElementById("filterStatus");
const scheduleModal = document.getElementById("scheduleModal");
const scheduleBody = document.getElementById("scheduleBody");
const selectAll = document.getElementById("selectAllRows");
const deleteSelectedBtn = document.getElementById("btnDeleteSelected");
const qs = new URLSearchParams(location.search);
const kyThiId = Number(qs.get("kyThiId"));
const caThiId = Number(qs.get("caThiId"));
const assignmentPanel = document.getElementById("assignmentPanel");
const examSelect = document.getElementById("examSelect");
const sessionSelect = document.getElementById("sessionSelect");
const proctorModal = document.getElementById("proctorModal");
const proctorForm = document.getElementById("proctorForm");
let candidates = [];
let allProctors = [];
let selectedCaThiId = Number.isInteger(caThiId) && caThiId > 0 ? caThiId : null;
let selectedProctorIds = new Set();

function syncSelectedRows() {
    selectedProctorIds = new Set([...document.querySelectorAll('.proctor-row-select:checked')].map(item => Number(item.dataset.id)));
    const rows = [...document.querySelectorAll('.proctor-row-select')];
    const allSelected = rows.length > 0 && rows.every(item => item.checked);
    selectAll.checked = allSelected;
    deleteSelectedBtn.disabled = selectedProctorIds.size === 0;
}

async function loadExams() {
    const response = await apiFetch("/exams?page=1&limit=100");
    const exams = response.items || response;
    examSelect.innerHTML = `<option value="">Chọn kỳ thi</option>` + exams.map(exam =>
        `<option value="${exam.kyThiId}" ${Number(exam.kyThiId) === Number(kyThiId) ? "selected" : ""}>${escapeHtml(exam.maKyThi)} - ${escapeHtml(exam.tenKyThi)}</option>`
    ).join("");

    if (Number.isInteger(kyThiId) && kyThiId > 0) {
        await loadSessionsForExam(kyThiId);
    }
}

async function loadSessionsForExam(examId) {
    if (!examId) {
        sessionSelect.innerHTML = `<option value="">Chọn ca thi</option>`;
        sessionSelect.disabled = true;
        selectedCaThiId = null;
        assignmentPanel.classList.add("hidden");
        return;
    }

    sessionSelect.disabled = true;
    sessionSelect.innerHTML = `<option value="">Đang tải ca thi...</option>`;
    let sessions;
    try {
        sessions = await apiFetch(`/exam-sessions?kyThiId=${examId}`);
    } catch (error) {
        sessionSelect.innerHTML = `<option value="">Không thể tải ca thi</option>`;
        selectedCaThiId = null;
        assignmentPanel.classList.add("hidden");
        toast("Không thể tải ca thi: " + error.message, "error");
        return;
    }

    if (!sessions.length) {
        sessionSelect.innerHTML = `<option value="">Kỳ thi này chưa có ca thi</option>`;
        selectedCaThiId = null;
        assignmentPanel.classList.add("hidden");
        return;
    }

    sessionSelect.innerHTML = `<option value="">Chọn ca thi</option>` + sessions.map(s =>
        `<option value="${s.caThiId}" ${Number(s.caThiId) === Number(selectedCaThiId ?? caThiId) ? "selected" : ""}>${escapeHtml(s.maPhong)} · ${formatDateTime(s.thoiGianBatDau)} - ${formatTime(s.thoiGianKetThuc)}</option>`
    ).join("");
    sessionSelect.disabled = false;

    const effectiveId = Number.isInteger(caThiId) && caThiId > 0 ? caThiId : selectedCaThiId ?? null;
    if (effectiveId && sessions.some(s => Number(s.caThiId) === effectiveId)) {
        selectedCaThiId = effectiveId;
        sessionSelect.value = String(effectiveId);
        await loadAssignmentPanel();
        return;
    }

    selectedCaThiId = null;
    sessionSelect.value = "";
    assignmentPanel.classList.add("hidden");
}

async function loadProctors() {
    try {
        const params = new URLSearchParams();
        if (search.value.trim()) params.set("keyword", search.value.trim());
        if (filterStatus.value) params.set("status", filterStatus.value);
        if (selectedCaThiId) params.set("caThiId", String(selectedCaThiId));
        allProctors = await apiFetch("/proctors?" + params.toString());
        renderProctors();
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="9"><div class="empty"><div class="icon">⚠️</div><div>${error.message}</div></div></td></tr>`;
    }
}

async function loadAssignmentPanel() {
    if (!selectedCaThiId) {
        assignmentPanel.classList.add("hidden");
        return;
    }

    assignmentPanel.classList.remove("hidden");
    const select = document.getElementById("proctorSelect");
    const assignButton = document.getElementById("btnAssign");
    const autoAssignButton = document.getElementById("btnAutoAssign");
    const roleSelect = document.getElementById("roleSelect");
    const assignmentInfo = document.getElementById("assignmentInfo");
    const assignmentList = document.getElementById("assignmentList");

    assignmentInfo.textContent = "Đang tải thông tin phân công...";
    assignmentList.innerHTML = `<div class="empty"><div class="icon">⏳</div><div>Đang tải danh sách giám thị...</div></div>`;
    select.innerHTML = `<option value="">Đang tải danh sách giám thị...</option>`;
    select.disabled = true;
    roleSelect.disabled = true;
    assignButton.disabled = true;
    autoAssignButton.disabled = true;

    try {
        const [summary, available] = await Promise.all([
            apiFetch(`/examsessions/${selectedCaThiId}/proctors`),
            apiFetch(`/proctors?caThiId=${selectedCaThiId}&status=active`)
        ]);
        document.getElementById("assignmentTitle").textContent = `Phân công giám thị: ${summary.maKyThi}`;
        assignmentInfo.textContent = `${formatDateTime(summary.start)} ${formatTime(summary.start)} · ${summary.tenPhong} · Đã phân công ${summary.assignedCount}/${summary.requiredCount}`;
        candidates = available;
        select.innerHTML = candidates.length
            ? candidates.map(p => `<option value="${p.proctorProfileId}">${escapeHtml(p.staffCode)} - ${escapeHtml(p.fullName)}</option>`).join("")
            : `<option value="">Không còn giám thị phù hợp cho ca này</option>`;
        select.disabled = candidates.length === 0;
        roleSelect.disabled = candidates.length === 0;
        assignButton.disabled = candidates.length === 0;
        autoAssignButton.disabled = candidates.length === 0;
        assignmentList.innerHTML = summary.assignments.length
            ? summary.assignments.map(a => `<div style="padding:8px 0; border-bottom:1px solid var(--border);"><strong>${roleLabel(a.role)}</strong>: ${escapeHtml(a.fullName)} <button class="btn-sm btn-del" onclick="unassign(${a.assignmentId})">Hủy</button></div>`).join("")
            : "<div class=\"empty\"><div>Chưa có giám thị</div></div>";
    } catch (error) {
        candidates = [];
        assignmentInfo.textContent = `Không thể tải thông tin phân công: ${error.message}`;
        assignmentList.innerHTML = "<div class=\"empty\"><div>Chưa tải được danh sách phân công.</div></div>";
        select.innerHTML = `<option value="">Không tải được danh sách giám thị</option>`;
    }
}

async function assignSelected() {
    if (!selectedCaThiId) {
        toast("Vui lòng chọn ca thi trước khi phân công.", "error");
        return;
    }
    const profileId = Number(document.getElementById("proctorSelect").value);
    if (!profileId) return;
    try {
        await apiFetch("/proctors/assign", { method: "POST", body: JSON.stringify({ caThiId: selectedCaThiId, proctorProfileId: profileId, role: document.getElementById("roleSelect").value }) });
        toast("Đã phân công giám thị", "success");
        await loadAssignmentPanel();
        await loadProctors();
    } catch (error) { toast(error.message, "error"); }
}

async function autoAssign() {
    if (!selectedCaThiId) {
        toast("Vui lòng chọn ca thi trước khi phân công tự động.", "error");
        return;
    }
    try {
        await apiFetch(`/proctors/sessions/${selectedCaThiId}/auto-assign`, { method: "POST" });
        toast("Đã phân công tự động", "success");
        await loadAssignmentPanel();
        await loadProctors();
    } catch (error) { toast(error.message, "error"); }
}

window.unassign = async function (assignmentId) {
    if (!confirm("Bạn có chắc muốn hủy phân công này không?")) return;
    try {
        await apiFetch(`/proctors/assignments/${assignmentId}`, { method: "DELETE" });
        toast("Đã hủy phân công", "success");
        await loadAssignmentPanel();
        await loadProctors();
    } catch (error) { toast(error.message, "error"); }
};

function renderProctors() {
    document.getElementById("countInfo").textContent = `Hiển thị ${allProctors.length} giám thị`;
    if (!allProctors.length) {
        tbody.innerHTML = `<tr><td colspan="8"><div class="empty"><div class="icon">👤</div><div>Chưa có giám thị phù hợp</div></div></td></tr>`;
        return;
    }
    tbody.innerHTML = allProctors.map(proctor => `
        <tr>
            <td class="checkbox-col"><input class="proctor-row-select" type="checkbox" data-id="${proctor.proctorProfileId}" ${selectedProctorIds.has(proctor.proctorProfileId) ? "checked" : ""}></td>
            <td><strong>${escapeHtml(proctor.staffCode)}</strong></td>
            <td>${escapeHtml(proctor.fullName)}</td>
            <td>${escapeHtml(proctor.department || "—")}</td>
            <td>${escapeHtml(proctor.email || "—")}</td>
            <td>${escapeHtml(proctor.phone || "—")}</td>
            <td><span class="badge ${proctor.isActive ? "Da_xep" : "Huy"}">${proctor.isActive ? "Hoạt động" : "Không hoạt động"}</span></td>
            <td>${proctor.assignmentCount}</td>
            <td>
                <div class="actions-cell">
                    <button class="btn-sm btn-edit" onclick="editProctor(${proctor.proctorProfileId})">✏️ Sửa</button>
                    <button class="btn-sm btn-del" onclick="deleteProctor(${proctor.proctorProfileId})">🗑️ Xóa</button>
                    <button class="btn-sm btn-view" onclick="showSchedule(${proctor.proctorProfileId}, '${escapeJs(proctor.fullName)}')">📅 Lịch</button>
                </div>
            </td>
        </tr>`).join("");
    syncSelectedRows();
}

window.deleteProctor = async function (proctorProfileId) {
    const proctor = allProctors.find(p => p.proctorProfileId === proctorProfileId);
    const label = proctor ? `${proctor.staffCode} - ${proctor.fullName}` : `giám thị #${proctorProfileId}`;
    if (!confirm(`Bạn có chắc muốn xóa ${label}? Tất cả lịch phân công và dữ liệu liên quan của giám thị này sẽ bị xóa khỏi cơ sở dữ liệu.`)) return;
    try {
        await apiFetch(`/proctors/${proctorProfileId}`, { method: "DELETE" });
        toast("Đã xóa giám thị", "success");
        await loadProctors();
    } catch (error) {
        toast("Không thể xóa giám thị: " + error.message, "error");
    }
};

window.editProctor = async function (proctorProfileId) {
    const proctor = allProctors.find(p => p.proctorProfileId === proctorProfileId);
    if (!proctor) return;

    document.getElementById("proctorId").value = proctor.proctorProfileId;
    document.getElementById("proctorStaffCode").value = proctor.staffCode;
    document.getElementById("proctorFullName").value = proctor.fullName;
    document.getElementById("proctorEmail").value = proctor.email || "";
    document.getElementById("proctorPhone").value = proctor.phone || "";
    document.getElementById("proctorDepartment").value = proctor.department || "";
    document.getElementById("proctorIsActive").value = proctor.isActive ? "true" : "false";
    document.getElementById("proctorModalTitle").textContent = "Cập nhật hồ sơ giám thị";
    document.getElementById("saveProctorBtn").textContent = "Cập nhật";
    proctorModal.classList.remove("hidden");
};

function resetProctorForm() {
    proctorForm.reset();
    document.getElementById("proctorId").value = "";
    document.getElementById("proctorModalTitle").textContent = "Thêm giám thị";
    document.getElementById("saveProctorBtn").textContent = "Lưu";
    document.getElementById("proctorIsActive").value = "true";
}

document.getElementById("btnAddProctor").onclick = () => {
    resetProctorForm();
    proctorModal.classList.remove("hidden");
};

document.getElementById("closeProctorModal").onclick = () => {
    proctorModal.classList.add("hidden");
    resetProctorForm();
};

proctorForm.onsubmit = async function (event) {
    event.preventDefault();
    const proctorId = document.getElementById("proctorId").value;
    const body = {
        staffCode: document.getElementById("proctorStaffCode").value.trim(),
        fullName: document.getElementById("proctorFullName").value.trim(),
        email: document.getElementById("proctorEmail").value.trim() || null,
        phone: document.getElementById("proctorPhone").value.trim() || null,
        department: document.getElementById("proctorDepartment").value.trim() || null,
        isActive: document.getElementById("proctorIsActive").value === "true"
    };

    if (!body.staffCode || !body.fullName) {
        toast("Vui lòng nhập mã giám thị và họ tên.", "error");
        return;
    }

    try {
        if (proctorId) {
            await apiFetch(`/proctors/${proctorId}`, {
                method: "PUT",
                body: JSON.stringify(body)
            });
            toast("Đã cập nhật hồ sơ giám thị", "success");
        } else {
            await apiFetch("/proctors", {
                method: "POST",
                body: JSON.stringify({
                    staffCode: body.staffCode,
                    fullName: body.fullName,
                    email: body.email,
                    phone: body.phone,
                    department: body.department
                })
            });
            toast("Đã thêm giám thị mới", "success");
        }

        proctorModal.classList.add("hidden");
        resetProctorForm();
        await loadProctors();
    } catch (error) {
        toast(error.message, "error");
    }
};

window.showSchedule = async function (profileId, fullName) {
    scheduleModal.classList.remove("hidden");
    document.getElementById("scheduleTitle").textContent = `Lịch coi thi: ${fullName}`;
    scheduleBody.innerHTML = `<div class="empty"><div>Đang tải...</div></div>`;
    try {
        const schedule = await apiFetch(`/proctors/${profileId}/schedule`);
        if (!schedule.length) {
            scheduleBody.innerHTML = `<div class="empty"><div class="icon">📅</div><div>Chưa có lịch coi thi</div></div>`;
            return;
        }
        scheduleBody.innerHTML = `<div class="card"><table><thead><tr><th>Kỳ thi</th><th>Thời gian</th><th>Phòng</th><th>Vai trò</th></tr></thead><tbody>${schedule.map(item => `
            <tr><td>${escapeHtml(item.tenKyThi)}</td><td>${formatDateTime(item.start)} ${formatTime(item.start)}</td><td>${escapeHtml(item.tenPhong)}</td><td>${roleLabel(item.role)}</td></tr>`).join("")}</tbody></table></div>`;
    } catch (error) {
        scheduleBody.innerHTML = `<div class="empty"><div>${error.message}</div></div>`;
    }
};

function roleLabel(role) {
    return { Truong_ca: "Trưởng ca", Giam_thi_1: "Giám thị 1", Giam_thi_2: "Giám thị 2" }[role] || role;
}
function formatDateTime(value) { return new Date(value).toLocaleDateString("vi-VN"); }
function formatTime(value) { return new Date(value).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }); }
function escapeHtml(value) { return String(value).replace(/[&<>"']/g, char => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[char])); }
function escapeJs(value) { return String(value).replace(/\\/g, "\\\\").replace(/'/g, "\\'"); }

document.getElementById("btnRefresh").onclick = async () => {
    if (examSelect && sessionSelect && Number.isInteger(kyThiId) && kyThiId > 0) {
        await loadExams();
    }
    selectedProctorIds.clear();
    await Promise.all([loadProctors(), selectedCaThiId ? loadAssignmentPanel() : Promise.resolve()]);
};

deleteSelectedBtn.onclick = async () => {
    if (!selectedProctorIds.size) return;
    const ids = [...selectedProctorIds];
    const selected = allProctors.filter(item => selectedProctorIds.has(item.proctorProfileId));
    const labels = selected.map(item => `${item.staffCode} - ${item.fullName}`).join("\n");
    if (!confirm(`Bạn đang xóa ${selected.length} giám thị đã chọn:\n${labels}\n\nTất cả lịch phân công và dữ liệu liên quan sẽ bị xóa khỏi cơ sở dữ liệu. Tiếp tục?`)) return;
    try {
        for (const id of ids) {
            await apiFetch(`/proctors/${id}`, { method: "DELETE" });
        }
        selectedProctorIds.clear();
        toast(`Đã xóa ${selected.length} giám thị`, "success");
        await loadProctors();
    } catch (error) {
        toast("Không thể xóa các giám thị đã chọn: " + error.message, "error");
    }
};

document.getElementById("btnAssign").onclick = assignSelected;
document.getElementById("btnAutoAssign").onclick = autoAssign;
document.getElementById("closeSchedule").onclick = () => scheduleModal.classList.add("hidden");
search.oninput = loadProctors;
filterStatus.onchange = loadProctors;
selectAll.addEventListener("change", (event) => {
    document.querySelectorAll(".proctor-row-select").forEach(item => item.checked = event.target.checked);
    syncSelectedRows();
});
tbody.addEventListener("change", (event) => {
    if (event.target.matches(".proctor-row-select")) {
        syncSelectedRows();
    }
});
if (examSelect) examSelect.onchange = async () => {
    const examId = Number(examSelect.value) || null;
    if (examId) {
        const url = new URL(location.href);
        url.searchParams.set("kyThiId", String(examId));
        history.replaceState({}, "", url);
        await loadSessionsForExam(examId);
        await loadProctors();
    } else {
        selectedCaThiId = null;
        selectedProctorIds.clear();
        assignmentPanel.classList.add("hidden");
        sessionSelect.innerHTML = `<option value="">Chọn ca thi</option>`;
        history.replaceState({}, "", location.pathname);
        await loadProctors();
    }
};
if (sessionSelect) sessionSelect.onchange = async () => {
    const selected = Number(sessionSelect.value) || null;
    selectedCaThiId = selected;
    const url = new URL(location.href);
    if (selected) {
        url.searchParams.set("caThiId", String(selected));
    } else {
        url.searchParams.delete("caThiId");
    }
    history.replaceState({}, "", url);
    await Promise.all([loadProctors(), loadAssignmentPanel()]);
};

loadProctors();
if (selectedCaThiId) loadAssignmentPanel();
if (examSelect && sessionSelect) {
    loadExams().catch(error => toast("Không thể tải kỳ thi: " + error.message, "error"));
}