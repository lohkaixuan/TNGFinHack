import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client.js";
import { EmptyState, Notice, StatCard, balanceOf, formatCurrency, walletIdOf } from "../components/Ui.jsx";
import { Icon } from "../components/Icons.jsx";
import { useAuth } from "../state/AuthContext.jsx";

const TIME_OPTIONS = [
  { key: "month", label: "This month" },
  { key: "3m", label: "3 months" },
  { key: "year", label: "This year" },
  { key: "all", label: "All" }
];

const PIE_COLORS = ["#059669", "#1d4ed8", "#e11d48", "#d97706", "#7c3aed", "#0891b2"];

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

function inSelectedRange(tx, range) {
  if (range === "all") return true;
  const raw = transactionDateValue(tx);
  if (!raw) return false;

  const date = new Date(raw);
  if (Number.isNaN(date.getTime())) return false;

  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1);

  if (range === "month") {
    return date >= start;
  }
  if (range === "3m") {
    const threeMonthsAgo = new Date(now.getFullYear(), now.getMonth() - 2, 1);
    return date >= threeMonthsAgo;
  }
  if (range === "year") {
    return date.getFullYear() === now.getFullYear();
  }
  return true;
}

export default function ChartDetailsPage({ home = false, title = "Dashboard" }) {
  const auth = useAuth();
  const [transactions, setTransactions] = useState([]);
  const [budgets, setBudgets] = useState([]);
  const [error, setError] = useState("");
  const [timeRange, setTimeRange] = useState("month");

  useEffect(() => {
    async function load() {
      try {
        const [tx, budget] = await Promise.allSettled([api.transactions(), api.budgets()]);
        if (tx.status === "fulfilled") setTransactions(Array.isArray(tx.value) ? tx.value : []);
        if (budget.status === "fulfilled") setBudgets(Array.isArray(budget.value) ? budget.value : []);
      } catch (err) {
        setError(err.message);
      }
    }
    load();
  }, []);

  const summary = useMemo(() => {
    const walletId = walletIdOf(auth.user);
    return transactions.reduce(
      (acc, tx) => {
        const amount = Math.abs(Number(tx.amount ?? tx.transaction_amount ?? 0));
        const from = tx.from ?? tx.from_wallet_id ?? tx.fromWalletId;
        const to = tx.to ?? tx.to_wallet_id ?? tx.toWalletId;
        if (from === walletId || Number(tx.amount) < 0) acc.debit += amount;
        if (to === walletId || Number(tx.amount) > 0) acc.credit += amount;
        const category = tx.category || tx.transaction_category || tx.type || "Other";
        acc.categories[category] = (acc.categories[category] || 0) + amount;
        return acc;
      },
      { debit: 0, credit: 0, categories: {} }
    );
  }, [transactions, auth.user]);

  const categoryEntries = useMemo(() => {
    const rangeSummary = transactions.reduce((acc, tx) => {
      if (!inSelectedRange(tx, timeRange)) return acc;
      const amount = Math.abs(Number(tx.amount ?? tx.transaction_amount ?? 0));
      const category = tx.category || tx.transaction_category || tx.type || "Other";
      acc[category] = (acc[category] || 0) + amount;
      return acc;
    }, {});

    return Object.entries(rangeSummary)
      .sort((a, b) => Number(b[1]) - Number(a[1]))
      .slice(0, 6);
  }, [transactions, timeRange]);

  const maxCategoryValue = useMemo(() => {
    if (!categoryEntries.length) return 1;
    return Math.max(...categoryEntries.map(([, value]) => Number(value || 0)), 1);
  }, [categoryEntries]);

  const pieData = useMemo(() => {
    const total = categoryEntries.reduce((sum, [, value]) => sum + Number(value || 0), 0);
    if (!total) return { gradient: "#eaf2ff", legend: [] };

    let offset = 0;
    const segments = categoryEntries.map(([label, value], index) => {
      const amount = Number(value || 0);
      const ratio = amount / total;
      const start = offset;
      const end = offset + ratio;
      offset = end;
      return {
        label,
        amount,
        ratio,
        color: PIE_COLORS[index % PIE_COLORS.length],
        start,
        end
      };
    });

    const gradient = `conic-gradient(${segments
      .map((segment) => `${segment.color} ${segment.start * 360}deg ${segment.end * 360}deg`)
      .join(", ")})`;

    return {
      gradient,
      legend: segments
    };
  }, [categoryEntries]);

  return (
    <section className="page-stack">
      <Notice type="error">{error}</Notice>
      <div className="hero-panel">
        <div>
          <p>{home ? "Wallet overview" : "Details"}</p>
          <h2>{home ? `Welcome, ${auth.user?.userName || auth.user?.user_name || "User"}` : title}</h2>
        </div>
        <strong>{formatCurrency(balanceOf(auth.user))}</strong>
      </div>

      <div className="action-grid">
        <Link className="action-tile" to="/reload"><Icon name="plus" />Reload</Link>
        <Link className="action-tile" to="/transfer"><Icon name="send" />Transfer</Link>
        <Link className="action-tile" to="/pay"><Icon name="qr" />Pay</Link>
        <Link className="action-tile" to="/bank/link"><Icon name="bank" />Link bank</Link>
      </div>

      <div className="stats-grid">
        <StatCard label="Debit" value={formatCurrency(summary.debit)} tone="red" />
        <StatCard label="Credit" value={formatCurrency(summary.credit)} tone="green" />
        <StatCard label="Transactions" value={transactions.length} />
        <StatCard label="Budgets" value={budgets.length} tone="gold" />
      </div>

      <section className="content-grid two">
        <article className="panel">
          <h3>Spending categories</h3>
          <div className="time-switch" role="tablist" aria-label="Spending time range">
            {TIME_OPTIONS.map((option) => (
              <button
                key={option.key}
                type="button"
                className={`time-switch-btn ${timeRange === option.key ? "active" : ""}`}
                onClick={() => setTimeRange(option.key)}
              >
                {option.label}
              </button>
            ))}
          </div>
          {categoryEntries.length ? (
            <div className="bar-list">
              {categoryEntries.map(([label, value]) => (
                <div key={label}>
                  <span>{label}</span>
                  <meter min="0" max={maxCategoryValue} value={value} />
                  <strong>{formatCurrency(value)}</strong>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState title="No spending yet" text="Transactions from the server will appear here." />
          )}

          {categoryEntries.length > 0 && (
            <div className="spending-chart pie-view" aria-label="Spending categories chart">
              <p>Spending share (pie chart)</p>
              <div className="pie-layout">
                <div className="pie-chart-wrap">
                  <div className="pie-chart" style={{ background: pieData.gradient }} />
                </div>
                <div className="pie-legend">
                  {pieData.legend.map((item) => (
                    <div className="pie-legend-row" key={`${item.label}-legend`}>
                      <span className="pie-dot" style={{ background: item.color }} />
                      <span title={item.label}>{item.label}</span>
                      <strong>{Math.round(item.ratio * 100)}%</strong>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </article>
        <article className="panel">
          <h3>Recent transactions</h3>
          <div className="list">
            {transactions.slice(0, 6).map((tx, index) => (
              <Link className="list-row" to={`/transactionDetails/${tx.transaction_id || tx.transactionId || tx.id || index}`} key={tx.transaction_id || index}>
                <span>{tx.description || tx.transaction_type || tx.type || "Transaction"}</span>
                <strong>{formatCurrency(tx.amount ?? tx.transaction_amount)}</strong>
              </Link>
            ))}
          </div>
        </article>
      </section>
    </section>
  );
}
