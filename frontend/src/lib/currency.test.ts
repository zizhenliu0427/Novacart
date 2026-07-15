import { describe, it, expect } from 'vitest';
import { convertFromAud, formatAudAsDisplay } from './currency';
import { DISPLAY_CURRENCIES } from '@/types/currency';

const sampleRates = {
  USD: 0.69,
  CNY: 4.71,
  JPY: 112.6,
  SGD: 0.9,
  GBP: 0.51,
  NZD: 1.09,
};

describe('currency helpers', () => {
  describe('convertFromAud', () => {
    it('returns the same amount for AUD', () => {
      expect(convertFromAud(100, 'AUD', sampleRates)).toBe(100);
    });

    it('multiplies by the rate for a foreign currency', () => {
      expect(convertFromAud(100, 'USD', sampleRates)).toBeCloseTo(69, 5);
      expect(convertFromAud(100, 'GBP', sampleRates)).toBeCloseTo(51, 5);
      expect(convertFromAud(100, 'NZD', sampleRates)).toBeCloseTo(109, 5);
    });

    it('falls back to AUD amount when rate is missing', () => {
      expect(convertFromAud(50, 'USD', null)).toBe(50);
      expect(convertFromAud(50, 'USD', {})).toBe(50);
    });
  });

  describe('formatAudAsDisplay', () => {
    it('formats AUD with dollar sign for en-AU locale', () => {
      expect(formatAudAsDisplay(199.99, 'AUD', sampleRates, 'en')).toMatch(/\$199\.99/);
    });

    it('formats converted USD amounts', () => {
      const formatted = formatAudAsDisplay(100, 'USD', sampleRates, 'en');
      expect(formatted).toMatch(/69(\.00)?/);
    });

    it('uses zero decimal places for JPY', () => {
      const formatted = formatAudAsDisplay(1, 'JPY', sampleRates, 'en');
      expect(formatted).not.toMatch(/\.\d{2}$/);
      expect(formatted).toMatch(/113|112/);
    });

    it('supports zh locale formatting', () => {
      const formatted = formatAudAsDisplay(100, 'CNY', sampleRates, 'zh');
      expect(formatted).toMatch(/471(\.00)?/);
    });
  });

  describe('DISPLAY_CURRENCIES', () => {
    it('includes all supported storefront currencies', () => {
      expect(DISPLAY_CURRENCIES).toEqual(['AUD', 'USD', 'CNY', 'JPY', 'SGD', 'GBP', 'NZD']);
    });
  });
});
