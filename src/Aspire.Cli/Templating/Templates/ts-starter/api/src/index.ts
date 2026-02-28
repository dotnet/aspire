import express from "express";

const app = express();
const port = process.env.PORT || 5000;

app.get("/api/weatherforecast", (_req, res) => {
  const summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
  const forecasts = Array.from({ length: 5 }, (_, i) => ({
    date: new Date(Date.now() + (i + 1) * 86400000).toISOString().split("T")[0],
    temperatureC: Math.floor(Math.random() * 75) - 20,
    summary: summaries[Math.floor(Math.random() * summaries.length)],
  }));
  res.json(forecasts);
});

app.listen(port, () => {
  console.log(`API server listening on port ${port}`);
});
