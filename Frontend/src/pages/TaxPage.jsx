import { useEffect, useMemo, useState } from "react";
import { api } from "../api/client.js";
import { EmptyState, Notice, formatCurrency, formatDateTime, userIdOf, walletIdOf } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";

const DEDUCTIBLE_CATEGORIES = new Set([
  "worklearning",
  "healthcare",
  "charity",
  "subscription",
  "travel"
]);

const TAX_KEYWORDS = [
  /education|tuition|course|book|training|exam|workshop/i,
  /medical|clinic|hospital|dental|pharmacy|health/i,
  /donation|charity|zakat|fitrah/i,
  /insurance|life insurance/i,
  /lifestyle|sports|gym/i
];

const RELIEF_GROUPS = {
  education: "Education & Learning",
  medical: "Medical & Healthcare",
  donation: "Donation & Zakat",
  insurance: "Insurance & Protection",
  lifestyle: "Lifestyle & Wellness",
  travel: "Travel & Work Related",
  other: "Other Potential Relief"
};

const RELIEF_RULES = [
  { key: "education", regex: /education|tuition|course|book|training|exam|workshop|learning/i, categories: ["worklearning"] },
  { key: "medical", regex: /medical|clinic|hospital|dental|pharmacy|health/i, categories: ["healthcare"] },
  { key: "donation", regex: /donation|charity|zakat|fitrah/i, categories: ["charity"] },
  { key: "insurance", regex: /insurance|takaful|life insurance|medical card/i, categories: ["subscription"] },
  { key: "lifestyle", regex: /lifestyle|sports|gym|equipment/i, categories: ["subscription"] },
  { key: "travel", regex: /travel|flight|hotel|transport/i, categories: ["travel"] }
];

function normalizeCategory(raw) {
  if (!raw) return "other";
  return String(raw).replace(/[^a-z]/gi, "").toLowerCase();
}

function txDate(tx) {
  return (
    tx.transaction_timestamp ||
    tx.timestamp ||
    tx.transaction_time ||
    tx.transaction_date ||
    tx.created_at ||
    tx.createdAt ||
    tx.date ||
    null
  );
}

function txAmount(tx) {
  return Number(tx.amount ?? tx.transaction_amount ?? 0);
}

function txText(tx) {
  return [
    tx.description,
    tx.transaction_item,
    tx.transaction_detail,
    tx.transaction_to,
    tx.merchant,
    tx.category
  ].filter(Boolean).join(" | ");
}

function historyKey(userId) {
  return `unipay_tax_history_${userId || "anonymous"}`;
}

function reliefGroupOf(category, detail) {
  const hit = RELIEF_RULES.find((rule) =>
    rule.categories.includes(category) || rule.regex.test(detail)
  );
  return hit?.key || "other";
}

function buildReliefGroups(items) {
  const map = new Map();

  for (const item of items) {
    const key = item.reliefGroup || "other";
    if (!map.has(key)) {
      map.set(key, {
        key,
        label: RELIEF_GROUPS[key] || RELIEF_GROUPS.other,
        count: 0,
        total: 0,
        items: []
      });
    }

    const group = map.get(key);
    group.count += 1;
    group.total += Number(item.amount || 0);
    group.items.push(item);
  }

  return Array.from(map.values())
    .sort((a, b) => b.total - a.total)
    .map((group) => ({ ...group, items: group.items.slice(0, 12) }));
}

