'use client';

import { useEffect, useState } from 'react';

interface Product {
  id: number;
  name: string;
  price: number;
  category: string;
  description: string;
}

export default function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/products')
      .then((res) => res.json())
      .then(setProducts)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      <h1 className="text-3xl font-bold mb-8">Products</h1>

      {loading && <p className="text-gray-400">Loading products...</p>}

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {products.map((product) => (
          <div
            key={product.id}
            className="bg-white rounded-lg shadow hover:shadow-lg transition p-6"
          >
            <div className="h-40 bg-gray-100 rounded mb-4 flex items-center justify-center text-gray-400">
              📦
            </div>
            <p className="text-xs text-indigo-600 mb-1">{product.category}</p>
            <h2 className="font-semibold text-lg mb-2">{product.name}</h2>
            <p className="text-sm text-gray-500 mb-4">{product.description}</p>
            <div className="flex items-center justify-between">
              <span className="text-xl font-bold text-gray-900">
                ${product.price.toFixed(2)}
              </span>
              <button className="bg-indigo-600 text-white text-sm px-4 py-2 rounded hover:bg-indigo-700 transition">
                Add to Cart
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
