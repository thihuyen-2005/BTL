if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Giám thị", "Danh sách cán bộ coi thi");

const tbody = document.querySelector("#tblProctors tbody");
const search = document.getElementById("search");
const filterStatus = document.getElementById("filterStatus");
const scheduleModal = document.getElementById("scheduleModal");
const scheduleBody = document.getElementById("scheduleBody");
const caThiId = Number(new URLSearchParams(location.search).get("caThiId"));
const assignmentPanel = document.getElementById("assignmentPanel");
let candidates = [];
let allProctors = [];

async function loadProctors() {
    try {
        const params = new URLSearchParams();
        if (search.value.trim()) params.set("keyword", search.value.trim());
        if (filterStatus.value) params.set("status", filterStatus.value);
        allProctors = await apiFetch("/proctors?" + params.toString());
        renderProctors();
        if (Number.isInteger(caThiId) && caThiId > 0) await loadAssignmentPanel();
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="7"><div class="empty"><div class="icon">⚠️</div><div>${error.message}</div></div></td></tr>`;
    }
}

async function loadAssignmentPanel() {
    assignmentPanel.classList.remove("hidden");
    const [summary, available] = await Promise.all([
        apiFetch(`/examsessions/${caThiId}/proctors`),
        apiFetch(`/proctors?caThiId=${caThiId}&status=active`)
    ]);
    document.getElementById("assignmentTitle").textContent = `Phân công giám thị: ${summary.maKyThi}`;
    document.getElementById("assignmentInfo").textContent = `${formatDateTime(summary.start)} ${formatTime(summary.start)} - ${formatTime(summary.end)} · ${summary.tenPhong} · Đã phân công ${summary.assignedCount}/${summary.requiredCount}`;
    candidates = available;
    const select = document.getElementById("proctorSelect");
    select.innerHTML = candidates.length
        ? candidates.map(p => `<option value="${p.proctorProfileId}">${escapeHtml(p.staffCode)} - ${escapeHtml(p.fullName)}</option>`).join("")
        : `<option value="">Không còn cán bộ phù hợp</option>`;
    document.getElementById("assignmentList").innerHTML = summary.assignments.length
        ? summary.assignments.map(a => `<div style="padding:8px 0; border-bottom:1px solid var(--border);"><strong>${roleLabel(a.role)}</strong>: ${escapeHtml(a.fullName)} <button class="btn-sm btn-del" onclick="unassign(${a.assignmentId})">Hủy</button></div>`).join("")
        : "<div class=\"empty\"><div>Chưa có giám thị</div></div>";
}

async function assignSelected() {
    const profileId = Number(document.getElementById("proctorSelect").value);
    if (!profileId) return;
    try {
        await apiFetch("/proctors/assign", { method: "POST", body: JSON.stringify({ caThiId, proctorProfileId: profileId, role: document.getElementById("roleSelect").value }) });
        toast("Đã phân công giám thị", "success");
        await loadAssignmentPanel();
        await loadProctors();
    } catch (error) { toast(error.message, "error"); }
}

async function autoAssign() {
    try {
        await apiFetch(`/proctors/sessions/${caThiId}/auto-assign`, { method: "POST" });
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
        tbody.innerHTML = `<tr><td colspan="7"><div class="empty"><div class="icon">👤</div><div>Chưa có giám thị phù hợp</div></div></td></tr>`;
        return;
    }
    tbody.innerHTML = allProctors.map(proctor => `
        <tr>
            <td><strong>${escapeHtml(proctor.staffCode)}</strong></td>
            <td>${escapeHtml(proctor.fullName)}</td>
            <td>${escapeHtml(proctor.department || "—")}</td>
            <td>${escapeHtml(proctor.email || "—")}</td>
            <td><span class="badge ${proctor.isActive ? "Da_xep" : "Huy"}">${proctor.isActive ? "Hoạt động" : "Không hoạt động"}</span></td>
            <td>${proctor.assignmentCount}</td>
            <td><button class="btn-sm btn-view" onclick="showSchedule(${proctor.proctorProfileId}, '${escapeJs(proctor.fullName)}')">📅 Xem lịch</button></td>
        </tr>`).join("");
}

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
            <tr><td>${escapeHtml(item.tenKyThi)}</td><td>${formatDateTime(item.start)} - ${formatTime(item.end)}</td><td>${escapeHtml(item.tenPhong)}</td><td>${roleLabel(item.role)}</td></tr>`).join("")}</tbody></table></div>`;
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

document.getElementById("btnRefresh").onclick = loadProctors;
document.getElementById("btnAssign").onclick = assignSelected;
document.getElementById("btnAutoAssign").onclick = autoAssign;
document.getElementById("closeSchedule").onclick = () => scheduleModal.classList.add("hidden");
search.oninput = loadProctors;
filterStatus.onchange = loadProctors;
loadProctors();