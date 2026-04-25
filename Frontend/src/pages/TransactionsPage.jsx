import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client.js";
import { EmptyState, Notice, formatCurrency, formatDateTime, userIdOf, walletIdOf } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";

function transactionDateValue(tx) {
  return (
    tx.timestamp ||
    tx.transaction_timestamp ||
    tx.transaction_time ||
    tx.transaction_date ||
    tx.created_at ||
    tx.createdAt ||
    tx.date ||
    null
  );
}

function inDateRange(tx, fromDate, toDate) {
  const source = transactionDateValue(tx);
  if (!source) return !fromDate && !toDate;

  const time = new Date(source).getTime();
  if (Number.isNaN(time)) return !fromDate && !toDate;

  if (fromDate) {
    const fromTime = new Date(fromDate).getTime();
    if (!Number.isNaN(fromTime) && time < fromTime) return false;
  }

  if (toDate) {
    const toTime = new Date(toDate).getTime() + 24 * 60 * 60 * 1000 - 1;
    if (!Number.isNaN(toTime) && time > toTime) return false;
  }

  return true;
}

export default function TransactionsPage() {
  const auth = useAuth();
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");
  const [reportError, setReportError] = useState("");
  const [report, setReport] = useState(null);
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const now = new Date();
  const [reportForm, setReportForm] = useState({ year: now.getFullYear(), month: now.getMonth() + 1 });

  useEffect(() => {
    const walletId = walletIdOf(auth.user);
    const userId = userIdOf(auth.user);

    if (!walletId && !userId) {
      setItems([]);
      return;
    }

    setError("");
    api.transactions(walletId ? { walletId } : { userId })
      .then((data) => setItems(Array.isArray(data) ? data : []))
      .catch((err) => setError(err.message));
  }, [auth.user]);

  const filteredItems = useMemo(
    () => items.filter((tx) => inDateRange(tx, fromDate, toDate)),
    [items, fromDate, toDate]
  );

  function clearFilter() {
    setFromDate("");
    setToDate("");
  }

  async function generateReport(event) {
    event.preventDefault();
    setReportError("");

    const walletId = walletIdOf(auth.user);
    if (!walletId) {
      setReport(null);
      setReportError("Wallet is not available for this user.");
      return;
    }

    try {
      const data = await api.generateMonthlyReport({
        wallet_id: walletId,
        year: Number(reportForm.year),
        month: Number(reportForm.month)
      });
      setReport(data);
    } catch (err) {
      setReport(null);
      setReportError(err.message);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-title">
        <h2>Transactions</h2>
        <div style={{ display: "flex", gap: 8 }}>
          <Link className="secondary-button" to="/tax">Tax page</Link>
          <Link className="primary-button small" to="/transfer">New transfer</Link>
        </div>
      </div>
      <Notice type="error">{error}</Notice>
      
      <form className="panel form-panel" onSubmit={generateReport}>
        <div className="page-title"><h3>Financial report</h3></div>
        <div className="tx-filter-grid">
          <label>
            Year
            <input
              type="number"
              value={reportForm.year}
              onChange={(e) => setReportForm({ ...reportForm, year: e.target.value })}
            />
          </label>
          <label>
            Month
            <input
              type="number"
              min="1"
              max="12"
              value={reportForm.month}
              onChange={(e) => setReportForm({ ...reportForm, month: e.target.value })}
            />
          </label>
        </div>
        <Notice type="error">{reportError}</Notice>
        <button className="primary-button" type="submit">Generate report</button>
      </form>

      {report && (
        <article className="panel detail-panel">
          {Object.entries(report).map(([key, value]) => (
            <div key={key}>
              <span>{key}</span>
              <strong>{String(value ?? "-")}</strong>
            </div>
          ))}
        </article>
      )}
      <form className="panel form-panel tx-filter-panel" onSubmit={(event) => event.preventDefault()}>
        <div className="tx-filter-grid">
          <label>
            From date
            <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </label>
          <label>
            To date
            <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </label>
        </div>
        <button type="button" className="secondary-button" onClick={clearFilter}>Clear filter</button>
      </form>

      <article className="panel">
        {filteredItems.length ? (
          <div className="table-list">
            {filteredItems.map((tx, index) => {
              const id = tx.transaction_id || tx.transactionId || tx.id || index;
              const timeValue = transactionDateValue(tx);
              return (
                <Link to={`/transactionDetails/${id}`} className="table-row transaction-row" key={id}>
                  <span>{tx.description || tx.transaction_type || tx.type || "Transaction"}</span>
                  <span>{tx.category || tx.status || "General"}</span>
                  <strong>{formatCurrency(tx.amount ?? tx.transaction_amount)}</strong>
                  <span>{formatDateTime(timeValue)|| tx.Date}</span>
                </Link>
              );
            })}
          </div>
        ) : (
          <EmptyState title="No transactions" text={fromDate || toDate ? "No records in this date range." : "Once the server returns wallet activity, it will be listed here."} />
        )}
      </article>
    </section>
  );
}
