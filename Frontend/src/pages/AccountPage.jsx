import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client.js";
import { Notice, StatCard, balanceOf, formatCurrency, userIdOf } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";

export default function AccountPage({ profile, edit, merchant, pin }) {
  const auth = useAuth();
  const { user, role } = auth;
  const [form, setForm] = useState({
    email: user?.user_email || user?.email || "",
    phone: user?.user_phone_number || user?.phone || "",
    currentPassword: "",
    newPassword: "",
    confirmPassword: ""
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [pinForm, setPinForm] = useState({
    currentPin: "",
    newPin: "",
    confirmPin: ""
  });
  const [showPin, setShowPin] = useState(false);
  const [pinSaving, setPinSaving] = useState(false);
  const [pinError, setPinError] = useState("");
  const [pinSuccess, setPinSuccess] = useState("");

  const baseEmail = useMemo(() => user?.user_email || user?.email || "", [user]);
  const basePhone = useMemo(() => user?.user_phone_number || user?.phone || "", [user]);

  function validateEmail(value) {
    const v = String(value || "").trim();
    if (!v) return "Email is required.";
    return /^\S+@\S+\.\S+$/.test(v) ? "" : "Invalid email format.";
  }

  function validatePhone(value) {
    const v = String(value || "").trim();
    return /^\+?[0-9]{9,15}$/.test(v) ? "" : "Invalid phone number.";
  }

  async function handleUpdateProfile(event) {
    event.preventDefault();
    setError("");
    setSuccess("");

    const email = form.email.trim();
    const phone = form.phone.trim();
    const currentPassword = form.currentPassword.trim();
    const newPassword = form.newPassword.trim();
    const confirmPassword = form.confirmPassword.trim();

    const emailError = validateEmail(email);
    if (emailError) {
      setError(emailError);
      return;
    }

    const phoneError = validatePhone(phone);
    if (phoneError) {
      setError(phoneError);
      return;
    }

    if (!currentPassword) {
      setError("Current password is required to verify identity.");
      return;
    }

    if (newPassword && newPassword.length < 6) {
      setError("New password must be at least 6 characters.");
      return;
    }

    if (newPassword && confirmPassword !== newPassword) {
      setError("New password and confirm password do not match.");
      return;
    }

    setSaving(true);
    try {
      await api.login({
        email: baseEmail || null,
        phone: basePhone || null,
        password: currentPassword
      });

      const uid = userIdOf(user);
      if (!uid) {
        throw new Error("User ID is missing.");
      }

      const emailChanged = email !== (baseEmail || "");
      const phoneChanged = phone !== (basePhone || "");

      if (emailChanged || phoneChanged) {
        await api.updateUser(uid, {
          user_email: email,
          user_phone_number: phone
        });
      }

      if (newPassword) {
        setError("Password change API is not available yet. Contact/profile info was saved.");
      } else {
        setSuccess("Profile updated successfully.");
      }

      setForm((prev) => ({
        ...prev,
        currentPassword: "",
        newPassword: "",
        confirmPassword: ""
      }));
      await auth.refreshMe();
    } catch (err) {
      setError(err.message || "Failed to update profile.");
    } finally {
      setSaving(false);
    }
  }

  function normalizePin(value) {
    return String(value || "").replace(/\D/g, "").slice(0, 6);
  }

  async function handleChangePin(event) {
    event.preventDefault();
    setPinError("");
    setPinSuccess("");

    const currentPin = normalizePin(pinForm.currentPin);
    const newPin = normalizePin(pinForm.newPin);
    const confirmPin = normalizePin(pinForm.confirmPin);

    if (currentPin.length !== 6) {
      setPinError("Current PIN must be 6 digits.");
      return;
    }
    if (newPin.length !== 6) {
      setPinError("New PIN must be 6 digits.");
      return;
    }
    if (confirmPin !== newPin) {
      setPinError("PINs do not match.");
      return;
    }

    setPinSaving(true);
    try {
      await api.changePasscode({
        currentPasscode: currentPin,
        newPasscode: newPin
      });
      setPinSuccess("Payment PIN updated successfully.");
      setPinForm({ currentPin: "", newPin: "", confirmPin: "" });
    } catch (err) {
      if (/Unauthorized|401|incorrect/i.test(err.message || "")) {
        setPinError("Current PIN is incorrect.");
      } else {
        setPinError(err.message || "Failed to update PIN.");
      }
    } finally {
      setPinSaving(false);
    }
  }

  if (pin) {
    return (
      <section className="page-stack narrow">
        <div className="page-title"><h2>Change Security PIN</h2></div>
        <p style={{ margin: 0, color: "#667085" }}>
          Please enter your current 6-digit PIN and set a new one.
        </p>
        <Notice type="error">{pinError}</Notice>
        <Notice type="success">{pinSuccess}</Notice>

        <form className="panel form-panel" onSubmit={handleChangePin}>
          <label>
            Current PIN
            <input
              type={showPin ? "text" : "password"}
              inputMode="numeric"
              maxLength={6}
              value={pinForm.currentPin}
              onChange={(e) => setPinForm({ ...pinForm, currentPin: normalizePin(e.target.value) })}
            />
          </label>

          <label>
            New PIN
            <input
              type={showPin ? "text" : "password"}
              inputMode="numeric"
              maxLength={6}
              value={pinForm.newPin}
              onChange={(e) => setPinForm({ ...pinForm, newPin: normalizePin(e.target.value) })}
            />
          </label>

          <label>
            Confirm New PIN
            <input
              type={showPin ? "text" : "password"}
              inputMode="numeric"
              maxLength={6}
              value={pinForm.confirmPin}
              onChange={(e) => setPinForm({ ...pinForm, confirmPin: normalizePin(e.target.value) })}
            />
          </label>

          <label style={{ display: "flex", alignItems: "center", gap: 10, fontWeight: 600 }}>
            <input
              type="checkbox"
              checked={showPin}
              onChange={(e) => setShowPin(e.target.checked)}
              style={{ width: 18, minHeight: 18 }}
            />
            Show PIN
          </label>

          <button className="primary-button" type="submit" disabled={pinSaving}>
            {pinSaving ? "Updating..." : "Update PIN"}
          </button>
        </form>
      </section>
    );
  }

  if (edit) {
    return (
      <section className="page-stack narrow">
        <div className="page-title"><h2>Update profile</h2></div>
        <Notice type="error">{error}</Notice>
        <Notice type="success">{success}</Notice>

        <form className="panel form-panel" onSubmit={handleUpdateProfile}>
          <h3>Contact Information</h3>
          <label>
            Email address
            <input
              type="email"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
              autoComplete="email"
            />
          </label>
          <label>
            Phone number
            <input
              type="tel"
              value={form.phone}
              onChange={(e) => setForm({ ...form, phone: e.target.value })}
              autoComplete="tel"
            />
          </label>

          <h3>Security Verification</h3>
          <label>
            Current password (required)
            <input
              type="password"
              value={form.currentPassword}
              onChange={(e) => setForm({ ...form, currentPassword: e.target.value })}
              autoComplete="current-password"
            />
          </label>

          <details>
            <summary style={{ cursor: "pointer", fontWeight: 700 }}>Change password</summary>
            <div style={{ display: "grid", gap: 12, marginTop: 12 }}>
              <label>
                New password
                <input
                  type="password"
                  value={form.newPassword}
                  onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
                  autoComplete="new-password"
                />
              </label>
              <label>
                Confirm new password
                <input
                  type="password"
                  value={form.confirmPassword}
                  onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
                  autoComplete="new-password"
                />
              </label>
              <Notice type="info">Password change endpoint is not available in current backend yet.</Notice>
            </div>
          </details>

          <button className="primary-button" type="submit" disabled={saving}>
            {saving ? "Saving..." : "Save & Update"}
          </button>
        </form>
      </section>
    );
  }

  return (
    <section className="page-stack">
      <div className="page-title"><h2>{profile ? "Profile" : edit ? "Update profile" : merchant ? "Merchant profile" : pin ? "Change Security Pin" : "Account"}</h2></div>
      <div className="stats-grid">
        <StatCard label="Name" value={user?.userName || user?.user_name || "User"} />
        <StatCard label="Role" value={role || "user"} tone="green" />
        <StatCard label="Wallet balance" value={formatCurrency(balanceOf(user))} tone="gold" />
      </div>
      <section className="content-grid two">
        <article className="panel detail-panel">
          {Object.entries(user || {}).slice(0, 12).map(([key, value]) => <div key={key}><span>{key}</span><strong>{String(value ?? "-")}</strong></div>)}
        </article>
        <article className="panel account-actions">
          <Link className="secondary-button" to="/merchant-apply">Apply merchant</Link>
          <Link className="secondary-button" to="/account/profile">View profile</Link>
          <Link className="secondary-button" to="/account/update">Update profile</Link>
          <Link className="secondary-button" to="/account/change-pin">Change Security Pin</Link>
          <Link className="secondary-button" to="/bank/link">Link bank</Link>
        </article>
      </section>
    </section>
  );
}
