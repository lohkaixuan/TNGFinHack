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
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");

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