export default function TaxPage() {
  const auth = useAuth();
  const [items, setItems] = useState([]);
  const [error, setError] = useState("");
  const [analysisError, setAnalysisError] = useState("");
  const [loading, setLoading] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);
  const [year, setYear] = useState(new Date().getFullYear());
  const [result, setResult] = useState(null);
  const [history, setHistory] = useState([]);

  const userId = userIdOf(auth.user);

  useEffect(() => {
    const walletId = walletIdOf(auth.user);
    if (!walletId && !userId) {
      setItems([]);
      return;
    }

    setLoading(true);
    setError("");
    api.transactions(walletId ? { walletId } : { userId })
      .then((data) => setItems(Array.isArray(data) ? data : []))
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [auth.user, userId]);

  useEffect(() => {
    try {
      const saved = localStorage.getItem(historyKey(userId));
      const parsed = saved ? JSON.parse(saved) : [];
      setHistory(Array.isArray(parsed) ? parsed : []);
    } catch {
      setHistory([]);
    }
  }, [userId]);

  const yearItems = useMemo(() => {
    return items.filter((tx) => {
      const d = txDate(tx);
      if (!d) return false;
      const value = new Date(d);
      if (Number.isNaN(value.getTime())) return false;
      return value.getFullYear() === Number(year);
    });
  }, [items, year]);

  async function inferCategory(tx) {
    const raw = tx.category || tx.final_category || tx.finalCategory;
    if (raw) return normalizeCategory(raw);

    try {
      const guess = await api.categorize({
        merchant: tx.transaction_to || tx.merchant || null,
        description: txText(tx),
        mcc: tx.mcc || null,
        amount: txAmount(tx),
        currency: "MYR",
        country: "MY"
      });
      return normalizeCategory(guess?.category);
    } catch {
      return "other";
    }
  }

  function findDeductibleReason(category, detail) {
    if (DEDUCTIBLE_CATEGORIES.has(category)) {
      return `Matched AI category: ${category}`;
    }
    const matched = TAX_KEYWORDS.find((re) => re.test(detail));
    if (matched) return "Matched tax-relief keyword in transaction details";
    return "";
  }

  async function runAnalysis() {
    setAnalysisError("");
    setAnalyzing(true);

    try {
      const base = yearItems.filter((tx) => txAmount(tx) > 0);
      const enriched = await Promise.all(
        base.map(async (tx) => {
          const category = await inferCategory(tx);
          const detail = txText(tx);
          const reason = findDeductibleReason(category, detail);
          return {
            id: tx.transaction_id || tx.transactionId || tx.id || crypto.randomUUID(),
            timestamp: txDate(tx),
            amount: txAmount(tx),
            detail,
            category,
            reliefGroup: reliefGroupOf(category, detail),
            reason,
            source: tx
          };
        })
      );

      const deductible = enriched.filter((row) => Boolean(row.reason));
      const total = deductible.reduce((sum, row) => sum + Number(row.amount || 0), 0);
      const reliefGroups = buildReliefGroups(deductible);

      let aiMessage = "";
      try {
        const ai = await api.aiTaxRelief({
          yearOfAssessment: Number(year),
          annualIncome: 0,
          employmentType: null,
          maritalStatus: null,
          dependents: 0,
          hasDisability: false,
          hasSpouseWithNoIncome: false,
          zakatOrFitrah: 0,
          donations: deductible
            .filter((d) => /charity|donation|zakat|fitrah/i.test(d.detail))
            .reduce((sum, d) => sum + d.amount, 0),
          expenses: deductible.slice(0, 50).map((d) => ({
            category: d.category,
            amount: Number(d.amount || 0),
            notes: d.detail
          })),
          notes: "Auto-generated from user transactions."
        });
        aiMessage = ai?.message || "";
      } catch {
        aiMessage = "AI tax guidance is temporarily unavailable. You can still review detected deductible transactions below.";
      }

      const snapshot = {
        id: Date.now(),
        year: Number(year),
        createdAt: new Date().toISOString(),
        count: deductible.length,
        total,
        reliefGroups,
        aiMessage,
        items: deductible.slice(0, 40).map((d) => ({
          id: d.id,
          timestamp: d.timestamp,
          amount: d.amount,
          detail: d.detail,
          category: d.category,
          reliefGroup: d.reliefGroup,
          reason: d.reason
        }))
      };

      setResult(snapshot);
      const nextHistory = [snapshot, ...history].slice(0, 12);
      setHistory(nextHistory);
      localStorage.setItem(historyKey(userId), JSON.stringify(nextHistory));
    } catch (err) {
      setAnalysisError(err.message || "Failed to run tax analysis.");
    } finally {
      setAnalyzing(false);
    }
  }

  return (
    <section className="page-stack">
      <div className="page-title">
        <h2>Tax Relief Review</h2>
      </div>
      <Notice type="error">{error || analysisError}</Notice>

      <form className="panel form-panel" onSubmit={(e) => e.preventDefault()}>
        <label>
          Year of assessment
          <input
            type="number"
            value={year}
            onChange={(e) => setYear(e.target.value)}
            min="2000"
            max="2100"
          />
        </label>
        <button type="button" className="primary-button" onClick={runAnalysis} disabled={analyzing || loading}>
          {analyzing ? "Analyzing..." : "Analyze deductible spending"}
        </button>
        <p style={{ margin: 0, opacity: 0.8 }}>
          AI will categorize your transactions and flag likely tax-relief items for later review.
        </p>
      </form>

      {result && (
        <article className="panel detail-panel">
          <div><span>Analysis time</span><strong>{formatDateTime(result.createdAt)}</strong></div>
          <div><span>Detected deductible transactions</span><strong>{result.count}</strong></div>
          <div><span>Estimated deductible amount</span><strong>{formatCurrency(result.total)}</strong></div>
        </article>
      )}

      {result && (
        <article className="panel">
          <h3 style={{ marginTop: 0 }}>Relief Category Summary</h3>
          {(result.reliefGroups?.length ? result.reliefGroups : buildReliefGroups(result.items || [])).length ? (
            <div className="table-list">
              {(result.reliefGroups?.length ? result.reliefGroups : buildReliefGroups(result.items || [])).map((group) => (
                <div className="table-row" key={group.key}>
                  <span>{group.label}</span>
                  <span>{group.count} items</span>
                  <strong>{formatCurrency(group.total)}</strong>
                  <span>{group.key}</span>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              title="No grouped relief categories"
              text="No deductible expenses were matched to relief categories."
            />
          )}
        </article>
      )}

      {result?.aiMessage && (
        <article className="panel">
          <h3 style={{ marginTop: 0 }}>AI Guidance</h3>
          <p style={{ marginBottom: 0, whiteSpace: "pre-wrap" }}>{result.aiMessage}</p>
        </article>
      )}

      {result && (
        <article className="panel">
          <h3 style={{ marginTop: 0 }}>Detected Items by Relief Category</h3>
          {result.items.length ? (
            <div style={{ display: "grid", gap: 14 }}>
              {(result.reliefGroups?.length ? result.reliefGroups : buildReliefGroups(result.items || [])).map((group) => (
                <section key={group.key}>
                  <h4 style={{ margin: "0 0 10px" }}>
                    {group.label} ({group.count})
                  </h4>
                  <div className="table-list">
                    {group.items.map((item) => (
                      <div className="table-row" key={item.id}>
                        <span>{item.detail || "Transaction"}</span>
                        <span>{item.category}</span>
                        <strong>{formatCurrency(item.amount)}</strong>
                        <span>{formatDateTime(item.timestamp)}</span>
                      </div>
                    ))}
                  </div>
                </section>
              ))}
            </div>
          ) : (
            <EmptyState
              title="No deductible items detected"
              text="Try another year or review transaction details for clearer categorization."
            />
          )}
        </article>
      )}

      <article className="panel">
        <h3 style={{ marginTop: 0 }}>Analysis History</h3>
        {history.length ? (
          <div className="table-list">
            {history.map((entry) => (
              <button
                key={entry.id}
                type="button"
                className="table-row"
                onClick={() => setResult(entry)}
                style={{ textAlign: "left", border: 0, background: "transparent", cursor: "pointer" }}
              >
                <span>{entry.year} snapshot</span>
                <span>{entry.count} items</span>
                <strong>{formatCurrency(entry.total)}</strong>
                <span>{formatDateTime(entry.createdAt)}</span>
              </button>
            ))}
          </div>
        ) : (
          <EmptyState
            title="No history yet"
            text="Run your first tax analysis to keep a timeline you can revisit later."
          />
        )}
      </article>
    </section>
  );
}
