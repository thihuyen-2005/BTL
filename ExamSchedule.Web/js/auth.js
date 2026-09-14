if (getToken()) location.href = "dashboard.html";

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