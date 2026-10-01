import { describe, expect, it } from 'vitest';

import { asc, desc, sortBy, withTiebreakers } from '../src/sort.js';

describe('asc / desc / sortBy', () => {
  it('builds sort items', () => {
    expect(asc('name')).toEqual({ field: 'name', dir: 'asc' });
    expect(desc('createdAt')).toEqual({ field: 'createdAt', dir: 'desc' });
    expect(sortBy('age', 'desc')).toEqual({ field: 'age', dir: 'desc' });
  });

  it('omits dir when sortBy gets no direction, so the server default applies', () => {
    const item = sortBy('age');

    expect(item).toEqual({ field: 'age' });
    expect('dir' in item).toBe(false);
  });
});

describe('withTiebreakers', () => {
  it('appends tiebreakers missing from the sort', () => {
    expect(withTiebreakers([desc('createDate')], desc('id'))).toEqual([desc('createDate'), desc('id')]);
  });

  it('keeps the caller direction when the field is already sorted, ignoring case', () => {
    expect(withTiebreakers([asc('Id')], desc('id'))).toEqual([asc('Id')]);
  });

  it('starts from an empty sort when none is given', () => {
    expect(withTiebreakers(undefined, desc('id'))).toEqual([desc('id')]);
  });

  it('adds each tiebreaker field once', () => {
    expect(withTiebreakers([], desc('id'), asc('ID'))).toEqual([desc('id')]);
  });

  it('does not mutate the input array', () => {
    const userSort = [asc('name')];

    withTiebreakers(userSort, desc('id'));

    expect(userSort).toEqual([asc('name')]);
  });
});
