// notifications + toast đơn giản
window.notify = {
  request: async () => {
    try {
      if (!("Notification" in window)) return false;
      if (Notification.permission === "granted") return true;
      const p = await Notification.requestPermission();
      return p === "granted";
    } catch { return false; }
  },
  show: (title, body) => {
    try {
      if (!("Notification" in window) || Notification.permission !== "granted") return;
      new Notification(title || "SkyPeek", { body: body || "", icon: "/favicon.ico" });
    } catch { /* no-op */ }
  }
};

window.toast = (msg) => {
  try { console.log("[SkyPeek]", msg); alert(msg); } catch { }
};