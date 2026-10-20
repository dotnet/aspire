import { useEffect, useState } from 'react';
import './App.css';

interface Product { id: number; name: string; price: number; }
interface Order { id: number; product: string; quantity: number; created: string; }

function App() {
  const [products, setProducts] = useState<Product[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);

  const load = async () => {
    setProducts(await (await fetch('/api/products')).json());
    setOrders(await (await fetch('/api/orders')).json());
  };

  const order = async (product: Product) => {
    await fetch('/api/orders', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ product: product.name, quantity: 1 }),
    });
    await load();
  };

  useEffect(() => { load(); }, []);

  return (
    <main>
      <h1>Contoso Outdoors</h1>
      <section className="products">
        {products.map((p) => (
          <article key={p.id}>
            <h2>{p.name}</h2>
            <p>${p.price.toFixed(2)}</p>
            <button onClick={() => order(p)}>Order</button>
          </article>
        ))}
      </section>
      <h2>Recent orders</h2>
      <ul className="orders">
        {orders.map((o) => (
          <li key={o.id}>#{o.id} · {o.quantity} × {o.product}</li>
        ))}
      </ul>
    </main>
  );
}

export default App;
