if (!getToken()) location.href = "index.html";

renderLayout("Xếp lịch thủ công", "Chọn kỳ thi, ca thi và thí sinh để xếp lịch");

const examSelect = document.getElementById("examSelect");
const sessionSelect = document.getElementById("sessionSelect");
const sessionSummary = document.getElementById("sessionSummary");
const tableBody = document.querySelector("#manualScheduleTable tbody");
const selectedTableBody = document.querySelector("#selectedTable tbody");
const candidateSearch = document.getElementById("candidateSearch");
const statusFilter = document.getElementById("statusFilter");
const registeredTableBody = document.querySelector("#registeredTable tbody");
const selectAllRows = document.getElementById("selectAllRows");

let exams = [];
let sessions = [];
let candidates = [];
let selectedIds = new Set();

function parseSelectedIdsFromUrl() {
    return [];
}

function extractItems(payload) {
    if (Array.isArray(payload)) return payload;
    if (payload && Array.isArray(payload.items)) return payload.items;
    if (payload && Array.isArray(payload.data)) return payload.data;
    return [];
}

function formatMoney(value) {
    if (value == null || value === "") return "Chưa nộp";
    return Number(value).toLocaleString("vi-VN") + " đ";
}

function formatDateTime(iso) {
    if (!iso) return "";
    const d = new Date(iso);
    const pad = (n) => String(n).padStart(2, "0");
    return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function badgeClassForStatus(candidate) {
    if (candidate.canSchedule) return "badge badge-success";
    if (candidate.caThiId != null) return "badge badge-info";
    if (candidate.soTien == null || Number(candidate.soTien) < 800) return "badge badge-warning";
    return "badge badge-muted";
}

function statusText(candidate) {
    if (candidate.caThiId != null) return "Đã có ca thi";
    if (candidate.canSchedule) return "Sẵn sàng xếp";
    if (candidate.soTien == null || Number(candidate.soTien) < 800) return "Chưa nộp";
    return candidate.eligibilityReason || "Không đủ điều kiện";
}

function getSelectedCandidates() {
    return candidates.filter((item) => selectedIds.has(item.thiSinhId));
}

function getSelectedExam() {
    const examId = Number(examSelect.value);
    return exams.find((exam) => Number(exam.kyThiId) === examId) || null;
}

function getCurrentSessionAssignedCount(selectedSession) {
    if (!selectedSession) return 0;

    const sessionId = Number(selectedSession.caThiId ?? 0);
    const fromApi = Number(selectedSession.daXep ?? selectedSession.DaXep ?? 0);
    const fromRegisteredList = candidates.filter((candidate) => Number(candidate.caThiId) === sessionId && candidate.caThiId != null).length;

    return fromApi > 0 ? fromApi : fromRegisteredList;
}

function renderSessionSummary() {
    const selectedExam = getSelectedExam();
    const selectedSession = sessions.find((item) => String(item.caThiId) === sessionSelect.value);
    if (!selectedSession) {
        sessionSummary.innerHTML = `
            <div class="small-label">Kỳ thi</div>
            <div class="session-name">${selectedExam ? (selectedExam.tenKyThi || selectedExam.maKyThi || "Kỳ thi") : "Chưa chọn kỳ thi"}</div>
            <div class="small-label mt-8">Ca thi</div>
            <div>Chưa chọn ca thi.</div>
        `;
        sessionSummary.classList.add("empty");
        return;
    }

    sessionSummary.classList.remove("empty");
    const sucChua = Number(selectedSession.sucChua ?? 0);
    const daXep = getCurrentSessionAssignedCount(selectedSession);
    const conLai = Math.max(0, sucChua - daXep);
    const chuaXep = conLai;

    sessionSummary.innerHTML = `
        <div class="small-label">Kỳ thi</div>
        <div class="session-name">${selectedExam ? (selectedExam.tenKyThi || selectedExam.maKyThi || "Kỳ thi") : "Chưa chọn"}</div>
        <div class="small-label mt-8">Ca thi đang chọn</div>
        <div class="session-name">${selectedSession.maPhong || "Phòng thi"}</div>
        <div>${formatDateTime(selectedSession.thoiGianBatDau)} - ${formatDateTime(selectedSession.thoiGianKetThuc)}</div>
        <div class="session-meta-row"><strong>Sức chứa:</strong> ${sucChua}</div>
        <div class="session-meta-row"><strong>Đã xếp:</strong> ${daXep}</div>
        <div class="session-meta-row"><strong>Còn trống:</strong> ${conLai}</div>
        <div class="session-meta-row"><strong>Chưa xếp:</strong> ${chuaXep}</div>
    `;
}

function renderSelectedSummary() {
    // Bảng "Thông tin đã chọn để xếp" đã được bỏ khỏi giao diện.
    // Giữ hàm này để không phá vỡ các handler cũ, nhưng không render ra màn hình.
    if (selectedTableBody) {
        selectedTableBody.innerHTML = `<tr><td colspan="9" class="empty-cell">Chưa có thí sinh nào được chọn.</td></tr>`;
    }
    if (registeredTableBody) {
        renderRegisteredTable();
    }
}

function renderRegisteredTable() {
    const examId = Number(examSelect.value);
    const sessionId = Number(sessionSelect.value);
    const summaryHeader = document.querySelector(".summary-header h3");
    const summaryCount = document.getElementById("registeredCountBadge");

    if (!examId || !sessionId) {
        registeredTableBody.innerHTML = `<tr><td colspan="6" class="empty-cell">Chưa có thí sinh nào được đăng ký vào ca thi này.</td></tr>`;
        if (summaryHeader) summaryHeader.textContent = "Thí sinh đã đăng ký của ca hiện tại";
        if (summaryCount) summaryCount.textContent = "0";
        return;
    }

    const registered = candidates.filter((candidate) => Number(candidate.caThiId) === sessionId && candidate.caThiId != null);
    if (summaryHeader) summaryHeader.textContent = `Thí sinh đã đăng ký của ca hiện tại`;
    if (summaryCount) summaryCount.textContent = String(registered.length);

    if (!registered.length) {
        registeredTableBody.innerHTML = `<tr><td colspan="6" class="empty-cell">Ca thi đang chọn chưa có thí sinh nào được xếp lịch.</td></tr>`;
        return;
    }

    registeredTableBody.innerHTML = registered.map((candidate) => `
        <tr>
            <td>${candidate.maThiSinh || "—"}</td>
            <td>${candidate.hoTen || "—"}</td>
            <td>${candidate.lop || "—"}</td>
            <td>${candidate.nganhHoc || "—"}</td>
            <td>${formatMoney(candidate.soTien)}</td>
            <td><span class="${badgeClassForStatus(candidate)}">${statusText(candidate)}</span></td>
        </tr>
    `).join("");
}

function renderTable() {
    const searchText = candidateSearch.value.trim().toLowerCase();
    const selectedStatus = statusFilter.value;

    let filtered = candidates.filter((candidate) => candidate.caThiId == null);

    filtered = filtered.filter((candidate) => {
        const haystack = [candidate.maThiSinh, candidate.hoTen, candidate.lop, candidate.khoa, candidate.nganhHoc].join(" ").toLowerCase();
        const matchesText = !searchText || haystack.includes(searchText);
        let matchesStatus = true;
        if (selectedStatus === "eligible") matchesStatus = candidate.canSchedule;
        if (selectedStatus === "already-scheduled") matchesStatus = false;
        if (selectedStatus === "unpaid") matchesStatus = candidate.soTien == null || Number(candidate.soTien) < 800;
        return matchesText && matchesStatus;
    });

    if (!filtered.length) {
        tableBody.innerHTML = `<tr><td colspan="9" class="empty-cell">Chưa có thí sinh nào được xếp lịch trong ca thi này.</td></tr>`;
        selectAllRows.checked = false;
        return;
    }

    const html = filtered.map((candidate) => {
        const checked = selectedIds.has(candidate.thiSinhId) ? "checked" : "";
        const disabled = !candidate.canSchedule ? "disabled" : "";
        return `
            <tr>
                <td class="checkbox-col"><input type="checkbox" class="candidate-row-select" data-id="${candidate.thiSinhId}" ${checked} ${disabled}></td>
                <td>${candidate.maThiSinh || "—"}</td>
                <td>${candidate.hoTen || "—"}</td>
                <td>${candidate.lop || "—"}</td>
                <td>${candidate.nganhHoc || "—"}</td>
                <td>${formatMoney(candidate.soTien)}</td>
                <td class="sticky-status"><span class="${badgeClassForStatus(candidate)}">${statusText(candidate)}</span></td>
                <td class="sticky-session">Chưa có ca</td>
                <td class="sticky-action">
                    <div class="actions-cell compact-row">
                        <button class="btn btn-small btn-primary" type="button" data-assign-id="${candidate.thiSinhId}" ${disabled}>Xếp</button>
                        <button class="btn btn-small btn-del" type="button" data-remove-id="${candidate.thiSinhId}" disabled>Bỏ xếp</button>
                    </div>
                </td>
            </tr>
        `;
    }).join("");

    tableBody.innerHTML = html;

    const selectable = filtered.filter((candidate) => candidate.canSchedule || candidate.caThiId != null);
    selectAllRows.checked = selectable.length > 0 && selectable.every((candidate) => selectedIds.has(candidate.thiSinhId));

    tableBody.querySelectorAll(".candidate-row-select").forEach((input) => {
        input.addEventListener("change", () => {
            const id = Number(input.dataset.id);
            if (input.checked) selectedIds.add(id); else selectedIds.delete(id);
            renderSelectedSummary();
            renderTable();
        });
    });

    tableBody.querySelectorAll("[data-assign-id]").forEach((button) => {
        button.addEventListener("click", async () => {
            const thiSinhId = Number(button.dataset.assignId);
            if (!thiSinhId) return;
            await assignSelectedCandidates([thiSinhId]);
        });
    });

    tableBody.querySelectorAll("[data-remove-id]").forEach((button) => {
        button.addEventListener("click", async () => {
            const thiSinhId = Number(button.dataset.removeId);
            if (!thiSinhId) return;
            await removeScheduleForCandidate(thiSinhId);
        });
    });
}

async function loadExams() {
    try {
        const payload = await apiFetch("/exams?page=1&limit=200");
        exams = extractItems(payload);
        examSelect.innerHTML = '<option value="">Chọn kỳ thi</option>' + exams.map((exam) => `<option value="${exam.kyThiId}">${exam.tenKyThi || exam.maKyThi || `Kỳ thi #${exam.kyThiId}`}</option>`).join("");

        const initialExamId = new URLSearchParams(location.search).get("kyThiId");
        const initialCaThiId = new URLSearchParams(location.search).get("caThiId");
        if (initialExamId) examSelect.value = String(initialExamId);

        if (examSelect.value) {
            await loadSessionsForExam(Number(examSelect.value));
            if (initialCaThiId && sessionSelect.querySelector(`option[value="${initialCaThiId}"]`)) {
                sessionSelect.value = String(initialCaThiId);
                renderSessionSummary();
            }
        } else {
            sessionSelect.innerHTML = '<option value="">Chọn kỳ thi trước</option>';
            sessions = [];
            renderSessionSummary();
            candidates = [];
            renderTable();
            renderSelectedSummary();
        }
    } catch (error) {
        examSelect.innerHTML = '<option value="">Không thể tải kỳ thi</option>';
        toast(error.message || "Không thể tải danh sách kỳ thi.", "error");
    }
}

async function loadSessionsForExam(examId) {
    if (!examId) {
        sessionSelect.innerHTML = '<option value="">Chọn kỳ thi trước</option>';
        sessions = [];
        renderSessionSummary();
        return;
    }

    try {
        const payload = await apiFetch(`/exam-sessions?kyThiId=${examId}`);
        sessions = extractItems(payload);

        const currentSelectedValue = sessionSelect.value;
        const currentSessionId = Number(currentSelectedValue || 0);
        const preferredSessionId = currentSessionId && sessions.some((item) => Number(item.caThiId) === currentSessionId)
            ? currentSessionId
            : (sessions.length ? Number(sessions[0].caThiId) : 0);

        if (preferredSessionId) {
            const preferredSession = sessions.find((item) => Number(item.caThiId) === preferredSessionId) || sessions[0];
            const otherSessions = sessions.filter((item) => Number(item.caThiId) !== Number(preferredSession.caThiId));
            sessions = [preferredSession, ...otherSessions];
        }

        sessionSelect.innerHTML = '<option value="">Chọn ca thi</option>' + sessions.map((session) => `<option value="${session.caThiId}">${session.maPhong || "Phòng thi"} • ${formatDateTime(session.thoiGianBatDau)} - ${formatDateTime(session.thoiGianKetThuc)}</option>`).join("");
        if (preferredSessionId) {
            sessionSelect.value = String(preferredSessionId);
        }

        renderSessionSummary();
        await loadCandidates();
    } catch (error) {
        sessionSelect.innerHTML = '<option value="">Không thể tải ca thi</option>';
        sessions = [];
        renderSessionSummary();
        candidates = [];
        renderTable();
        renderSelectedSummary();
        toast(error.message || "Không thể tải danh sách ca thi.", "error");
    }
}

async function loadCandidates() {
    const examId = Number(examSelect.value);
    if (!examId) {
        candidates = [];
        renderTable();
        renderSelectedSummary();
        return;
    }

    try {
        const query = new URLSearchParams({ kyThiId: String(examId) });
        const payload = await apiFetch(`/thisinh/schedule-candidates?${query.toString()}`);
        candidates = extractItems(payload);
        selectedIds.clear();

        const legalIds = new Set(candidates.map((candidate) => candidate.thiSinhId));
        [...selectedIds].forEach((id) => {
            if (!legalIds.has(id)) selectedIds.delete(id);
        });
        renderRegisteredTable();
    } catch (error) {
        candidates = [];
        toast(error.message || "Không thể tải danh sách thí sinh cho kỳ thi này.", "error");
    }

    renderTable();
    renderSelectedSummary();
}

async function assignSelectedCandidates(idsOverride) {
    const examId = Number(examSelect.value);
    const sessionId = Number(sessionSelect.value);
    const ids = idsOverride && idsOverride.length ? idsOverride : [...selectedIds];

    if (!examId) {
        toast("Vui lòng chọn kỳ thi trước khi xếp lịch.", "error");
        return;
    }

    if (!sessionId) {
        toast("Vui lòng chọn ca thi để xếp lịch.", "error");
        return;
    }

    if (!ids.length) {
        toast("Chưa có thí sinh nào được chọn.", "error");
        return;
    }

    const confirmed = window.confirm(`Bạn có chắc muốn xếp ${ids.length} thí sinh vào ca thi đang chọn?`);
    if (!confirmed) return;

    try {
        const result = await apiFetch("/thisinh/manual-schedule", {
            method: "POST",
            body: JSON.stringify({ kyThiId: examId, caThiId: sessionId, thiSinhIds: ids })
        });
        selectedIds.clear();
        toast(`Đã xếp ${result.count ?? ids.length} thí sinh vào ca thi.`, "success");
        await loadCandidates();
        renderSelectedSummary();
    } catch (error) {
        toast(error.message || "Không thể xếp lịch thủ công.", "error");
    }
}

async function removeScheduleForCandidate(thiSinhId) {
    const examId = Number(examSelect.value);
    if (!examId) return;

    try {
        const result = await apiFetch(`/thisinh/manual-schedule?kyThiId=${examId}&thiSinhId=${thiSinhId}`, { method: "DELETE" });
        selectedIds.delete(thiSinhId);
        toast(`Đã bỏ xếp lịch cho thí sinh #${thiSinhId}.`, "success");
        await loadCandidates();
        renderSelectedSummary();
    } catch (error) {
        toast(error.message || "Không thể bỏ xếp lịch.", "error");
    }
}

async function runAutoSchedule() {
    const examId = Number(examSelect.value);
    if (!examId) {
        toast("Vui lòng chọn kỳ thi trước khi xếp tự động.", "error");
        return;
    }

    try {
        const result = await apiFetch(`/thisinh/ky-thi/${examId}/xep-lich`, { method: "POST" });
        toast(`Đã xếp tự động ${result.daXep ?? 0} thí sinh trong kỳ thi này.`, "success");
        selectedIds.clear();
        await loadCandidates();
        renderSelectedSummary();
    } catch (error) {
        toast(error.message || "Không thể xếp tự động.", "error");
    }
}

function syncSelectionState() {
    const tableBoxes = tableBody.querySelectorAll(".candidate-row-select");
    const selectable = [...tableBoxes].filter((input) => !input.disabled);
    selectAllRows.checked = selectable.length > 0 && selectable.every((input) => input.checked || input.disabled);
}

examSelect.addEventListener("change", async () => {
    selectedIds.clear();
    const examId = Number(examSelect.value);
    if (examId) {
        await loadSessionsForExam(examId);
    } else {
        sessionSelect.innerHTML = '<option value="">Chọn kỳ thi trước</option>';
        sessions = [];
        renderSessionSummary();
        candidates = [];
        renderTable();
        renderSelectedSummary();
    }
});

sessionSelect.addEventListener("change", async () => {
    renderSessionSummary();
    renderSelectedSummary();
    renderRegisteredTable();
    await loadCandidates();
    const sessionId = Number(sessionSelect.value || 0);
    if (sessionId) {
        const current = sessions.find((item) => Number(item.caThiId) === sessionId);
        if (current) {
            sessions = [current, ...sessions.filter((item) => Number(item.caThiId) !== sessionId)];
            sessionSelect.innerHTML = '<option value="">Chọn ca thi</option>' + sessions.map((session) => `<option value="${session.caThiId}">${session.maPhong || "Phòng thi"} • ${formatDateTime(session.thoiGianBatDau)} - ${formatDateTime(session.thoiGianKetThuc)}</option>`).join("");
            sessionSelect.value = String(sessionId);
        }
    }
    renderSessionSummary();
    renderRegisteredTable();
});

selectAllRows.addEventListener("change", () => {
    const rows = [...tableBody.querySelectorAll(".candidate-row-select")];
    rows.forEach((checkbox) => {
        if (checkbox.disabled) return;
        checkbox.checked = selectAllRows.checked;
        const id = Number(checkbox.dataset.id);
        if (checkbox.checked) selectedIds.add(id); else selectedIds.delete(id);
    });
    renderSelectedSummary();
    renderTable();
});

candidateSearch.addEventListener("input", () => renderTable());
statusFilter.addEventListener("change", () => renderTable());

document.getElementById("assignSelectedBtn").addEventListener("click", () => assignSelectedCandidates([...selectedIds]));
document.getElementById("clearSelectionBtn").addEventListener("click", () => {
    selectedIds.clear();
    renderSelectedSummary();
    renderTable();
});
document.getElementById("refreshBtn").addEventListener("click", async () => {
    selectedIds.clear();
    await loadExams();
    renderSelectedSummary();
});
document.getElementById("autoScheduleBtn").addEventListener("click", runAutoSchedule);

selectedIds.clear();
loadExams();
