import express from "express";

const app = express();
const port = process.env.PORT || 5000;

app.get("/api/weatherforecast", (_req, res) => {
  const summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];
  const forecasts = Array.from({ length: 5 }, (_, i) => {
    const temperatureC = Math.floor(Math.random() * 75) - 20;
    return {
      date: new Date(Date.now() + (i + 1) * 86400000).toISOString(),
      temperatureC,
      temperatureF: 32 + Math.trunc(temperatureC / 0.5556),
      summary: summaries[Math.floor(Math.random() * summaries.length)],
    };
  });
  res.json(forecasts);
});

app.get("/health", (_req, res) => {
  res.send("Healthy");
});

app.listen(port, () => {
  console.log(`API server listening on port ${port}`);
});
