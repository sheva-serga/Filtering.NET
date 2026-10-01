import type { SortDir, SortItem } from './types.js';

export function asc(field: string): SortItem {
  return { field, dir: 'asc' };
}

export function desc(field: string): SortItem {
  return { field, dir: 'desc' };
}

export function sortBy(field: string, dir?: SortDir): SortItem {
  return dir === undefined ? { field } : { field, dir };
}

// The server matches field names case-insensitively, so a tiebreaker on "id" is already covered by a user sort on "Id".
export function withTiebreakers(sort: readonly SortItem[] | undefined, ...tiebreakers: SortItem[]): SortItem[] {
  const result = [...(sort ?? [])];
  const sortedFields = new Set(result.map((item) => item.field.toLowerCase()));
  for (const tiebreaker of tiebreakers) {
    const fieldKey = tiebreaker.field.toLowerCase();
    if (sortedFields.has(fieldKey)) {
      continue;
    }
    sortedFields.add(fieldKey);
    result.push(tiebreaker);
  }
  return result;
}
