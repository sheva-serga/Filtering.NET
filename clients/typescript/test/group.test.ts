import { describe, expect, it } from 'vitest';

import { and, not, or } from '../src/group.js';
import { field } from '../src/leaf.js';

describe('and / or', () => {
  it('wraps the children under the group key', () => {
    const left = field('a').eq(1);
    const right = field('b').eq(2);

    expect(and(left, right)).toEqual({ and: [left, right] });
    expect(or(left, right)).toEqual({ or: [left, right] });
  });

  it('skips false, null, undefined and empty-string children', () => {
    const leaf = field('a').eq(1);
    const search = '';

    expect(and(false, null, undefined, search && field('name').contains(search), leaf)).toEqual({ and: [leaf] });
  });

  it('returns undefined when no child remains', () => {
    expect(and()).toBeUndefined();
    expect(or(false, undefined)).toBeUndefined();
  });

  it('lets an empty group disappear up the tree', () => {
    const leaf = field('a').eq(1);

    expect(and(or(), leaf)).toEqual({ and: [leaf] });
    expect(and(or(), and())).toBeUndefined();
  });

  it('keeps nesting and single-child groups exactly as written', () => {
    const leaf = field('a').eq(1);

    expect(and(and(leaf))).toEqual({ and: [{ and: [leaf] }] });
  });
});

describe('not', () => {
  it('wraps one child in a one-element array', () => {
    const leaf = field('department.name').isNull();

    expect(not(leaf)).toEqual({ not: [leaf] });
  });

  it('returns undefined for a skipped child', () => {
    expect(not(undefined)).toBeUndefined();
    expect(not(false)).toBeUndefined();
    expect(not(and())).toBeUndefined();
  });
});
