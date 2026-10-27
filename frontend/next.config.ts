import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  allowedDevOrigins: ['192.168.56.1'],
  // La sección IA ahora se llama Recomendaciones: los enlaces viejos siguen funcionando (con su query).
  async redirects() {
    return [{ source: "/ia", destination: "/recomendaciones", permanent: false }];
  },
};

export default nextConfig;
