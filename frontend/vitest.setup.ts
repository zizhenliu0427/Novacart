// vitest.setup.ts — runs before each test file
// Extends vitest's expect with jest-dom matchers (toBeInTheDocument, toHaveClass, etc.)
import '@testing-library/jest-dom';

// Make React available globally so test files using JSX don't need to import it.
import React from 'react';
(globalThis as Record<string, unknown>).React = React;
