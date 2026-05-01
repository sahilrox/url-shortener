import { useEffect, useState } from "react";
import { authFetch } from "./api";
import "./App.css";

function MyUrls() {
  const [urls, setUrls] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [deleting, setDeleting] = useState(null);
  const [sort, setSort] = useState("latest");
  const [toast, setToast] = useState("");

  const showToast = (msg) => {
  setToast(msg);
  setTimeout(() => setToast(""), 2000);
  };
  

  const fetchUrls = async () => {
    setLoading(true);

    const res = await authFetch("/my-urls");

    if (!res.ok) {
      alert("Failed to load URLs");
      setLoading(false);
      return;
    }

    const data = await res.json();
    setUrls(data);
    setLoading(false);
  };

  useEffect(() => {
    fetchUrls();
  }, []);

  const deleteUrl = async (code) => {
  const confirmDelete = window.confirm("Delete this link?");
  if (!confirmDelete) return;

  setDeleting(code); // 👈 trigger animation

  setTimeout(async () => {
    const res = await authFetch(`/delete/${code}`, {
      method: "DELETE"
    });

    if (!res.ok) {
      alert("Delete failed");
      setDeleting(null);
      return;
    }

    setUrls((prev) => prev.filter((u) => u.shortCode !== code));
    showToast("Deleted!");
  }, 300); // 👈 match animation time
};

  return (
    <div className="card">
      <h2>📂 My Links</h2>

      <input
        className="input"
        placeholder="Search links..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />

      <select
        className="input"
        value={sort}
        onChange={(e) => setSort(e.target.value)}
        >
        <option value="latest">Latest</option>
        <option value="oldest">Oldest</option>
        <option value="clicks">Most Clicks</option>
      </select>

      {loading && <p>Loading...</p>}
      
      {!loading && urls.length === 0 && (
        <p>No links yet. Create one 🚀</p>
      )}
      {toast && <div className="toast">{toast}</div>}
      {/* 🔥 LIST CONTAINER */}
      <div className="url-list">
        {urls
          .filter((u) =>
            u.longUrl.toLowerCase().includes(search.toLowerCase()) ||
            u.shortCode.toLowerCase().includes(search.toLowerCase())
          )
          .map((u, i) => {
            const shortUrl = `https://url-shortener-f45d.onrender.com/${u.shortCode}`;

            return (
              <div
                key={i}
                className={`url-item ${
                    deleting === u.shortCode ? "deleting" : ""
                }`}
                >
                <div className="url-left">
                  <a href={shortUrl} target="_blank" rel="noreferrer">
                    {shortUrl}
                  </a>
                  <p>{u.longUrl}</p>
                </div>

                <div className="url-right">
                  <p>📊 {u.hitCount}</p>
                  <p>📅 {new Date(u.createdAt).toLocaleDateString()}</p>

                  <button
                    className="button small"
                    onClick={() => {
                      navigator.clipboard.writeText(shortUrl);
                      showToast("Copied!");
                    }}
                  >
                    Copy
                  </button>

                  <button
                    className="button small danger"
                    onClick={() => deleteUrl(u.shortCode)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            );
          })}
      </div>
    </div>
  );
}

export default MyUrls;