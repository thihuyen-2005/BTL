// ============================================================
// Layout: render sidebar + topbar dùng chung
// ============================================================

const MENU = [
    { section: "Tổng quan" },
    { href: "dashboard.html",       icon: "🏠", label: "Trang chủ",       active: "dashboard",
      roles: ["Admin", "CBKT", "QuanLy", "KeToan", "SinhVien"] },

    { section: "Quản lý" },
    { href: "exams.html",           icon: "📋", label: "Kỳ thi",           active: "exams",
      roles: ["Admin", "CBKT", "QuanLy"] },
    { href: "exams.html",            icon: "🕐", label: "Ca thi",           active: "exam-sessions",
      roles: ["Admin", "CBKT", "QuanLy"] },
    { href: "#",                    icon: "👤", label: "Giám thị",         disabled: true, tag: "Sắp có",
      roles: ["Admin", "CBKT"] },
    { href: "#",                    icon: "🗓️", label: "Xếp lịch tự động", disabled: true, tag: "Sắp có",
      roles: ["Admin", "CBKT"] },

    { section: "Quản trị" },
    { href: "users.html",           icon: "👥", label: "Người dùng",       active: "users",
      roles: ["Admin"] },

    { section: "Tra cứu & Báo cáo" },
    { href: "#",                    icon: "🔍", label: "Tra cứu lịch thi", disabled: true, tag: "Sắp có",
      roles: ["Admin", "CBKT", "QuanLy", "SinhVien"] },
    { href: "#",                    icon: "📊", label: "Báo cáo thống kê", disabled: true, tag: "Sắp có",
      roles: ["Admin", "QuanLy", "KeToan"] },

    { section: "Tài khoản" },
    { href: "profile.html",         icon: "🔑", label: "Đổi mật khẩu",     active: "profile",
      roles: ["Admin", "CBKT", "QuanLy", "KeToan", "SinhVien"] },
];

