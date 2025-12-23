import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.ticketspro.scanner',
  appName: 'ticketspro',
  webDir: 'dist',
  android: {
    allowMixedContent: true
  }
};

export default config;
