import { describe, expect, it } from 'vitest';

import { field } from '../src/leaf.js';

describe('field', () => {
  it('builds a binary leaf for every built-in operator', () => {
    expect(field('name').eq('Ann')).toEqual({ field: 'name', op: 'eq', value: 'Ann' });
    expect(field('name').ne('Bob')).toEqual({ field: 'name', op: 'ne', value: 'Bob' });
    expect(field('age').gt(18)).toEqual({ field: 'age', op: 'gt', value: 18 });
    expect(field('age').gte(21)).toEqual({ field: 'age', op: 'gte', value: 21 });
    expect(field('age').lt(65)).toEqual({ field: 'age', op: 'lt', value: 65 });
    expect(field('age').lte(60)).toEqual({ field: 'age', op: 'lte', value: 60 });
    expect(field('name').contains('an')).toEqual({ field: 'name', op: 'contains', value: 'an' });
    expect(field('name').startsWith('A')).toEqual({ field: 'name', op: 'startsWith', value: 'A' });
    expect(field('name').endsWith('n')).toEqual({ field: 'name', op: 'endsWith', value: 'n' });
    expect(field('age').in([30, 40])).toEqual({ field: 'age', op: 'in', value: [30, 40] });
  });

  it('omits the value key for isNull', () => {
    const leaf = field('nickname').isNull();

    expect(leaf).toEqual({ field: 'nickname', op: 'isNull' });
    expect('value' in leaf).toBe(false);
    expect(JSON.stringify(leaf)).toBe('{"field":"nickname","op":"isNull"}');
  });

  it('builds custom operators with and without a value', () => {
    const unary = field('tags').op('isEmpty');

    expect(field('name').op('ilike', 'an%')).toEqual({ field: 'name', op: 'ilike', value: 'an%' });
    expect(unary).toEqual({ field: 'tags', op: 'isEmpty' });
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
