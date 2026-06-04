'use client';

import { useEffect, useState } from 'react';

interface HealthStatus {
  status: string;
  timestamp: string;
  environment: string;
}

export default function HomePage() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/health')
      .then((res) => res.json())
      .then(setHealth)
      .catch((err) => setError(err.message));
  }, []);

  return (
    <div className="space-y-8">
      {/* Hero */}
      <section className="text-center py-16">
        <h1 className="text-5xl font-bold text-gray-900 mb-4">
          Welcome to <span className="text-indigo-600">Novacart</span>
        </h1>
        <p className="text-xl text-gray-600 mb-8">
          A modern e-commerce platform for handmade crafts
        </p>
        <a
          href="/products"
          className="inline-block bg-indigo-600 text-white px-8 py-3 rounded-lg hover:bg-indigo-700 transition"
        >
          Browse Products
        </a>
      </section>

      {/* API Health Check */}
      <section className="bg-white rounded-lg shadow p-6 max-w-md mx-auto">
        <h2 className="text-lg font-semibold mb-4">Backend Status</h2>
        {error && (
          <p className="text-red-600">❌ Backend unreachable: {error}</p>
        )}
        {health && (
          <div className="space-y-2 text-sm">
            <p className="text-green-600">✅ Status: {health.status}</p>
            <p className="text-gray-500">Environment: {health.environment}</p>
            <p className="text-gray-500">
              Time: {new Date(health.timestamp).toLocaleString()}
            </p>
          </div>
        )}
        {!health && !error && (
          <p className="text-gray-400">Connecting to backend...</p>
        )}
      </section>
    </div>
  );
}
