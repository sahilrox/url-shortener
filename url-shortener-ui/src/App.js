import { useState } from "react";
import { authFetch } from "./api";
import Stats from "./Stats";
import Login from "./Login";
import MyUrls from "./MyUrls";
import { QRCodeCanvas } from "qrcode.react";

function App() {
  const [url, setUrl] = useState("");
  const [customCode, setCustomCode] = useState("");
  const [result, setResult] = useState("");
  const [loggedIn, setLoggedIn] = useState(
    !!localStorage.getItem("token")
  );
  const [tab, setTab] = useState("shorten");

  const handleSubmit = async () => {
    if (!url) {
      alert("Enter URL");
      return;
    }

    try {
      const response = await authFetch("/shorten", {
        method: "POST",
        body: JSON.stringify({
          url,
          customCode: customCode || null
        })
      });

      if (!response.ok) {
        const error = await response.text();
        alert(error);
        return;
      }

      const data = await response.json();
      setResult(`${window.location.origin}/${data.shortCode}`);
    } catch {
      alert("Error");
    }
  };

  const logout = () => {
    localStorage.removeItem("token");
    window.location.reload();
  };

  
 if (!loggedIn) {
  return (
    <div className="app-container">
      <Login onSuccess={() => setLoggedIn(true)} />
    </div>
  );
}

  return (
      <div className="app">
          <div style={{ display: "flex", justifyContent: "space-between" }}>
            <h1>🔗 LinkPulse</h1>
            <button className="button" onClick={logout}>
              Logout
            </button>
          </div>

          <div className="tabs">
            <div
              className={`tab ${tab === "shorten" ? "active" : ""}`}
              onClick={() => setTab("shorten")}
            >
              Shorten
            </div>

            <div
              className={`tab ${tab === "analytics" ? "active" : ""}`}
              onClick={() => setTab("analytics")}
            >
              Analytics
            </div>

            <div
              className={`tab ${tab === "myurls" ? "active" : ""}`}
              onClick={() => setTab("myurls")}
            >
              My Links
            </div>
          </div>

          {tab === "shorten" && (
          <div className="card">
            <input
              className="input"
              placeholder="Enter URL"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
            />

            <input
              className="input"
              placeholder="Custom code (optional)"
              value={customCode}
              onChange={(e) => setCustomCode(e.target.value)}
            />

            <button className="button" onClick={handleSubmit}>
              Shorten
            </button>

            {result && (
            <div className="result-box">
              <div className="result-left">
                <p className="result-label">Your Short Link</p>

                <div className="result-row">
                  <a href={result} target="_blank" rel="noreferrer">
                    {result}
                  </a>

                  <button
                    className="button small"
                    onClick={() => navigator.clipboard.writeText(result)}
                  >
                    Copy
                  </button>
                </div>
              </div>

              <div className="result-right">
                <p className="qr-label">Scan QR</p>
                <div className="qr-box">
                  <QRCodeCanvas value={result} size={110} />
                </div>
              </div>
            </div>
          )}
          </div>
        )}

        {tab === "analytics" && <Stats />}

        {tab === "myurls" && <MyUrls />}
        </div>
  );
}

export default App;