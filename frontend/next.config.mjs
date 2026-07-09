/** @type {import('next').NextConfig} */

// In Docker the backend is reachable by service name; for local `next dev` it's on
// localhost. Override explicitly with BACKEND_ORIGIN if needed.
const backendOrigin =
  process.env.BACKEND_ORIGIN ||
  (process.env.NODE_ENV === 'production' ? 'http://backend:5000' : 'http://localhost:5000');

const nextConfig = {
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: `${backendOrigin}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
