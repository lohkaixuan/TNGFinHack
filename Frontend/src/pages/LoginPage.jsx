import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Notice } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";

export default function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ account: "", password: "" });
  const [error, setError] = useState("");
  const [isEmailLogin, setIsEmailLogin] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function submit(event) {
    event.preventDefault();
    setError("");
    setIsSubmitting(true);

    try {
      const result = await auth.login({
        email: isEmailLogin ? form.account : null,
        phone: isEmailLogin ? null : form.account,
        password: form.password
      });
      const role = String(result?.role || "").toLowerCase();
      const fallback = role.includes("admin") ? "/admin" : role.includes("provider") ? "/provider" : "/home";
      navigate(location.state?.from?.pathname || fallback, { replace: true });
    } catch (err) {
      setError(err.message || "Invalid credentials.");
    } finally {
      setIsSubmitting(false);
    }
  }

  const handleToggle = () => {
    setIsEmailLogin(!isEmailLogin);
    setForm({ ...form, account: "" });
  };

  return (
    <main className="auth-page">
      <section className="auth-visual" >
        <span className="auth-logo">
        <h1>UniPay</h1>
        <p>An integrate function of wallet web app.</p>
        </span>
      </section>
      <form className="auth-card" onSubmit={submit}>
        <h2>Login Your UNIPAY</h2>
        <Notice type="error">{error}</Notice>

        <div className="auth-login-switch">
          <span className={`auth-login-mode-label ${isEmailLogin ? "active" : ""}`}>
            Email
          </span>

          <label className="auth-login-toggle">
            <input 
              className="auth-login-toggle-input"
              type="checkbox" 
              checked={!isEmailLogin} 
              onChange={handleToggle} 
            />
            <span className={`auth-login-toggle-track ${isEmailLogin ? "" : "phone"}`}>
              <span className={`auth-login-toggle-thumb ${isEmailLogin ? "" : "phone"}`} />
            </span>
          </label>

          <span className={`auth-login-mode-label ${!isEmailLogin ? "active" : ""}`}>
            Phone
          </span>
        </div>

        <label>
        {isEmailLogin ? "Email Address" : "Phone Number"}
          <input 
            type={isEmailLogin ? "email" : "tel"} 
            placeholder={isEmailLogin ? "example@gmail.com" : "+60 123456789"}
            value={form.account} 
            onChange={(e) => setForm({ ...form, account: e.target.value })} 
            required/>
        </label>
        <label>
          Password
          <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required />
        </label>
        
        {/* 4. USE LOCAL STATE FOR THE BUTTON */}
        <button className="primary-button" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>

        <h4 className="auth-card-subtitle">Start your UNIPAY journey</h4>
        <Link to="/signup">Create an account</Link>
      </form>
    </main>
  );
}