import { useState } from "react";
import { authFetch } from "./api";
import Stats from "./Stats";
import Login from "./Login";

function App() {
  const [url, setUrl] = useState("");
  const [customCode, setCustomCode] = useState("");
  const [result, setResult] = useState("");
  const [loggedIn, setLoggedIn] = useState(
    !!localStorage.getItem("token")
  );

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
      setResult(data.shortUrl);
    } catch {
      alert("Error");
    }
  };

  const logout = () => {
    localStorage.removeItem("token");
    window.location.reload();
  };

  if (!loggedIn) {
    return <Login onSuccess={() => setLoggedIn(true)} />;
  }

  return (
    <div style={{ padding: "40px", fontFamily: "Arial" }}>
      <div style={{ display: "flex", justifyContent: "space-between" }}>
        <h1>🔗 URL Shortener</h1>
        <button className="button" onClick={logout}>
          Logout
        </button>
      </div>

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
          <div style={{ marginTop: "15px" }}>
            <a href={result} target="_blank" rel="noreferrer">
              {result}
            </a>
          </div>
        )}
      </div>

      <Stats />
    </div>
  );
}

export default App;