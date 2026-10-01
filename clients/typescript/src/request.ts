import type { FilterNode, FilterRequest, SortItem } from './types.js';

// Explicit `| undefined` lets callers pass `and(...)` (which may be undefined) under exactOptionalPropertyTypes.
export interface RequestParts {
  where?: FilterNode | undefined;
  sort?: readonly SortItem[] | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
}

export function request(parts: RequestParts): FilterRequest {
  const result: { where?: FilterNode; sort?: readonly SortItem[]; page?: number; pageSize?: number } = {};
  if (parts.where !== undefined) {
    result.where = parts.where;
  }
  if (parts.sort !== undefined && parts.sort.length > 0) {
    result.sort = parts.sort;
  }
  if (parts.page !== undefined) {
    result.page = parts.page;
  }
  if (parts.pageSize !== undefined) {
    result.pageSize = parts.pageSize;
  }
  return result;
}
