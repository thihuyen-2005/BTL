const API_BASE = "http://localhost:5000/api";

function getToken()    { return localStorage.getItem("accessToken"); }
function getRefresh()  { return localStorage.getItem("refreshToken"); }
function setTokens(a, r) { localStorage.setItem("accessToken", a); if (r) localStorage.setItem("refreshToken", r); }
function clearTokens()  { localStorage.clear(); }

async function apiFetch(path, options = {}) {
  const headers = {
    "Content-Type": "application/json",
    ...(options.headers || {})
  };
  const token = getToken();
  if (token) headers["Authorization"] = "Bearer " + token;

  let res = await fetch(API_BASE + path, { ...options, headers });

  // Tự động refresh khi 401
  if (res.status === 401 && getRefresh()) {
    const r = await fetch(`${API_BASE}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: getRefresh() })
    });
    if (r.ok) {
      const data = await r.json();
      setTokens(data.accessToken, data.refreshToken);
      headers["Authorization"] = "Bearer " + data.accessToken;
      res = await fetch(API_BASE + path, { ...options, headers });
    } else {
      clearTokens();
      location.href = "index.html";
      return;
    }
  }

  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(err.error || "Lỗi không xác định");
  }
  return res.status === 204 ? null : res.json();
}