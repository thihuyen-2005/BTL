if (!getToken()) location.href = "index.html";
renderLayout("Quản lý Người dùng", "");

const tbody = document.querySelector("#tblUsers tbody");
const modal = document.getElementById("modal");
const form = document.getElementById("formUser");
const modalPw = document.getElementById("modalPw");
const formReset = document.getElementById("formReset");

let allUsers = [];

// ===== Load danh sách user =====
async function loadUsers() {
    try {
        allUsers = await apiFetch("/users");
        renderUsers();
    } catch (e) {
        tbody.innerHTML = `<tr><td colspan="7"><div class="empty"><div class="icon">⚠️</div><div>${e.message}</div></div></td></tr>`;
    }
}

// ===== Render danh sách + lọc =====
function renderUsers() {
    const kw = document.getElementById("search").value.trim().toLowerCase();
    const roleFilter = document.getElementById("filterRole").value;

    let list = allUsers;

    // Lọc theo vai trò
    if (roleFilter === "CANBO") {
        const canboRoles = ["Admin", "CBKT", "QuanLy"];
        list = list.filter(u => u.roles.some(r => canboRoles.includes(r)));
    } else if (roleFilter === "SinhVien") {
        list = list.filter(u => u.roles.includes("SinhVien"));
    } else if (roleFilter) {
        list = list.filter(u => u.roles.includes(roleFilter));
    }

    // Lọc theo từ khóa
    if (kw) {
        list = list.filter(u =>
            u.username.toLowerCase().includes(kw) ||
            u.fullName.toLowerCase().includes(kw));
    }

    // Update counter
    const countEl = document.getElementById("countInfo");
    if (countEl) {
        countEl.textContent = `Hiển thị ${list.length} / ${allUsers.length} người dùng`;
    }

    // Render
    if (list.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7"><div class="empty"><div class="icon">👥</div><div>Không có user nào</div></div></td></tr>`;
        return;
    }

    tbody.innerHTML = list.map(u => `
        <tr>
            <td>${u.userId}</td>
            <td><strong>${u.username}</strong></td>
            <td>${u.fullName}</td>
            <td>${u.email || "—"}</td>
            <td>${u.roles.map(r => `<span class="badge badge-${r}">${r}</span>`).join(" ")}</td>
            <td>
                <span class="badge ${u.isActive ? 'Da_xep' : 'Huy'}">
                    ${u.isActive ? 'Hoạt động' : 'Đã khóa'}
                </span>
            </td>
            <td>
                <div class="actions-cell">
                    <button class="btn-sm btn-edit" onclick="resetPw(${u.userId}, '${u.username}', '${u.roles[0]}')">🔑 Reset PW</button>
                    <button class="btn-sm btn-view" onclick="toggleActive(${u.userId})">${u.isActive ? '🚫 Khóa' : '✅ Mở'}</button>
                    <button class="btn-sm btn-del" onclick="deleteUser(${u.userId}, '${u.username}')">🗑️ Xóa</button>
                </div>
            </td>
        </tr>`).join("");
}

// ===== Đổi nút "Thêm" theo dropdown =====
function updateAddButton() {
    const role = document.getElementById("filterRole").value;
    const btnCanBo = document.getElementById("btnAddCanBo");
    const btnSV = document.getElementById("btnAddSV");

    if (role === "SinhVien") {
        btnCanBo.style.display = "none";
        btnSV.style.display = "inline-flex";
    } else {
        btnCanBo.style.display = "inline-flex";
        btnSV.style.display = "none";
    }
}

// ===== Nút "Thêm cán bộ" =====
document.getElementById("btnAddCanBo").onclick = () => {
    form.reset();
    document.getElementById("roleName").value = "CBKT";
    document.getElementById("modalTitle").textContent = "Tạo tài khoản cán bộ";
    modal.classList.remove("hidden");
};

// ===== Nút "Thêm sinh viên" =====
document.getElementById("btnAddSV").onclick = () => {
    form.reset();
    document.getElementById("roleName").value = "SinhVien";
    document.getElementById("modalTitle").textContent = "Tạo tài khoản sinh viên";
    modal.classList.remove("hidden");
};

// ===== Gọi lần đầu =====
updateAddButton();

// ===== Nút Hủy modal =====
document.getElementById("btnCancel").onclick = () => modal.classList.add("hidden");
document.getElementById("btnCancelPw").onclick = () => modalPw.classList.add("hidden");

// ===== Submit form tạo user =====
form.onsubmit = async (e) => {
    e.preventDefault();
    const body = {
        username: document.getElementById("username").value.trim(),
        password: document.getElementById("password").value,
        fullName: document.getElementById("fullName").value.trim(),
        email: document.getElementById("email").value.trim() || null,
        roleName: document.getElementById("roleName").value
    };
    try {
        await apiFetch("/users", { method: "POST", body: JSON.stringify(body) });
        toast("Đã tạo tài khoản!", "success");
        modal.classList.add("hidden");
        loadUsers();
    } catch (err) {
        toast(err.message, "error");
    }
};

// ===== Mở modal reset password =====
window.resetPw = (id, username, role) => {
    let defaultPw;
    if (role === "SinhVien") {
        defaultPw = `${username}@`;
    } else {
        defaultPw = `${username}@2026`;
    }

    document.getElementById("resetUserId").value = id;
    document.getElementById("resetPw").value = defaultPw;
    modalPw.classList.remove("hidden");
};

// ===== Submit form reset password =====
formReset.onsubmit = async (e) => {
    e.preventDefault();
    const id = document.getElementById("resetUserId").value;
    const pw = document.getElementById("resetPw").value;
    try {
        await apiFetch(`/users/${id}/reset-password`, {
            method: "POST",
            body: JSON.stringify({ newPassword: pw })
        });
        toast("Đã reset password!", "success");
        modalPw.classList.add("hidden");
    } catch (err) {
        toast(err.message, "error");
    }
};

// ===== Đổi trạng thái hoạt động/khóa =====
window.toggleActive = async (id) => {
    try {
        await apiFetch(`/users/${id}/toggle-active`, { method: "PUT" });
        toast("Đã đổi trạng thái!", "success");
        loadUsers();
    } catch (err) {
        toast(err.message, "error");
    }
};

// ===== Xóa user =====
window.deleteUser = async (id, username) => {
    if (!confirm(`Xóa tài khoản "${username}"?`)) return;
    try {
        await apiFetch(`/users/${id}`, { method: "DELETE" });
        toast("Đã xóa!", "success");
        loadUsers();
    } catch (err) {
        toast(err.message, "error");
    }
};

// ===== Listeners cho search + filter =====
document.getElementById("search").oninput = renderUsers;
document.getElementById("filterRole").onchange = () => {
    updateAddButton();
    renderUsers();
};

// ===== Chạy lần đầu =====
loadUsers();