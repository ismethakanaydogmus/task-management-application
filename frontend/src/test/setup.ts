// Her test dosyasından önce çalışır (vite.config.ts → test.setupFiles).
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// expect(...).toBeInTheDocument(), toHaveTextContent(), toBeDisabled() gibi DOM'a özel doğrulamalar.
import '@testing-library/jest-dom/vitest';

// Her testten sonra çizilen component'leri DOM'dan kaldır → testler birbirini etkilemesin.
afterEach(() => {
  cleanup();
});
