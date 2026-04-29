import { useState } from "react";
import "chart.js/auto";
import { Line } from "react-chartjs-2";

const API_BASE = "https://url-shortener-f45d.onrender.com";

function Stats() {
  const [code, setCode] = useState("");
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  const fetchStats = async () => {
    try {
      setError("");
      const res = await fetch(`${API_BASE}/stats/${code}`);

      if (!res.ok) {
        throw new Error("Invalid code");
      }

      const result = await res.json();
      setData(result);
    } catch (err) {
      setError("Failed to fetch stats");
      setData(null);
    }
  };

  // 📈 Chart data
  const chartData = data && {
    labels: data.clicksByDate.map(x =>
      new Date(x.date).toLocaleDateString("en-IN", {
        day: "numeric",
        month: "short"
      })
    ),
    datasets: [
      {
        label: "Clicks",
        data: data.clicksByDate.map(x => x.count),
        borderWidth: 2,
        tension: 0.3
      }
    ]
  };

  return (
    <div className="card">
        <div style={{ padding: "30px", maxWidth: "600px", margin: "auto" }}>
        <h2>📊 URL Analytics</h2>

        <input
            placeholder="Enter short code (e.g. abc123)"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            style={{ padding: "10px", width: "70%" }}
        />

        <button
            onClick={fetchStats}
            style={{ padding: "10px", marginLeft: "10px" }}
        >
            Get Stats
        </button>

        {error && <p style={{ color: "red" }}>{error}</p>}

        {data && (
            <div style={{ marginTop: "20px" }}>
            <p><b>Short Code:</b> {data.shortCode}</p>
            <p><b>Original URL:</b> {data.longUrl}</p>
            <p><b>Total Clicks:</b> {data.totalClicks}</p>

            <h3>Recent Clicks</h3>
            <ul>
                {data.recentClicks.map((c, i) => (
                <li key={i}>
                    {new Date(c.timestamp).toLocaleString()} — {c.ipAddress}
                </li>
                ))}
            </ul>

            {chartData && (
                <div style={{ marginTop: "30px" }}>
                <h3>📈 Clicks Over Time</h3>
                <Line data={chartData} />
                </div>
            )}
            </div>
        )}
        </div>
    </div>
  );
}

export default Stats;