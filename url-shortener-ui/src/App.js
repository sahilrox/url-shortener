import { useState } from "react";
import Stats from "./Stats";
import "./App.css";

function App() {
  const [url, setUrl] = useState("");
  const [customCode, setCustomCode] = useState("");
  const [result, setResult] = useState("");
  const [view, setView] = useState("shorten"); // 🔥 toggle

  const handleSubmit = async () => {
    if (!url) {
      alert("Please enter a URL");
      return;
    }

    try {
      const response = await fetch("https://url-shortener-f45d.onrender.com/shorten", {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          url: url,
          customCode: customCode || null
        })
      });

      if (!response.ok) {
        const error = await response.text();
        alert(error);
        return;
      }

      const data = await response.json();
      setResult(data.shortUrl);
    } catch (err) {
      console.error(err);
      alert("Something went wrong");
    }
  };

  return (
    <div className="container">
  <h1>🔗 URL Shortener</h1>

  <div className="tabs">
    <div
      className={`tab ${view === "shorten" ? "active" : ""}`}
      onClick={() => setView("shorten")}
    >
      Shorten
    </div>
    <div
      className={`tab ${view === "stats" ? "active" : ""}`}
      onClick={() => setView("stats")}
    >
      Analytics
    </div>
  </div>

  {view === "shorten" && (
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
        Shorten 🚀
      </button>

      {result && (
        <div className="result">
          <a href={result} target="_blank" rel="noreferrer">
            {result}
          </a>

          <button
            onClick={() => navigator.clipboard.writeText(result)}
          >
            Copy
          </button>
        </div>
      )}
    </div>
  )}

  {view === "stats" && <Stats />}
</div>
  );
}

export default App;