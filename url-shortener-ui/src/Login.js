import { useState } from "react";

const API_BASE = "https://url-shortener-f45d.onrender.com";

export default function Login({ onSuccess }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [mode, setMode] = useState("login"); // or "register"
  const [error, setError] = useState("");

  const submit = async () => {
    setError("");
    const endpoint = mode === "login" ? "/login" : "/register";

    console.log("Sending register:", { email, password });
    const res = await fetch(`${API_BASE}${endpoint}`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            email: email,
            password: password
        })
    });

    if (!res.ok) {
        const txt = await res.text();
        console.error("Register error:", txt);
        alert(txt);
        setError(txt || "Failed");
        return;
    }

    if (mode === "login") {
      const data = await res.json();
      localStorage.setItem("token", data.token);
      onSuccess?.();
    } else {
      alert("Registered! Now login.");
      setMode("login");
    }
  };

  return (
    <div className="card">
      <h2>{mode === "login" ? "🔐 Login" : "📝 Register"}</h2>

      <input className="input" placeholder="Email"
        value={email} onChange={(e) => setEmail(e.target.value)} />

      <input className="input" type="password" placeholder="Password"
        value={password} onChange={(e) => setPassword(e.target.value)} /> 

      {error && <p style={{ color: "red" }}>{error}</p>}

      <button className="button" onClick={submit}>
        {mode === "login" ? "Login" : "Register"}
      </button>

      <p style={{ marginTop: 10 }}>
        {mode === "login" ? "No account?" : "Have an account?"}{" "}
        <span style={{ cursor: "pointer", color: "#3b82f6" }}
          onClick={() => setMode(mode === "login" ? "register" : "login")}>
          {mode === "login" ? "Register" : "Login"}
        </span>
      </p>
    </div>
  );
}