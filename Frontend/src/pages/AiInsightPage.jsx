import { useEffect, useRef, useState } from "react";
import { api } from "../api/client.js";
import { Notice, walletIdOf } from "../components/Ui.jsx";
import { useAuth } from "../state/AuthContext.jsx";

const QUICK_REPLIES = [
  "What are my spending trends?",
  "When will I receive my refund?",
  "How can I save more?",
  "What are my recent transactions?"
];

// Add bounce animation for typing indicator
const animationStyle = `
  @keyframes bounce {
    0%, 80%, 100% { opacity: 0.3; }
    40% { opacity: 1; }
  }
`;

export default function AiInsightPage() {
  const auth = useAuth();
  const [messages, setMessages] = useState([
    {
      role: "assistant",
      content: "Welcome to UniPay AI Insight! How can I help you today?",
      isInitial: true
    }
  ]);
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const messagesEndRef = useRef(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function sendMessage(event, messageText = null) {
    event?.preventDefault?.();
    const textToSend = messageText || input.trim();
    if (!textToSend) return;

    setInput("");
    setMessages((prev) => [...prev, { role: "user", content: textToSend }]);
    setLoading(true);
    setError("");

    try {
      const walletId = walletIdOf(auth.user);
      if (!walletId) {
        setError("Wallet information not available.");
        setMessages((prev) => prev.slice(0, -1));
        return;
      }

      // TODO: Replace with actual AI API endpoint once available
      const response = await api.aiChat?.({
        wallet_id: walletId,
        message: textToSend
      });

      if (response?.reply) {
        setMessages((prev) => [...prev, { role: "assistant", content: response.reply }]);
      }
    } catch (err) {
      setError(err.message);
      setMessages((prev) => prev.slice(0, -1));
    } finally {
      setLoading(false);
    }
  }

  const showQuickReplies = messages.length === 1 && messages[0].isInitial;

  return (
    <section
      className="page-stack ai-chat-page"
      style={{
        backgroundColor: "#f0f4ff",
        height: "100vh",
        display: "flex",
        flexDirection: "column"
      }}
    >
      <style>{animationStyle}</style>
      <div
        className="ai-chat-header-bar"
        style={{
          backgroundColor: "#1e40af",
          color: "#fff",
          padding: "16px 20px",
          borderRadius: "0 0 16px 16px"
        }}
      >
        <div className="ai-header-info">
          <div className="ai-header-text">
            <h1 style={{ margin: "0", fontSize: "20px", fontWeight: "600" }}>UniPay AI Assistant</h1>
          </div>
        </div>
      </div>

      <div
        className="ai-messages-container"
        style={{
          flex: 1,
          overflow: "auto",
          padding: "20px",
          display: "flex",
          flexDirection: "column"
        }}
      >
        <div className="ai-chat-messages">
          {messages.map((msg, index) => (
            <div
              key={index}
              className={`ai-msg-group ${msg.role}`}
              style={{
                display: "flex",
                alignItems: "flex-end",
                marginBottom: "12px",
                justifyContent: msg.role === "assistant" ? "flex-start" : "flex-end"
              }}
            >
              {msg.role === "assistant" && (
                <div style={{ fontSize: "28px", marginRight: "8px" }}>🤖</div>
              )}
              <div
                className={`ai-bubble-msg ${msg.role}`}
                style={{
                  maxWidth: "70%",
                  padding: "12px 16px",
                  borderRadius: msg.role === "assistant" ? "16px 16px 16px 4px" : "16px 16px 4px 16px",
                  backgroundColor: msg.role === "assistant" ? "#dbeafe" : "#1e40af",
                  color: msg.role === "assistant" ? "#1e40af" : "#fff",
                  wordWrap: "break-word",
                  fontSize: "14px",
                  lineHeight: "1.5",
                  boxShadow: "0 2px 4px rgba(0, 0, 0, 0.08)"
                }}
              >
                {msg.content}
              </div>
              {msg.role === "user" && (
                <div style={{ fontSize: "28px", marginLeft: "8px" }}>👤</div>
              )}
            </div>
          ))}
          {loading && (
            <div
              className="ai-msg-group assistant"
              style={{
                display: "flex",
                alignItems: "flex-end",
                marginBottom: "12px"
              }}
            >
              <div style={{ fontSize: "28px", marginRight: "8px" }}>🤖</div>
              <div
                style={{
                  padding: "12px 16px",
                  borderRadius: "16px 16px 16px 4px",
                  backgroundColor: "#dbeafe",
                  color: "#1e40af",
                  boxShadow: "0 2px 4px rgba(0, 0, 0, 0.08)"
                }}
              >
                <span style={{ display: "flex", gap: "4px" }}>
                  <span
                    style={{
                      display: "inline-block",
                      width: "8px",
                      height: "8px",
                      borderRadius: "50%",
                      backgroundColor: "#1e40af",
                      animation: "bounce 1.4s infinite"
                    }}
                  ></span>
                  <span
                    style={{
                      display: "inline-block",
                      width: "8px",
                      height: "8px",
                      borderRadius: "50%",
                      backgroundColor: "#1e40af",
                      animation: "bounce 1.4s infinite 0.2s"
                    }}
                  ></span>
                  <span
                    style={{
                      display: "inline-block",
                      width: "8px",
                      height: "8px",
                      borderRadius: "50%",
                      backgroundColor: "#1e40af",
                      animation: "bounce 1.4s infinite 0.4s"
                    }}
                  ></span>
                </span>
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {showQuickReplies && (
          <div
            className="quick-replies-section"
            style={{
              display: "flex",
              flexDirection: "column",
              gap: "8px",
              marginTop: "16px"
            }}
          >
            {QUICK_REPLIES.map((reply, idx) => (
              <button
                key={idx}
                className="quick-reply-btn"
                onClick={(e) => sendMessage(e, reply)}
                disabled={loading}
                style={{
                  padding: "12px 16px",
                  borderRadius: "20px",
                  border: "1.5px solid #1e40af",
                  backgroundColor: "#fff",
                  color: "#1e40af",
                  cursor: "pointer",
                  fontSize: "13px",
                  fontWeight: "500",
                  textAlign: "left",
                  transition: "all 0.2s",
                  opacity: loading ? 0.5 : 1,
                  boxShadow: "0 2px 4px rgba(30, 64, 175, 0.1)"
                }}
              >
                {reply}
              </button>
            ))}
          </div>
        )}
      </div>

      {error && <Notice type="error">{error}</Notice>}

      <form
        className="ai-input-form"
        onSubmit={sendMessage}
        style={{
          padding: "20px",
          backgroundColor: "#fff",
          borderTop: "1px solid #e0e7ff",
          display: "flex",
          gap: "10px"
        }}
      >
        <input
          type="text"
          placeholder="Type your message here"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={loading}
          className="ai-text-input"
          style={{
            flex: 1,
            padding: "12px 16px",
            border: "1px solid #e0e7ff",
            borderRadius: "20px",
            fontSize: "14px",
            outline: "none",
            transition: "border-color 0.2s"
          }}
        />
        <button
          type="submit"
          className="ai-send-button"
          disabled={loading}
          title="Send message"
          style={{
            padding: "12px 16px",
            backgroundColor: "#1e40af",
            border: "none",
            borderRadius: "20px",
            color: "#fff",
            fontSize: "18px",
            cursor: loading ? "not-allowed" : "pointer",
            opacity: loading ? 0.6 : 1,
            transition: "all 0.2s"
          }}
        >
          ✈️
        </button>
      </form>
    </section>
  );
}
