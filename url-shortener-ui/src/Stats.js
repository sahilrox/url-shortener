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

      if (!res.ok) throw new Error();

      const result = await res.json();
      setData(result);
    } catch {
      setError("Invalid code or failed to fetch stats");
      setData(null);
    }
  };

  // 📊 Chart Data
  const chartData = data && {
    labels: data.clicksByDate.map(x =>
      new Date(x.date).toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit"
      })
    ),
    datasets: [
      {
        label: "Clicks",
        data: data.clicksByDate.map(x => x.count),
        borderColor: "#3b82f6",
        backgroundColor: "rgba(59,130,246,0.25)",
        fill: true,
        tension: 0.4,
        pointRadius: 4,
        pointHoverRadius: 7
      }
    ]
  };

  // ⚙️ Chart Options
  const options = {
    responsive: true,
    plugins: {
      legend: {
        labels: {
          color: "white"
        }
      },
      tooltip: {
        mode: "index",
        intersect: false
      }
    },
    scales: {
      x: {
        ticks: { color: "white" },
        grid: { color: "#334155" }
      },
      y: {
        ticks: { color: "white" },
        grid: { color: "#334155" }
      }
    }
  };

  return (
    <div className="card">
      <h2>📊 Analytics</h2>

      {/* Input */}
      <div style={{ display: "flex", gap: "10px", marginTop: "15px" }}>
        <input
          className="input"
          placeholder="Enter short code"
          value={code}
          onChange={(e) => setCode(e.target.value)}
        />

        <button className="button" onClick={fetchStats}>
          Go
        </button>
      </div>

      {error && <p style={{ color: "red", marginTop: "10px" }}>{error}</p>}

      {data && (
        <>
          {/* Stats Cards */}
          <div className="stats-grid">
            <div className="stat-card">
              <p>Total Clicks</p>
              <h2>{data.totalClicks}</h2>
            </div>

            <div className="stat-card">
              <p>Short Code</p>
              <h3>{data.shortCode}</h3>
            </div>

            <div className="stat-card">
              <p>Open Link</p>
              <a href={data.longUrl} target="_blank" rel="noreferrer">
                Visit
              </a>
            </div>
          </div>

          {/* 📈 Chart */}
          {chartData && (
            <div style={{ marginTop: "20px" }}>
              <Line data={chartData} options={options} />
            </div>
          )}

          {/* 🌍 Top Countries */}
          {data.clicksByCountry && data.clicksByCountry.length > 0 && (
            <div style={{ marginTop: "20px" }}>
              <h3>🌍 Top Countries</h3>

              {data.clicksByCountry.map((c, i) => (
                <div key={i} className="recent-item">
                  🌍 {c.country} — {c.count} clicks
                </div>
              ))}
            </div>
          )}

          {/* 🧾 Recent Clicks */}
          <div className="recent">
            <h3>Recent Clicks</h3>

            {data.recentClicks.map((c, i) => (
              <div key={i} className="recent-item">
                {new Date(c.timestamp).toLocaleString()}
                {" — "}
                {c.country && c.country !== "Unknown"
                  ? `🌍 ${c.country}`
                  : `IP: ${c.ipAddress}`}
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

export default Stats;