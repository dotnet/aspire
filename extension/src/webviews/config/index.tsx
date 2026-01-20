// @ts-nocheck - This file is compiled with Babel (ESNext modules), not ts-loader
import React from 'react';
import { createRoot } from 'react-dom/client';
import { ConfigWebview } from './ConfigWebview';

console.log('ConfigWebview script loaded');

const container = document.getElementById('root');
if (container) {
  console.log('Root container found, rendering React app');
  const root = createRoot(container);
  root.render(<ConfigWebview />);
} else {
  console.error('Root container not found!');
}
