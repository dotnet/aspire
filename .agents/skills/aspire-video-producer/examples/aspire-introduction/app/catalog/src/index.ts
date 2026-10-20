import "./instrumentation.ts";
import express from "express";
import { createClient } from "redis";

const app = express();
const port = process.env.PORT || 5000;

// Aspire injects the address and the password of the Redis cache.
const cache = createClient({ url: process.env.CACHE_URI });
await cache.connect();

const products = [
  { id: 1, name: "Trail Backpack", price: 89.0 },
  { id: 2, name: "Camping Stove", price: 54.5 },
  { id: 3, name: "Hiking Boots", price: 129.0 },
  { id: 4, name: "Rain Jacket", price: 99.0 },
  { id: 5, name: "Water Filter", price: 39.9 },
  { id: 6, name: "Headlamp", price: 24.0 },
];

/** Returns the products, from the cache when they are there. */
app.get("/products", async (_req, res) => {
  const cached = await cache.get("products");
  if (cached) {
    res.json(JSON.parse(cached));
    return;
  }

  // Simulates a slow source of truth.
  await new Promise((resolve) => setTimeout(resolve, 120));
  await cache.set("products", JSON.stringify(products), { EX: 10 });
  console.log(`Loaded ${products.length} products into the cache`);
  res.json(products);
});

app.get("/health", async (_req, res) => {
  res.send((await cache.ping()) === "PONG" ? "Healthy" : "Unhealthy");
});

app.listen(port, () => {
  console.log(`Catalog service listening on port ${port}`);
});
