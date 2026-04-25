import { useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client.js";
import { Notice, walletIdOf } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";
import { QRCodeSVG } from "qrcode.react";

export default function QrPayPage() {
  const auth = useAuth();
  const [form, setForm] = useState({ to_wallet_id: "", amount: "" });
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const currentWalletId = walletIdOf(auth.user);

  async function submit(event) {
    event.preventDefault();
    setError("");

    if (!currentWalletId) {
      setError("No wallet ID found for this account.");
      return;
    }

    try {
      await api.pay({
        from_wallet_id: currentWalletId,
        to_wallet_id: form.to_wallet_id,
        amount: Number(form.amount),
        method: "qr"
      });
      setMessage("QR payment submitted.");
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-title" style={{ justifyContent: "center" }}><h2>QR payment</h2></div>{/*<Link className="secondary-button" to="/pay/nfc">NFC</Link>*/}
      <Notice type="success">{message}</Notice>
      <Notice type="error">{error}</Notice>
      <section className="content-grid two">
        <article className="panel qr-card">
          <div style={{ background: "white", padding: "16px", borderRadius: "12px", boxShadow: "0 10px 25px rgba(0,0,0,0.05)" }}>
            <QRCodeSVG
              value={currentWalletId || "no-wallet-id"}
              size={220}
              level="H"
              includeMargin={false}
              imageSettings={{
                src: "/backupLogo.png",
                x: undefined,
                y: undefined,
                height: 40,
                width: 40,
                excavate: true,
              }}
            />
          </div>
          
          <div style={{ marginTop: '24px', textAlign: 'center' }}>
            <p style={{ margin: 0, fontWeight: '700', color: 'var(--ink)' }}>Your Wallet QR</p>
            <p className="muted" style={{ fontSize: '12px', marginTop: '4px' }}>{currentWalletId || "No wallet ID"}</p>
          </div>
          {!currentWalletId && <Notice type="info">This account does not have a wallet yet.</Notice>}
        </article>
        <form className="panel form-panel" onSubmit={submit}>
          <h3>Pay by wallet ID</h3>
          <label>Merchant wallet ID<input value={form.to_wallet_id} onChange={(e) => setForm({ ...form, to_wallet_id: e.target.value })} required /></label>
          <label>Amount<input type="number" min="1" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} required /></label>
          <button className="primary-button">Pay</button>
        </form>
      </section>
    </section>
  );
}
