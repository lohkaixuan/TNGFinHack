import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client.js";
import { Notice, walletIdOf } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";

export default function TransferPage({ mode = "transfer", confirm = false }) {
  const navigate = useNavigate();
  const auth = useAuth();
  const [form, setForm] = useState({ to_wallet_id: "", amount: "", description: "", passcode: "" });
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  
  const [recipient, setRecipient] = useState(null);
  const [selectedWalletId, setSelectedWalletId] = useState(null);

  useEffect(() => {
    if (!form.to_wallet_id) {
      setRecipient(null);
      setSelectedWalletId(null);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        let res;
        if (form.to_wallet_id.length === 36 && form.to_wallet_id.includes("-")) {
          res = await api.walletLookup({ wallet_id: form.to_wallet_id });
        } else {
          res = await api.walletLookup({ search: form.to_wallet_id });
        }
        setRecipient(res);
        setSelectedWalletId(res.preferred_wallet_id || res.wallet_id || res.user_wallet_id);
      } catch (err) {
        setRecipient({ error: "Wallet not found" });
        setSelectedWalletId(null);
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [form.to_wallet_id]);

  const [scamAlert, setScamAlert] = useState(null);

  async function submit(event, confirmRisk = false) {
    if (event) event.preventDefault();
    setError("");

    const targetWalletId = selectedWalletId || form.to_wallet_id;
    if (!targetWalletId) {
      setError("Please select a recipient.");
      return;
    }

    try {
      await api.transfer({
        from_wallet_id: walletIdOf(auth.user),
        to_wallet_id: targetWalletId,
        amount: Number(form.amount),
        detail: form.description,
        confirm_risk: confirmRisk
      });
      setMessage("Transfer submitted.");
      setScamAlert(null);
      setForm({ ...form, amount: "", description: "", passcode: "" }); // Reset form nicely
      await auth.refreshMe();
    } catch (err) {
      if (err.payload?.scam && !confirmRisk) {
        setScamAlert(err.payload.scam);
      } else {
        setError(err.message);
      }
    }
  }

  return (
    <section className="page-stack narrow">
      <div className="page-title"><h2>{confirm ? "Security code" : mode === "nfc" ? "NFC payment" : "Transfer"}</h2></div>
      
      {scamAlert && (
        <div style={{ position: "fixed", inset: 0, backgroundColor: "rgba(15, 23, 42, 0.7)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000, backdropFilter: "blur(2px)" }}>
          <div style={{ backgroundColor: "#ffffff", padding: "2rem", borderRadius: "1rem", maxWidth: "450px", width: "90%", boxShadow: "0 20px 25px -5px rgba(0,0,0,0.1), 0 10px 10px -5px rgba(0,0,0,0.04)", border: "1px solid #ef4444" }}>
            <div style={{ display: "flex", alignItems: "center", gap: "0.75rem", marginBottom: "1rem" }}>
              <div style={{ backgroundColor: "#fee2e2", padding: "0.5rem", borderRadius: "999px" }}>
                <svg viewBox="0 0 24 24" fill="none" stroke="#ef4444" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: "2rem", height: "2rem" }}>
                  <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"></path>
                  <path d="M12 9v4"></path>
                  <path d="M12 17h.01"></path>
                </svg>
              </div>
              <h3 style={{ margin: 0, color: "#0f172a", fontSize: "1.25rem", fontWeight: "600" }}>Security Warning</h3>
            </div>
            
            <p style={{ color: "#334155", lineHeight: "1.6", marginBottom: "1.25rem" }}>
              {scamAlert.message}
            </p>
            
            {scamAlert.reasons && scamAlert.reasons.length > 0 && (
              <div style={{ backgroundColor: "#f8fafc", padding: "1rem", borderRadius: "0.5rem", border: "1px solid #e2e8f0", marginBottom: "1.5rem" }}>
                <span style={{ fontWeight: "600", fontSize: "0.85rem", color: "#64748b", textTransform: "uppercase" }}>Risks Identified:</span>
                <ul style={{ color: "#ef4444", fontSize: "0.9rem", margin: "0.5rem 0 0 0", paddingLeft: "1.2rem", display: "flex", flexDirection: "column", gap: "0.35rem" }}>
                  {scamAlert.reasons.map((r, i) => <li key={i}>{r}</li>)}
                </ul>
              </div>
            )}
            
            <div style={{ display: "flex", gap: "0.75rem", justifyContent: "flex-end", marginTop: "1.5rem" }}>
              <button 
                type="button" 
                onClick={() => { setScamAlert(null); navigate("/"); }} 
                style={{ padding: "0.6rem 1.2rem", borderRadius: "0.5rem", border: "1px solid #cbd5e1", backgroundColor: "#ffffff", color: "#334155", cursor: "pointer", fontWeight: "500", transition: "background-color 0.2s" }}>
                Cancel Transfer
              </button>
              <button 
                type="button" 
                onClick={() => submit(null, true)} 
                style={{ padding: "0.6rem 1.2rem", borderRadius: "0.5rem", border: "none", backgroundColor: "#ef4444", color: "#ffffff", cursor: "pointer", fontWeight: "500", transition: "background-color 0.2s" }}>
                Proceed Anyway
              </button>
            </div>
          </div>
        </div>
      )}

      <Notice type="success">{message}</Notice>
      <Notice type="error">{error}</Notice>
      <form className="panel form-panel" onSubmit={(e) => submit(e, false)}>
        {!recipient || recipient.error ? (
          <label style={{ display: "block", marginBottom: "1rem" }}>
            <span style={{ fontWeight: "600", fontSize: "0.85rem", color: "#64748b", textTransform: "uppercase", display: "inline-block", marginBottom: "0.5rem" }}>TO</span>
            <input 
              placeholder="Phone / Email / Username / Wallet ID"
              value={form.to_wallet_id} 
              onChange={(e) => setForm({ ...form, to_wallet_id: e.target.value })} 
              required={!selectedWalletId} 
              style={{ width: "100%", padding: "0.75rem", borderRadius: "0.5rem", border: "1px solid #cbd5e1", backgroundColor: "#ffffff", color: "#1e293b" }}
            />
            {recipient?.error && <div style={{ color: "#ef4444", fontSize: "0.85rem", marginTop: "0.25rem" }}>{recipient.error}</div>}
          </label>
        ) : (
          <div style={{ marginBottom: "1.5rem" }}>
            <div style={{ display: "block", marginBottom: "0.5rem", fontWeight: "600", fontSize: "0.85rem", color: "#64748b", textTransform: "uppercase" }}>TO</div>
            <div style={{
              backgroundColor: "#ffffff",
              borderRadius: "1rem",
              padding: "1.25rem",
              color: "#1e293b",
              border: "1px solid #e2e8f0",
              boxShadow: "0 4px 6px -1px rgba(0,0,0,0.05), 0 2px 4px -2px rgba(0,0,0,0.05)"
            }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "1.25rem" }}>
                <div style={{ display: "flex", gap: "1rem", alignItems: "flex-start" }}>
                  <div style={{ marginTop: "0.25rem" }}>
                    <svg viewBox="0 0 24 24" fill="none" stroke="#64748b" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: "1.5rem", height: "1.5rem" }}>
                      <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                      <circle cx="12" cy="7" r="4"></circle>
                    </svg>
                  </div>
                  <div style={{ display: "flex", flexDirection: "column", gap: "0.25rem" }}>
                    <span style={{ fontWeight: "700", fontSize: "1.15rem", color: "#0f172a", letterSpacing: "0.01em" }}>
                      {recipient.match_type === "merchant" && recipient.merchant_wallet 
                        ? recipient.merchant_wallet.merchant_name 
                        : recipient.user?.user_name || "Unknown User"}
                    </span>
                    <span style={{ fontSize: "0.85rem", color: "#64748b", lineHeight: "1.4" }}>
                      {[
                        recipient.user?.user_phone_number,
                        recipient.user?.user_email,
                        recipient.user?.user_username ? `@${recipient.user.user_username}` : null
                      ].filter(Boolean).join(" | ")}
                    </span>
                  </div>
                </div>
                <div>
                  <svg viewBox="0 0 24 24" fill="none" stroke="#64748b" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ width: "1.5rem", height: "1.5rem" }}>
                    <rect x="3" y="3" width="7" height="7" rx="1"></rect>
                    <rect x="14" y="3" width="7" height="7" rx="1"></rect>
                    <rect x="14" y="14" width="7" height="7" rx="1"></rect>
                    <path d="M3 14h7v7H3z"></path>
                  </svg>
                </div>
              </div>
              <div style={{ display: "flex", gap: "0.75rem", flexWrap: "wrap" }}>
                {(recipient.user_wallet_id || recipient.user_wallet) && (
                  <button
                    type="button"
                    onClick={() => setSelectedWalletId(recipient.user_wallet_id || recipient.user_wallet?.wallet_id)}
                    style={{
                      padding: "0.5rem 1rem",
                      borderRadius: "0.5rem",
                      border: selectedWalletId === (recipient.user_wallet_id || recipient.user_wallet?.wallet_id) ? "1px solid transparent" : "1px solid #cbd5e1",
                      cursor: "pointer",
                      fontSize: "0.9rem",
                      backgroundColor: selectedWalletId === (recipient.user_wallet_id || recipient.user_wallet?.wallet_id) ? "#4f46e5" : "#f1f5f9",
                      color: selectedWalletId === (recipient.user_wallet_id || recipient.user_wallet?.wallet_id) ? "#ffffff" : "#475569",
                      display: "flex",
                      alignItems: "center",
                      gap: "0.5rem",
                      transition: "all 0.2s"
                    }}
                  >
                    {selectedWalletId === (recipient.user_wallet_id || recipient.user_wallet?.wallet_id) && <span style={{ fontSize: "1.1em" }}>✓</span>} User Wallet
                  </button>
                )}
                {(recipient.merchant_wallet_id || recipient.merchant_wallet) && (
                  <button
                    type="button"
                    onClick={() => setSelectedWalletId(recipient.merchant_wallet_id || recipient.merchant_wallet?.wallet_id)}
                    style={{
                      padding: "0.5rem 1rem",
                      borderRadius: "0.5rem",
                      border: selectedWalletId === (recipient.merchant_wallet_id || recipient.merchant_wallet?.wallet_id) ? "1px solid transparent" : "1px solid #cbd5e1",
                      cursor: "pointer",
                      fontSize: "0.9rem",
                      backgroundColor: selectedWalletId === (recipient.merchant_wallet_id || recipient.merchant_wallet?.wallet_id) ? "#4f46e5" : "#f1f5f9",
                      color: selectedWalletId === (recipient.merchant_wallet_id || recipient.merchant_wallet?.wallet_id) ? "#ffffff" : "#475569",
                      display: "flex",
                      alignItems: "center",
                      gap: "0.5rem",
                      transition: "all 0.2s"
                    }}
                  >
                    {selectedWalletId === (recipient.merchant_wallet_id || recipient.merchant_wallet?.wallet_id) && <span style={{ fontSize: "1.1em" }}>✓</span>} {recipient.merchant_wallet?.merchant_name || "Merchant"}
                  </button>
                )}
              </div>
            </div>
            
            <div style={{ display: "flex", justifyContent: "flex-end", marginTop: "1rem" }}>
              <button 
                type="button" 
                onClick={() => { setRecipient(null); setSelectedWalletId(null); setForm({ ...form, to_wallet_id: "" }); }}
                style={{ background: "none", border: "none", color: "#4f46e5", cursor: "pointer", fontSize: "0.9rem", display: "flex", gap: "0.25rem", alignItems: "center" }}
              >
                <span style={{ fontSize: "1.1em" }}>✕</span> <span style={{ textDecoration: "underline" }}>Clear recipient</span>
              </button>
            </div>
          </div>
        )}

        <label>Amount<input type="number" min="1" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} required /></label>
        <label>Description<input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
        <label>Passcode<input type="password" inputMode="numeric" value={form.passcode} onChange={(e) => setForm({ ...form, passcode: e.target.value })} /></label>
        <button className="primary-button">Send</button>
      </form>
    </section>
  );
}
