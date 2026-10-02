import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

import {
  and,
  asc,
  desc,
  field,
  FilterQueryStringError,
  fromQueryString,
  not,
  or,
  request,
  sortBy,
  toQueryString,
  type FilterRequest,
} from '../src/index.js';

interface WireFixture {
  readonly name: string;
  readonly request: FilterRequest;
  readonly queryString: string;
}

interface InvalidQueryString {
  readonly name: string;
  readonly queryString: string;
  readonly parameter: FilterQueryStringError['parameter'];
  readonly clientAccepts?: boolean;
}

const fixtureDirectory = fileURLToPath(new URL('../../../tests/wire-fixtures/', import.meta.url));
const fixtures: WireFixture[] = readdirSync(fixtureDirectory)
  .filter((fileName) => fileName.endsWith('.json'))
  .sort()
  .map((fileName) => JSON.parse(readFileSync(join(fixtureDirectory, fileName), 'utf8')) as WireFixture);
const invalidQueryStrings = JSON.parse(
  readFileSync(join(fixtureDirectory, 'invalid', 'query-strings.json'), 'utf8'),
) as InvalidQueryString[];

const builders: Record<string, () => FilterRequest> = {
  'every-builtin-operator': () =>
    request({
      where: and(
        field('name').eq('Ann'),
        field('name').ne('Bob'),
        field('age').gt(18),
        field('age').gte(21),
        field('age').lt(65),
        field('age').lte(60),
        field('name').contains('an'),
        field('name').startsWith('A'),
        field('name').endsWith('n'),
        field('age').in([30, 40]),
        field('nickname').isNull(),
      ),
    }),
  'custom-operators': () => request({ where: and(field('name').op('ilike', 'an%'), field('tags').op('isEmpty')) }),
  'nested-groups-and-not': () =>
    request({
      where: or(and(field('isActive').eq(true), field('age').gte(18)), not(field('department.name').isNull())),
    }),
  'dates-and-in-values': () =>
    request({
      where: and(
        field('createdAt').gt(new Date(Date.UTC(2026, 9, 1, 0, 0, 0))),
        field('externalId').in(['11111111-1111-1111-1111-111111111111']),
        field('name').in([]),
      ),
    }),
  'sort-and-paging': () =>
    request({
      where: field('isActive').eq(true),
      sort: [desc('createdAt'), asc('name'), sortBy('age')],
      page: 2,
      pageSize: 25,
    }),
  'special-characters': () =>
    request({ where: and(field('name').eq('a b&c=d+e%f"g'), field('name').contains('Київ')) }),
  'paging-only': () => request({ page: 1, pageSize: 20 }),
  'empty-groups-collapse': () => request({ where: and(or(), false, null, '', not(undefined)), sort: [], page: 1 }),
};

describe('wire fixtures', () => {
  it('has exactly one builder per fixture file', () => {
    expect(fixtures.map((fixture) => fixture.name).sort()).toEqual(Object.keys(builders).sort());
  });

  describe.each(fixtures)('$name', (fixture) => {
    it('is produced by the combinators', () => {
      expect(builders[fixture.name]?.()).toEqual(fixture.request);
    });

    it('encodes to the fixture query string', () => {
      expect(toQueryString(fixture.request)).toBe(fixture.queryString);
    });

    it('decodes the fixture query string back to the request', () => {
      expect(fromQueryString(fixture.queryString)).toEqual(fixture.request);
    });
  });
});

describe('invalid query strings', () => {
  it('has rows for every parameter', () => {
    const parameters = new Set(invalidQueryStrings.map((row) => row.parameter));

    expect(parameters).toEqual(new Set(['where', 'sort', 'page', 'pageSize']));
  });

  it.each(invalidQueryStrings.filter((row) => !row.clientAccepts))(
    '$name is rejected with parameter $parameter',
    (row) => {
      expect(() => fromQueryString(row.queryString)).toThrow(
        expect.objectContaining({ name: 'FilterQueryStringError', parameter: row.parameter }),
      );
    },
  );

  it.each(invalidQueryStrings.filter((row) => row.clientAccepts))(
    '$name is accepted because the client validates no tree shape',
    (row) => {
      expect(() => fromQueryString(row.queryString)).not.toThrow();
    },
  );
});
