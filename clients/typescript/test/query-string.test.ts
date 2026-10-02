import { describe, expect, it } from 'vitest';

import { field } from '../src/leaf.js';
import { FilterQueryStringError, fromQueryString, toQueryString } from '../src/query-string.js';
import { request } from '../src/request.js';
import { desc } from '../src/sort.js';

function captureError(action: () => unknown): FilterQueryStringError {
  try {
    action();
  } catch (error) {
    if (error instanceof FilterQueryStringError) {
      return error;
    }
    throw error;
  }
  throw new Error('Expected a FilterQueryStringError.');
}

describe('toQueryString', () => {
  it('omits absent parts and never adds a leading question mark', () => {
    expect(toQueryString(request({}))).toBe('');
    expect(toQueryString(request({ page: 1 }))).toBe('page=1');
  });
});

describe('fromQueryString', () => {
  const expected = request({ where: field('a').eq(1), sort: [desc('b')], page: 2, pageSize: 10 });
  const encoded = toQueryString(expected);

  it('accepts a bare string, a leading question mark, or URLSearchParams', () => {
    expect(fromQueryString(encoded)).toEqual(expected);
    expect(fromQueryString(`?${encoded}`)).toEqual(expected);
    expect(fromQueryString(new URLSearchParams(encoded))).toEqual(expected);
  });

  it('ignores parameters it does not own', () => {
    expect(fromQueryString(`tab=prices&${encoded}&debug=1`)).toEqual(expected);
  });

  it('returns an empty request for an empty query', () => {
    expect(fromQueryString('')).toEqual({});
  });

  it('matches parameter names case-insensitively like ASP.NET', () => {
    const upperCased = encoded.replace('where=', 'WHERE=').replace('sort=', 'Sort=').replace('page=', 'Page=').replace('pageSize=', 'PageSize=');

    expect(fromQueryString(upperCased)).toEqual(expected);
  });

  it('keeps sort items in query order across casings', () => {
    expect(fromQueryString('sort=a&SORT=b&Sort=c').sort).toEqual([{ field: 'a' }, { field: 'b' }, { field: 'c' }]);
  });

  it('splits sort items on the last colon and accepts any direction case', () => {
    expect(fromQueryString('sort=meta%3Akey%3Adesc&sort=name%3AASC').sort).toEqual([
      { field: 'meta:key', dir: 'desc' },
      { field: 'name', dir: 'asc' },
    ]);
  });

  it('names the offending parameter on the error', () => {
    const error = captureError(() => fromQueryString('where=5'));

    expect(error.name).toBe('FilterQueryStringError');
    expect(error.parameter).toBe('where');
  });

  it.each([
    ['page=%2B5', 5],
    ['page=-7', -7],
    ['page=007', 7],
    ['page=%20%095%0D%0A', 5],
    ['page=2147483647', 2147483647],
    ['page=-2147483648', -2147483648],
  ] as const)('parses %s like int.TryParse to %i', (query, value) => {
    expect(fromQueryString(query).page).toBe(value);
  });
});
