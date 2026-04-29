import { useState } from "react";
import "chart.js/auto";
import { Line, Pie } from "react-chartjs-2";

const API_BASE = "https://url-shortener-f45d.onrender.com";

function Stats() {
  const [code, setCode] = useState("");
  const [data, setData] = useState({});
  const [error, setError] = useState("");
  const [range, setRange] = useState("all");
  

  const fetchStats = async () => {
    try {
      setError("");
      const res = await fetch(`${API_BASE}/stats/${code}?range=${range}`);

      if (!res.ok) throw new Error();

      const result = await res.json();
      setData(result);
    } catch {
      setError("Invalid code or failed to fetch stats");
      setData({}); // ✅ NEVER set null
    }
  };

  // 📊 Line Chart Data (SAFE)
  const chartData =
    data?.clicksByDate?.length > 0
      ? {
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
        }
      : null;

  // 🥧 Pie Chart Data (SAFE)
  const pieData =
    data?.clicksByCountry?.length > 0
      ? {
          labels: data.clicksByCountry.map(c => c.country || "Unknown"),
          datasets: [
            {
              data: data.clicksByCountry.map(c => c.count),
              backgroundColor: [
                "#3b82f6",
                "#22c55e",
                "#f59e0b",
                "#ef4444",
                "#a855f7",
                "#06b6d4"
              ],
              borderWidth: 2,
              borderColor: "#1e293b"
            }
          ]
        }
      : null;

  // ⚙️ Chart Options
  const options = {
    responsive: true,
    plugins: {
      legend: {
        labels: { color: "white" }
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

  const pieOptions = {
    plugins: {
      legend: {
        labels: { color: "white" }
      }
    }
  };

  const devicePieData =
  data?.clicksByDevice?.length > 0
    ? {
        labels: data.clicksByDevice.map(d => d.device),
        datasets: [
          {
            data: data.clicksByDevice.map(d => d.count),
            backgroundColor: [
              "#22c55e",
              "#3b82f6",
              "#f59e0b",
              "#ef4444"
            ]
          }
        ]
      }
    : null;

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
        <select
            className="input"
            value={range}
            onChange={(e) => setRange(e.target.value)}
            >
            <option value="all">All Time</option>
            <option value="24h">Last 24h</option>
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
        </select>

        <button className="button" onClick={fetchStats}>
          Go
        </button>
      </div>

      {error && (
        <p style={{ color: "red", marginTop: "10px" }}>{error}</p>
      )}

      {/* Only render when data is loaded */}
      {data?.totalClicks !== undefined && (
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

          {/* 📈 Line Chart */}
          {chartData && (
            <div style={{ marginTop: "20px" }}>
              <Line data={chartData} options={options} />
            </div>
          )}

          {/* Low data message */}
          {data.totalClicks < 2 && (
            <p style={{ marginTop: "10px", opacity: 0.7 }}>
              Not enough data for trends yet
            </p>
          )}

          {/* 🌍 Top Countries */}
          {data?.clicksByCountry?.length > 0 && (
            <div style={{ marginTop: "20px" }}>
              <h3>🌍 Top Countries</h3>

              {data.clicksByCountry.map((c, i) => (
                <div key={i} className="recent-item">
                  {c.country && c.country !== "Unknown"
                    ? `🌍 ${c.country}`
                    : "🌍 Unknown"}{" "}
                  — {c.count} clicks
                </div>
              ))}
            </div>
          )}

          {/* 🥧 Pie Chart */}
          {pieData && (
            <div style={{ marginTop: "30px" }}>
              <h3>🌍 Click Distribution</h3>
              <Pie data={pieData} options={pieOptions} />
            </div>
          )}

          

          {/* 🧾 Recent Clicks */}
          {data?.recentClicks?.length > 0 && (
            <div className="recent">
              <h3>Recent Clicks</h3>

              {data.recentClicks.map((c, i) => (
                <div key={i} className="recent-item">
                  {new Date(c.timestamp).toLocaleString()} —{" "}
                  {c.country && c.country !== "Unknown"
                    ? `🌍 ${c.country}`
                    : `IP: ${c.ipAddress}`}
                </div>
              ))}
            </div>
          )}

          {data?.clicksByDevice?.length > 0 && (
            <div style={{ marginTop: "20px" }}>
                <h3>📱 Devices</h3>

                {data.clicksByDevice.map((d, i) => (
                <div key={i} className="recent-item">
                    {d.device} — {d.count} clicks
                </div>
                ))}
            </div>
)}

          {devicePieData && (
            <div style={{ marginTop: "30px" }}>
                <h3>📱 Device Distribution</h3>
                <Pie data={devicePieData} options={pieOptions} />
            </div>
)}


        </>
      )}
    </div>
  );
}

export default Stats;