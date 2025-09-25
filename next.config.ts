import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
    async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5027/api/:path*", // ASP.NET서버로 프록시
      },
         {
        source: "/static/:path*",
        destination: "http://localhost:5027/static/:path*", // ASP.NET서버로 프록시
      },
    ]
  },
};

export default nextConfig;
