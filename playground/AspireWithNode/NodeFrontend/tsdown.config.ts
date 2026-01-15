import { defineConfig } from 'tsdown';

export default defineConfig({
  entry: ['./app.js', './instrumentation.js'],
  noExternal: () => true,
  copy: ['./views'],
});
