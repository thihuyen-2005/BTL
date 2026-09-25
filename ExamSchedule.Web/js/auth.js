if (getToken()) location.href = "dashboard.html";

const passwordInput = document.getElementById("password");
const togglePassword = document.getElementById("togglePassword");

togglePassword.onclick = () => {
    const isHidden = passwordInput.type === "password";
    passwordInput.type = isHidden ? "text" : "password";
    togglePassword.textContent = isHidden ? "🙈" : "👁";
    togglePassword.setAttribute("aria-label", isHidden ? "Ẩn mật khẩu" : "Hiện mật khẩu");
    togglePassword.title = isHidden ? "Ẩn mật khẩu" : "Hiện mật khẩu";
};

document.getElementById("formLogin").onsubmit = async (e) => {
    e.preventDefault();
    const errEl = document.getElementById("error");
    errEl.textContent = "";

    try {
        const res = await apiFetch("/auth/login", {
            method: "POST",
            body: JSON.stringify({
                username: document.getElementById("username").value,
                password: document.getElementById("password").value
            })
        });
        setTokens(res.accessToken, res.refreshToken);
        localStorage.setItem("role", res.role);
        localStorage.setItem("fullName", res.fullName);
        location.href = "dashboard.html";
    } catch (err) {
        errEl.textContent = err.message;
    }
};