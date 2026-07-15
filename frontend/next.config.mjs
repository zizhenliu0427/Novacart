import createNextIntlPlugin from 'next-intl/plugin';

/** @type {import('next').NextConfig} */

// In Docker (microservices) the API gateway is reachable as `gateway:8080`.
// Monolith compose uses `backend:5000`. Override with BACKEND_ORIGIN when building/running.
const backendOrigin =
  process.env.BACKEND_ORIGIN ||
  (process.env.NODE_ENV === 'production' ? 'http://gateway:8080' : 'http://localhost:5000');

const withNextIntl = createNextIntlPlugin('./src/i18n/request.ts');

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

export default withNextIntl(nextConfig);
