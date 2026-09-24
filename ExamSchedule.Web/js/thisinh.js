if (!getToken()) location.href = "index.html";

renderLayout("Quản lý Thí sinh", "");

const tbody = document.querySelector("#tblCandidates tbody");
const manualModal = document.getElementById("manualModal");
const importModal = document.getElementById("importModal");
let candidates = [];

function formatDate(value) {
    return value ? new Date(value).toLocaleDateString("vi-VN") : "";
}

async function loadCandidates() {
    const keyword = document.getElementById("search").value.trim();
    try {
        const params = new URLSearchParams({ tuKhoa: keyword,
            lop: document.getElementById("filterLop").value.trim(),
            nganhHoc: document.getElementById("filterNganh").value.trim(),
            khoa: document.getElementById("filterKhoa").value.trim() });
        const list = await apiFetch(`/thisinh?${params}`);
        candidates = list;
        tbody.innerHTML = list.length ? list.map(x => `
            <tr><td><strong>${x.maThiSinh}</strong></td><td>${x.hoTen}</td>
            <td>${formatDate(x.ngaySinh)}</td><td>${x.lop || ""}</td>
            <td>${x.nganhHoc || ""}</td><td>${x.soTien == null ? "Chưa nộp" : Number(x.soTien).toLocaleString("vi-VN")}</td>
            <td>${x.emailCaNhan || ""}</td><td><button class="btn-sm btn-edit" onclick="editCandidate(${x.thiSinhId})">✏️ Sửa</button>
            <button class="btn-sm btn-del" onclick="deleteCandidate(${x.thiSinhId})">🗑️ Xóa</button></td></tr>`).join("") :
            `<tr><td colspan="8"><div class="empty">Chưa có thí sinh</div></td></tr>`;
    } catch (error) {
        tbody.innerHTML = `<tr><td colspan="8"><div class="empty">${error.message}</div></td></tr>`;
    }
}

document.getElementById("btnManual").onclick = () => {
    document.getElementById("candidateForm").reset();
    document.getElementById("candidateId").value = "";
    document.getElementById("manualTitle").textContent = "Thêm thí sinh";
    manualModal.classList.remove("hidden");
};
document.getElementById("btnImport").onclick = () => importModal.classList.remove("hidden");
document.getElementById("btnCancelManual").onclick = () => manualModal.classList.add("hidden");
document.getElementById("btnCancelImport").onclick = () => importModal.classList.add("hidden");
document.querySelectorAll("#search, #filterLop, #filterNganh, #filterKhoa").forEach(input => input.oninput = loadCandidates);
window.editCandidate = id => {
    const candidate = candidates.find(x => x.thiSinhId === id);
    if (!candidate) return;
    const set = (name, value) => document.getElementById(name).value = value || "";
    set("candidateId", candidate.thiSinhId); set("maThiSinh", candidate.maThiSinh); set("hoTen", candidate.hoTen);
    set("ngaySinh", candidate.ngaySinh?.substring(0, 10)); set("gioiTinh", candidate.gioiTinh);
    set("soCccdHoChieu", candidate.soCccdHoChieu); set("soDienThoai", candidate.soDienThoai);
    set("lop", candidate.lop); set("nganhHoc", candidate.nganhHoc); set("khoa", candidate.khoa);
    set("soTien", candidate.soTien == null ? "" : "800000"); set("emailCaNhan", candidate.emailCaNhan);
    document.getElementById("manualTitle").textContent = "Sửa thông tin thí sinh";
    manualModal.classList.remove("hidden");
};
window.deleteCandidate = async id => {
    if (!confirm("Xóa thí sinh này? Thí sinh đã đăng ký thi sẽ không thể xóa.")) return;
    try { await apiFetch(`/thisinh/${id}`, { method: "DELETE" }); toast("Đã xóa thí sinh", "success"); loadCandidates(); }
    catch (error) { toast("Không thể xóa: " + error.message, "error"); }
};
document.getElementById("candidateForm").onsubmit = async event => {
    event.preventDefault();
    const value = id => document.getElementById(id).value.trim();
    try {
        const id = value("candidateId");
        const payload = {
            maThiSinh: value("maThiSinh"), hoTen: value("hoTen"),
            ngaySinh: value("ngaySinh") || null, gioiTinh: value("gioiTinh") || null,
            soCccdHoChieu: value("soCccdHoChieu") || null, soDienThoai: value("soDienThoai") || null,
            lop: value("lop") || null, nganhHoc: value("nganhHoc") || null,
            khoa: value("khoa") || null, soTien: value("soTien") ? Number(value("soTien")) : null,
            emailCaNhan: value("emailCaNhan") || null
        };
        await apiFetch(id ? `/thisinh/${id}` : "/thisinh", { method: id ? "PUT" : "POST", body: JSON.stringify(payload) });
        manualModal.classList.add("hidden");
        event.target.reset();
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