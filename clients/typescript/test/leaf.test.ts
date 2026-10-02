import { describe, expect, it } from 'vitest';

import { field } from '../src/leaf.js';

describe('field', () => {
  it('builds a binary leaf', () => {
    expect(field('age').gte(18)).toEqual({ field: 'age', op: 'gte', value: 18 });
  });

  it('omits the value key for isNull', () => {
    const leaf = field('nickname').isNull();

    expect(leaf).toEqual({ field: 'nickname', op: 'isNull' });
    expect('value' in leaf).toBe(false);
    expect(JSON.stringify(leaf)).toBe('{"field":"nickname","op":"isNull"}');
  });

  it('omits the value key for a custom operator without a value', () => {
    const unary = field('tags').op('isEmpty');

    expect('value' in unary).toBe(false);
  });

  it('passes field and operator names through unchanged', () => {
    expect(field('Product.Brand.Id').op('InList', [1])).toEqual({ field: 'Product.Brand.Id', op: 'InList', value: [1] });
  });

  it('converts Date values to ISO strings, including inside arrays', () => {
    const start = new Date(Date.UTC(2026, 9, 1, 0, 0, 0));

    expect(field('createdAt').gt(start)).toEqual({ field: 'createdAt', op: 'gt', value: '2026-10-01T00:00:00.000Z' });
    expect(field('createdAt').in([start])).toEqual({ field: 'createdAt', op: 'in', value: ['2026-10-01T00:00:00.000Z'] });
  });

  it('emits an empty in array as is', () => {
    expect(field('name').in([])).toEqual({ field: 'name', op: 'in', value: [] });
  });

  it('throws on an invalid Date', () => {
    expect(() => field('createdAt').gt(new Date(Number.NaN))).toThrow(RangeError);
  });

  it('throws on non-finite numbers instead of sending null', () => {
    expect(() => field('age').eq(Number.NaN)).toThrow(TypeError);
    expect(() => field('age').in([1, Number.POSITIVE_INFINITY])).toThrow(TypeError);
  });
});
