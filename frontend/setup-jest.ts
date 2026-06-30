import { setupZonelessTestEnv } from 'jest-preset-angular/setup-env/zoneless';
import '@testing-library/jest-dom';

// The app is zoneless (Angular 22 default, no zone.js); use the matching test env.
setupZonelessTestEnv();
