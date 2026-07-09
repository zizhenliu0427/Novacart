import { describe, it, expect } from 'vitest';
import { formatPrice, parseMetadata, formatMetadataValue, humanizeKey } from './product';

describe('Product Helpers', () => {
  describe('formatPrice', () => {
    it('should format numbers into AUD currency string by default', () => {
      // Use string match or regular expression since spaces can vary (non-breaking space)
      expect(formatPrice(199.99)).toMatch(/\$199\.99/);
    });

    it('should format numbers with different currencies', () => {
      expect(formatPrice(45.5, 'USD')).toMatch(/USD\s*45\.50/);
    });
  });

  describe('parseMetadata', () => {
    it('should return empty object for null, undefined, or empty string', () => {
      expect(parseMetadata(null)).toEqual({});
      expect(parseMetadata(undefined)).toEqual({});
      expect(parseMetadata('')).toEqual({});
    });

    it('should parse valid JSON metadata string', () => {
      const jsonStr = '{"brand":"SoundPro","connectivity":"Bluetooth"}';
      expect(parseMetadata(jsonStr)).toEqual({
        brand: 'SoundPro',
        connectivity: 'Bluetooth',
      });
    });

    it('should return empty object on JSON parsing failure', () => {
      const invalidJson = '{invalid_json}';
      expect(parseMetadata(invalidJson)).toEqual({});
    });
  });

  describe('formatMetadataValue', () => {
    it('should join arrays with comma and space', () => {
      expect(formatMetadataValue(['XS', 'S', 'M'])).toBe('XS, S, M');
    });

    it('should format boolean values as Yes or No', () => {
      expect(formatMetadataValue(true)).toBe('Yes');
      expect(formatMetadataValue(false)).toBe('No');
    });

    it('should return dash for null or undefined', () => {
      expect(formatMetadataValue(null)).toBe('—');
      expect(formatMetadataValue(undefined)).toBe('—');
    });

    it('should convert strings and numbers directly to string', () => {
      expect(formatMetadataValue(123)).toBe('123');
      expect(formatMetadataValue('text')).toBe('text');
    });
  });

  describe('humanizeKey', () => {
    it('should convert snake_case to Title Case', () => {
      expect(humanizeKey('battery_hours')).toBe('Battery Hours');
      expect(humanizeKey('ports_list_details')).toBe('Ports List Details');
    });

    it('should convert camelCase to Title Case', () => {
      expect(humanizeKey('batteryHours')).toBe('Battery Hours');
      expect(humanizeKey('portsListDetails')).toBe('Ports List Details');
    });

    it('should capitalize the first letter', () => {
      expect(humanizeKey('brand')).toBe('Brand');
    });
  });
});
