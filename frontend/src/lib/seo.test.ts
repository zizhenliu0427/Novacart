import { routing } from '@/i18n/routing';
import { buildHreflangAlternates } from '@/lib/seo';

describe('buildHreflangAlternates', () => {
  it('emits en-AU, zh-CN and x-default for a path', () => {
    const result = buildHreflangAlternates('/products', 'en');

    expect(result.canonical).toBe('http://localhost:3000/en/products');
    expect(result.languages['en-AU']).toBe('http://localhost:3000/en/products');
    expect(result.languages['zh-CN']).toBe('http://localhost:3000/zh/products');
    expect(result.languages['x-default']).toBe('http://localhost:3000/en/products');
  });

  it('supports all configured locales', () => {
    const result = buildHreflangAlternates('/', 'zh');
    expect(Object.keys(result.languages)).toEqual(expect.arrayContaining(['en-AU', 'zh-CN', 'x-default']));
    expect(routing.locales).toContain('en');
    expect(routing.locales).toContain('zh');
  });
});
