import { describe, expect, it } from 'vitest';

import { field } from '../src/leaf.js';
import { FilterQueryStringError, fromQueryString, toQueryString } from '../src/query-string.js';
import { request } from '../src/request.js';
import { desc, sortBy } from '../src/sort.js';

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
  it('writes where as JSON, one sort key per item, then paging', () => {
    const query = toQueryString(
      request({ where: field('a').eq(1), sort: [desc('createdAt'), sortBy('name')], page: 2, pageSize: 10 }),
    );

    expect([...new URLSearchParams(query)]).toEqual([
      ['where', '{"field":"a","op":"eq","value":1}'],
      ['sort', 'createdAt:desc'],
      ['sort', 'name'],
      ['page', '2'],
      ['pageSize', '10'],
    ]);
  });

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

  it.each(['where=%7B', 'where=', 'where=5', 'where=%5B%5D', 'where=%22x%22', 'where=null', 'where=%7B%7D%20x'])(
    'rejects %s with parameter where',
    (query) => {
      expect(captureError(() => fromQueryString(query)).parameter).toBe('where');
    },
  );

  it.each(['sort=name%3Aup', 'sort=a%3Ab', 'sort=%3Adesc', 'sort='])('rejects %s with parameter sort', (query) => {
    expect(captureError(() => fromQueryString(query)).parameter).toBe('sort');
  });

  it.each([
    ['page=1.5', 'page'],
    ['page=abc', 'page'],
    ['page=', 'page'],
    ['pageSize=2e3', 'pageSize'],
  ] as const)('rejects %s with parameter %s', (query, parameter) => {
    expect(captureError(() => fromQueryString(query)).parameter).toBe(parameter);
  });

  it.each([
    ['page=1&page=2', 'page'],
    ['pageSize=10&pageSize=20', 'pageSize'],
    ['page=1&Page=2', 'page'],
    ['pageSize=10&PAGESIZE=20', 'pageSize'],
    ['where=%7B%22field%22%3A%22a%22%2C%22op%22%3A%22isNull%22%7D&where=%7B%22field%22%3A%22b%22%2C%22op%22%3A%22isNull%22%7D', 'where'],
  ] as const)('rejects the repeated scalar parameter in %s', (query, parameter) => {
    expect(captureError(() => fromQueryString(query)).parameter).toBe(parameter);
  });
});
