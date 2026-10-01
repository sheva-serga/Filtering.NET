import { describe, expect, it } from 'vitest';

import { and, or } from '../src/group.js';
import { field } from '../src/leaf.js';
import { request } from '../src/request.js';
import { asc } from '../src/sort.js';

describe('request', () => {
  it('drops undefined parts and an empty sort', () => {
    const result = request({ where: undefined, sort: [], page: 1, pageSize: undefined });

    expect(result).toEqual({ page: 1 });
    expect(Object.keys(result)).toEqual(['page']);
  });

  it('orders the parts as where, sort, page, pageSize', () => {
    const result = request({ pageSize: 20, page: 1, sort: [asc('a')], where: field('a').eq(1) });

    expect(Object.keys(result)).toEqual(['where', 'sort', 'page', 'pageSize']);
  });

  it('accepts a collapsed group directly', () => {
    expect(request({ where: and(or()), page: 1 })).toEqual({ page: 1 });
  });

  it('returns an empty request when nothing is given', () => {
    expect(request({})).toEqual({});
  });
});
