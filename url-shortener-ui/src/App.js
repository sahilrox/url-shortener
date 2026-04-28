import { useState } from "react";

function App() {
  const [url, setUrl] = useState("");
  const [customCode, setCustomCode] = useState("");
  const [result, setResult] = useState("");

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
    <div style={{ padding: "40px", fontFamily: "Arial" }}>
      <h1>🔗 URL Shortener</h1>

      <input
        type="text"
        placeholder="Enter URL"
        value={url}
        onChange={(e) => setUrl(e.target.value)}
        style={{ width: "300px", marginBottom: "10px" }}
      />

      <br />

      <input
        type="text"
        placeholder="Custom code (optional)"
        value={customCode}
        onChange={(e) => setCustomCode(e.target.value)}
        style={{ width: "300px", marginBottom: "10px" }}
      />

      <br />

      <button onClick={handleSubmit}>Shorten</button>

      {result && (
        <div style={{ marginTop: "20px" }}>
          <strong>Short URL:</strong>
          <br />
          <a href={result} target="_blank" rel="noreferrer">
            {result}
          </a>
        </div>
      )}
    </div>
  );
}

export default App;