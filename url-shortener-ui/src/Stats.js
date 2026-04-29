import { useState } from "react";
import { authFetch } from "./api";
import "chart.js/auto";
import { Line, Pie } from "react-chartjs-2";

function Stats() {
  const [code, setCode] = useState("");
  const [range, setRange] = useState("all");
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  const fetchStats = async () => {
    try {
      setError("");

      const res = await authFetch(`/stats/${code}?range=${range}`);

      if (!res.ok) throw new Error();

      const result = await res.json();
      setData(result);
    } catch {
      setError("Invalid code or failed to fetch stats");
      setData(null);
    }
  };

  // 📊 Line chart
  const chartData =
    data && {
      labels: data.clicksByDate.map((x) =>
        new Date(x.date).toLocaleDateString()
      ),
      datasets: [
        {
          label: "Clicks",
          data: data.clicksByDate.map((x) => x.count),
          borderColor: "#3b82f6",
          backgroundColor: "rgba(59,130,246,0.2)",
          fill: true
        }
      ]
    };

  // 🌍 Country Pie
  const countryPie =
    data?.clicksByCountry?.length > 0
      ? {
          labels: data.clicksByCountry.map((c) => c.country),
          datasets: [
            {
              data: data.clicksByCountry.map((c) => c.count),
              backgroundColor: ["#3b82f6", "#22c55e", "#f59e0b", "#ef4444"]
            }
          ]
        }
      : null;

  // 📱 Device Pie
  const devicePie =
    data?.clicksByDevice?.length > 0
      ? {
          labels: data.clicksByDevice.map((d) => d.device),
          datasets: [
            {
              data: data.clicksByDevice.map((d) => d.count),
              backgroundColor: ["#22c55e", "#3b82f6", "#f59e0b"]
            }
          ]
        }
      : null;

  return (
    <div className="card">
      <h2>📊 Analytics</h2>

      {/* Input + filter */}
      <div style={{ display: "flex", gap: "10px", marginTop: "10px" }}>
        <input
          className="input"
          placeholder="Enter short code"
          value={code}
          onChange={(e) => setCode(e.target.value)}
        />

        <select
          className="input"
          value={range}
          onChange={(e) => setRange(e.target.value)}
        >
          <option value="all">All Time</option>
          <option value="24h">24h</option>
          <option value="7d">7 days</option>
          <option value="30d">30 days</option>
        </select>

        <button className="button" onClick={fetchStats}>
          Go
        </button>
      </div>

      {error && <p style={{ color: "red" }}>{error}</p>}

      {data && (
        <>
          {/* Stats */}
          <div className="stats-grid">
            <div className="stat-card">
              <p>Total Clicks</p>
              <h2>{data.totalClicks}</h2>
            </div>

            <div className="stat-card">
              <p>Short Code</p>
              <h3>{code}</h3>
            </div>
          </div>

          {/* Line chart */}
          {chartData && (
            <div style={{ marginTop: "20px" }}>
              <Line data={chartData} />
            </div>
          )}

          {/* Country pie */}
          {countryPie && (
            <div style={{ marginTop: "30px" }}>
              <h3>🌍 Country Distribution</h3>
              <Pie data={countryPie} />
            </div>
          )}

          {/* Device pie */}
          {devicePie && (
            <div style={{ marginTop: "30px" }}>
              <h3>📱 Device Distribution</h3>
              <Pie data={devicePie} />
            </div>
          )}

          {/* Recent clicks */}
          <div style={{ marginTop: "20px" }}>
            <h3>Recent Clicks</h3>

            {data.recentClicks.map((c, i) => (
              <div key={i} className="recent-item">
                {new Date(c.timestamp).toLocaleString()} — 🌍 {c.country} — 📱{" "}
                {c.device}
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

export default Stats;