function renderLayout(pageTitle, pageSubtitle) {
    const currentPage = location.pathname.split("/").pop().replace(".html", "");

    // ⚠️ LẤY THÔNG TIN USER TRƯỚC — để filter menu dùng được
    const fullName = localStorage.getItem("fullName") || "User";
    const role = localStorage.getItem("role") || "—";
    const initial = fullName.trim().charAt(0).toUpperCase();

    // ===== Sidebar — LỌC MENU THEO ROLE =====
    let navHTML = "";
    MENU.forEach(item => {
        // ⚠️ THÊM DÒNG NÀY — ẩn menu nếu role không có quyền
        if (item.roles && !item.roles.includes(role)) return;

        if (item.section) {
            navHTML += `<div class="sidebar-section">${item.section}</div>`;
            return;
        }
        const isActive = item.active === currentPage;
        const cls = `nav-item${isActive ? " active" : ""}${item.disabled ? " disabled" : ""}`;
        const tag = item.tag ? `<span class="tag">${item.tag}</span>` : "";
        navHTML += `
            <a href="${item.href}" class="${cls}">
                <span class="icon">${item.icon}</span>
                <span class="label">${item.label}</span>
                ${tag}
            </a>`;
    });

    const sidebarHTML = `
        <aside class="sidebar" id="sidebar">
            <div class="sidebar-brand" id="sidebarBrand" title="Nhấn để mở/đóng menu">
                <div class="logo">🎓</div>
                <div class="title">
                    Quản lý Lịch thi
                    <small>Chứng chỉ NLS</small>
                </div>
            </div>
            <nav class="sidebar-nav">${navHTML}</nav>
            <div class="sidebar-footer">v1.0 · Nhóm 12</div>
        </aside>`;

    const topbarHTML = `
        <header class="topbar">
            <div style="display:flex; align-items:center; min-width:0;">
                <button class="btn-hamburger" id="btnHamburger" title="Menu">☰</button>
                <div class="page-title">${pageTitle || ""}${pageSubtitle ? `<small>${pageSubtitle}</small>` : ""}</div>
            </div>
            <div class="user-area">
                <div class="user-chip">
                    <div class="avatar">${initial}</div>
                    <div class="user-info">
                        <div class="name">${fullName}</div>
                        <div class="role">${role}</div>
                    </div>
                </div>
                <button class="btn-logout" id="btnLogout">Đăng xuất</button>
            </div>
        </header>`;

    // Chèn vào body — phải có div#app trong HTML
    const app = document.getElementById("app");
    if (!app) {
        console.error("Không tìm thấy #app");
        return;
    }

    const existingMain = app.innerHTML;
    app.className = "app";
    app.innerHTML = `
        ${sidebarHTML}
        <div class="sidebar-overlay" id="sidebarOverlay"></div>
        <div class="main-wrap">
            ${topbarHTML}
            <main class="main-content">${existingMain}</main>
        </div>`;

    // Đồng bộ tên từ database để loại bỏ dữ liệu tên cũ trong localStorage.
    apiFetch("/auth/me").then(user => {
        localStorage.setItem("fullName", user.fullName);
        localStorage.setItem("role", user.role);
        const nameEl = document.querySelector(".user-info .name");
        const roleEl = document.querySelector(".user-info .role");
        if (nameEl) nameEl.textContent = user.fullName;
        if (roleEl) roleEl.textContent = user.role;
        const welcomeEl = document.getElementById("welcomeName");
        if (welcomeEl) welcomeEl.textContent = `Xin chào, ${user.fullName} 👋`;
    }).catch(() => {});

    // ===== Logout =====
    document.getElementById("btnLogout").onclick = async () => {
        try { await apiFetch("/auth/logout", { method: "POST" }); } catch (e) {}
        clearTokens();
        location.href = "index.html";
    };

    // ===== Sidebar toggle =====
    const sidebar = document.getElementById("sidebar");
    const overlay = document.getElementById("sidebarOverlay");
    const btnHam  = document.getElementById("btnHamburger");
    const brand   = document.getElementById("sidebarBrand");

    function toggleSidebar(force) {
        const shouldOpen = force !== undefined ? force : !sidebar.classList.contains("open");
        sidebar.classList.toggle("open", shouldOpen);
        overlay.classList.toggle("show", shouldOpen);
    }

    // Nút ☰ — chỉ hiện trên mobile
    if (btnHam) btnHam.onclick = () => toggleSidebar();

    // Click overlay → đóng
    if (overlay) overlay.onclick = () => toggleSidebar(false);

    // Click logo (brand) → toggle sidebar
    // Desktop (>992px): không làm gì (sidebar luôn mở sẵn)
    // Tablet (768-992px): mở rộng sidebar thành overlay
    // Mobile (<768px): mở sidebar trượt vào
    if (brand) {
        brand.onclick = () => {
            if (window.innerWidth <= 992) {
                toggleSidebar();
            }
        };
    }

    // Đóng sidebar khi click menu (chỉ trên tablet/mobile)
    document.querySelectorAll(".sidebar-nav a:not(.disabled)").forEach(a => {
        a.addEventListener("click", () => {
            if (window.innerWidth <= 992) toggleSidebar(false);
        });
    });

    // Xử lý khi resize
    window.addEventListener("resize", () => {
        // Lên desktop → đảm bảo sidebar không còn class open
        if (window.innerWidth > 992) {
            sidebar.classList.remove("open");
            overlay.classList.remove("show");
        }
    });
}

// ============================================================
// Toast helper
// ============================================================
function toast(message, type = "info") {
    const existing = document.querySelector(".toast");
    if (existing) existing.remove();

    const el = document.createElement("div");
    el.className = "toast " + type;
    el.textContent = message;
    document.body.appendChild(el);
    requestAnimationFrame(() => el.classList.add("show"));
    setTimeout(() => {
        el.classList.remove("show");
        setTimeout(() => el.remove(), 300);
    }, 2600);
}