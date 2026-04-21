const API_ORIGIN = 'http://localhost:5014';
const API_BASE = `${API_ORIGIN}/api`;

const auth = {
  getToken() {
    return localStorage.getItem('token');
  },
  saveToken(token) {
    localStorage.setItem('token', token);
  },
  saveUser(data) {
    localStorage.setItem('user', JSON.stringify(data));
  },
  getUser() {
    try {
      return JSON.parse(localStorage.getItem('user') || 'null');
    } catch {
      return null;
    }
  },
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  },
  async api(url, options = {}) {
    const token = this.getToken();
    const headers = { ...options.headers };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return fetch(url.startsWith('http') ? url : `${API_BASE}${url}`, { ...options, headers });
  },
  /** Parse API error response for display. Handles { error }, { errors: { field: [msg] } }, ProblemDetails. */
  parseError(data) {
    if (!data) return 'Something went wrong';
    if (data.error) return data.error;
    if (data.errors && typeof data.errors === 'object') {
      const msgs = Object.values(data.errors).flat().filter(Boolean);
      return msgs.length ? msgs.join(' ') : data.title || 'Validation error';
    }
    return data.title || data.detail || JSON.stringify(data);
  },
  /** Init nav: hide Login when logged in, show Logout; redirect to index on logout. Call after DOM ready. */
  initNav() {
    const indexPath = location.pathname.includes('/pages/') ? '../index.html' : 'index.html';
    const user = this.getUser();
    const authLinks = document.getElementById('authLinks');
    const userSpan = document.getElementById('userSpan');
    const navLogout = document.getElementById('navLogout');
    const logoutBtn = navLogout || userSpan?.querySelector('button');

    if (authLinks) authLinks.classList.toggle('hidden', !!user);
    if (userSpan) userSpan.classList.toggle('hidden', !user);
    if (user && logoutBtn) logoutBtn.onclick = () => { this.logout(); location.href = indexPath; };
    const empNav = document.getElementById('empNav');
    const appNav = document.getElementById('appNav');
    if (empNav) empNav.classList.toggle('hidden', !user || user.role !== 'Employee');
    if (appNav) appNav.classList.toggle('hidden', !user || user.role !== 'Applicant');
  }
};
